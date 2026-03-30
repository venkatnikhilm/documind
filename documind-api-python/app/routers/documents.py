import asyncio
import json
import logging
import os
from typing import AsyncGenerator, Optional

from fastapi import APIRouter, UploadFile, File
from fastapi.responses import JSONResponse, StreamingResponse

from app.models import ErrorResponse, QueryRequest, UploadResponse
from app.exceptions import ServiceUnavailableError
from app.agents.nodes import set_status_queue
from app.services.blob_service import BlobService
from app.services.document_service import DocumentService
from app.services.embedding_service import EmbeddingService
from app.services.search_service import SearchService

logger = logging.getLogger(__name__)

router = APIRouter()

_blob_service: Optional[BlobService] = None
_document_service: Optional[DocumentService] = None
_embedding_service: Optional[EmbeddingService] = None
_search_service: Optional[SearchService] = None
_rag_graph = None


def init_documents_router(
    blob_service: BlobService,
    document_service: DocumentService,
    embedding_service: EmbeddingService,
    search_service: SearchService,
    rag_graph,
) -> None:
    global _blob_service, _document_service, _embedding_service, _search_service, _rag_graph
    _blob_service = blob_service
    _document_service = document_service
    _embedding_service = embedding_service
    _search_service = search_service
    _rag_graph = rag_graph


def _sse_event(event_type: str, data: dict) -> str:
    return f"event: {event_type}\ndata: {json.dumps(data)}\n\n"


@router.post("/upload")
async def upload_document(file: UploadFile = File(...)):
    try:
        file_data = await file.read()
        if not file_data or len(file_data) == 0:
            return JSONResponse(
                status_code=400,
                content=ErrorResponse(
                    error="ValidationError",
                    message="File is missing or empty.",
                ).model_dump(by_alias=True),
            )

        _, ext = os.path.splitext(file.filename)
        if ext.lower() not in (".pdf", ".docx"):
            return JSONResponse(
                status_code=400,
                content=ErrorResponse(
                    error="ValidationError",
                    message="Unsupported file format. Supported formats: .pdf, .docx",
                ).model_dump(by_alias=True),
            )

        document_id, blob_uri = await _blob_service.upload_document(file_data, file.filename)
        text = await _document_service.extract_text(file_data, ext)
        chunks = _document_service.chunk_text(text, document_id)
        chunk_texts = [c.text for c in chunks]
        embeddings = await _embedding_service.generate_embeddings_batch(chunk_texts)
        await _search_service.index_chunks(chunks, embeddings)

        response = UploadResponse(
            document_id=document_id,
            chunk_count=len(chunks),
            file_name=file.filename,
        )
        logger.info(
            "Upload successful: document_id=%s, file_name=%s, chunk_count=%d",
            document_id,
            file.filename,
            len(chunks),
        )
        return JSONResponse(status_code=200, content=response.model_dump(by_alias=True))

    except ValueError as e:
        return JSONResponse(
            status_code=400,
            content=ErrorResponse(error="ValueError", message=str(e)).model_dump(by_alias=True),
        )
    except ServiceUnavailableError as e:
        return JSONResponse(
            status_code=503,
            content=ErrorResponse(
                error="ServiceUnavailable",
                message=str(e),
                retry_guidance="Please retry after 30 seconds.",
            ).model_dump(by_alias=True),
        )
    except Exception as e:
        logger.exception("Unexpected error during upload")
        return JSONResponse(
            status_code=500,
            content=ErrorResponse(error="InternalError", message=str(e)).model_dump(by_alias=True),
        )


@router.post("/query")
async def query_document(request: QueryRequest):
    if not request.question or not request.question.strip():
        return JSONResponse(
            status_code=400,
            content=ErrorResponse(
                error="ValidationError",
                message="Question is required and cannot be empty.",
            ).model_dump(by_alias=True),
        )

    async def event_stream() -> AsyncGenerator[str, None]:
        status_queue: asyncio.Queue = asyncio.Queue()
        set_status_queue(status_queue)
        # Sentinel value pushed onto the queue when the graph finishes
        _DONE = object()
        graph_result: dict = {}
        graph_error: Exception | None = None

        async def _run_graph() -> None:
            nonlocal graph_result, graph_error
            try:
                graph_result = await _rag_graph.ainvoke(
                    {
                        "question": request.question,
                        "document_id": request.document_id,
                        "retry_count": 0,
                        "is_grounded": False,
                        "retrieved_chunks": [],
                        "relevance_scores": [],
                        "answer": "",
                        "citations": [],
                        "query_type": "",
                        "rewritten_question": None,
                        "generation_count": 0,
                    }
                )
            except Exception as e:
                graph_error = e
            finally:
                await status_queue.put(_DONE)

        try:
            # Run the graph in the background so we can stream status events live
            graph_task = asyncio.create_task(_run_graph())

            # Yield status events in real-time as nodes push them onto the queue
            while True:
                item = await status_queue.get()
                if item is _DONE:
                    break
                yield _sse_event("status", item)

            # Wait for the task to fully complete (should already be done)
            await graph_task

            if graph_error is not None:
                raise graph_error

            # Now stream the answer tokens
            answer = graph_result.get("answer", "")
            words = answer.split()
            for i, word in enumerate(words):
                content = word if i == len(words) - 1 else f"{word} "
                yield _sse_event("token", {"content": content})

            for citation in graph_result.get("citations", []):
                yield _sse_event(
                    "citation",
                    {
                        "chunkId": citation.get("chunkId", ""),
                        "documentId": citation.get("documentId", ""),
                        "score": citation.get("score", 0.0),
                    },
                )

            yield _sse_event("complete", {"status": "completed"})

        except asyncio.CancelledError:
            logger.info("Query stream cancelled by client")
            return
        except Exception as e:
            logger.exception("Error during query streaming")
            yield _sse_event("error", {"error": type(e).__name__, "message": str(e)})
        finally:
            set_status_queue(None)

    return StreamingResponse(
        event_stream(),
        media_type="text/event-stream",
        headers={"Cache-Control": "no-cache", "Connection": "keep-alive"},
    )

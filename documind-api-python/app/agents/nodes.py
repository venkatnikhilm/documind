"""LangGraph RAG pipeline node implementations.

Each node is an async function that accepts RAGState and returns a partial
state update dict.  All LLM interactions use LCEL chains
(ChatPromptTemplate | AzureChatOpenAI) — no deprecated APIs.
"""

import asyncio
import logging

from langchain_core.output_parsers import StrOutputParser
from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import AzureChatOpenAI

from app.agents.state import RAGState
from app.config import Config
from app.services.embedding_service import EmbeddingService
from app.services.search_service import SearchService

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Module-level references injected at graph-build time via ``init_nodes``
# ---------------------------------------------------------------------------
_config: Config | None = None
_search_service: SearchService | None = None
_embedding_service: EmbeddingService | None = None
_llm: AzureChatOpenAI | None = None
_status_queue: asyncio.Queue | None = None


def set_status_queue(queue: asyncio.Queue | None) -> None:
    """Set (or clear) the module-level status queue used by ``_emit``."""
    global _status_queue  # noqa: PLW0603
    _status_queue = queue


async def _emit(node: str, message: str, detail: str | None = None) -> None:
    """Push a status payload onto the queue when one is configured."""
    if _status_queue is not None:
        await _status_queue.put({"node": node, "message": message, "detail": detail})


def init_nodes(
    config: Config,
    search_service: SearchService,
    embedding_service: EmbeddingService,
) -> None:
    """Initialise module-level dependencies used by every node."""
    global _config, _search_service, _embedding_service, _llm  # noqa: PLW0603
    _config = config
    _search_service = search_service
    _embedding_service = embedding_service
    _llm = AzureChatOpenAI(
        azure_endpoint=config.azure_openai_endpoint,
        api_key=config.azure_openai_key,
        azure_deployment=config.azure_openai_gpt_deployment,
        api_version="2024-12-01-preview",
        temperature=0,
    )


def _get_llm() -> AzureChatOpenAI:
    if _llm is None:
        raise RuntimeError("Nodes not initialised — call init_nodes() first.")
    return _llm


# ---------------------------------------------------------------------------
# 1. Router Node
# ---------------------------------------------------------------------------

_ROUTER_PROMPT = ChatPromptTemplate.from_messages(
    [
        (
            "system",
            "You are a query classifier. Classify the user question as "
            '"simple" or "complex". A simple question can be answered with a '
            "short factual lookup. A complex question requires synthesis, "
            "comparison, or multi-step reasoning. Reply with ONLY the word "
            '"simple" or "complex".',
        ),
        ("human", "{question}"),
    ]
)


async def router_node(state: RAGState) -> dict:
    """Classify the incoming question as *simple* or *complex*."""
    chain = _ROUTER_PROMPT | _get_llm() | StrOutputParser()
    raw = await chain.ainvoke({"question": state["question"]})
    query_type = raw.strip().lower()
    if query_type not in ("simple", "complex"):
        query_type = "complex"
    logger.info("Router classified question as '%s'.", query_type)
    return {"query_type": query_type}


# ---------------------------------------------------------------------------
# 2. Retriever Node
# ---------------------------------------------------------------------------


async def retriever_node(state: RAGState) -> dict:
    """Perform vector search and populate ``retrieved_chunks``."""
    if _embedding_service is None or _search_service is None:
        raise RuntimeError("Nodes not initialised — call init_nodes() first.")

    question = state.get("rewritten_question") or state["question"]
    query_embedding = await _embedding_service.generate_embedding(question)
    results = await _search_service.search(
        query_embedding=query_embedding,
        top_n=5,
        document_id=state.get("document_id"),
    )
    logger.info("Retriever fetched %d chunks.", len(results))
    return {"retrieved_chunks": results}


# ---------------------------------------------------------------------------
# 3. Grader Node
# ---------------------------------------------------------------------------

_GRADER_PROMPT = ChatPromptTemplate.from_messages(
    [
        (
            "system",
            "You are a relevance grader. Given a user question and a "
            "document chunk, score the chunk's relevance to the question on "
            "a scale of 1 to 5 (1 = not relevant, 5 = highly relevant). "
            "Reply with ONLY a single integer between 1 and 5.",
        ),
        (
            "human",
            "Question: {question}\n\nChunk:\n{chunk_text}",
        ),
    ]
)

_REWRITE_PROMPT = ChatPromptTemplate.from_messages(
    [
        (
            "system",
            "You are a question rewriter. Rewrite the user question to "
            "improve retrieval results. Keep the meaning but make it more "
            "specific and search-friendly. Reply with ONLY the rewritten "
            "question.",
        ),
        ("human", "{question}"),
    ]
)


async def grader_node(state: RAGState) -> dict:
    """Score each retrieved chunk and decide whether to retry retrieval."""
    llm = _get_llm()
    grade_chain = _GRADER_PROMPT | llm | StrOutputParser()
    rewrite_chain = _REWRITE_PROMPT | llm | StrOutputParser()

    question = state.get("rewritten_question") or state["question"]
    chunks = state.get("retrieved_chunks", [])

    scores: list[float] = []
    for result in chunks:
        raw = await grade_chain.ainvoke(
            {"question": question, "chunk_text": result.chunk.text}
        )
        try:
            score = int(raw.strip())
            score = max(1, min(5, score))
        except ValueError:
            score = 3
        scores.append(float(score))

    avg_score = sum(scores) / len(scores) if scores else 0.0
    retry_count = state.get("retry_count", 0)

    logger.info(
        "Grader avg=%.2f, retry_count=%d, scores=%s",
        avg_score,
        retry_count,
        scores,
    )

    if avg_score < 3 and retry_count < 2:
        rewritten = await rewrite_chain.ainvoke({"question": question})
        logger.info("Grader rewriting question: '%s'", rewritten.strip())
        return {
            "relevance_scores": scores,
            "rewritten_question": rewritten.strip(),
            "retry_count": retry_count + 1,
        }

    return {"relevance_scores": scores}


# ---------------------------------------------------------------------------
# 4. Generator Node
# ---------------------------------------------------------------------------

_GENERATOR_PROMPT = ChatPromptTemplate.from_messages(
    [
        (
            "system",
            "You are a helpful document assistant. Answer the user's "
            "question using ONLY the provided context. If the context does "
            "not contain enough information, say so. Cite the chunk IDs you "
            "used in your answer.",
        ),
        (
            "human",
            "Context:\n{context}\n\nQuestion: {question}",
        ),
    ]
)

_GROUNDEDNESS_PROMPT = ChatPromptTemplate.from_messages(
    [
        (
            "system",
            "You are a groundedness checker. Given a context and an answer, "
            "determine whether the answer stays within the bounds of the "
            "provided context. Reply with ONLY the word 'grounded' or "
            "'hallucinated'.",
        ),
        (
            "human",
            "Context:\n{context}\n\nAnswer:\n{answer}",
        ),
    ]
)


async def generator_node(state: RAGState) -> dict:
    """Generate an answer from retrieved chunks and check groundedness."""
    llm = _get_llm()
    gen_chain = _GENERATOR_PROMPT | llm | StrOutputParser()
    ground_chain = _GROUNDEDNESS_PROMPT | llm | StrOutputParser()

    question = state.get("rewritten_question") or state["question"]
    chunks = state.get("retrieved_chunks", [])

    # Build context string from retrieved chunks
    context_parts: list[str] = []
    for result in chunks:
        context_parts.append(
            f"[Chunk {result.chunk.chunk_id}]: {result.chunk.text}"
        )
    context = "\n\n".join(context_parts) if context_parts else "(no context)"

    # Generate answer
    answer = await gen_chain.ainvoke({"context": context, "question": question})
    logger.info("Generator produced answer of length %d.", len(answer))

    # Build citations from retrieved chunks metadata
    citations = [
        {
            "chunkId": result.chunk.chunk_id,
            "documentId": result.chunk.document_id,
            "score": result.similarity_score,
        }
        for result in chunks
    ]

    # Check groundedness
    ground_raw = await ground_chain.ainvoke(
        {"context": context, "answer": answer}
    )
    is_grounded = "grounded" in ground_raw.strip().lower()
    logger.info("Groundedness check: %s", "grounded" if is_grounded else "hallucinated")

    return {
        "answer": answer,
        "citations": citations,
        "is_grounded": is_grounded,
        "generation_count": state.get("generation_count", 0) + 1,
    }
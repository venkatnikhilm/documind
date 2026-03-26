"""DocuMind API — FastAPI entry point."""

import logging

from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from app.config import load_config
from app.exceptions import ServiceUnavailableError
from app.models import ErrorResponse
from app.services.blob_service import BlobService
from app.services.document_service import DocumentService
from app.services.embedding_service import EmbeddingService
from app.services.search_service import SearchService
from app.agents.rag_graph import build_rag_graph
from app.routers.documents import init_documents_router, router as documents_router
from app.routers.health import init_health_router, router as health_router

# ---------------------------------------------------------------------------
# Logging configuration
# ---------------------------------------------------------------------------
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Configuration & service initialisation
# ---------------------------------------------------------------------------
config = load_config()

blob_service = BlobService(config)
document_service = DocumentService()
embedding_service = EmbeddingService(config)
search_service = SearchService(config)

rag_graph = build_rag_graph(config, search_service, embedding_service)

# Inject services into routers
init_documents_router(blob_service, document_service, embedding_service, search_service, rag_graph)
init_health_router(blob_service, embedding_service, search_service)

# ---------------------------------------------------------------------------
# FastAPI application
# ---------------------------------------------------------------------------
app = FastAPI(title="DocuMind API")

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=config.cors_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Routers
app.include_router(documents_router, prefix="/api/documents")
app.include_router(health_router)


@app.on_event("startup")
async def startup_event():
    """Create search index if it doesn't exist."""
    await search_service.ensure_index()
    logger.info("Search index ensured.")


# ---------------------------------------------------------------------------
# Global exception handlers
# ---------------------------------------------------------------------------
@app.exception_handler(ValueError)
async def value_error_handler(request: Request, exc: ValueError) -> JSONResponse:
    """Map ValueError → HTTP 400."""
    logger.warning("Validation error: %s", exc)
    return JSONResponse(
        status_code=400,
        content=ErrorResponse(
            error="ValidationError",
            message=str(exc),
        ).model_dump(by_alias=True),
    )


@app.exception_handler(ServiceUnavailableError)
async def service_unavailable_handler(
    request: Request, exc: ServiceUnavailableError
) -> JSONResponse:
    """Map ServiceUnavailableError → HTTP 503 with retry guidance."""
    logger.error("Service unavailable: %s", exc.message)
    return JSONResponse(
        status_code=503,
        content=ErrorResponse(
            error="ServiceUnavailable",
            message=exc.message,
            retry_guidance="Please retry after 30 seconds.",
        ).model_dump(by_alias=True),
    )


@app.exception_handler(Exception)
async def generic_error_handler(request: Request, exc: Exception) -> JSONResponse:
    """Map unexpected exceptions → HTTP 500."""
    logger.error("Unexpected error: %s", exc, exc_info=True)
    return JSONResponse(
        status_code=500,
        content=ErrorResponse(
            error="InternalServerError",
            message="An unexpected error occurred.",
        ).model_dump(by_alias=True),
    )


logger.info("DocuMind API initialised. CORS origins: %s", config.cors_origins)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)

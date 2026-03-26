import logging
from typing import Optional

from fastapi import APIRouter
from fastapi.responses import JSONResponse

from app.services.blob_service import BlobService
from app.services.embedding_service import EmbeddingService
from app.services.search_service import SearchService

logger = logging.getLogger(__name__)

router = APIRouter()

_blob_service: Optional[BlobService] = None
_embedding_service: Optional[EmbeddingService] = None
_search_service: Optional[SearchService] = None


def init_health_router(
    blob_service: BlobService,
    embedding_service: EmbeddingService,
    search_service: SearchService,
) -> None:
    global _blob_service, _embedding_service, _search_service
    _blob_service = blob_service
    _embedding_service = embedding_service
    _search_service = search_service


@router.get("/health")
async def health_check() -> JSONResponse:
    services = {
        "azure_openai": "reachable",
        "azure_ai_search": "reachable",
        "azure_blob_storage": "reachable",
    }
    healthy = True

    for name, svc in [
        ("azure_openai", _embedding_service),
        ("azure_ai_search", _search_service),
        ("azure_blob_storage", _blob_service),
    ]:
        try:
            await svc.check_health()
        except Exception as exc:
            logger.error("Health check failed for %s: %s", name, exc)
            services[name] = "unreachable"
            healthy = False

    status_code = 200 if healthy else 503
    status = "healthy" if healthy else "unhealthy"
    return JSONResponse(status_code=status_code, content={"status": status, "services": services})

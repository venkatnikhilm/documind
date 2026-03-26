"""Azure Blob Storage service for document upload and health checks."""

import logging
import os
import uuid

from azure.core.exceptions import HttpResponseError
from azure.storage.blob.aio import BlobServiceClient, ContainerClient

from app.config import Config
from app.exceptions import ServiceUnavailableError

logger = logging.getLogger(__name__)

_CONTENT_TYPE_MAP: dict[str, str] = {
    ".pdf": "application/pdf",
    ".docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
}


class BlobService:
    """Manages document uploads to Azure Blob Storage."""

    def __init__(self, config: Config) -> None:
        self._connection_string = config.azure_blob_connection_string
        self._container_name = config.azure_blob_container

    def _get_client(self) -> BlobServiceClient:
        return BlobServiceClient.from_connection_string(self._connection_string)

    async def upload_document(
        self, file_data: bytes, filename: str
    ) -> tuple[str, str]:
        """Upload a file to Azure Blob Storage.

        Args:
            file_data: Raw file bytes.
            filename: Original filename (used to preserve extension).

        Returns:
            Tuple of (document_id, blob_uri).

        Raises:
            RuntimeError: When Azure Blob Storage is unavailable or upload fails.
        """
        document_id = str(uuid.uuid4())
        _, ext = os.path.splitext(filename)
        blob_name = f"{document_id}{ext}"
        content_type = _CONTENT_TYPE_MAP.get(ext.lower(), "application/octet-stream")

        try:
            async with self._get_client() as client:
                container: ContainerClient = client.get_container_client(
                    self._container_name
                )
                blob_client = container.get_blob_client(blob_name)
                await blob_client.upload_blob(
                    file_data,
                    overwrite=True,
                    content_settings=_blob_content_settings(content_type),
                )
                blob_uri = blob_client.url

            logger.info(
                "Document uploaded. document_id=%s blob_name=%s blob_uri=%s",
                document_id,
                blob_name,
                blob_uri,
            )
            return document_id, blob_uri

        except HttpResponseError as exc:
            if exc.status_code in (500, 503):
                logger.error(
                    "Azure Blob Storage unavailable. status=%s message=%s",
                    exc.status_code,
                    exc.message,
                )
                raise ServiceUnavailableError(
                    "Azure Blob Storage service is currently unavailable."
                ) from exc
            logger.error(
                "Blob upload failed. status=%s error_code=%s message=%s",
                exc.status_code,
                exc.error_code,
                exc.message,
            )
            raise ServiceUnavailableError(
                f"Failed to upload document to blob storage: {exc.message}"
            ) from exc
        except Exception as exc:
            logger.error("Unexpected error during blob upload: %s", exc)
            raise

    async def check_health(self) -> bool:
        """Verify connectivity to Azure Blob Storage.

        Returns:
            True if the container is reachable.

        Raises:
            RuntimeError: When Azure Blob Storage is unreachable.
        """
        try:
            async with self._get_client() as client:
                container = client.get_container_client(self._container_name)
                await container.get_container_properties()
            return True
        except Exception as exc:
            logger.error("Blob storage health check failed: %s", exc)
            raise ServiceUnavailableError(
                "Azure Blob Storage is unreachable."
            ) from exc


def _blob_content_settings(content_type: str):
    """Create ContentSettings for blob upload."""
    from azure.storage.blob import ContentSettings

    return ContentSettings(content_type=content_type)

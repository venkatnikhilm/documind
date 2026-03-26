"""Azure OpenAI embedding service using LangChain AzureOpenAIEmbeddings."""

import asyncio
import logging

from langchain_openai import AzureOpenAIEmbeddings

from app.config import Config
from app.exceptions import ServiceUnavailableError

logger = logging.getLogger(__name__)

_BATCH_SIZE = 16


class EmbeddingService:
    """Generates vector embeddings via Azure OpenAI."""

    def __init__(self, config: Config) -> None:
        self.embeddings = AzureOpenAIEmbeddings(
            azure_endpoint=config.azure_openai_endpoint,
            api_key=config.azure_openai_key,
            azure_deployment=config.azure_openai_embedding_deployment,
            api_version="2024-12-01-preview",
        )

    async def generate_embedding(self, text: str) -> list[float]:
        """Generate a single embedding vector for a query text.

        Args:
            text: The text to embed.

        Returns:
            A list of floats representing the embedding vector.

        Raises:
            RuntimeError: When Azure OpenAI is unavailable.
        """
        try:
            vector = await self.embeddings.aembed_query(text)
            logger.info("Generated single embedding. text_length=%d", len(text))
            return vector
        except Exception as exc:
            logger.error("Embedding generation failed: %s", exc)
            raise ServiceUnavailableError(
                "Azure OpenAI embedding service is currently unavailable."
            ) from exc

    async def generate_embeddings_batch(
        self, texts: list[str]
    ) -> list[list[float]]:
        """Generate embeddings for multiple texts in batches of 16.

        Waits 1 second between consecutive batches to avoid rate limiting.

        Args:
            texts: List of text strings to embed.

        Returns:
            List of embedding vectors in the same order as the input texts.

        Raises:
            RuntimeError: When Azure OpenAI is unavailable.
        """
        all_embeddings: list[list[float]] = []

        try:
            for i in range(0, len(texts), _BATCH_SIZE):
                if i > 0:
                    await asyncio.sleep(1)

                batch = texts[i : i + _BATCH_SIZE]
                batch_embeddings = await self.embeddings.aembed_documents(batch)
                all_embeddings.extend(batch_embeddings)

                logger.info(
                    "Embedded batch %d/%d (%d texts)",
                    i // _BATCH_SIZE + 1,
                    -(-len(texts) // _BATCH_SIZE),  # ceil division
                    len(batch),
                )

            return all_embeddings
        except Exception as exc:
            logger.error("Batch embedding generation failed: %s", exc)
            raise ServiceUnavailableError(
                "Azure OpenAI embedding service is currently unavailable."
            ) from exc

    async def check_health(self) -> bool:
        """Verify connectivity to Azure OpenAI embedding service.

        Returns:
            True if the service is reachable.

        Raises:
            ServiceUnavailableError: When Azure OpenAI is unreachable.
        """
        try:
            await self.embeddings.aembed_query("health check")
            return True
        except Exception as exc:
            logger.error("OpenAI health check failed: %s", exc)
            raise ServiceUnavailableError(
                "Azure OpenAI is unreachable."
            ) from exc


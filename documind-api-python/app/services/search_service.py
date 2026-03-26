"""Azure AI Search service for vector indexing and similarity search."""

import logging

from azure.core.credentials import AzureKeyCredential
from azure.core.exceptions import ResourceNotFoundError
from azure.search.documents.aio import SearchClient
from azure.search.documents.indexes.aio import SearchIndexClient
from azure.search.documents.indexes.models import (
    HnswAlgorithmConfiguration,
    SearchField,
    SearchFieldDataType,
    SearchIndex,
    SearchableField,
    SimpleField,
    VectorSearch,
    VectorSearchProfile,
)
from azure.search.documents.models import VectorizedQuery

from app.config import Config
from app.models import DocumentChunk, SearchResult

logger = logging.getLogger(__name__)


class SearchService:
    """Manages vector indexing and similarity search in Azure AI Search."""

    def __init__(self, config: Config) -> None:
        self._index_name = config.azure_search_index_name
        credential = AzureKeyCredential(config.azure_search_key)
        self._index_client = SearchIndexClient(
            endpoint=config.azure_search_endpoint,
            credential=credential,
        )
        self._search_client = SearchClient(
            endpoint=config.azure_search_endpoint,
            index_name=self._index_name,
            credential=credential,
        )

    async def ensure_index(self) -> None:
        """Create the search index if it does not already exist.

        The index includes fields for chunk metadata and a vector field
        configured with HNSW algorithm and cosine similarity (1536 dims).

        Raises:
            RuntimeError: When Azure AI Search is unavailable.
        """
        try:
            await self._index_client.get_index(self._index_name)
            logger.info(
                "Search index '%s' already exists.", self._index_name
            )
            return
        except ResourceNotFoundError:
            logger.info(
                "Index '%s' not found. Creating...", self._index_name
            )
        except Exception as exc:
            logger.error(
                "Failed to check index '%s': %s", self._index_name, exc
            )
            raise RuntimeError(
                "Azure AI Search service is currently unavailable."
            ) from exc

        try:
            index = SearchIndex(
                name=self._index_name,
                fields=[
                    SimpleField(
                        name="chunkId",
                        type=SearchFieldDataType.String,
                        key=True,
                    ),
                    SimpleField(
                        name="documentId",
                        type=SearchFieldDataType.String,
                        filterable=True,
                    ),
                    SearchableField(
                        name="text",
                        type=SearchFieldDataType.String,
                    ),
                    SearchField(
                        name="embedding",
                        type=SearchFieldDataType.Collection(
                            SearchFieldDataType.Single
                        ),
                        searchable=True,
                        vector_search_dimensions=1536,
                        vector_search_profile_name="cosine-profile",
                    ),
                    SimpleField(
                        name="chunkIndex",
                        type=SearchFieldDataType.Int32,
                        filterable=True,
                        sortable=True,
                    ),
                    SimpleField(
                        name="startPosition",
                        type=SearchFieldDataType.Int32,
                    ),
                    SimpleField(
                        name="endPosition",
                        type=SearchFieldDataType.Int32,
                    ),
                ],
                vector_search=VectorSearch(
                    profiles=[
                        VectorSearchProfile(
                            name="cosine-profile",
                            algorithm_configuration_name="hnsw-config",
                        ),
                    ],
                    algorithms=[
                        HnswAlgorithmConfiguration(name="hnsw-config"),
                    ],
                ),
            )
            await self._index_client.create_index(index)
            logger.info(
                "Successfully created search index '%s'.", self._index_name
            )
        except Exception as exc:
            logger.error(
                "Failed to create index '%s': %s", self._index_name, exc
            )
            raise RuntimeError(
                "Azure AI Search service is currently unavailable."
            ) from exc

    async def index_chunks(
        self,
        chunks: list[DocumentChunk],
        embeddings: list[list[float]],
    ) -> None:
        """Upload document chunks with embeddings to the search index.

        Args:
            chunks: List of document chunks to index.
            embeddings: Corresponding embedding vectors (same order as chunks).

        Raises:
            RuntimeError: When Azure AI Search is unavailable.
        """
        try:
            documents = [
                {
                    "chunkId": chunk.chunk_id,
                    "documentId": chunk.document_id,
                    "text": chunk.text,
                    "embedding": embedding,
                    "chunkIndex": chunk.chunk_index,
                    "startPosition": chunk.start_position,
                    "endPosition": chunk.end_position,
                }
                for chunk, embedding in zip(chunks, embeddings)
            ]
            await self._search_client.upload_documents(documents=documents)
            logger.info(
                "Indexed %d chunks for document_id=%s",
                len(chunks),
                chunks[0].document_id if chunks else "unknown",
            )
        except Exception as exc:
            logger.error("Failed to index chunks: %s", exc)
            raise RuntimeError(
                "Azure AI Search service is currently unavailable."
            ) from exc

    async def search(
        self,
        query_embedding: list[float],
        top_n: int = 5,
        document_id: str | None = None,
    ) -> list[SearchResult]:
        """Perform vector similarity search.

        Args:
            query_embedding: The query embedding vector.
            top_n: Maximum number of results to return.
            document_id: Optional filter to restrict results to a single document.

        Returns:
            List of SearchResult ordered by similarity score descending.

        Raises:
            RuntimeError: When Azure AI Search is unavailable.
        """
        try:
            vector_query = VectorizedQuery(
                vector=query_embedding,
                k_nearest_neighbors=top_n,
                fields="embedding",
            )

            filter_expr = (
                f"documentId eq '{document_id}'" if document_id else None
            )

            results: list[SearchResult] = []
            async for result in await self._search_client.search(
                search_text=None,
                vector_queries=[vector_query],
                filter=filter_expr,
                top=top_n,
                select=["chunkId", "documentId", "text", "chunkIndex", "startPosition", "endPosition"],
            ):
                chunk = DocumentChunk(
                    chunk_id=result["chunkId"],
                    document_id=result["documentId"],
                    text=result["text"],
                    chunk_index=result["chunkIndex"],
                    start_position=result["startPosition"],
                    end_position=result["endPosition"],
                )
                results.append(
                    SearchResult(
                        chunk=chunk,
                        similarity_score=result["@search.score"] or 0.0,
                    )
                )

            results.sort(key=lambda r: r.similarity_score, reverse=True)

            logger.info(
                "Vector search returned %d results. filter=%s",
                len(results),
                document_id or "none",
            )
            return results
        except Exception as exc:
            logger.error("Vector search failed: %s", exc)
            raise RuntimeError(
                "Azure AI Search service is currently unavailable."
            ) from exc

    async def check_health(self) -> bool:
        """Verify connectivity to Azure AI Search.

        Returns:
            True if the service is reachable.

        Raises:
            RuntimeError: When Azure AI Search is unreachable.
        """
        try:
            async for _ in self._index_client.list_index_names():
                break
            return True
        except Exception as exc:
            logger.error("Search service health check failed: %s", exc)
            raise RuntimeError(
                "Azure AI Search is unreachable."
            ) from exc

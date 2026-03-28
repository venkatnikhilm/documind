"""Pydantic request/response models for DocuMind API."""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class StatusEvent(BaseModel):
    """A pipeline status event emitted by a LangGraph node."""

    model_config = ConfigDict(populate_by_name=True)

    node: Literal["router", "retriever", "grader", "generator"]
    message: str
    detail: str | None = None


class QueryRequest(BaseModel):
    """Request model for document queries."""

    model_config = ConfigDict(populate_by_name=True)

    question: str
    document_id: str | None = Field(default=None, alias="documentId")


class UploadResponse(BaseModel):
    """Response model for document uploads."""

    model_config = ConfigDict(populate_by_name=True)

    document_id: str = Field(alias="documentId")
    chunk_count: int = Field(alias="chunkCount")
    file_name: str = Field(alias="fileName")


class ErrorResponse(BaseModel):
    """Response model for error responses."""

    model_config = ConfigDict(populate_by_name=True)

    error: str
    message: str
    retry_guidance: str | None = Field(default=None, alias="retryGuidance")


class DocumentChunk(BaseModel):
    """Represents a text chunk with metadata."""

    model_config = ConfigDict(populate_by_name=True)

    chunk_id: str
    document_id: str
    text: str
    chunk_index: int
    start_position: int
    end_position: int


class SearchResult(BaseModel):
    """Vector search result with similarity score."""

    model_config = ConfigDict(populate_by_name=True)

    chunk: DocumentChunk
    similarity_score: float

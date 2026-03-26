"""RAG pipeline state definition for LangGraph."""

from typing import Optional, TypedDict


class RAGState(TypedDict):
    """State passed between LangGraph RAG pipeline nodes."""

    question: str
    document_id: Optional[str]
    rewritten_question: Optional[str]
    retrieved_chunks: list
    relevance_scores: list
    answer: str
    citations: list
    retry_count: int
    is_grounded: bool
    query_type: str
    generation_count: int

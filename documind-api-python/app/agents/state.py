# """RAG pipeline state definition for LangGraph."""

# from typing import Optional, TypedDict


# class RAGState(TypedDict):
#     """State passed between LangGraph RAG pipeline nodes."""

#     question: str
#     document_id: Optional[str]
#     rewritten_question: Optional[str]
#     retrieved_chunks: list
#     relevance_scores: list
#     answer: str
#     citations: list
#     retry_count: int
#     is_grounded: bool
#     query_type: str
#     generation_count: int

"""RAG pipeline state definition for LangGraph."""
from typing import Annotated, Optional, TypedDict
import operator

class RAGState(TypedDict):
    """State passed between LangGraph RAG pipeline nodes."""
    question: str
    document_id: Optional[str]
    rewritten_question: Optional[str]
    retrieved_chunks: Annotated[list, lambda x, y: y]
    relevance_scores: Annotated[list, lambda x, y: y]
    answer: str
    citations: Annotated[list, lambda x, y: y]
    retry_count: int
    is_grounded: bool
    query_type: str
    generation_count: int
"""LangGraph RAG pipeline assembly.

Builds and compiles the StateGraph that orchestrates the four RAG nodes:
Router → Retriever → Grader → Generator, with conditional retry and
regeneration edges.
"""

import logging

from langgraph.graph import END, START, StateGraph

from app.agents.nodes import (
    generator_node,
    grader_node,
    init_nodes,
    retriever_node,
    router_node,
)
from app.agents.state import RAGState
from app.config import Config
from app.services.embedding_service import EmbeddingService
from app.services.search_service import SearchService

logger = logging.getLogger(__name__)


def _grade_decision(state: RAGState) -> str:
    """Route after grading: retry retrieval or proceed to generation."""
    scores = state.get("relevance_scores", [])
    avg = sum(scores) / len(scores) if scores else 0.0
    retry_count = state.get("retry_count", 0)

    if avg < 3 and retry_count < 2:
        return "retry"
    return "generate"


def _groundedness_decision(state: RAGState) -> str:
    """Route after generation: end if grounded, regenerate once if not."""
    if state.get("is_grounded", False):
        return "end"
    if state.get("generation_count", 0) >= 2:
        return "end"
    return "regenerate"


def build_rag_graph(
    config: Config,
    search_service: SearchService,
    embedding_service: EmbeddingService,
) -> StateGraph:
    """Build and compile the RAG StateGraph.

    Args:
        config: Application configuration.
        search_service: Azure AI Search service instance.
        embedding_service: Azure OpenAI embedding service instance.

    Returns:
        A compiled LangGraph ready for invocation.
    """
    # Inject dependencies into node functions
    init_nodes(config, search_service, embedding_service)

    graph = StateGraph(RAGState)

    # Add nodes
    graph.add_node("router", router_node)
    graph.add_node("retriever", retriever_node)
    graph.add_node("grader", grader_node)
    graph.add_node("generator", generator_node)

    # Fixed edges
    graph.add_edge(START, "router")
    graph.add_edge("router", "retriever")
    graph.add_edge("retriever", "grader")

    # Conditional edge after grading
    graph.add_conditional_edges(
        "grader",
        _grade_decision,
        {"generate": "generator", "retry": "retriever"},
    )

    # Conditional edge after generation
    graph.add_conditional_edges(
        "generator",
        _groundedness_decision,
        {"end": END, "regenerate": "generator"},
    )

    compiled = graph.compile()
    logger.info("RAG graph compiled successfully.")
    return compiled

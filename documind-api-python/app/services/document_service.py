"""Document text extraction and chunking service."""

import io
import logging
import uuid

from pypdf import PdfReader
from docx import Document as DocxDocument
from langchain_text_splitters import RecursiveCharacterTextSplitter

from app.models import DocumentChunk

logger = logging.getLogger(__name__)


class DocumentService:
    """Extracts text from PDF/DOCX documents and chunks text for embedding."""

    async def extract_text(self, file_data: bytes, file_extension: str) -> str:
        """Extract text from a PDF or DOCX document.

        Args:
            file_data: Raw file bytes.
            file_extension: File extension including dot (e.g. ".pdf", ".docx").

        Returns:
            Extracted text with preserved paragraph structure.

        Raises:
            ValueError: When the document is empty or the format is unsupported.
            RuntimeError: When the file is corrupted or extraction fails.
        """
        ext = file_extension.lower()

        try:
            if ext == ".pdf":
                text = self._extract_pdf(file_data)
            elif ext == ".docx":
                text = self._extract_docx(file_data)
            else:
                raise ValueError(f"Unsupported file format: {file_extension}")
        except ValueError:
            raise
        except Exception as exc:
            logger.error(
                "Failed to extract text from %s document: %s",
                file_extension,
                exc,
            )
            raise RuntimeError(
                f"Failed to extract text from {file_extension} document. "
                "The file may be corrupted."
            ) from exc

        if not text or not text.strip():
            raise ValueError("Document contains no extractable text.")

        logger.info(
            "Extracted text from %s document. Length: %d characters",
            file_extension,
            len(text),
        )
        return text

    def chunk_text(self, text: str, document_id: str) -> list[DocumentChunk]:
        """Chunk text using RecursiveCharacterTextSplitter with tiktoken cl100k_base.

        Args:
            text: Full document text.
            document_id: ID of the source document.

        Returns:
            List of DocumentChunk objects with metadata.
        """
        splitter = RecursiveCharacterTextSplitter.from_tiktoken_encoder(
            encoding_name="cl100k_base",
            chunk_size=500,
            chunk_overlap=50,
        )

        chunks_text = splitter.split_text(text)

        chunks: list[DocumentChunk] = []
        search_start = 0

        for index, chunk_text in enumerate(chunks_text):
            position = text.find(chunk_text, search_start)
            if position == -1:
                # Fallback: search from beginning if not found after search_start
                position = text.find(chunk_text)
            if position == -1:
                # Last resort: use current search_start
                position = search_start

            start_position = position
            end_position = position + len(chunk_text)

            chunks.append(
                DocumentChunk(
                    chunk_id=str(uuid.uuid4()),
                    document_id=document_id,
                    text=chunk_text,
                    chunk_index=index,
                    start_position=start_position,
                    end_position=end_position,
                )
            )

            # Advance search_start for next chunk to handle overlapping text
            search_start = start_position + 1

        logger.info(
            "Text chunked into %d chunk(s) for document %s",
            len(chunks),
            document_id,
        )
        return chunks

    # ------------------------------------------------------------------
    # Private extraction helpers
    # ------------------------------------------------------------------

    def _extract_pdf(self, file_data: bytes) -> str:
        """Extract text from PDF bytes using pypdf."""
        reader = PdfReader(io.BytesIO(file_data))
        paragraphs: list[str] = []

        for page in reader.pages:
            page_text = page.extract_text()
            if page_text and page_text.strip():
                paragraphs.append(page_text.strip())

        return "\n\n".join(paragraphs)

    def _extract_docx(self, file_data: bytes) -> str:
        """Extract text from DOCX bytes using python-docx."""
        doc = DocxDocument(io.BytesIO(file_data))
        paragraphs: list[str] = []

        for para in doc.paragraphs:
            if para.text and para.text.strip():
                paragraphs.append(para.text)

        return "\n".join(paragraphs)

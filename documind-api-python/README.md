# DocuMind API (Python)

Python FastAPI backend for DocuMind — an intelligent document Q&A system using LangChain and LangGraph.

## Setup

1. Create a virtual environment and install dependencies:

```bash
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
pip install -r requirements.txt
```

2. Copy `.env.example` to `.env` and fill in your Azure credentials:

```bash
cp .env.example .env
```

3. Run the server:

```bash
python main.py
```

The API will be available at `http://localhost:8000`.

## API Endpoints

- `POST /api/documents/upload` — Upload a PDF or DOCX document
- `POST /api/documents/query` — Query documents with SSE streaming
- `GET /health` — Health check

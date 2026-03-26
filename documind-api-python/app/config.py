"""Configuration loading and validation for DocuMind API."""

import os
from dataclasses import dataclass, field

from dotenv import load_dotenv


@dataclass
class Config:
    azure_openai_endpoint: str
    azure_openai_key: str
    azure_openai_gpt_deployment: str
    azure_openai_embedding_deployment: str
    azure_search_endpoint: str
    azure_search_key: str
    azure_search_index_name: str
    azure_blob_connection_string: str
    azure_blob_container: str
    cors_origins: list[str] = field(
        default_factory=lambda: ["http://localhost:5173", "http://localhost:3000"]
    )


_REQUIRED_ENV_VARS = [
    ("AZURE_OPENAI_ENDPOINT", "azure_openai_endpoint"),
    ("AZURE_OPENAI_KEY", "azure_openai_key"),
    ("AZURE_OPENAI_GPT_DEPLOYMENT", "azure_openai_gpt_deployment"),
    ("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", "azure_openai_embedding_deployment"),
    ("AZURE_SEARCH_ENDPOINT", "azure_search_endpoint"),
    ("AZURE_SEARCH_KEY", "azure_search_key"),
    ("AZURE_SEARCH_INDEX_NAME", "azure_search_index_name"),
    ("AZURE_BLOB_CONNECTION_STRING", "azure_blob_connection_string"),
    ("AZURE_BLOB_CONTAINER", "azure_blob_container"),
]


def load_config() -> Config:
    """Load and validate configuration from environment variables.

    Raises ValueError if any required variable is missing or empty.
    """
    load_dotenv()

    missing = [
        env_var
        for env_var, _ in _REQUIRED_ENV_VARS
        if not os.getenv(env_var, "").strip()
    ]
    if missing:
        raise ValueError(
            f"Missing required environment variable(s): {', '.join(missing)}"
        )

    kwargs: dict[str, str] = {
        field_name: os.environ[env_var] for env_var, field_name in _REQUIRED_ENV_VARS
    }

    cors_raw = os.getenv("CORS_ORIGINS", "").strip()
    if cors_raw:
        cors_origins = [origin.strip() for origin in cors_raw.split(",") if origin.strip()]
    else:
        cors_origins = ["http://localhost:5173", "http://localhost:3000"]

    return Config(**kwargs, cors_origins=cors_origins)

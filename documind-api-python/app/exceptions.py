"""Custom exception classes for DocuMind API."""


class ServiceUnavailableError(Exception):
    """Raised when an Azure service is unavailable."""

    def __init__(self, message: str = "Service is currently unavailable.") -> None:
        self.message = message
        super().__init__(self.message)

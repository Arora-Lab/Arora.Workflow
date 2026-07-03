namespace Arora.Workflow.Domain.Exceptions;

/// <summary>
/// The base class for all exceptions thrown by Arora.Workflow.
/// Catch this type to handle any Arora.Workflow error.
/// Catch a specific derived type to handle a particular error condition.
/// </summary>
public abstract class WorkflowException : Exception
{
    /// <summary>
    /// A machine-readable code identifying the specific error condition.
    /// Suitable for use in API error responses and structured logging.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>Initializes the exception with a message and error code.</summary>
    protected WorkflowException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>Initializes the exception with a message, error code, and inner exception.</summary>
    protected WorkflowException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

namespace RecallCommander.AI;

/// <summary>
/// Thrown when AI evaluation cannot produce a result: the provider is
/// misconfigured or unreachable, or its response is unusable. The message is
/// safe to show to the user.
/// </summary>
public sealed class AiException(string message, Exception? innerException = null)
    : Exception(message, innerException);

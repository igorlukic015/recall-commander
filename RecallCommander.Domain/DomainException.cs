namespace RecallCommander.Domain;

/// <summary>
/// Thrown when a domain invariant is violated.
/// </summary>
public sealed class DomainException(string message) : Exception(message);

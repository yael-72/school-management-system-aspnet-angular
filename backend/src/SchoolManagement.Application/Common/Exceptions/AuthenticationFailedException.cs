namespace SchoolManagement.Application.Common.Exceptions;

/// <summary>Login failed or the user is no longer allowed in. Mapped to HTTP 401.</summary>
public class AuthenticationFailedException(string message) : Exception(message);

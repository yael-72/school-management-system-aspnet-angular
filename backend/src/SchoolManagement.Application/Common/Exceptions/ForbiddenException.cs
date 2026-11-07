namespace SchoolManagement.Application.Common.Exceptions;

/// <summary>The user is logged in but may not access this resource. Mapped to HTTP 403.</summary>
public class ForbiddenException(string message = "You do not have permission to access this resource.")
    : Exception(message);

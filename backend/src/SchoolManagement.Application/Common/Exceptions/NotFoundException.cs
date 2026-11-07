namespace SchoolManagement.Application.Common.Exceptions;

/// <summary>Mapped to HTTP 404 by the API's global exception handler.</summary>
public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with id '{key}' was not found.");

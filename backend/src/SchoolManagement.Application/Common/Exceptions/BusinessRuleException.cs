namespace SchoolManagement.Application.Common.Exceptions;

/// <summary>
/// The request is valid in shape but breaks a business rule
/// (for example "a score cannot exceed the assessment's maximum"). Mapped to HTTP 400.
/// </summary>
public class BusinessRuleException(string message) : Exception(message);

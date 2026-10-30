namespace APIHandler.Exceptions;

public class SubmittedSessionsHaveNoDishesException() : ValidationException("Submitted guest sessions do not contain any dishes.");

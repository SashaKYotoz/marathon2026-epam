namespace APIHandler.Exceptions;

public class NoSubmittedGuestSessionsException() : ValidationException("There are no submitted guest sessions to confirm.");

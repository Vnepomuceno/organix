using System;

public class InvalidLanguageException : Exception
{
	public InvalidLanguageException() : base() {}
	public InvalidLanguageException(string message) : base(message) {}
}
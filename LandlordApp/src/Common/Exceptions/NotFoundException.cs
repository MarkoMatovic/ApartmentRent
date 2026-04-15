namespace Lander.src.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resource, object key) : base($"{resource} with key '{key}' was not found.") { }
}

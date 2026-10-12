namespace BiblioGo.Application.Exceptions;

public class NotFoundException : BiblioGoException
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
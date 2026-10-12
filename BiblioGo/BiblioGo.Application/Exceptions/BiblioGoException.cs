namespace BiblioGo.Application.Exceptions;

public class BiblioGoException : Exception
{
    public BiblioGoException()
    {
    }

    public BiblioGoException(string message) : base(message)
    {
    }

    public BiblioGoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
namespace BiblioGo.Application.Exceptions
{
    public class InputValidationException : BiblioGoException
    {
        public InputValidationException()
        {
        }

        public InputValidationException(string message) : base(message)
        {
        }

        public InputValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

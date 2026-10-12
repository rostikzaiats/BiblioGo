namespace BiblioGo.Application.Exceptions
{
    public class BusinessRuleException : BiblioGoException
    {
        public BusinessRuleException()
        {
        }

        public BusinessRuleException(string message) : base(message)
        {
        }

        public BusinessRuleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

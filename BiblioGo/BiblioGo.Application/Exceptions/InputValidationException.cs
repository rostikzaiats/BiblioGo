using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

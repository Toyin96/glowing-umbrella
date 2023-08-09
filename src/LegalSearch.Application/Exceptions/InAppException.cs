using System.Globalization;
using System.Runtime.Serialization;

namespace LegalSearch.Application.Exceptions
{
    [Serializable]
    public class InAppException : Exception
    {
        protected InAppException(string message) : base(message)
        {

        }

        protected InAppException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InAppException(string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
        }

        protected InAppException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

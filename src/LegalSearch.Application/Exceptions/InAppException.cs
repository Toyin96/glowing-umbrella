using System.Globalization;
using System.Runtime.Serialization;

namespace LegalSearch.Application.Exceptions
{
    [Serializable]
    public class InAppException : Exception
    {
        public InAppException(string message) : base(message)
        {

        }

        public InAppException(string message, Exception inner) : base(message, inner)
        {
        }

        public InAppException(string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
        }

        public InAppException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

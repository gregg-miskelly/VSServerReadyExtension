using System;
using System.Runtime.Serialization;

namespace VSServerReadyLauncher
{
    [Serializable]
    internal class ServerReadyException : Exception
    {
        public ServerReadyException(string message) : base(message)
        {
        }

        public ServerReadyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ServerReadyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
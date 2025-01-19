using System;
using System.Runtime.Serialization;

namespace U3A.Services.APIClient.Exceptions
{
    [Serializable]
    public class APIClientResponseException : Exception
    {
        public string Body { get; private set; }

        public APIClientResponseException(string body)
        {
            Body = body;
        }

        public APIClientResponseException(string message, string body) : base(message)
        {
            Body = body;
        }

        public APIClientResponseException(string message, string body, Exception inner) : base(message, inner)
        {
            Body = body;
        }

        protected APIClientResponseException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}

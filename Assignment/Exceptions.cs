using System;
using System.Net;

namespace Assignment
{
    class RequestLimitExceededException : Exception
    {
        public RequestLimitExceededException(string reasonPhrase)
            : base(reasonPhrase)
        { }
    }

    public class UnexpectedApiResponseException : Exception
    {
        public UnexpectedApiResponseException(string message)
            : base(message)
        { }

        public UnexpectedApiResponseException(Exception innerException)
            : base("Failed to parse response from API", innerException)
        { }

        public UnexpectedApiResponseException(HttpStatusCode statusCode, string reasonPhrase) 
            : base($"Unexpected status code returned from API: [{statusCode}] {reasonPhrase}")
        { }
    }
}

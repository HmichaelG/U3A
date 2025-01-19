using System;

namespace U3A.Services.APIClient.Exceptions
{
    /// <summary>
    /// An exception thrown when the Postmark API rejects a request. See the Response property for exact information on the issue.
    /// </summary>
    public class APIClientValidationException : Exception
    {
        public APIClientValidationException(APIClientResponse response)
            : base(response.Message)
        {
            Response = response;
        }

        /// <summary>
        /// The complete response returned from the Postmark API.
        /// </summary>
        public APIClientResponse Response { get; set; }
    }
}

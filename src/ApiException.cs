using System;
using Xamarin.Social;
using Xamarin.Auth;

namespace Stampsy.Social
{
    public class ApiException : SocialException
    {
        public Response Response { get; private set; }
        public ApiExceptionKind Kind { get; private set; }
        public int Code { get; private set; }

        public ApiException (string message, Exception innerException, ApiExceptionKind kind)
            : base (message, innerException)
        {
            Kind = kind;
        }

        public ApiException (string message, int code, Exception innerException, ApiExceptionKind kind) 
            : this (message, innerException, kind)
        {
            Code = code;
        }

        public ApiException (string message, Response response, ApiExceptionKind kind)
            : base (message)
        {
            Response = response;
            Kind = kind;
        }

        public ApiException (string message, int code, Response response, ApiExceptionKind kind) 
            : this (message, response, kind)
        {
            Code = code;
        }

        public ApiException (string message, Response response, Exception innerException, ApiExceptionKind kind)
            : base (message, innerException)
        {
            Response = response;
            Kind = kind;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Social;

namespace Stampsy.Social.Providers
{
    internal interface ISessionProvider
    {        
        Task<Session> Login (LoginOptions options, string [] scope, CancellationToken token);
    }
}
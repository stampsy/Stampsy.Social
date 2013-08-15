using System;
using Xamarin.Auth;
using Xamarin.Social;

namespace Stampsy.Social
{
    public sealed class Session
    {
        public Service Service { get; private set; }
        public Account Account { get; private set; }

        public Session (Service service, Account account)
        {
            Service = service;
            Account = account;
        }
    }
}


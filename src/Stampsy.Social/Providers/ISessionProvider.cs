using System.Threading;
using System.Threading.Tasks;

namespace Stampsy.Social.Providers
{
    internal interface ISessionProvider
    {        
        Task<Session> Login (LoginOptions options, string [] scope, CancellationToken token);
    }
}
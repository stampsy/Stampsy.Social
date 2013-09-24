using Newtonsoft.Json.Linq;

namespace Stampsy.Social
{
    internal static class JsonExtensions
    {
        public static T Value<T> (this JToken token, string outer, string inner)
        {
            var o = token [outer];
            if (o == null)
                return default (T);

            return o.Value<T> (inner);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stampsy.Social
{
    public interface IChoiceProvider<T>
    {
        Task<T> ChooseAsync (IEnumerable<T> options, Func<T, string> toString);
    }
}
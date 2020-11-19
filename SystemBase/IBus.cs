using System.Collections.Generic;

namespace SystemBase
{
    public interface IBus
    {
        IEnumerable<IBusComponent> AllComponents { get; }
    }
}

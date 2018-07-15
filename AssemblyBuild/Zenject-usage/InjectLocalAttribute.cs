using System;
using Zenject.Internal;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Parameter
        | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class InjectLocalAttribute : InjectAttributeBase
    {
        [Preserve]
        public InjectLocalAttribute()
        {
            Source = InjectSources.Local;
        }
    }
}

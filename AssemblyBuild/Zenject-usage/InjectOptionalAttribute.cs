using System;
using Zenject.Internal;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Parameter
        | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class InjectOptionalAttribute : InjectAttributeBase
    {
        [Preserve]
        public InjectOptionalAttribute()
        {
            Optional = true;
        }
    }
}


using System;
using Zenject.Internal;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Constructor
        | AttributeTargets.Method | AttributeTargets.Parameter
        | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class InjectAttribute : InjectAttributeBase
    {
        [Preserve]
        public InjectAttribute()
        {
        }
    }
}


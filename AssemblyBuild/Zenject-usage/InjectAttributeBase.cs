using System;
using Zenject.Internal;

namespace Zenject
{
    public abstract class InjectAttributeBase : PreserveAttribute
    {
        [Preserve]
        public InjectAttributeBase()
        {
        }

        public bool Optional
        {
            get;
            set;
        }

        public object Id
        {
            get;
            set;
        }

        public InjectSources Source
        {
            get;
            set;
        }
    }
}

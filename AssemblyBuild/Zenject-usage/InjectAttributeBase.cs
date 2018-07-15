using System;
using Zenject.Internal;

namespace Zenject
{
    public abstract class InjectAttributeBase : Attribute
    // TODO: Uncomment this once Unity 2018.2 fixes the IL2CPP bug that it causes
    // If you uncomment this then you get NullReferenceException during IL2CPP build
    //public abstract class InjectAttributeBase : PreserveAttribute
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

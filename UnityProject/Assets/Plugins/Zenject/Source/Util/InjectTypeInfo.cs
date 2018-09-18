using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Zenject.Internal
{
    public delegate void ZenInjectMethod(object obj, object[] args);
    public delegate object ZenFactoryMethod(object[] args);
    public delegate void ZenMemberSetterMethod(object obj, object value);

    [NoReflectionCodeWeaving]
    public class InjectTypeInfo
    {
        public readonly Type Type;
        public readonly InjectTypeInfo BaseTypeInfo;
        public readonly InjectMethodInfo[] InjectMethods;
        public readonly InjectMemberInfo[] InjectMembers;
        public readonly InjectConstructorInfo InjectConstructor;

        public InjectTypeInfo(
            Type type,
            InjectConstructorInfo injectConstructor,
            InjectMethodInfo[] injectMethods,
            InjectMemberInfo[] injectMembers,
            InjectTypeInfo baseTypeInfo)
        {
            Type = type;
            BaseTypeInfo = baseTypeInfo;
            InjectMethods = injectMethods;
            InjectMembers = injectMembers;
            InjectConstructor = injectConstructor;
        }

        public IEnumerable<InjectableInfo> AllInjectables
        {
            get
            {
                return InjectConstructor.Parameters
                    .Concat(InjectMembers.Select(x => x.Info))
                    .Concat(InjectMethods.SelectMany(x => x.Parameters));
            }
        }

        [NoReflectionCodeWeaving]
        public class InjectMemberInfo
        {
            public readonly ZenMemberSetterMethod Setter;
            public readonly InjectableInfo Info;

            public InjectMemberInfo(
                ZenMemberSetterMethod setter,
                InjectableInfo info)
            {
                Setter = setter;
                Info = info;
            }
        }

        [NoReflectionCodeWeaving]
        public class InjectConstructorInfo
        {
            // Null for abstract types
            public readonly ZenFactoryMethod Factory;

            public readonly InjectableInfo[] Parameters;

            public InjectConstructorInfo(
                ZenFactoryMethod factory,
                InjectableInfo[] parameters)
            {
                Parameters = parameters;
                Factory = factory;
            }
        }

        [NoReflectionCodeWeaving]
        public class InjectMethodInfo
        {
            public readonly string Name;
            public readonly ZenInjectMethod Action;
            public readonly InjectableInfo[] Parameters;

            public InjectMethodInfo(
                ZenInjectMethod action,
                InjectableInfo[] parameters,
                string name)
            {
                Parameters = parameters;
                Action = action;
                Name = name;
            }
        }
    }
}

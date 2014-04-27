﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ModestTree.Zenject
{
    public class ResolveContext
    {
        public Type Target;
        public object TargetInstance;
        public string FieldName;
        public string Name;
        public List<Type> Parents;
    }

    public delegate bool BindingCondition(ResolveContext c);

    public class BindingConditionSetter
    {
        readonly private ProviderBase _provider;

        public BindingConditionSetter(ProviderBase provider)
        {
            _provider = provider;
        }

        public void When(BindingCondition condition)
        {
            _provider.SetCondition(condition);
        }

        public void WhenInjectedIntoInstance(object instance)
        {
            _provider.SetCondition(r => ReferenceEquals(r.TargetInstance, instance));
        }

        public void WhenInjectedInto(params Type[] targets)
        {
            _provider.SetCondition(r => targets.Contains(r.Target));
        }

        public void WhenInjectedInto<T>()
        {
            _provider.SetCondition(r => r.Target == typeof (T));
        }

        public void WhenInjectedInto<T>(string name)
        {
            _provider.SetCondition(r => r.Target == typeof(T) && r.Name == name);
        }

        public void WhenInjectedInto(string name)
        {
            _provider.SetCondition(r => r.Name == name);
        }
    }
}

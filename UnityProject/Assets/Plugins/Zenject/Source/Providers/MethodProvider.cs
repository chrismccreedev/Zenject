using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public class MethodProvider<TReturn> : IProvider
    {
        readonly DiContainer _container;
        readonly Func<InjectContext, TReturn> _method;

        public MethodProvider(
            Func<InjectContext, TReturn> method,
            DiContainer container)
        {
            _container = container;
            _method = method;
        }

        public Type GetInstanceType(InjectContext context)
        {
            return typeof(TReturn);
        }

        public List<object> GetAllInstancesWithInjectSplit(
            InjectContext context, List<TypeValuePair> args, out Action injectAction)
        {
            Assert.IsEmpty(args);
            Assert.IsNotNull(context);

            Assert.That(typeof(TReturn).DerivesFromOrEqual(context.MemberType));

            injectAction = null;
            if (_container.IsValidating && !DiContainer.CanCreateOrInjectDuringValidation(context.MemberType))
            {
                return new List<object>() { new ValidationMarker(typeof(TReturn)) };
            }
            else
            {
                return new List<object>() { _method(context) };
            }
        }
    }
}

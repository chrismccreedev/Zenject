using System;
using System.Collections.Generic;
using System.Linq;
#if !NOT_UNITY3D
using UnityEngine;
#endif
using ModestTree;

namespace Zenject
{
    public class FactoryFromBinder<TContract> : FactoryFromBinderBase
    {
        public FactoryFromBinder(
            DiContainer container, BindInfo bindInfo, FactoryBindInfo factoryBindInfo)
            : base(container, typeof(TContract), bindInfo, factoryBindInfo)
        {
        }

        public ConditionCopyNonLazyBinder FromResolveGetter<TObj>(Func<TObj, TContract> method)
        {
            return FromResolveGetter<TObj>(null, method);
        }

        public ConditionCopyNonLazyBinder FromResolveGetter<TObj>(
            object subIdentifier, Func<TObj, TContract> method)
        {
            return FromResolveGetter<TObj>(subIdentifier, method, InjectSources.Any);
        }

        public ConditionCopyNonLazyBinder FromResolveGetter<TObj>(
            object subIdentifier, Func<TObj, TContract> method, InjectSources source)
        {
            FactoryBindInfo.ProviderFunc =
                (container) => new GetterProvider<TObj, TContract>(subIdentifier, method, container, source, false);

            return this;
        }

        public ConditionCopyNonLazyBinder FromMethod(Func<DiContainer, TContract> method)
        {
            ProviderFunc =
                (container) => new MethodProviderWithContainer<TContract>(method);

            return this;
        }

        // Shortcut for FromIFactory and also for backwards compatibility
        public ArgConditionCopyNonLazyBinder FromFactory<TSubFactory>()
            where TSubFactory : IFactory<TContract>
        {
            return FromIFactory(x => x.To<TSubFactory>().AsCached());
        }

        public ArgConditionCopyNonLazyBinder FromIFactory(
            Action<ConcreteBinderGeneric<IFactory<TContract>>> factoryBindGenerator)
        {
            Guid factoryId;
            factoryBindGenerator(
                CreateIFactoryBinder<IFactory<TContract>>(out factoryId));

            ProviderFunc =
                (container) => { return new IFactoryProvider<TContract>(container, factoryId); };

            return new ArgConditionCopyNonLazyBinder(BindInfo);
        }

        public FactorySubContainerBinder<TContract> FromSubContainerResolve()
        {
            return FromSubContainerResolve(null);
        }

        public FactorySubContainerBinder<TContract> FromSubContainerResolve(object subIdentifier)
        {
            return new FactorySubContainerBinder<TContract>(
                BindContainer, BindInfo, FactoryBindInfo, subIdentifier);
        }

        public ArgConditionCopyNonLazyBinder FromPoolableMemoryPool<TContractAgain>(
            Action<MemoryPoolInitialSizeMaxSizeBinder<TContractAgain>> poolBindGenerator)
            // Unfortunately we have to pass the same contract in again to satisfy the generic
            // constraints below
            where TContractAgain : IPoolable<IMemoryPool>
        {
            return FromPoolableMemoryPoolInternal<TContractAgain, PoolableMemoryPool<IMemoryPool, TContractAgain>>(poolBindGenerator);
        }

        ArgConditionCopyNonLazyBinder FromPoolableMemoryPoolInternal<TContractAgain, TMemoryPool>(
            Action<MemoryPoolInitialSizeMaxSizeBinder<TContractAgain>> poolBindGenerator)
            // Unfortunately we have to pass the same contract in again to satisfy the generic
            // constraints below
            where TContractAgain : IPoolable<IMemoryPool>
            where TMemoryPool : MemoryPool<IMemoryPool, TContractAgain>
        {
            Assert.IsEqual(typeof(TContractAgain), typeof(TContract));

            // Use a random ID so that our provider is the only one that can find it and so it doesn't
            // conflict with anything else
            var poolId = Guid.NewGuid();

            var binder = BindContainer.BindMemoryPoolCustomInterface<TContractAgain, TMemoryPool, TMemoryPool>(
                false,
                // Very important here that we call StartBinding with false otherwise the other
                // binding will be finalized early
                BindContainer.StartBinding(null, false))
                .WithId(poolId);

            // Always make it non lazy by default in case the user sets an InitialSize
            binder.NonLazy();

            poolBindGenerator(binder);

            ProviderFunc =
                (container) => { return new PoolableMemoryPoolProvider<TContractAgain, TMemoryPool>(container, poolId); };

            return new ArgConditionCopyNonLazyBinder(BindInfo);
        }

#if !NOT_UNITY3D

        public ArgConditionCopyNonLazyBinder FromMonoPoolableMemoryPool<TContractAgain>(
            Action<MemoryPoolInitialSizeMaxSizeBinder<TContractAgain>> poolBindGenerator)
            // Unfortunately we have to pass the same contract in again to satisfy the generic
            // constraints below
            where TContractAgain : Component, IPoolable<IMemoryPool>
        {
            return FromPoolableMemoryPoolInternal<TContractAgain, MonoPoolableMemoryPool<IMemoryPool, TContractAgain>>(poolBindGenerator);
        }

        public ConditionCopyNonLazyBinder FromComponentInHierarchy(
            bool includeInactive = true)
        {
            BindingUtil.AssertIsInterfaceOrComponent(ContractType);

            return FromMethod(_ =>
                {
                    var res = BindContainer.Resolve<Context>().GetRootGameObjects()
                        .Select(x => x.GetComponentInChildren<TContract>(includeInactive))
                        .Where(x => x != null).FirstOrDefault();

                    Assert.IsNotNull(res,
                        "Could not find component '{0}' through FromComponentInHierarchy factory binding", typeof(TContract));

                    return res;
                });
        }
#endif
    }
}

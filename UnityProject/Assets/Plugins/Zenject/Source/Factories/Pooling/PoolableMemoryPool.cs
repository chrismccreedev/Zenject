using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public interface IPoolableCommon
    {
        void OnDespawned();
    }

    public interface IPoolable : IPoolableCommon
    {
        void OnSpawned();
    }

    public interface IPoolable<TParam1> : IPoolableCommon
    {
        void OnSpawned(TParam1 p1);
    }

    public interface IPoolable<TParam1, TParam2> : IPoolableCommon
    {
        void OnSpawned(TParam1 p1, TParam2 p2);
    }

    public interface IPoolable<TParam1, TParam2, TParam3> : IPoolableCommon
    {
        void OnSpawned(TParam1 p1, TParam2 p2, TParam3 p3);
    }

    public interface IPoolable<TParam1, TParam2, TParam3, TParam4> : IPoolableCommon
    {
        void OnSpawned(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4);
    }

    public interface IPoolable<TParam1, TParam2, TParam3, TParam4, TParam5> : IPoolableCommon
    {
        void OnSpawned(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TParam5 p5);
    }

    public interface IPoolable<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> : IPoolableCommon
    {
        void OnSpawned(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TParam5 p5, TParam6 p6);
    }

    // Zero parameters
    public class PoolableMemoryPool<TValue>
        : MemoryPool<TValue>
        where TValue : IPoolable
    {
        protected override void OnDespawned(TValue item)
        {
            item.OnDespawned();
        }

        protected override void Reinitialize(TValue item)
        {
            item.OnSpawned();
        }
    }

    // One parameters
    public class PoolableMemoryPool<TParam1, TValue>
        : MemoryPool<TParam1, TValue>
        where TValue : IPoolable<TParam1>
    {
        protected override void OnDespawned(TValue item)
        {
            item.OnDespawned();
        }

        protected override void Reinitialize(TParam1 p1, TValue item)
        {
            item.OnSpawned(p1);
        }
    }

    // Two parameters
    public class PoolableMemoryPool<TParam1, TParam2, TValue>
        : MemoryPool<TParam1, TParam2, TValue>
        where TValue : IPoolable<TParam1, TParam2>
    {
        protected override void OnDespawned(TValue item)
        {
            item.OnDespawned();
        }

        protected override void Reinitialize(TParam1 p1, TParam2 p2, TValue item)
        {
            item.OnSpawned(p1, p2);
        }
    }

    // Three parameters
    public class PoolableMemoryPool<TParam1, TParam2, TParam3, TValue>
        : MemoryPool<TParam1, TParam2, TParam3, TValue>
        where TValue : IPoolable<TParam1, TParam2, TParam3>
    {
        protected override void OnDespawned(TValue item)
        {
            item.OnDespawned();
        }

        protected override void Reinitialize(TParam1 p1, TParam2 p2, TParam3 p3, TValue item)
        {
            item.OnSpawned(p1, p2, p3);
        }
    }

    // Four parameters
    public class PoolableMemoryPool<TParam1, TParam2, TParam3, TParam4, TValue>
        : MemoryPool<TParam1, TParam2, TParam3, TParam4, TValue>
        where TValue : IPoolable<TParam1, TParam2, TParam3, TParam4>
    {
        protected override void OnDespawned(TValue item)
        {
            item.OnDespawned();
        }

        protected override void Reinitialize(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TValue item)
        {
            item.OnSpawned(p1, p2, p3, p4);
        }
    }

    // Five parameters
    public class PoolableMemoryPool<TParam1, TParam2, TParam3, TParam4, TParam5, TValue>
        : MemoryPool<TParam1, TParam2, TParam3, TParam4, TParam5, TValue>
        where TValue : IPoolable<TParam1, TParam2, TParam3, TParam4, TParam5>
    {
        protected override void OnDespawned(TValue item)
        {
            item.OnDespawned();
        }

        protected override void Reinitialize(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TParam5 p5, TValue item)
        {
            item.OnSpawned(p1, p2, p3, p4, p5);
        }
    }

    // Six parameters
    public class PoolableMemoryPool<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TValue>
        : MemoryPool<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TValue>
        where TValue : IPoolable<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>
    {
        protected override void OnDespawned(TValue item)
        {
            item.OnDespawned();
        }

        protected override void Reinitialize(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TParam5 p5, TParam6 p6, TValue item)
        {
            item.OnSpawned(p1, p2, p3, p4, p5, p6);
        }
    }
}


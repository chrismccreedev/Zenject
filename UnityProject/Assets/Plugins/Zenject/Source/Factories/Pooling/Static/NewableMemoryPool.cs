using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    public abstract class NewableMemoryPoolBase<TValue> : IDespawnableMemoryPool<TValue>
        where TValue : class, new()
    {
        readonly Stack<TValue> _stack = new Stack<TValue>();

        Action<TValue> _onDespawnedMethod;

        public NewableMemoryPoolBase(Action<TValue> onDespawnedMethod)
        {
            _onDespawnedMethod = onDespawnedMethod;
        }

        public Action<TValue> OnDespawnedMethod
        {
            set { _onDespawnedMethod = value; }
        }

        public int NumTotal
        {
            get; private set;
        }

        public int NumActive
        {
            get { return NumTotal - NumInactive; }
        }

        public int NumInactive
        {
            get { return _stack.Count; }
        }

        public Type ItemType
        {
            get { return typeof(TValue); }
        }

        public void ClearPool()
        {
            _stack.Clear();
            NumTotal = 0;
        }

        public void ExpandPoolBy(int additionalSize)
        {
            for (int i = 0; i < additionalSize; i++)
            {
                _stack.Push(Alloc());
            }
        }

        TValue Alloc()
        {
            var value = new TValue();
            NumTotal++;
            return value;
        }

        protected TValue SpawnInternal()
        {
            TValue element;

            if (_stack.Count == 0)
            {
                element = Alloc();
            }
            else
            {
                element = _stack.Pop();
            }

            return element;
        }

        protected DisposeWrapper<TValue> SpawnWrapper(TValue instance)
        {
            return DisposeWrapper<TValue>.Pool.Spawn(instance, this);
        }

        public void Despawn(TValue element)
        {
            if (_onDespawnedMethod != null)
            {
                _onDespawnedMethod(element);
            }

            if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), element))
            {
                ModestTree.Log.Error("Despawn error. Trying to destroy object that is already released to pool.");
            }

            Assert.That(!_stack.Contains(element), "Attempted to despawn element twice!");

            _stack.Push(element);
        }
    }

    // Zero parameters

    public class NewableMemoryPool<TValue> : NewableMemoryPoolBase<TValue>, IMemoryPool<TValue>
        where TValue : class, new()
    {
        Action<TValue> _onSpawnMethod;

        public NewableMemoryPool(
            Action<TValue> onSpawnMethod = null, Action<TValue> onDespawnedMethod = null)
            : base(onDespawnedMethod)
        {
            _onSpawnMethod = onSpawnMethod;
        }

        public Action<TValue> OnSpawnMethod
        {
            set { _onSpawnMethod = value; }
        }

        public DisposeWrapper<TValue> SpawnWrapper()
        {
            return base.SpawnWrapper(Spawn());
        }

        public TValue Spawn()
        {
            var item = SpawnInternal();

            if (_onSpawnMethod != null)
            {
                _onSpawnMethod(item);
            }

            return item;
        }
    }

    // One parameter

    public class NewableMemoryPool<TParam1, TValue> : NewableMemoryPoolBase<TValue>, IMemoryPool<TParam1, TValue>
        where TValue : class, new()
    {
        Action<TParam1, TValue> _onSpawnMethod;

        public NewableMemoryPool(
            Action<TParam1, TValue> onSpawnMethod, Action<TValue> onDespawnedMethod = null)
            : base(onDespawnedMethod)
        {
            // What's the point of having a param otherwise?
            Assert.IsNotNull(onSpawnMethod);
            _onSpawnMethod = onSpawnMethod;
        }

        public Action<TParam1, TValue> OnSpawnMethod
        {
            set { _onSpawnMethod = value; }
        }

        public DisposeWrapper<TValue> SpawnWrapper(TParam1 param)
        {
            return base.SpawnWrapper(Spawn(param));
        }

        public TValue Spawn(TParam1 param)
        {
            var item = SpawnInternal();

            if (_onSpawnMethod != null)
            {
                _onSpawnMethod(param, item);
            }

            return item;
        }
    }

    // Two parameter

    public class NewableMemoryPool<TParam1, TParam2, TValue> : NewableMemoryPoolBase<TValue>, IMemoryPool<TParam1, TParam2, TValue>
        where TValue : class, new()
    {
        Action<TParam1, TParam2, TValue> _onSpawnMethod;

        public NewableMemoryPool(
            Action<TParam1, TParam2, TValue> onSpawnMethod, Action<TValue> onDespawnedMethod = null)
            : base(onDespawnedMethod)
        {
            // What's the point of having a param otherwise?
            Assert.IsNotNull(onSpawnMethod);
            _onSpawnMethod = onSpawnMethod;
        }

        public Action<TParam1, TParam2, TValue> OnSpawnMethod
        {
            set { _onSpawnMethod = value; }
        }

        public DisposeWrapper<TValue> SpawnWrapper(TParam1 p1, TParam2 p2)
        {
            return base.SpawnWrapper(Spawn(p1, p2));
        }

        public TValue Spawn(TParam1 p1, TParam2 p2)
        {
            var item = SpawnInternal();

            if (_onSpawnMethod != null)
            {
                _onSpawnMethod(p1, p2, item);
            }

            return item;
        }
    }

    // Three parameters

    public class NewableMemoryPool<TParam1, TParam2, TParam3, TValue> : NewableMemoryPoolBase<TValue>, IMemoryPool<TParam1, TParam2, TParam3, TValue>
        where TValue : class, new()
    {
        Action<TParam1, TParam2, TParam3, TValue> _onSpawnMethod;

        public NewableMemoryPool(
            Action<TParam1, TParam2, TParam3, TValue> onSpawnMethod, Action<TValue> onDespawnedMethod = null)
            : base(onDespawnedMethod)
        {
            // What's the point of having a param otherwise?
            Assert.IsNotNull(onSpawnMethod);
            _onSpawnMethod = onSpawnMethod;
        }

        public Action<TParam1, TParam2, TParam3, TValue> OnSpawnMethod
        {
            set { _onSpawnMethod = value; }
        }

        public DisposeWrapper<TValue> SpawnWrapper(TParam1 p1, TParam2 p2, TParam3 p3)
        {
            return base.SpawnWrapper(Spawn(p1, p2, p3));
        }

        public TValue Spawn(TParam1 p1, TParam2 p2, TParam3 p3)
        {
            var item = SpawnInternal();

            if (_onSpawnMethod != null)
            {
                _onSpawnMethod(p1, p2, p3, item);
            }

            return item;
        }
    }

    // Four parameters

    public class NewableMemoryPool<TParam1, TParam2, TParam3, TParam4, TValue> : NewableMemoryPoolBase<TValue>, IMemoryPool<TParam1, TParam2, TParam3, TParam4, TValue>
        where TValue : class, new()
    {
        ModestTree.Util.Action<TParam1, TParam2, TParam3, TParam4, TValue> _onSpawnMethod;

        public NewableMemoryPool(
            ModestTree.Util.Action<TParam1, TParam2, TParam3, TParam4, TValue> onSpawnMethod, Action<TValue> onDespawnedMethod = null)
            : base(onDespawnedMethod)
        {
            // What's the point of having a param otherwise?
            Assert.IsNotNull(onSpawnMethod);
            _onSpawnMethod = onSpawnMethod;
        }

        public ModestTree.Util.Action<TParam1, TParam2, TParam3, TParam4, TValue> OnSpawnMethod
        {
            set { _onSpawnMethod = value; }
        }

        public DisposeWrapper<TValue> SpawnWrapper(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4)
        {
            return base.SpawnWrapper(Spawn(p1, p2, p3, p4));
        }

        public TValue Spawn(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4)
        {
            var item = SpawnInternal();

            if (_onSpawnMethod != null)
            {
                _onSpawnMethod(p1, p2, p3, p4, item);
            }

            return item;
        }
    }

    // Five parameters

    public class NewableMemoryPool<TParam1, TParam2, TParam3, TParam4, TParam5, TValue> : NewableMemoryPoolBase<TValue>, IMemoryPool<TParam1, TParam2, TParam3, TParam4, TParam5, TValue>
        where TValue : class, new()
    {
        ModestTree.Util.Action<TParam1, TParam2, TParam3, TParam4, TParam5, TValue> _onSpawnMethod;

        public NewableMemoryPool(
            ModestTree.Util.Action<TParam1, TParam2, TParam3, TParam4, TParam5, TValue> onSpawnMethod, Action<TValue> onDespawnedMethod = null)
            : base(onDespawnedMethod)
        {
            // What's the point of having a param otherwise?
            Assert.IsNotNull(onSpawnMethod);
            _onSpawnMethod = onSpawnMethod;
        }

        public ModestTree.Util.Action<TParam1, TParam2, TParam3, TParam4, TParam5, TValue> OnSpawnMethod
        {
            set { _onSpawnMethod = value; }
        }

        public DisposeWrapper<TValue> SpawnWrapper(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TParam5 p5)
        {
            return base.SpawnWrapper(Spawn(p1, p2, p3, p4, p5));
        }

        public TValue Spawn(TParam1 p1, TParam2 p2, TParam3 p3, TParam4 p4, TParam5 p5)
        {
            var item = SpawnInternal();

            if (_onSpawnMethod != null)
            {
                _onSpawnMethod(p1, p2, p3, p4, p5, item);
            }

            return item;
        }
    }
}

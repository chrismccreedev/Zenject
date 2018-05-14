using System;
using System.Collections.Generic;
using Zenject;

namespace ModestTree
{
    public class ListPool<T> : NewableMemoryPool<List<T>>
    {
        static ListPool<T> _instance = new ListPool<T>();

        public ListPool()
        {
            OnDespawnedMethod = OnDespawned;
        }

        public static ListPool<T> Instance
        {
            get { return _instance; }
        }

        void OnDespawned(List<T> list)
        {
            list.Clear();
        }

        public DisposeWrapper<List<T>> SpawnWrapper(IEnumerable<T> values)
        {
            var wrapper = base.SpawnWrapper(Spawn());
            wrapper.Value.AddRange(values);
            return wrapper;
        }
    }
}

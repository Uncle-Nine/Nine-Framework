﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework.Pool
{
    public class PrefabPool<TComponent> : Pool<TComponent> where TComponent : Component
    {
        public PrefabPool(TComponent prefab, int initCount = 1, Action<TComponent> onAlloc = null,
            Action<TComponent> onFree = null, Action<TComponent> onDispose = null) : base(
            () => Object.Instantiate(prefab), initCount, onAlloc, onFree, onDispose)
        {
        }

        public override TComponent Allocate()
        {
            var result = base.Allocate();
            result.gameObject.SetActive(true);
            return result;
        }

        public override void Free(TComponent obj)
        {
            base.Free(obj);
            obj.gameObject.SetActive(false);
        }
    }

    public class PrefabPool : Pool<GameObject>
    {
        public PrefabPool(GameObject prefab, int initCount = 1, Action<GameObject> onAlloc = null,
            Action<GameObject> onFree = null, Action<GameObject> onDispose = null) : base(
            () => Object.Instantiate(prefab), initCount, onAlloc, onFree, onDispose)
        {
        }

        public override GameObject Allocate()
        {
            var result = base.Allocate();
            result.gameObject.SetActive(true);
            return result;
        }

        public override void Free(GameObject obj)
        {
            base.Free(obj);
            obj.gameObject.SetActive(false);
        }
    }

    public class PrefabPoolWithKey<TKey, TComponent> : PoolWithKey<TKey, TComponent> where TComponent : Component
    {
        private Dictionary<TKey, TComponent> key2Component = new Dictionary<TKey, TComponent>();

        protected PrefabPoolWithKey(Func<TKey, TComponent> factory, Action<TComponent> onAlloc,
            Action<TComponent> onFree, Action<TComponent> onDispose) : base(factory, onAlloc, onFree, onDispose)
        {
            Factory = key =>
            {
                if (!key2Component.ContainsKey(key))
                    key2Component[key] = factory(key);
                return Object.Instantiate(key2Component[key]);
            };
        }

        public override TComponent Allocate(TKey key)
        {
            var result = base.Allocate(key);
            result.gameObject.SetActive(true);
            return result;
        }

        public override void Free(TKey key, TComponent obj)
        {
            base.Free(key, obj);
            obj.gameObject.SetActive(false);
        }
    }
}
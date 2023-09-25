using System;
using System.Collections.Generic;
using UnityEngine;

namespace PinGUReplay.ProvenanceGraphViewerModule.Nodes.Pool
{
    public static class ProvenanceNodesPoolManager
    {
        private static readonly PoolSource _poolSource = new PoolSource();

        public static void OnDestroyPooled(IProvenanceNodePoolComponent poolComponent)
        {
            if (poolComponent.gameObject == null || !poolComponent.gameObject.activeInHierarchy)
                return;

            _poolSource.OnDestroyPooled(poolComponent);
        }

        /// <summary>
        /// NOT PUT THIS ON AWAKE! BECAUSE WILL MAKE A INFINITY LOOP!
        /// </summary>
        /// <param name="poolComponent"></param>
        public static void OnStartPooled(IProvenanceNodePoolComponent poolComponent)
        {
            var uid = poolComponent.UID;
            if (!_poolSource.ContainReferenceFor(uid))
            {
                _poolSource.GenerateReference(poolComponent);
                Debug.Log("Create PoolReference on Start for: " + poolComponent.UID);
            }
        }

        public static GameObject CreatePooledOrDefault(GameObject o, Vector3 position, Quaternion rotation,
            Transform parent)
        {
            if (o == null)
                return null;
            var component = o.GetComponent<IProvenanceNodePoolComponent>();
            if (component == null || !Application.isPlaying)
                return GameObject.Instantiate(o, position, rotation, parent);
            if (!_poolSource.ContainReferenceFor(component.UID))
                _poolSource.GenerateReference(component);

            return _poolSource.GetOrCreate(component, position, rotation, parent);
        }
    }

    public interface IProvenanceNodePoolCleanUp
    {
        void CleanUp(GameObject reference);
    }

    public interface IProvenanceNodePoolComponent
    {
        string UID { get; }
        GameObject gameObject { get; }
        void DoPool(GameObject reference);

    }

    public class PoolSource
    {
        private Dictionary<string, Queue<IProvenanceNodePoolComponent>> _pool =
            new Dictionary<string, Queue<IProvenanceNodePoolComponent>>();

        private Dictionary<string, IProvenanceNodePoolComponent> _references =
            new Dictionary<string, IProvenanceNodePoolComponent>();

        public void OnDestroyPooled(IProvenanceNodePoolComponent obj)
        {
            if (obj == null || obj.gameObject == null)
                return;

            obj.gameObject.SetActive(false);
            IProvenanceNodePoolComponent poolComponent = GetReferenceObject(obj.UID);
            if (poolComponent != null && poolComponent.gameObject != null)
            {
                obj.DoPool(poolComponent.gameObject);
                _pool[obj.UID].Enqueue(obj);
            }
        }

        public void AddReference(IProvenanceNodePoolComponent obj)
        {
            _references[obj.UID] = obj;
            _pool[obj.UID] = new Queue<IProvenanceNodePoolComponent>();
        }

        private IProvenanceNodePoolComponent GetReferenceObject(string id)
        {
            IProvenanceNodePoolComponent obj;
            _references.TryGetValue(id, out obj);
            return obj;
        }

        public void GenerateReference(IProvenanceNodePoolComponent poolComponent)
        {
            var obj = GameObject.Instantiate(poolComponent.gameObject, Vector3.one * 5,
                poolComponent.gameObject.transform.rotation);
            obj.SetActive(false);
            obj.name = poolComponent.gameObject.name;
            var p = obj.GetComponent<IProvenanceNodePoolComponent>();
            AddReference(p);
        }

        public bool ContainReferenceFor(string uid)
        {
            try
            {
                if (!_references.TryGetValue(uid, out var provenanceNodePoolComponent))
                    return false;

                //Access gameobject to check if has invalid and catch exception if this happens.
                if (provenanceNodePoolComponent.gameObject != null)
                    return true;
            }
            catch (Exception e)
            {
                _references.Remove(uid);
                return false;
            }

            _references.Remove(uid);
            return false;
        }

        private bool ContainPooledFree(string uid)
        {
            Queue<IProvenanceNodePoolComponent> list;
            _pool.TryGetValue(uid, out list);
            return list.Count != 0;
        }

        public GameObject GetOrCreate(IProvenanceNodePoolComponent component, Vector3 position, Quaternion rotation,
            Transform parent)
        {
            if (component == null || component.gameObject == null)
                return null;

            while (ContainPooledFree(component.UID))
            {
                try
                {
                    IProvenanceNodePoolComponent poolComponent = _pool[component.UID].Dequeue();
                    if (poolComponent != null && poolComponent.gameObject != null)
                    {
                        var pooled = poolComponent.gameObject;
                        pooled.transform.position = position;
                        pooled.transform.rotation = rotation;
                        pooled.transform.parent = parent;
                        pooled.SetActive(true);
                        return pooled;
                    }
                }
                catch (MissingReferenceException e)
                {

                }
            }

            var o = GameObject.Instantiate(component.gameObject, position, rotation, parent);
            return o;
        }

        public void Clear()
        {
            _pool.Clear();
            _references.Clear();
        }
    }
}
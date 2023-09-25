using UnityEngine;

namespace PinGUReplay.ProvenanceGraphViewerModule.Nodes.Pool
{
    public class ProvenanceNodesPoolComponent : MonoBehaviour, IProvenanceNodePoolComponent
    {
        private IProvenanceNodePoolCleanUp[] _cleanUpsObjects;
        [SerializeField] protected string _uid;

        public string UID
        {
            get { return _uid; }
        }

        public void Awake()
        {
            if (StartLogic())
                return;

            _cleanUpsObjects = GetComponentsInChildren<IProvenanceNodePoolCleanUp>();
        }

        private bool StartLogic()
        {
            if (!string.IsNullOrEmpty(UID))
                return false;
            Destroy(this);
            return true;
        }

        public void Start()
        {
            ProvenanceNodesPoolManager.OnStartPooled(this);
        }

        public void DoPool(GameObject reference)
        {
            for (int i = 0; i < _cleanUpsObjects.Length; i++)
            {
                var obj = _cleanUpsObjects[i];
                if (obj == null)
                    continue;
                obj.CleanUp(reference);
            }
        }

        public void DestroyPoolObject()
        {
            ProvenanceNodesPoolManager.OnDestroyPooled(this);
        }
    }
}
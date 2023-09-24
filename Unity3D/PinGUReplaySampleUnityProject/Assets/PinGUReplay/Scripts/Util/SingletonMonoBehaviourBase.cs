using UnityEngine;

namespace PinGUReplay.Util
{
    [DefaultExecutionOrder(-10)]
    public abstract class SingletonMonoBehaviourBase<TypeOfMonoBehaviour> : MonoBehaviour 
        where TypeOfMonoBehaviour : SingletonMonoBehaviourBase<TypeOfMonoBehaviour>
    {
        public static TypeOfMonoBehaviour Instance;
        public static bool Initialized => Instance != null;

        [SerializeField]
        private bool _destroyOnLoad;
        
        public void Awake()
        {
            if (Instance == null)
            {
                Instance = (TypeOfMonoBehaviour) this;
                if(!_destroyOnLoad)
                    DontDestroyOnLoad(this.gameObject);
            }

            if (Instance == this)
            {
                OnAwakeValidInstance();
                return;
            }
            
            Destroy(this.gameObject);
        }

        protected virtual void OnAwakeValidInstance()
        {
            
        }

        public void OnEnable()
        {
            if (Instance != this)
                return;

            OnEnableValidInstance();
        }
        
        protected virtual void OnEnableValidInstance()
        {
            
        }
        
        public void Start()
        {
            if (Instance != this)
                return;

            StartValidInstance();
        }
        
        protected virtual void StartValidInstance()
        {
            
        }
        
        
        public void OnDisable()
        {
            if (Instance != this)
                return;

            OnDisableValidInstance();
        }
        
        protected virtual void OnDisableValidInstance()
        {
            
        }
        
        public void OnDestroy()
        {
            if(Instance != (TypeOfMonoBehaviour) this)
                return;

            Instance = null;
            OnDestroyValidInstance();
        }

        protected virtual void OnDestroyValidInstance()
        {
            
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PinGUReplay.ReplayModule.Core
{
    [DisallowMultipleComponent]
    public class ReplayObject : MonoBehaviour
    {
        [SerializeField] private string _replayObjectId;
        public string ReplayObjectId => _replayObjectId;

        private int _originalSaveInstanceId;
        public int OriginalSaveInstanceId => _originalSaveInstanceId;
        
        [SerializeField] private List<MonoBehaviour> _behaviourToDisableOnReplayMode; 
        private List<IReplayComponent> _replayComponents;
        
        private void Reset()
        {
            if(!string.IsNullOrEmpty(_replayObjectId))
                return;

            _replayObjectId = Guid.NewGuid().ToString();
        }
        
        private void Awake()
        {
            LoadReplayComponentList();
            
            if(!ReplayController.Initialized)
                return;
            
            ReplayController.Instance.OnAwakeReplayObject(this);
            
            if(ReplayController.Instance.ReplaySystemState == ReplaySystemState.Stopped ||
               ReplayController.Instance.ReplaySystemState == ReplaySystemState.Recording)
                return;
            
            for (int i = 0; i < _behaviourToDisableOnReplayMode.Count; i++)
                Destroy(_behaviourToDisableOnReplayMode[i]);        
        }
        
        private void LoadReplayComponentList()
        {
            _replayComponents = transform.GetComponentsInChildren<IReplayComponent>(true).ToList();
        }

        private void OnDestroy()
        {
            if(!ReplayController.Initialized)
                return;
            
            ReplayController.Instance.OnDestroyReplayObject(this);
        }

        public void SetupToReplay(int originalSaveInstanceId)
        {
            _originalSaveInstanceId = originalSaveInstanceId;
            
            for (int i = 0; i < _behaviourToDisableOnReplayMode.Count; i++)
                Destroy(_behaviourToDisableOnReplayMode[i]);    
            
            for (int i = 0; i < _replayComponents.Count; i++)
                _replayComponents[i].SetupToReplay();
        }
        
        public void OnExitReplay()
        {
            for (int i = 0; i < _replayComponents.Count; i++)
                _replayComponents[i].OnExitReplay();
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(gameObject.GetInstanceID());
            writer.Write(ReplayObjectId);
            writer.Write(gameObject.activeSelf);
            
            if(!gameObject.activeSelf)
                return;
            
            for (int i = 0; i < _replayComponents.Count; i++)
                _replayComponents[i].SaveState(writer);
        }
        
        public void LoadState(BinaryReader reader)
        {
            gameObject.SetActive(reader.ReadBoolean());
            
            if(!gameObject.activeSelf)
                return;
            
            for (int i = 0; i < _replayComponents.Count; i++)
                _replayComponents[i].LoadState(reader);
        }
    }
}
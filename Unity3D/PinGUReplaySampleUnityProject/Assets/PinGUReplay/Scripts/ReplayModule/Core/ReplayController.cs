using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PinGUReplay.Util;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace PinGUReplay.ReplayModule.Core
{
    public class ReplayController : SingletonMonoBehaviourBase<ReplayController>
    {
        [SerializeField] private ReplaySettings _replaySettings;
        [SerializeField] private ReplayDataSerializer _replayDataSerializer;
        [SerializeField] private Object _replayDataAsset;
        [SerializeField] private bool _playFirstReplayFrameAndPauseAtStart;
        [SerializeField] [ReadOnly] private ReplaySystemState _replaySystemState = ReplaySystemState.Stopped;

        public ReplaySystemState ReplaySystemState => _replaySystemState;
        
        private ReplayData _replayData = new ReplayData();
        private List<int> _replayObjectsOnTick = new List<int>();
        private List<GameObject> _replayObjectsToDestroy = new List<GameObject>();
        private Dictionary<int, int> _replayObjectsOriginalInstanceIdToReplayInstanceId = new Dictionary<int, int>();
        private Dictionary<int, ReplayObject> _replayObjects = new Dictionary<int, ReplayObject>();
        private BinaryWriter _binaryWriter;
        private BinaryReader _binaryReader;
        private int _tickNumber;

        public ReplaySystemStateUnityEvent OnChangeReplayState = new ReplaySystemStateUnityEvent();
        public UnityEvent OnChangeReplayTickOrTime = new UnityEvent();

        //Session Hierarchy
        private Transform _replaySessionParent;
        public Transform ReplaySessionParent => _replaySessionParent;
        private Transform _replayObjectsParent;
        public Transform ReplayObjectsParent => _replayObjectsParent;
        
        protected override void OnAwakeValidInstance()
        {
            CreateSessionHierarchy();
        }
        
        private void CreateSessionHierarchy()
        {
            _replaySessionParent = new GameObject("Replay Session").transform;
            _replayObjectsParent = new GameObject("Replay Objects").transform;
            _replayObjectsParent.SetParent(_replaySessionParent);
        }

        protected override void StartValidInstance()
        {
            if(!_playFirstReplayFrameAndPauseAtStart)
                return;

            if (_replayDataSerializer == null || _replayData == null || _replayDataAsset == null)
            {
                Debug.LogWarning("Invalid configuration on replay controller to play first replay frame!");
                return;
            }
            
            StartCoroutine(nameof(PlayFirstReplayFrameRoutine));
        }

        private IEnumerator PlayFirstReplayFrameRoutine()
        {
            LoadReplayData();
            StartReplay();
            yield return new WaitForFixedUpdate();
            PauseReplay();
        }
        
        public void SaveReplayData()
        {
            _replayDataSerializer.SaveReplayData(_replayData);
        }

        public void LoadReplayData()
        {
            if (!_replayDataSerializer.TryLoadReplayData(_replayDataAsset, out _replayData))
            {
                Debug.LogError("Can't load replay data from asset");
                return;
            }
            
            _binaryWriter = new BinaryWriter(_replayData.MemoryStream);
            _binaryReader = new BinaryReader(_replayData.MemoryStream);
        }
        
        public bool IsInReplayMode()
        {
            return !_replaySystemState.Equals(ReplaySystemState.Recording) &&
                   !_replaySystemState.Equals(ReplaySystemState.Stopped);
        }

        public void StartRecording()
        {
            if(_replaySystemState != ReplaySystemState.Stopped)
                return;
            
            _tickNumber = 0;
            _replayData.SaveReplayDataEveryTickCount = _replaySettings.saveReplayDataEveryTickCount;
            _replayData.TicksMemoryStreamEndPosition.Clear();
            _replayData.MemoryStream = new MemoryStream();
            _binaryWriter = new BinaryWriter(_replayData.MemoryStream);
            _binaryReader = new BinaryReader(_replayData.MemoryStream);
            _replaySystemState = ReplaySystemState.Recording;
            OnChangeReplayState.Invoke(_replaySystemState);
        }

        public void StopRecording()
        {
            if(_replaySystemState != ReplaySystemState.Recording)
                return;

            //This force save last replay tick before stop
            _tickNumber++;
            SaveReplayTick();
            
            _replaySystemState = ReplaySystemState.Stopped;
            OnChangeReplayState.Invoke(_replaySystemState);
        }
        
        public void StartReplay()
        {
            if(_replaySystemState != ReplaySystemState.Stopped)
                return;
            
            _tickNumber = 0;
            _replayObjectsOriginalInstanceIdToReplayInstanceId.Clear();
            _replayData.MemoryStream.Seek(0, SeekOrigin.Begin);
            _binaryWriter.Seek(0, SeekOrigin.Begin);
            _replaySystemState = ReplaySystemState.ReplayPlaying;
            OnChangeReplayState.Invoke(_replaySystemState);

            foreach (KeyValuePair<int, ReplayObject> replayObj in _replayObjects)
            {
                replayObj.Value.transform.SetParent(_replayObjectsParent);
                replayObj.Value.SetupToReplay(replayObj.Value.gameObject.GetInstanceID());
            }
        }

        public void PutReplayAtTick(int tick)
        {
            if(_replaySystemState == ReplaySystemState.Stopped ||
               _replaySystemState == ReplaySystemState.Recording)
                return;
            
            GetValidTickAndMemoryStreamStartPosition(tick, out var validNextTick, out var memoryStreamStartPosition);
            _tickNumber = validNextTick;
            _replayData.MemoryStream.Seek(memoryStreamStartPosition, SeekOrigin.Begin);
            _binaryWriter.Seek((int)memoryStreamStartPosition, SeekOrigin.Begin);
            ProcessReplayMode();
            OnChangeReplayTickOrTime.Invoke();
        }
        
        public void PutReplayAtTime(decimal tickTime)
        {
            if(_replaySystemState == ReplaySystemState.Stopped ||
               _replaySystemState == ReplaySystemState.Recording)
                return;

            var tickTimeIndex = _replayData.TicksTime.FindIndex(x => x.Equals(tickTime));

            if (tickTimeIndex == -1)
            {
                Debug.Log($"Can't find tick on this time: {tickTime}");
                return;
            }

            PutReplayAtTick(tickTimeIndex * _replayData.SaveReplayDataEveryTickCount);
        }


        //Need change tick number to consider SaveReplayDataEveryTickCount configuration
        private void GetValidTickAndMemoryStreamStartPosition(int tick, out int validCurrentTick, out long memoryStreamStartPosition)
        {
            memoryStreamStartPosition = 0;

            if (tick <= _replayData.SaveReplayDataEveryTickCount * 2)
            {
                validCurrentTick = 0;
                memoryStreamStartPosition = 0;
                return;
            }

            if (tick > GetLastReplayTick())
                tick = GetLastReplayTick();

            validCurrentTick = tick;
            for (int i = 1; i < _replayData.SaveReplayDataEveryTickCount + 1; i++)
            {
                validCurrentTick = tick - i;
                if (validCurrentTick % _replayData.SaveReplayDataEveryTickCount == 0)
                    break;
            }
            
            var previousValidTick = validCurrentTick;
            for (int i = 1; i < _replayData.SaveReplayDataEveryTickCount + 1; i++)
            {
                previousValidTick -= i;
                if (previousValidTick % _replayData.SaveReplayDataEveryTickCount == 0)
                    break;
            }
            
            memoryStreamStartPosition = _replayData.TicksMemoryStreamEndPosition[previousValidTick / _replayData.SaveReplayDataEveryTickCount];
        }
        
        public void ResumeReplay()
        {
            if(_replaySystemState != ReplaySystemState.ReplayPaused)
                return;
            
            _replaySystemState = ReplaySystemState.ReplayPlaying;
            OnChangeReplayState.Invoke(_replaySystemState);
        }
        
        public void PauseReplay()
        {
            if(_replaySystemState != ReplaySystemState.ReplayPlaying)
                return;
            
            _replaySystemState = ReplaySystemState.ReplayPaused;
            OnChangeReplayState.Invoke(_replaySystemState);
        }
        
        public void StopReplay()
        {
            if(_replaySystemState != ReplaySystemState.ReplayPlaying &&
               _replaySystemState != ReplaySystemState.ReplayPaused &&
               _replaySystemState != ReplaySystemState.ReplayFinished)
                return;
            
            foreach (KeyValuePair<int, ReplayObject> replayObj in _replayObjects)
                replayObj.Value.OnExitReplay();
            
            _replayObjectsParent.DetachChildren();
            _replaySystemState = ReplaySystemState.Stopped;
            OnChangeReplayState.Invoke(_replaySystemState);
        }
        
        public void OnAwakeReplayObject(ReplayObject replayObject)
        {
            Instance._replayObjects.Add(replayObject.gameObject.GetInstanceID(), replayObject);
        }

        public void OnDestroyReplayObject(ReplayObject replayObject)
        {
            Instance._replayObjects.Remove(replayObject.gameObject.GetInstanceID());
        }

        private void FixedUpdate()
        {
            if(_replaySystemState == ReplaySystemState.Stopped)
                return;
            
            if (IsInReplayMode())
            {
                if(_replaySystemState != ReplaySystemState.ReplayPlaying)
                    return;
                
                ProcessReplayMode();
                return;
            }

            if (!_replaySystemState.Equals(ReplaySystemState.Recording)) 
                return;
            
            ProcessRecordingMode();
        }

        private void ProcessReplayMode()
        {
            if (!TryProcessReplayTick()) 
                return;

            //Destroy game objects without replay information
            TryDestroyReplayObjects();
            
            //Finish replay when memory stream finish
            if (_replayData.MemoryStream.Position >= _replayData.MemoryStream.Length ||
                _tickNumber / _replayData.SaveReplayDataEveryTickCount >= _replayData.TicksMemoryStreamEndPosition.Count)
            {
                _replaySystemState = ReplaySystemState.ReplayFinished;
                OnChangeReplayState.Invoke(_replaySystemState);
                return;
            }

            if (_replaySystemState == ReplaySystemState.ReplayFinished)
            {
                _replaySystemState = ReplaySystemState.ReplayPaused;
                OnChangeReplayState.Invoke(_replaySystemState);
            }
        }
        
        private bool TryProcessReplayTick()
        {
            if (_tickNumber % _replayData.SaveReplayDataEveryTickCount != 0)
            {
                _tickNumber++;
                return false;
            }
            
            _replayObjectsOnTick.Clear();
            var tickIndex = _tickNumber / _replayData.SaveReplayDataEveryTickCount;
            while (_replayData.MemoryStream.Position < _replayData.TicksMemoryStreamEndPosition[tickIndex])
            {
                var saveObjInstanceId = _binaryReader.ReadInt32();
                var saveObjReplayId = _binaryReader.ReadString();
                ReplayObject replayObject = null;

                //Try localize object instance, if doesn't find create a object.
                if (!_replayObjectsOriginalInstanceIdToReplayInstanceId.TryGetValue(saveObjInstanceId,
                        out int replayObjInstanceId))
                {
                    //Scene objects will have the save obj instance id
                    if (_replayObjects.TryGetValue(saveObjInstanceId, out replayObject))
                        replayObjInstanceId = saveObjInstanceId;
                    //Spawned objects
                    else
                    {
                        if (!_replaySettings.TryGetReplayObjectPrefabById(saveObjReplayId, out var replayObjectPrefab))
                        {
                            Debug.LogError($"Can't find ReplayObjectPrefab with Id {saveObjReplayId}");
                            return false;
                        }

                        replayObject = Instantiate(replayObjectPrefab, _replayObjectsParent);
                        replayObject.SetupToReplay(saveObjInstanceId);
                        replayObjInstanceId = replayObject.gameObject.GetInstanceID();
                    }

                    _replayObjectsOriginalInstanceIdToReplayInstanceId.Add(saveObjInstanceId, replayObjInstanceId);
                }

                if (replayObject == null && !_replayObjects.TryGetValue(replayObjInstanceId, out replayObject))
                    return false;

                _replayObjectsOnTick.Add(replayObjInstanceId);
                replayObject.LoadState(_binaryReader);
            }
            
            _tickNumber++;
            return true;
        }
        
        private void TryDestroyReplayObjects()
        {
            if (_replayObjectsOnTick.Count > 0)
            {
                foreach (KeyValuePair<int, ReplayObject> replayObj in _replayObjects)
                {
                    var findIndex = _replayObjectsOnTick.FindIndex(x => x.Equals(replayObj.Key));

                    if (findIndex >= 0)
                        continue;

                    _replayObjectsOriginalInstanceIdToReplayInstanceId.Remove(replayObj.Value.OriginalSaveInstanceId);
                    _replayObjectsToDestroy.Add(replayObj.Value.gameObject);
                }

                foreach (GameObject objToDestroy in _replayObjectsToDestroy)
                    Destroy(objToDestroy);

                _replayObjectsToDestroy.Clear();
            }
        }

        private void ProcessRecordingMode()
        {
            if (_tickNumber % _replayData.SaveReplayDataEveryTickCount != 0)
            {
                _tickNumber++;
                return;
            }

            SaveReplayTick();
        }

        private void SaveReplayTick()
        {
            foreach (KeyValuePair<int, ReplayObject> replayObj in _replayObjects)
                replayObj.Value.SaveState(_binaryWriter);
            
            _replayData.TicksTime.Add(Decimal.Round((decimal)Time.time, 1));
            _replayData.TicksMemoryStreamEndPosition.Add(_replayData.MemoryStream.Position);
            _tickNumber++;
        }

        public int GetLastReplayTick()
        {
            return _replayData == null ? 0 : _replayData.TicksMemoryStreamEndPosition.Count * _replayData.SaveReplayDataEveryTickCount;
        }
        
        public decimal GetLastReplayTickTime()
        {
            return _replayData == null ? 0 : _replayData.TicksTime.Last();
        }
        
        public int GetCurrentReplayTick()
        {
            return _tickNumber;
        }
        
        public bool TryGetCurrentReplayTickTime(out decimal tickTime)
        {
            tickTime = -1;
            if (!IsInReplayMode())
                return false;
            
            tickTime = _replayData.TicksTime[_tickNumber / _replayData.SaveReplayDataEveryTickCount];
            return true;
        }
    }
    
    [Serializable]
    public class ReplayData
    {
        public int SaveReplayDataEveryTickCount = 0;
        public List<long> TicksMemoryStreamEndPosition = new List<long>();
        public List<decimal> TicksTime = new List<decimal>();
        public MemoryStream MemoryStream = new MemoryStream();
    }

    [Serializable]
    public class ReplaySystemStateUnityEvent : UnityEvent<ReplaySystemState>
    {
    }

    public enum ReplaySystemState
    {
        Stopped,
        Recording,
        ReplayPlaying,
        ReplayPaused,
        ReplayFinished
    }
}
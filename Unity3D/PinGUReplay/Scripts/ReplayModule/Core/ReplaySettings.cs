using System;
using System.Collections.Generic;
using UnityEngine;

namespace PinGUReplay.ReplayModule.Core
{
    [CreateAssetMenu(fileName = "ReplaySettings", menuName = "PinGU Replay/Settings")]
    public class ReplaySettings : ScriptableObject
    {
        [SerializeField] private int _saveReplayDataEveryTickCount = 1;
        public int saveReplayDataEveryTickCount => _saveReplayDataEveryTickCount;

        
        [Header("Save File Definitions")]
        [SerializeField] private string _saveReplayFileName = "replay-data-";
        
        public string SaveFileName => string.Concat(_saveReplayFileName, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"), ".bin");
        public string SaveFileNameWithPath => string.Concat(Application.dataPath, "/pingu-replay-data/", SaveFileName);
        
        [SerializeField] private List<ReplayObject> _replayObjectPrefabs = new List<ReplayObject>();


        public bool TryGetReplayObjectPrefabById(string replayObjectId, out ReplayObject replayObject)
        {
            replayObject = null;

            var findIndex = _replayObjectPrefabs.FindIndex(x => x.ReplayObjectId.Equals(replayObjectId));

            if (findIndex >= 0)
                replayObject = _replayObjectPrefabs[findIndex];

            return replayObject != null;
        }
    }
}
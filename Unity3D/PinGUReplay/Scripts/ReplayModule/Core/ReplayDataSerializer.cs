using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PinGUReplay.ReplayModule.Core
{
    [DisallowMultipleComponent]
    public class ReplayDataSerializer : MonoBehaviour
    {
        [SerializeField] 
        private ReplaySettings _replaySettings;

        public void SaveReplayData(ReplayData replayData)
        {
            var fileNameWithPath =  _replaySettings.SaveFileNameWithPath;
         
            var folderPath = fileNameWithPath.Substring(0, fileNameWithPath.LastIndexOf("/", StringComparison.InvariantCulture));

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            Stream stream = File.Open(fileNameWithPath, FileMode.Create);
            var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            binaryFormatter.Serialize(stream, replayData);
            stream.Close();
            Debug.Log($"Save replay at path: '{fileNameWithPath}' completed!");
        }

        public bool TryLoadReplayData(Object unityObject, out ReplayData replayData)
        {
            #if UNITY_EDITOR
                string assetPath = AssetDatabase.GetAssetPath(unityObject);
                Stream stream = File.Open(assetPath, FileMode.Open);
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                replayData = (ReplayData)binaryFormatter.Deserialize(stream);
                stream.Close();
                return true;
            #else
                replayData = null;
                return false;
            #endif
        }
    }
}
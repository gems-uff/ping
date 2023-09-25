using System;
using PinGUReplay.ProvenanceGraphViewerModule.Nodes.Pool;
using UnityEditor;
using UnityEngine;

namespace PinGUReplay.ProvenanceGraphViewerModule
{
    [CustomEditor(typeof(ProvenanceNodesPoolComponent))]
    public class ProvenanceNodesPoolComponentEditor : Editor
    {
        private SerializedProperty uid;

        private void OnEnable()
        {
            uid = serializedObject.FindProperty("_uid");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.UpdateIfRequiredOrScript();
            if (string.IsNullOrEmpty(uid.stringValue))
            {
                var id = Guid.NewGuid().ToString();
                uid.stringValue = id;
                Debug.Log("New GUID Generate for PoolComponent: " + id);
                serializedObject.SetIsDifferentCacheDirty();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("RegenerateGUID"))
            {
                uid.stringValue = null;
                serializedObject.SetIsDifferentCacheDirty();
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
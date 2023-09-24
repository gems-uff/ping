using PinGUReplay.ProvenanceGraphViewerModule.Nodes;
using PinGUReplay.ReplayModule.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PinGUReplay.ProvenanceGraphViewerModule
{
    public class ProvenanceGraphViewerEditorWindow : EditorWindow
    {
        [SerializeField] private ProvenanceGraphVisualizationSettings _visualizationSettings;
        [SerializeField] private ProvenanceGraphFilterSettings _filterSettings;
        [SerializeField] private bool _editVisualizationSettings;
        [SerializeField] private bool _editFilterSettings;
        [SerializeField] private bool _showGraphValuesHelper;

        private ReplayController _replayController;
        private ProvenanceGraphViewerController _provenanceGraphViewController;
        private SerializedObject _visualizationSettingsSerializedObject;
        private SerializedObject _filterSettingsSerializedObject;

        private Vector2 _scrollPosition = Vector2.zero;
        private bool _needClickOnForceUpdateGraphButton;
        private GameObject _selectedObject;
        private ReplayProvenanceVertexNode _selectedObjectVertexNode;
        private ReplayProvenanceEdgeNode _selectedObjectEdgeNode;

        [MenuItem("Window/Provenance Graph Viewer Inspector")]
        public static void ShowWindow()
        {
            var window = CreateInstance<ProvenanceGraphViewerEditorWindow>();
            window.titleContent = new GUIContent("Provenance Graph Viewer Inspector");
            window.Show();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void Update()
        {
            if (_replayController == null)
                return;

            if (_replayController.ReplaySystemState == ReplaySystemState.ReplayPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            if (_replayController == null)
                _replayController = FindObjectOfType<ReplayController>();

            if (_provenanceGraphViewController == null)
                _provenanceGraphViewController = FindObjectOfType<ProvenanceGraphViewerController>();

            if (_provenanceGraphViewController == null || _replayController == null)
            {
                GUILayout.Label("Can't find objects with ReplayController or ProvenanceGraphViewerController in Scene");
                GUILayout.EndVertical();
                return;
            }

            if (_visualizationSettings != _provenanceGraphViewController.ProvenanceGraphVisualizationSettings ||
                _visualizationSettingsSerializedObject == null)
            {
                _visualizationSettings = _provenanceGraphViewController.ProvenanceGraphVisualizationSettings;
                _visualizationSettingsSerializedObject = new SerializedObject(_visualizationSettings);
            }

            if (_filterSettings != _provenanceGraphViewController.ProvenanceGraphFilterSettings ||
                _filterSettingsSerializedObject == null)
            {
                _filterSettings = _provenanceGraphViewController.ProvenanceGraphFilterSettings;
                _filterSettingsSerializedObject = new SerializedObject(_filterSettings);
            }

            var oldColor = GUI.backgroundColor;
            GUILayout.Label("Use this button every time you changed filter or visualization settings values!");

            if (!Application.isPlaying)
                _needClickOnForceUpdateGraphButton = false;

            if (_needClickOnForceUpdateGraphButton)
                GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Force Update Graph"))
            {
                _provenanceGraphViewController.ForceUpdateGraphVisualization();
                _needClickOnForceUpdateGraphButton = false;
            }

            GUI.backgroundColor = oldColor;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false);

            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Visualization and Filter Settings", EditorStyles.boldLabel);

            _visualizationSettings = (ProvenanceGraphVisualizationSettings)EditorGUILayout.ObjectField(
                "Visualization Settings", _visualizationSettings, typeof(ProvenanceGraphVisualizationSettings), false);
            _filterSettings = (ProvenanceGraphFilterSettings)EditorGUILayout.ObjectField("Filter Settings",
                _filterSettings, typeof(ProvenanceGraphFilterSettings), false);

            if (EditorGUI.EndChangeCheck())
            {
                _provenanceGraphViewController.SetProvenanceGraphVisualizationSettings(_visualizationSettings);
                _provenanceGraphViewController.SetProvenanceGraphFilterSettings(_filterSettings);
				
				_filterSettings = _provenanceGraphViewController.ProvenanceGraphFilterSettings;
				_filterSettingsSerializedObject = new SerializedObject(_filterSettings);
				_visualizationSettings = _provenanceGraphViewController.ProvenanceGraphVisualizationSettings;
				_visualizationSettingsSerializedObject = new SerializedObject(_visualizationSettings);

                if (Application.isPlaying)
                    _needClickOnForceUpdateGraphButton = true;
                else
                {
                    EditorUtility.SetDirty(_provenanceGraphViewController.gameObject);
                    EditorUtility.SetDirty(_provenanceGraphViewController);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            _editVisualizationSettings =
                EditorGUILayout.BeginToggleGroup("Edit Visualization Settings", _editVisualizationSettings);
            if (_editVisualizationSettings)
            {
                EditorGUI.BeginChangeCheck();
                _visualizationSettingsSerializedObject.Update();
                SerializedProperty iterator = _visualizationSettingsSerializedObject.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    EditorGUILayout.PropertyField(iterator, true);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _visualizationSettingsSerializedObject.ApplyModifiedProperties();
                    if (Application.isPlaying)
                        _needClickOnForceUpdateGraphButton = true;
                }
            }

            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            _editFilterSettings = EditorGUILayout.BeginToggleGroup("Edit Filter Settings", _editFilterSettings);
            if (_editFilterSettings)
            {
                EditorGUI.BeginChangeCheck();
                _filterSettingsSerializedObject.Update();
                SerializedProperty iterator = _filterSettingsSerializedObject.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    EditorGUILayout.PropertyField(iterator, true);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _filterSettingsSerializedObject.ApplyModifiedProperties();
                    if (Application.isPlaying)
                        _needClickOnForceUpdateGraphButton = true;
                }
            }

            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            _showGraphValuesHelper =
                EditorGUILayout.BeginToggleGroup("Show Graph Values Helper", _showGraphValuesHelper);
            if (_showGraphValuesHelper)
            {
                EditorGUI.BeginDisabledGroup(true);
                var provenanceGraphViewerControllerSerializeObject =
                    new SerializedObject(_provenanceGraphViewController);
                var graphValuesHelperProperty =
                    provenanceGraphViewerControllerSerializeObject.FindProperty("_currentGraphValuesHelper");
                EditorGUILayout.PropertyField(graphValuesHelperProperty);
                EditorGUI.EndDisabledGroup();

            }

            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GameObject selectedObject = Selection.activeGameObject;

            if ((_selectedObject == null && selectedObject != null) ||
                (_selectedObject != null && !_selectedObject.Equals(selectedObject)))
            {
                _selectedObject = selectedObject;
                _selectedObjectVertexNode = selectedObject?.GetComponentInParent<ReplayProvenanceVertexNode>();
                _selectedObjectEdgeNode = selectedObject?.GetComponentInParent<ReplayProvenanceEdgeNode>();
            }

            if (_selectedObjectVertexNode != null)
            {
                GUILayout.Label("Selected Vertex Node Information", EditorStyles.boldLabel);
                Editor vertexNodeEditor = Editor.CreateEditor(_selectedObjectVertexNode);
                vertexNodeEditor.DrawDefaultInspector();
                if (GUILayout.Button("Update Replay To Vertex Node Time"))
                    _selectedObjectVertexNode.UpdateReplayToThisTime();
            }

            if (_selectedObjectEdgeNode != null)
            {
                GUILayout.Label("Selected Edge Node Information", EditorStyles.boldLabel);
                Editor edgeNodeEditor = Editor.CreateEditor(_selectedObjectEdgeNode);
                edgeNodeEditor.DrawDefaultInspector();
                if (GUILayout.Button("Update Replay To Edge Node Time"))
                    _selectedObjectEdgeNode.UpdateReplayToThisTime();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}

using System;
using System.Collections.Generic;
using PinGUReplay.ProvenanceGraphViewerModule.Nodes.Pool;
using PinGUReplay.ReplayModule.Core;
using PinGUReplay.Util;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PinGUReplay.ProvenanceGraphViewerModule.Nodes
{
    public class ReplayProvenanceVertexNode : MonoBehaviour, IProvenanceNodePoolCleanUp
    {
        [SerializeField] [ReadOnly] private ProvenanceGraphViewerController.VertexInfo _vertexInfo;
        public ProvenanceGraphViewerController.VertexInfo VertexInfo => _vertexInfo;
        [SerializeField] private ProvenanceNodesPoolComponent _provenanceNodesPoolComponent;
        public ProvenanceNodesPoolComponent ProvenanceNodesPoolComponent => _provenanceNodesPoolComponent;

        [SerializeField] [ReadOnly]
        private List<ReplayProvenanceEdgeNode> _linkedSourceEdges = new List<ReplayProvenanceEdgeNode>();

        [SerializeField] [ReadOnly]
        private List<ReplayProvenanceEdgeNode> _linkedTargetEdges = new List<ReplayProvenanceEdgeNode>();

        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _vertexInfoTitle;
        [SerializeField] private TextMeshProUGUI _vertexTime;
        [SerializeField] private TextMeshProUGUI _vertexType;
        [SerializeField] private TextMeshProUGUI _vertexLabel;
        [SerializeField] private TextMeshProUGUI _vertexObjectName;
        [SerializeField] private TextMeshProUGUI _vertexObjectTag;
        [SerializeField] private Transform _transformToChangeSize;
        [SerializeField] private CanvasGroup _canvasGroup;

        private ProvenanceGraphVisualizationSettings _provenanceGraphVisualizationSettings;
        private Vector3 _originalLocalScale;

        private void Awake()
        {
            _originalLocalScale = transform.localScale;
        }

        public void SetupVertexInfo(ProvenanceGraphVisualizationSettings visualizationSettings,
            ProvenanceGraphViewerController.VertexInfo vertexInfo)
        {
            _provenanceGraphVisualizationSettings = visualizationSettings;
            _vertexInfo = vertexInfo;
            _vertexInfoTitle.SetText($"Vertex Info ({_vertexInfo.Vertex.ID})");
            _vertexTime.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings.ShowVertexTime);
            _vertexTime.SetText(_vertexInfo.VertexTime.ToString("#.#"));
            _vertexType.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings.ShowVertexType);
            _vertexType.SetText(vertexInfo.Vertex.type);
            _vertexLabel.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings.ShowVertexLabel);
            _vertexLabel.SetText(vertexInfo.Vertex.label);
            _vertexObjectName.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings
                .ShowVertexObjectName);
            _vertexObjectName.SetText(vertexInfo.NodeAttribututes["ObjectName"]);
            _vertexObjectTag.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings
                .ShowVertexObjectTag);
            _vertexObjectTag.SetText(vertexInfo.NodeAttribututes["ObjectTag"]);

            TryUpdateVertexColor();
            ShowVertexShape();

            _canvasGroup.alpha = _provenanceGraphVisualizationSettings.RenderInfoAlpha;
        }

        private void ShowVertexShape()
        {
            var vertexType = _vertexInfo.Vertex.type;

            switch ((ProvenanceGraphViewerController.VertexType)Enum.Parse(
                        typeof(ProvenanceGraphViewerController.VertexType), vertexType))
            {
                case ProvenanceGraphViewerController.VertexType.Activity:
                    _background.sprite = _provenanceGraphVisualizationSettings.ActivityBackground;
                    break;
                case ProvenanceGraphViewerController.VertexType.Agent:
                    _background.sprite = _provenanceGraphVisualizationSettings.AgentBackground;
                    break;
                case ProvenanceGraphViewerController.VertexType.Entity:
                    _background.sprite = _provenanceGraphVisualizationSettings.EntityBackground;
                    break;
            }
        }

        private void TryUpdateVertexColor()
        {
            var findIndex =
                _provenanceGraphVisualizationSettings.VertexObjectNameColors.FindIndex(x =>
                    x.ObjectName.Equals(_vertexObjectName.text));

            if (findIndex >= 0)
            {
                _background.color = _provenanceGraphVisualizationSettings.VertexObjectNameColors[findIndex].Color;
                return;
            }

            findIndex = _provenanceGraphVisualizationSettings.VertexObjectTagColors.FindIndex(x =>
                x.ObjectTag.Equals(_vertexObjectTag.text));

            if (findIndex >= 0)
            {
                _background.color = _provenanceGraphVisualizationSettings.VertexObjectTagColors[findIndex].Color;
                return;
            }

            findIndex = _provenanceGraphVisualizationSettings.VertexLabelColors.FindIndex(x =>
                x.VertexLabel.Equals(_vertexLabel.text));

            if (findIndex >= 0)
            {
                _background.color = _provenanceGraphVisualizationSettings.VertexLabelColors[findIndex].Color;
                return;
            }

            var vertexTypeStr = _vertexInfo.Vertex.type;
            var vertexType =
                (ProvenanceGraphViewerController.VertexType)Enum.Parse(
                    typeof(ProvenanceGraphViewerController.VertexType), vertexTypeStr);

            findIndex = _provenanceGraphVisualizationSettings.VertexTypeColors.FindIndex(x =>
                x.VertexType.Equals(vertexType));

            if (findIndex >= 0)
            {
                _background.color = _provenanceGraphVisualizationSettings.VertexTypeColors[findIndex].Color;
                return;
            }

            _background.color = _provenanceGraphVisualizationSettings.VertexNodeColor;
        }

        public void UpdateNodeScale(decimal pastTime, decimal curTime, decimal futureTime)
        {
            float lerpTime = -1;

            if (_vertexInfo.VertexTime < curTime)
                lerpTime = Mathf.InverseLerp((float)pastTime, (float)curTime, (float)_vertexInfo.VertexTime);
            else
                lerpTime = Mathf.InverseLerp((float)futureTime, (float)curTime, (float)_vertexInfo.VertexTime);

            var newSize = _provenanceGraphVisualizationSettings.NodeScaleOverTimeAnimationCurve.Evaluate(lerpTime);
            _transformToChangeSize.localScale = new Vector3(_originalLocalScale.x * newSize,
                _originalLocalScale.y * newSize, _transformToChangeSize.localScale.z);
        }

        [InspectorButton]
        public void UpdateReplayToThisTime()
        {
            if (!gameObject.activeInHierarchy)
                return;
            ReplayController.Instance.PutReplayAtTime(_vertexInfo.VertexTime);
        }

        public void ClearLinkedEdges()
        {
            _linkedSourceEdges.Clear();
            _linkedTargetEdges.Clear();
        }

        public void AddLinkedSourceEdge(ReplayProvenanceEdgeNode edgeNode)
        {
            _linkedSourceEdges.Add(edgeNode);
        }

        public void AddLinkedTargetEdge(ReplayProvenanceEdgeNode edgeNode)
        {
            _linkedTargetEdges.Add(edgeNode);
        }


        public void CleanUp(GameObject reference)
        {
            _vertexInfo = default;
            _transformToChangeSize.localScale = _originalLocalScale;
            _background.color = Color.white;
            _background.sprite = null;
            ClearLinkedEdges();
        }
    }
}
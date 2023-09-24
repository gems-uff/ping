using System;
using PinGUReplay.ProvenanceGraphViewerModule.Nodes.Pool;
using PinGUReplay.ReplayModule.Core;
using PinGUReplay.Util;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PinGUReplay.ProvenanceGraphViewerModule.Nodes
{
    public class ReplayProvenanceEdgeNode : MonoBehaviour, IProvenanceNodePoolCleanUp
    {
        [SerializeField] [ReadOnly] private ProvenanceGraphViewerController.EdgeInfo _edgeInfo;
        public ProvenanceGraphViewerController.EdgeInfo EdgeInfo => _edgeInfo;
        [SerializeField] [ReadOnly] private ReplayProvenanceVertexNode _linkedNode;
        public ReplayProvenanceVertexNode LinkedNode => _linkedNode;
        [SerializeField] private ProvenanceNodesPoolComponent _provenanceNodesPoolComponent;
        public ProvenanceNodesPoolComponent ProvenanceNodesPoolComponent => _provenanceNodesPoolComponent;

        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _edgeInfoTitle;
        [SerializeField] private TextMeshProUGUI _edgeType;
        [SerializeField] private TextMeshProUGUI _edgeLabel;
        [SerializeField] private TextMeshProUGUI _vertexInfoTitle;
        [SerializeField] private TextMeshProUGUI _vertexRelation;
        [SerializeField] private TextMeshProUGUI _vertexTime;
        [SerializeField] private TextMeshProUGUI _vertexType;
        [SerializeField] private TextMeshProUGUI _vertexLabel;
        [SerializeField] private TextMeshProUGUI _vertexObjectName;
        [SerializeField] private TextMeshProUGUI _vertexObjectTag;
        [SerializeField] private Transform _transformToChangeSize;
        [SerializeField] private CanvasGroup _canvasGroup;

        private ProvenanceGraphVisualizationSettings _provenanceGraphVisualizationSettings;
        private Vector3 _originalLocalScale;
        private bool _showSourceNameAndTag;

        private void Awake()
        {
            _originalLocalScale = _transformToChangeSize.localScale;
        }

        public void SetupEdgeInfo(ProvenanceGraphVisualizationSettings visualizationSettings,
            ProvenanceGraphViewerController.EdgeInfo edgeInfo, ReplayProvenanceVertexNode linkedNode,
            bool showSourceNameAndTag)
        {
            _provenanceGraphVisualizationSettings = visualizationSettings;
            _edgeInfo = edgeInfo;
            _linkedNode = linkedNode;
            _showSourceNameAndTag = showSourceNameAndTag;
            _canvasGroup.gameObject.SetActive(true);
            ShowEdgeLine();
            ShowEdgesInfo();
            ShowVertexInfo();
        }

        public void HideEdgeCanvas()
        {
            _canvasGroup.gameObject.SetActive(false);
        }

        private void ShowEdgeLine()
        {
            bool canGetEdgeValue = float.TryParse(_edgeInfo.Value, out var edgeValue);
            float colorAlpha = Mathf.Lerp(0, _provenanceGraphVisualizationSettings.NeutralRelationEdgeLineColor.a,
                _provenanceGraphVisualizationSettings.RenderInfoAlpha);
            Color color = _provenanceGraphVisualizationSettings.NeutralRelationEdgeLineColor;
            color.a = colorAlpha;

            if (canGetEdgeValue)
            {
                if (edgeValue > 0)
                {
                    color = _provenanceGraphVisualizationSettings.PositiveRelationEdgeLineColor;
                    color.a = colorAlpha;
                }
                else if (edgeValue < 0)
                {
                    color = _provenanceGraphVisualizationSettings.NegativeRelationEdgeLineColor;
                    color.a = colorAlpha;
                }
            }

            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;
            _lineRenderer.SetPosition(0, _showSourceNameAndTag ? transform.position : _linkedNode.transform.position);
            _lineRenderer.SetPosition(1, _showSourceNameAndTag ? _linkedNode.transform.position : transform.position);
            _lineRenderer.startWidth = _provenanceGraphVisualizationSettings.EdgeLineWidth;
            _lineRenderer.endWidth = _provenanceGraphVisualizationSettings.EdgeLineWidth / 10;

        }

        private void ShowEdgesInfo()
        {
            _edgeInfoTitle.SetText($"Edge Info ({_edgeInfo.Id})");

            _edgeLabel.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings.ShowEdgeLabel);
            _edgeLabel.SetText(_edgeInfo.Label);
            _edgeType.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings.ShowEdgeType);
            _edgeType.SetText(_edgeInfo.Type.ToString());
        }

        private void ShowVertexInfo()
        {
            _vertexRelation.SetText(_showSourceNameAndTag ? "Source" : "Target");

            _vertexInfoTitle.SetText(
                $"Vertex Info ({(_showSourceNameAndTag ? _edgeInfo.SourceVertexInfo.Vertex.ID : _edgeInfo.TargetVertexInfo.Vertex.ID)})");

            _vertexTime.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings.ShowVertexTime);
            _vertexTime.SetText(_showSourceNameAndTag
                ? _edgeInfo.SourceVertexInfo.VertexTime.ToString("#.#")
                : _edgeInfo.TargetVertexInfo.VertexTime.ToString("#.#"));

            _vertexType.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings.ShowVertexType);
            _vertexType.SetText(_showSourceNameAndTag
                ? _edgeInfo.SourceVertexInfo.NodeAttribututes["type"]
                : _edgeInfo.TargetVertexInfo.NodeAttribututes["type"]);

            _vertexLabel.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings.ShowVertexLabel);
            _vertexLabel.SetText(_showSourceNameAndTag
                ? _edgeInfo.SourceVertexInfo.NodeAttribututes["label"]
                : _edgeInfo.TargetVertexInfo.NodeAttribututes["label"]);

            _vertexObjectName.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings
                .ShowVertexObjectName);
            _vertexObjectName.SetText(_showSourceNameAndTag
                ? _edgeInfo.SourceVertexInfo.NodeAttribututes["ObjectName"]
                : _edgeInfo.TargetVertexInfo.NodeAttribututes["ObjectName"]);

            _vertexObjectTag.transform.parent.gameObject.SetActive(_provenanceGraphVisualizationSettings
                .ShowVertexObjectTag);
            _vertexObjectTag.SetText(_showSourceNameAndTag
                ? _edgeInfo.SourceVertexInfo.NodeAttribututes["ObjectTag"]
                : _edgeInfo.TargetVertexInfo.NodeAttribututes["ObjectTag"]);

            TryUpdateVertexColor();
            ShowVertexShape();

            _canvasGroup.alpha = _provenanceGraphVisualizationSettings.RenderInfoAlpha;
        }

        private void ShowVertexShape()
        {
            var vertexType = _showSourceNameAndTag
                ? _edgeInfo.SourceVertexInfo.Vertex.type
                : _edgeInfo.TargetVertexInfo.Vertex.type;

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

            var vertexTypeStr = _showSourceNameAndTag
                ? _edgeInfo.SourceVertexInfo.Vertex.type
                : _edgeInfo.TargetVertexInfo.Vertex.type;
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

            decimal vertexTime = _showSourceNameAndTag
                ? _edgeInfo.SourceVertexInfo.VertexTime
                : _edgeInfo.TargetVertexInfo.VertexTime;

            if (vertexTime < curTime)
                lerpTime = Mathf.InverseLerp((float)pastTime, (float)curTime, (float)vertexTime);
            else
                lerpTime = Mathf.InverseLerp((float)futureTime, (float)curTime, (float)vertexTime);

            var newSize = _provenanceGraphVisualizationSettings.NodeScaleOverTimeAnimationCurve.Evaluate(lerpTime);
            _transformToChangeSize.localScale = new Vector3(_originalLocalScale.x * newSize,
                _originalLocalScale.y * newSize, _transformToChangeSize.localScale.z);
        }

        [InspectorButton]
        public void UpdateReplayToThisTime()
        {
            if (!gameObject.activeInHierarchy)
                return;
            ReplayController.Instance.PutReplayAtTime(_showSourceNameAndTag
                ? _edgeInfo.SourceVertexInfo.VertexTime
                : _edgeInfo.TargetVertexInfo.VertexTime);
        }

        public void CleanUp(GameObject reference)
        {
            _edgeInfo = default;
            _linkedNode = null;
            _showSourceNameAndTag = false;
            _transformToChangeSize.localScale = _originalLocalScale;
            _background.color = Color.white;
            _background.sprite = null;
        }
    }
}
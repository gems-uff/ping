using System;
using System.Collections.Generic;
using PinGUReplay.ProvenanceGraphViewerModule.Nodes;
using UnityEngine;

namespace PinGUReplay.ProvenanceGraphViewerModule
{
    [CreateAssetMenu(fileName = "ProvenanceGraphVisualizationSettings", menuName = "PinGU Replay/ProvenanceGraphVisualizationSettings")]
    public class ProvenanceGraphVisualizationSettings : ScriptableObject
    {
        [Header("Render Information Alpha")] [SerializeField] [Range(0, 1)]
        private float renderInfoAlpha = 1;

        public float RenderInfoAlpha => renderInfoAlpha;

        [Header("Vertex Node")] [SerializeField]
        private ReplayProvenanceVertexNode _vertexNodePrefab;

        public ReplayProvenanceVertexNode VertexNodePrefab => _vertexNodePrefab;

        [SerializeField] private Color _vertexNodeColor = Color.blue;
        public Color VertexNodeColor => _vertexNodeColor;

        [SerializeField] private Sprite _activityBackground;
        public Sprite ActivityBackground => _activityBackground;

        [SerializeField] private Sprite _agentBackground;
        public Sprite AgentBackground => _agentBackground;

        [SerializeField] private Sprite _entityBackground;
        public Sprite EntityBackground => _entityBackground;



        [Header("Vertex Specific Color")] [SerializeField]
        private List<VertexObjectNameColorConfig> _vertexObjectNameColors;

        public List<VertexObjectNameColorConfig> VertexObjectNameColors => _vertexObjectNameColors;

        [SerializeField] private List<VertexObjectTagColorConfig> _vertexObjectTagColors;
        public List<VertexObjectTagColorConfig> VertexObjectTagColors => _vertexObjectTagColors;

        [SerializeField] private List<VertexLabelColorConfig> _vertexLabelColors;
        public List<VertexLabelColorConfig> VertexLabelColors => _vertexLabelColors;

        [SerializeField] private List<VertexTypeColorConfig> _vertexTypeColors;
        public List<VertexTypeColorConfig> VertexTypeColors => _vertexTypeColors;

        [Header("Vertex Node Display Information")] [SerializeField]
        private bool _showVertexTime = true;

        public bool ShowVertexTime => _showVertexTime;
        [SerializeField] private bool _showVertexType = true;
        public bool ShowVertexType => _showVertexType;
        [SerializeField] private bool _showVertexLabel = true;
        public bool ShowVertexLabel => _showVertexLabel;
        [SerializeField] private bool _showVertexObjectName = true;
        public bool ShowVertexObjectName => _showVertexObjectName;
        [SerializeField] private bool _showVertexObjectTag = true;
        public bool ShowVertexObjectTag => _showVertexObjectTag;

        [Header("Edges Node")] [SerializeField]
        private ReplayProvenanceEdgeNode _edgeNodePrefab;

        public ReplayProvenanceEdgeNode EdgeNodePrefab => _edgeNodePrefab;

        [SerializeField] private Color _positiveRelationEdgeLineColor = Color.green;
        public Color PositiveRelationEdgeLineColor => _positiveRelationEdgeLineColor;

        [SerializeField] private Color _neutralRelationEdgeLineColor = Color.gray;
        public Color NeutralRelationEdgeLineColor => _neutralRelationEdgeLineColor;

        [SerializeField] private Color _negativeRelationEdgeLineColor = Color.red;
        public Color NegativeRelationEdgeLineColor => _negativeRelationEdgeLineColor;

        [SerializeField] private float _edgeLineWidth = 3;
        public float EdgeLineWidth => _edgeLineWidth;

        [Header("Edge Node Display Information")] [SerializeField]
        private bool _showEdgeType = true;

        public bool ShowEdgeType => _showEdgeType;
        [SerializeField] private bool _showEdgeLabel = true;
        public bool ShowEdgeLabel => _showEdgeLabel;

        [Header("Node Size Configuration")] [SerializeField]
        private AnimationCurve _nodeScaleOverTimeAnimationCurve;

        public AnimationCurve NodeScaleOverTimeAnimationCurve => _nodeScaleOverTimeAnimationCurve;


        [Serializable]
        public struct VertexObjectNameColorConfig
        {
            public string ObjectName;
            public Color Color;
        }

        [Serializable]
        public struct VertexObjectTagColorConfig
        {
            public string ObjectTag;
            public Color Color;
        }

        [Serializable]
        public struct VertexLabelColorConfig
        {
            public string VertexLabel;
            public Color Color;
        }

        [Serializable]
        public struct VertexTypeColorConfig
        {
            public ProvenanceGraphViewerController.VertexType VertexType;
            public Color Color;
        }
    }
}
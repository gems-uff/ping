using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using PinGU;
using PinGUReplay.ProvenanceGraphViewerModule.Nodes;
using PinGUReplay.ProvenanceGraphViewerModule.Nodes.Pool;
using PinGUReplay.ReplayModule.Core;
using PinGUReplay.Util;
using RotaryHeart.Lib.SerializableDictionary;
using UnityEngine;

namespace PinGUReplay.ProvenanceGraphViewerModule
{
    public class ProvenanceGraphViewerController : MonoBehaviour
    {
        [Header("Provenance Graphic")] [SerializeField]
        private TextAsset _inputGraph;

        [Header("Provenance Graph Visualization Settings")] [SerializeField]
        private ProvenanceGraphVisualizationSettings _provenanceGraphVisualizationSettings;

        public ProvenanceGraphVisualizationSettings ProvenanceGraphVisualizationSettings =>
            _provenanceGraphVisualizationSettings;

        [Header("Provenance Graph Filters Settings")] [SerializeField]
        private ProvenanceGraphFilterSettings _provenanceGraphFilterSettings;

        public ProvenanceGraphFilterSettings ProvenanceGraphFilterSettings => _provenanceGraphFilterSettings;

        [Header("Provenance Graph Values Helper")] [SerializeField] [Unity.Collections.ReadOnly]
        private GraphValuesHelper _currentGraphValuesHelper;

        private ProvenanceContainer _graph;
        private decimal _minVertexTime;
        private decimal _maxVertexTime;

        private decimal _graphDuration;

        //dictionary keys are the time of vertex
        private Dictionary<decimal, List<VertexInfo>> _entityVertexDict = new Dictionary<decimal, List<VertexInfo>>();
        private Dictionary<decimal, List<VertexInfo>> _agentVertexDict = new Dictionary<decimal, List<VertexInfo>>();
        private Dictionary<decimal, List<VertexInfo>> _activityVertexDict = new Dictionary<decimal, List<VertexInfo>>();
        private decimal _lastTimeRender;
        private decimal _lastPastTimeRender;

        private decimal _lastFutureTimeRender;

        //dictionary keys are the id of vertex
        private Dictionary<string, EdgeVertexInfo> _edgesDict = new Dictionary<string, EdgeVertexInfo>();

        private List<ReplayProvenanceVertexNode> _vertexNodesInTick = new List<ReplayProvenanceVertexNode>();
        private List<ReplayProvenanceEdgeNode> _edgeNodesInTick = new List<ReplayProvenanceEdgeNode>();
        private List<int> _indexesOfNodesToDestroy = new List<int>();

        //Session Hierarchy
        private Transform _provenanceNodesParent;

        private bool _ignoreAutoSyncConfiguration;

        public void SetProvenanceGraphFilterSettings(ProvenanceGraphFilterSettings provenanceGraphFilterSettings)
        {
            _provenanceGraphFilterSettings = provenanceGraphFilterSettings;
        }

        public void SetProvenanceGraphVisualizationSettings(
            ProvenanceGraphVisualizationSettings provenanceGraphVisualizationSettings)
        {
            _provenanceGraphVisualizationSettings = provenanceGraphVisualizationSettings;
        }

        [InspectorButton]
        public void ForceUpdateGraphVisualization()
        {
            _lastTimeRender = -1;
            DestroyAllNodes();
            _ignoreAutoSyncConfiguration = true;
            LateUpdate();
            _ignoreAutoSyncConfiguration = false;
        }

        void Start()
        {
            ReplayController.Instance.OnChangeReplayState.AddListener(OnChangeReplayState);
            _lastTimeRender = -1;
            XmlSerializer ser = new XmlSerializer(typeof(ProvenanceContainer));
            StringReader sr = new StringReader(_inputGraph.text);
            _graph = (ProvenanceContainer)ser.Deserialize(sr);
            var firstVertex = _graph.vertexList.First();
            var lastVertex = _graph.vertexList.Last();
            _minVertexTime = Decimal.Round(Decimal.Parse(firstVertex.date, CultureInfo.InvariantCulture), 1);
            _maxVertexTime = Decimal.Round(Decimal.Parse(lastVertex.date, CultureInfo.InvariantCulture), 1);
            _graphDuration = _maxVertexTime - _minVertexTime;
            _currentGraphValuesHelper.GraphDuration = (float)_graphDuration;
            CreateVertexDictionaries(_graph);
            CreateEdgesDictionaries(_graph);
            OrderGraphValuesHelper();
        }

        private void OnDestroy()
        {
            if (ReplayController.Initialized)
                ReplayController.Instance.OnChangeReplayState.RemoveListener(OnChangeReplayState);

            DestroySessionHierarchy();
        }

        private void OnChangeReplayState(ReplaySystemState arg0)
        {
            if (ReplayController.Instance.IsInReplayMode())
            {
                CreateSessionHierarchy();
                return;
            }

            _lastTimeRender = -1;
            DestroyAllNodes();
            DestroySessionHierarchy();
        }

        private void CreateSessionHierarchy()
        {
            if (_provenanceNodesParent != null)
                return;
            _provenanceNodesParent = new GameObject("Provenance Nodes").transform;
            _provenanceNodesParent.SetParent(ReplayController.Instance.ReplaySessionParent);
        }

        private void DestroySessionHierarchy()
        {
            if (_provenanceNodesParent == null)
                return;

            Destroy(_provenanceNodesParent.gameObject);
            _provenanceNodesParent = null;
        }

        private void CreateVertexDictionaries(ProvenanceContainer graph)
        {
            decimal curVertexTime = -1;

            for (int i = 0; i < graph.vertexList.Count; i++)
            {
                var vertex = graph.vertexList[i];
                var vertexTime = Decimal.Round(Decimal.Parse(vertex.date, CultureInfo.InvariantCulture), 1);
                if (vertexTime != curVertexTime)
                    curVertexTime = vertexTime;

                switch ((VertexType)Enum.Parse(typeof(VertexType), vertex.type))
                {
                    case VertexType.Activity:
                        AddVertexInfoOnDict(_activityVertexDict, curVertexTime, vertex);
                        break;
                    case VertexType.Agent:
                        AddVertexInfoOnDict(_agentVertexDict, curVertexTime, vertex);
                        break;
                    case VertexType.Entity:
                        AddVertexInfoOnDict(_entityVertexDict, curVertexTime, vertex);
                        break;
                }
            }

            Debug.Log("Created vertex dictionaries!");
        }

        private void AddVertexInfoOnDict(Dictionary<decimal, List<VertexInfo>> dict, decimal curVertexTime,
            Vertex vertex)
        {
            if (!dict.TryGetValue(curVertexTime, out var vertexList))
            {
                vertexList = new List<VertexInfo>();
                dict.Add(curVertexTime, vertexList);
            }

            VertexInfo vertexInfo = new VertexInfo()
            {
                Vertex = vertex,
                VertexTime = Decimal.Round(Decimal.Parse(vertex.date, CultureInfo.InvariantCulture), 1),
                NodeAttribututes = CreateNodeAttributeDict(vertex)
            };

            if (!_currentGraphValuesHelper.VertexLabels.Contains(vertexInfo.Vertex.label))
                _currentGraphValuesHelper.VertexLabels.Add(vertexInfo.Vertex.label);

            if (!_currentGraphValuesHelper.VertexTags.Contains(vertexInfo.NodeAttribututes["ObjectTag"]))
                _currentGraphValuesHelper.VertexTags.Add(vertexInfo.NodeAttribututes["ObjectTag"]);

            if (!_currentGraphValuesHelper.VertexObjectsName.Contains(vertexInfo.NodeAttribututes["ObjectName"]))
                _currentGraphValuesHelper.VertexObjectsName.Add(vertexInfo.NodeAttribututes["ObjectName"]);

            vertexList.Add(vertexInfo);
        }

        //provenance graph use time since game start as vertex time, this function converts it to a
        private decimal ConvertVertexTime(Vertex vertex)
        {
            var vertexTime = Decimal.Round(Decimal.Parse(vertex.date, CultureInfo.InvariantCulture), 1);
            var convertedVertexTime = Mathf.Lerp(0, (float)_graphDuration,
                (float)((vertexTime - _minVertexTime) / _graphDuration));
            return Decimal.Round((decimal)convertedVertexTime, 1);
        }

        private void CreateEdgesDictionaries(ProvenanceContainer graph)
        {
            long curVertexTime = -1;

            for (int i = 0; i < graph.edgeList.Count; i++)
            {
                var edge = graph.edgeList[i];

                var sourceVertexIndex = graph.vertexList.FindIndex(x => x.ID.Equals(edge.sourceID));
                var targetVertexIndex = graph.vertexList.FindIndex(x => x.ID.Equals(edge.targetID));

                if (sourceVertexIndex == -1 || targetVertexIndex == -1)
                    continue;

                var sourceVertex = graph.vertexList[sourceVertexIndex];
                var targetVertex = graph.vertexList[targetVertexIndex];
                var sourceVertexEdgeInfo = CreateOrGetEdgeVertexInfo(sourceVertex);
                var targetVertexEdgeInfo = CreateOrGetEdgeVertexInfo(targetVertex);

                EdgeInfo edgeInfo = new EdgeInfo()
                {
                    Id = edge.ID,
                    Label = edge.label,
                    Type = (EdgeType)Enum.Parse(typeof(EdgeType), edge.type),
                    Value = edge.value,
                    SourceVertexInfo = new VertexInfo()
                    {
                        Vertex = sourceVertex,
                        VertexTime = Decimal.Round(Decimal.Parse(sourceVertex.date, CultureInfo.InvariantCulture), 1),
                        NodeAttribututes = CreateNodeAttributeDict(sourceVertex)
                    },
                    TargetVertexInfo = new VertexInfo()
                    {
                        Vertex = targetVertex,
                        VertexTime = Decimal.Round(Decimal.Parse(targetVertex.date, CultureInfo.InvariantCulture), 1),
                        NodeAttribututes = CreateNodeAttributeDict(targetVertex)
                    }
                };

                if (!_currentGraphValuesHelper.EdgesLabels.Contains(edgeInfo.Label))
                    _currentGraphValuesHelper.EdgesLabels.Add(edgeInfo.Label);

                if (!_currentGraphValuesHelper.EdgesTypes.Contains(edge.type))
                    _currentGraphValuesHelper.EdgesTypes.Add(edge.type);

                sourceVertexEdgeInfo.SourceEdges.Add(edgeInfo);
                targetVertexEdgeInfo.TargetEdges.Add(edgeInfo);
            }

            Debug.Log("Created edges dictionaries!");
        }

        private EdgeVertexInfo CreateOrGetEdgeVertexInfo(Vertex vertex)
        {
            if (!_edgesDict.TryGetValue(vertex.ID, out var edgeVertexInfo))
            {
                edgeVertexInfo = new EdgeVertexInfo()
                {
                    SourceEdges = new List<EdgeInfo>(),
                    TargetEdges = new List<EdgeInfo>()
                };
                _edgesDict.Add(vertex.ID, edgeVertexInfo);
            }

            return edgeVertexInfo;
        }

        private StringSerializableDictionary CreateNodeAttributeDict(Vertex vertex)
        {
            StringSerializableDictionary atbDict = new StringSerializableDictionary();
            foreach (PinGU.Attribute atb in vertex.attributes)
                atbDict.Add(atb.name, atb.value);

            atbDict.Add("type", vertex.type);
            atbDict.Add("label", vertex.label);
            atbDict.Add("ID", vertex.ID);
            atbDict.Add("date", vertex.date);
            return atbDict;
        }

        private void OrderGraphValuesHelper()
        {
            _currentGraphValuesHelper.EdgesLabels.Sort();
            _currentGraphValuesHelper.EdgesTypes.Sort();
            _currentGraphValuesHelper.VertexLabels.Sort();
            _currentGraphValuesHelper.VertexTags.Sort();
            _currentGraphValuesHelper.VertexObjectsName.Sort();
        }

        private void LateUpdate()
        {
            if (_provenanceGraphFilterSettings == null || _provenanceGraphVisualizationSettings == null)
                return;

            if (!ReplayController.Instance.TryGetCurrentReplayTickTime(out var curTickTime))
                return;

            _currentGraphValuesHelper.CurrentGraphTime = (float)curTickTime;

            if (!_ignoreAutoSyncConfiguration && !_provenanceGraphFilterSettings.AutoSyncGraphReplay)
                return;

            if (!ReplayController.Instance.IsInReplayMode())
                return;

            if (_lastTimeRender == curTickTime)
                return;

            if (curTickTime < 0)
                curTickTime = 0;

            var lastReplayTickTime = ReplayController.Instance.GetLastReplayTickTime();

            if (curTickTime > lastReplayTickTime)
                curTickTime = lastReplayTickTime;


            _lastTimeRender = curTickTime;
            _lastPastTimeRender = _lastTimeRender - (decimal)_provenanceGraphFilterSettings.PastSecondsToShow;
            _lastFutureTimeRender = _lastTimeRender + (decimal)_provenanceGraphFilterSettings.FutureSecondsToShow;

            if (_lastPastTimeRender < 0)
                _lastPastTimeRender = 0;

            if (_lastFutureTimeRender > lastReplayTickTime)
                _lastFutureTimeRender = lastReplayTickTime;

            _currentGraphValuesHelper.MinDisplayedTime = (float)_lastPastTimeRender;
            _currentGraphValuesHelper.MaxDisplayedTime = (float)_lastFutureTimeRender;

            DestroyAllNodesOutsideTimeAndUpdateValidNodes();
            RenderEntitiesNodes();
            RenderActivitiesNodes();
            RenderAgentsNodes();
        }

        private void RenderEntitiesNodes()
        {
            if (!TryCreateReplayProvenanceNode(_entityVertexDict,
                    _provenanceGraphVisualizationSettings.VertexNodePrefab))
                return;
        }

        private void RenderActivitiesNodes()
        {
            if (!TryCreateReplayProvenanceNode(_activityVertexDict,
                    _provenanceGraphVisualizationSettings.VertexNodePrefab))
                return;
        }

        private void RenderAgentsNodes()
        {
            if (!TryCreateReplayProvenanceNode(_agentVertexDict,
                    _provenanceGraphVisualizationSettings.VertexNodePrefab))
                return;
        }

        private bool TryCreateReplayProvenanceNode(Dictionary<decimal, List<VertexInfo>> dict,
            ReplayProvenanceVertexNode vertexTypeVertexNodePrefab)
        {
            decimal curTime = _lastTimeRender;
            if (_provenanceGraphFilterSettings.PastSecondsToShow > 0)
            {
                curTime = _lastPastTimeRender - (decimal).1f;
                while (curTime < _lastTimeRender)
                {
                    curTime += (decimal).1f;
                    TryCreateReplayProvenanceNodeAtThisTick(dict, vertexTypeVertexNodePrefab, curTime);
                }
            }

            TryCreateReplayProvenanceNodeAtThisTick(dict, vertexTypeVertexNodePrefab, curTime);

            if (_provenanceGraphFilterSettings.FutureSecondsToShow > 0)
            {
                while (curTime < _lastFutureTimeRender)
                {
                    curTime += (decimal).1f;
                    TryCreateReplayProvenanceNodeAtThisTick(dict, vertexTypeVertexNodePrefab, curTime);
                }
            }

            return true;
        }

        private void TryCreateReplayProvenanceNodeAtThisTick(Dictionary<decimal, List<VertexInfo>> dict,
            ReplayProvenanceVertexNode vertexTypeVertexNodePrefab, decimal curTime)
        {
            if (!dict.TryGetValue(curTime, out var vertexInTick))
                return;

            for (int i = 0; i < vertexInTick.Count; i++)
            {
                var vertexInfo = vertexInTick[i];

                if (_provenanceGraphFilterSettings.FilterMode ==
                    ProvenanceGraphFilterSettings.VisualizationFilterMode.Include)
                {
                    TryCreateReplayProvenanceNodeWithIncludeMode(vertexTypeVertexNodePrefab, vertexInfo);
                    continue;
                }

                TryCreateReplayProvenanceNodeWithExcludeMode(vertexTypeVertexNodePrefab, vertexInfo);
            }
        }

        private void TryCreateReplayProvenanceNodeWithIncludeMode(ReplayProvenanceVertexNode vertexTypeVertexNodePrefab,
            VertexInfo vertexInfo)
        {
            int includeVisualizationFilterIndex = _provenanceGraphFilterSettings.IncludeVisualizationFilters.FindIndex(
                x => x.IncludeVertexLabel.Equals(vertexInfo.Vertex.label));

            if (includeVisualizationFilterIndex == -1)
                return;

            var includeVisualizationFilter =
                _provenanceGraphFilterSettings.IncludeVisualizationFilters[includeVisualizationFilterIndex];

            if (includeVisualizationFilter.IgnoreThisFilter)
                return;

            if (includeVisualizationFilter.ExcludeVertexType.Exists(x => x.Equals((VertexType)Enum.Parse(
                    typeof(VertexType),
                    vertexInfo.Vertex.type))))
                return;

            if (includeVisualizationFilter.ExcludeVertexObjectsName.Exists(x =>
                    x.Equals(vertexInfo.NodeAttribututes["ObjectName"])))
                return;

            if (includeVisualizationFilter.ExcludeVertexObjectsTag.Exists(x =>
                    x.Equals(vertexInfo.NodeAttribututes["ObjectTag"])))
                return;

            var nodeIndex = _vertexNodesInTick.FindIndex(x => x.VertexInfo.Vertex.ID.Equals(vertexInfo.Vertex.ID));


            // This node exist so we need ignore to don't duplicate
            if (nodeIndex >= 0)
                return;

            if (!TryCreateVertexNode(vertexTypeVertexNodePrefab, vertexInfo, out var vertexNode))
                return;

            if (!_edgesDict.TryGetValue(vertexInfo.Vertex.ID, out var edgeVertexInfo))
                return;

            vertexNode.ClearLinkedEdges();
            TryCreateSourceEdgeNodes(includeVisualizationFilter, edgeVertexInfo, vertexNode);
            TryCreateTargetEdgeNodes(includeVisualizationFilter, edgeVertexInfo, vertexNode);
        }

        private void TryCreateReplayProvenanceNodeWithExcludeMode(ReplayProvenanceVertexNode vertexTypeVertexNodePrefab,
            VertexInfo vertexInfo)
        {
            if (_provenanceGraphFilterSettings.ExcludeRemoveVertexVisualizationFilters.VertexLabels.Exists(x =>
                    x.Equals(vertexInfo.Vertex.label)))
                return;

            if (_provenanceGraphFilterSettings.ExcludeRemoveVertexVisualizationFilters.VertexTypes.Exists(x =>
                    x.Equals((VertexType)Enum.Parse(typeof(VertexType),
                        vertexInfo.Vertex.type))))
                return;

            if (_provenanceGraphFilterSettings.ExcludeRemoveVertexVisualizationFilters.VertexObjectsName.Exists(x =>
                    x.Equals(vertexInfo.NodeAttribututes["ObjectName"])))
                return;


            if (_provenanceGraphFilterSettings.ExcludeRemoveVertexVisualizationFilters.VertexObjectsTag.Exists(x =>
                    x.Equals(vertexInfo.NodeAttribututes["ObjectTag"])))
                return;

            if (!TryCreateVertexNode(vertexTypeVertexNodePrefab, vertexInfo, out var vertexNode))
                return;

            if (!_edgesDict.TryGetValue(vertexInfo.Vertex.ID, out var edgeVertexInfo))
                return;

            var specificVisualizationIgnore = new ProvenanceGraphFilterSettings.IncludeVisualizationIncludeFilter
            {
                IgnoreThisFilter = true
            };

            if (_provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.EdgeRelation !=
                EdgeRelation.All &&
                _provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.EdgeRelation !=
                EdgeRelation.Source)
                TryCreateSourceEdgeNodes(specificVisualizationIgnore, edgeVertexInfo, vertexNode);


            if (_provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.EdgeRelation !=
                EdgeRelation.All &&
                _provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.EdgeRelation !=
                EdgeRelation.Target)
                TryCreateTargetEdgeNodes(specificVisualizationIgnore, edgeVertexInfo, vertexNode);
        }


        private bool TryCreateVertexNode(ReplayProvenanceVertexNode vertexTypeVertexNodePrefab, VertexInfo vertexInfo,
            out ReplayProvenanceVertexNode vertexNode)
        {
            vertexNode = null;

            if (_vertexNodesInTick.Exists(x => x.VertexInfo.Vertex.ID.Equals(vertexInfo.Vertex.ID)))
                return false;

            Vector3 position = new Vector3()
            {
                x = float.Parse(vertexInfo.NodeAttribututes["ObjectPosition_X"]),
                y = float.Parse(vertexInfo.NodeAttribututes["ObjectPosition_Y"]),
                z = float.Parse(vertexInfo.NodeAttribututes["ObjectPosition_Z"])
            };

            vertexNode = ProvenanceNodesPoolManager
                .CreatePooledOrDefault(vertexTypeVertexNodePrefab.gameObject, position,
                    vertexTypeVertexNodePrefab.gameObject.transform.rotation, _provenanceNodesParent)
                .GetComponent<ReplayProvenanceVertexNode>();
            vertexNode.SetupVertexInfo(_provenanceGraphVisualizationSettings, vertexInfo);
            vertexNode.UpdateNodeScale(_lastPastTimeRender, _lastTimeRender, _lastFutureTimeRender);

            //Hide edge nodes if they are linked with this vertex
            var edges = _edgeNodesInTick.FindAll(x =>
                (x.EdgeInfo.SourceVertexInfo.Vertex != null &&
                 x.EdgeInfo.SourceVertexInfo.Vertex.ID.Equals(vertexInfo.Vertex.ID)) ||
                (x.EdgeInfo.TargetVertexInfo.Vertex != null &&
                 x.EdgeInfo.TargetVertexInfo.Vertex.ID.Equals(vertexInfo.Vertex.ID)));

            for (int i = 0; i < edges.Count; i++)
                edges[i].HideEdgeCanvas();

            _vertexNodesInTick.Add(vertexNode);
            return true;
        }

        private void TryCreateSourceEdgeNodes(
            ProvenanceGraphFilterSettings.IncludeVisualizationIncludeFilter includeVisualizationIncludeFilter,
            EdgeVertexInfo edgeVertexInfo,
            ReplayProvenanceVertexNode linkedNode)
        {
            if (!includeVisualizationIncludeFilter.IgnoreThisFilter &&
                !(includeVisualizationIncludeFilter.IncludeEdgeRelation == EdgeRelation.All ||
                  includeVisualizationIncludeFilter.IncludeEdgeRelation == EdgeRelation.Source))
                return;

            for (int j = 0; j < edgeVertexInfo.SourceEdges.Count; j++)
            {
                var edgeInfo = edgeVertexInfo.SourceEdges[j];

                if (VerifyNeedSkipEdgeThisInfo(includeVisualizationIncludeFilter, edgeInfo, edgeInfo.TargetVertexInfo))
                    continue;

                if (_edgeNodesInTick.Exists(x => x.EdgeInfo.Id.Equals(edgeInfo.Id)))
                    continue;

                Vector3 edgeNodePosition = new Vector3()
                {
                    x = float.Parse(edgeInfo.TargetVertexInfo.NodeAttribututes["ObjectPosition_X"]),
                    y = float.Parse(edgeInfo.TargetVertexInfo.NodeAttribututes["ObjectPosition_Y"]),
                    z = float.Parse(edgeInfo.TargetVertexInfo.NodeAttribututes["ObjectPosition_Z"])
                };

                ReplayProvenanceEdgeNode edgeNode = ProvenanceNodesPoolManager
                    .CreatePooledOrDefault(_provenanceGraphVisualizationSettings.EdgeNodePrefab.gameObject,
                        edgeNodePosition,
                        _provenanceGraphVisualizationSettings.EdgeNodePrefab.gameObject.transform.rotation,
                        _provenanceNodesParent).GetComponent<ReplayProvenanceEdgeNode>();
                edgeNode.SetupEdgeInfo(_provenanceGraphVisualizationSettings, edgeInfo, linkedNode, false);
                edgeNode.UpdateNodeScale(_lastPastTimeRender, _lastTimeRender, _lastFutureTimeRender);
                linkedNode.AddLinkedSourceEdge(edgeNode);

                //Hide edge node if exist a vertex with this id
                if (_vertexNodesInTick.Exists(x => x.VertexInfo.Vertex.ID.Equals(edgeInfo.TargetVertexInfo.Vertex.ID)))
                    edgeNode.HideEdgeCanvas();

                _edgeNodesInTick.Add(edgeNode);
            }
        }

        private void TryCreateTargetEdgeNodes(
            ProvenanceGraphFilterSettings.IncludeVisualizationIncludeFilter includeVisualizationIncludeFilter,
            EdgeVertexInfo edgeVertexInfo,
            ReplayProvenanceVertexNode linkedNode)
        {
            if (!includeVisualizationIncludeFilter.IgnoreThisFilter &&
                !(includeVisualizationIncludeFilter.IncludeEdgeRelation == EdgeRelation.All ||
                  includeVisualizationIncludeFilter.IncludeEdgeRelation == EdgeRelation.Target))
                return;

            for (int j = 0; j < edgeVertexInfo.TargetEdges.Count; j++)
            {
                var edgeInfo = edgeVertexInfo.TargetEdges[j];

                if (VerifyNeedSkipEdgeThisInfo(includeVisualizationIncludeFilter, edgeInfo, edgeInfo.SourceVertexInfo))
                    continue;

                if (_edgeNodesInTick.Exists(x => x.EdgeInfo.Id.Equals(edgeInfo.Id)))
                    continue;

                Vector3 edgeNodePosition = new Vector3()
                {
                    x = float.Parse(edgeInfo.SourceVertexInfo.NodeAttribututes["ObjectPosition_X"]),
                    y = float.Parse(edgeInfo.SourceVertexInfo.NodeAttribututes["ObjectPosition_Y"]),
                    z = float.Parse(edgeInfo.SourceVertexInfo.NodeAttribututes["ObjectPosition_Z"])
                };

                ReplayProvenanceEdgeNode edgeNode = ProvenanceNodesPoolManager
                    .CreatePooledOrDefault(_provenanceGraphVisualizationSettings.EdgeNodePrefab.gameObject,
                        edgeNodePosition,
                        _provenanceGraphVisualizationSettings.EdgeNodePrefab.gameObject.transform.rotation,
                        _provenanceNodesParent).GetComponent<ReplayProvenanceEdgeNode>();
                edgeNode.SetupEdgeInfo(_provenanceGraphVisualizationSettings, edgeInfo, linkedNode, true);
                edgeNode.UpdateNodeScale(_lastPastTimeRender, _lastTimeRender, _lastFutureTimeRender);
                linkedNode.AddLinkedTargetEdge(edgeNode);

                //Hide edge node if exist a vertex with this id
                if (_vertexNodesInTick.Exists(x => x.VertexInfo.Vertex.ID.Equals(edgeInfo.SourceVertexInfo.Vertex.ID)))
                    edgeNode.HideEdgeCanvas();

                _edgeNodesInTick.Add(edgeNode);
            }
        }

        private bool VerifyNeedSkipEdgeThisInfo(
            ProvenanceGraphFilterSettings.IncludeVisualizationIncludeFilter includeVisualizationIncludeFilter,
            EdgeInfo edgeInfo, VertexInfo edgeVertexInfo)
        {
            if (_provenanceGraphFilterSettings.FilterMode ==
                ProvenanceGraphFilterSettings.VisualizationFilterMode.Exclude)
            {
                if (_provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.EdgesLabels.Exists(x =>
                        x.Equals(edgeInfo.Label)))
                    return true;

                if (_provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.EdgesTypes.Exists(x =>
                        x.Equals(edgeInfo.Type)))
                    return true;

                if (_provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.VertexObjectsType.Exists(x =>
                        x.Equals((VertexType)Enum.Parse(typeof(VertexType), edgeVertexInfo.Vertex.type))))
                    return true;

                if (_provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.VertexObjectsLabels.Exists(
                        x =>
                            x.Equals(edgeVertexInfo.Vertex.label)))
                    return true;

                if (_provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.VertexObjectsName.Exists(x =>
                        x.Equals(edgeVertexInfo.NodeAttribututes["ObjectName"])))
                    return true;

                if (_provenanceGraphFilterSettings.ExcludeRemoveEdgesVisualizationFilters.VertexObjectsTag.Exists(x =>
                        x.Equals(edgeVertexInfo.NodeAttribututes["ObjectTag"])))
                    return true;
            }
            else if (!includeVisualizationIncludeFilter.IgnoreThisFilter)
            {
                if (includeVisualizationIncludeFilter.ExcludeEdgeLabels.Exists(x => x.Equals(edgeInfo.Label)))
                    return true;

                if (includeVisualizationIncludeFilter.ExcludeEdgeTypes.Exists(x => x.Equals(edgeInfo.Type)))
                    return true;

                if (includeVisualizationIncludeFilter.ExcludeEdgeVertexObjectsType.Exists(x =>
                        x.Equals((VertexType)Enum.Parse(typeof(VertexType), edgeVertexInfo.Vertex.type))))
                    return true;

                if (includeVisualizationIncludeFilter.ExcludeEdgeVertexObjectsLabels.Exists(x =>
                        x.Equals(edgeVertexInfo.Vertex.label)))
                    return true;

                if (includeVisualizationIncludeFilter.ExcludeEdgeVertexObjectsName.Exists(x =>
                        x.Equals(edgeVertexInfo.NodeAttribututes["ObjectName"])))
                    return true;

                if (includeVisualizationIncludeFilter.ExcludeEdgeVertexObjectsTag.Exists(x =>
                        x.Equals(edgeVertexInfo.NodeAttribututes["ObjectTag"])))
                    return true;
            }

            return false;
        }

        private void DestroyAllNodesOutsideTimeAndUpdateValidNodes()
        {
            //TODO: LEMBRAR DE CRIAR UMA FORMA DE APAGAR OS NODES QUANDO O JOGADOR MUDAR A CONFIGURAÇÃO DE VISUALIZAÇÃO
            _indexesOfNodesToDestroy.Clear();

            for (var i = _vertexNodesInTick.Count - 1; i >= 0; i--)
            {
                var node = _vertexNodesInTick[i];
                if (node.VertexInfo.VertexTime < _lastPastTimeRender)
                {
                    _indexesOfNodesToDestroy.Add(i);
                    continue;
                }

                if (node.VertexInfo.VertexTime > _lastFutureTimeRender)
                {
                    _indexesOfNodesToDestroy.Add(i);
                    continue;
                }

                node.UpdateNodeScale(_lastPastTimeRender, _lastTimeRender, _lastFutureTimeRender);
            }

            for (int i = 0; i < _indexesOfNodesToDestroy.Count; i++)
            {
                var vertexNodeIndex = _indexesOfNodesToDestroy[i];
                var vertexNode = _vertexNodesInTick[vertexNodeIndex];

                for (int j = _edgeNodesInTick.Count - 1; j >= 0; j--)
                {
                    var edgeNode = _edgeNodesInTick[j];

                    if (!edgeNode.LinkedNode.VertexInfo.Vertex.ID.Equals(vertexNode.VertexInfo.Vertex.ID))
                        continue;

                    _edgeNodesInTick.RemoveAt(j);
                    edgeNode.ProvenanceNodesPoolComponent.DestroyPoolObject();
                }

                _vertexNodesInTick.RemoveAt(vertexNodeIndex);
                vertexNode.ProvenanceNodesPoolComponent.DestroyPoolObject();
            }

            for (int i = 0; i < _edgeNodesInTick.Count; i++)
            {
                _edgeNodesInTick[i].UpdateNodeScale(_lastPastTimeRender, _lastTimeRender, _lastFutureTimeRender);
            }
        }

        private void DestroyAllNodes()
        {
            for (var i = _vertexNodesInTick.Count - 1; i >= 0; i--)
            {
                var node = _vertexNodesInTick[i];
                _vertexNodesInTick.RemoveAt(i);
                node.ProvenanceNodesPoolComponent.DestroyPoolObject();
            }

            for (var i = _edgeNodesInTick.Count - 1; i >= 0; i--)
            {
                var node = _edgeNodesInTick[i];
                _edgeNodesInTick.RemoveAt(i);
                node.ProvenanceNodesPoolComponent.DestroyPoolObject();
            }
        }

        [Serializable]
        public struct VertexInfo
        {
            public Vertex Vertex;
            public decimal VertexTime;
            public StringSerializableDictionary NodeAttribututes;
        }

        [Serializable]
        public struct EdgeVertexInfo
        {
            public List<EdgeInfo> SourceEdges;
            public List<EdgeInfo> TargetEdges;
        }

        [Serializable]
        public struct EdgeInfo
        {
            public VertexInfo SourceVertexInfo;
            public VertexInfo TargetVertexInfo;
            public EdgeType Type;
            public string Label;
            public string Id;
            public string Value;
        }


        [Serializable]
        public enum VertexType
        {
            Activity = 1,
            Agent = 2,
            Entity = 4
        }

        [Serializable]
        public enum EdgeType
        {
            All = 0,

            //Source -> Target
            //Activity -> Activity
            WasInformedBy = 1,

            //Activity -> Agent / Default
            WasAssociatedTo = 2,

            //Activity -> Entity
            Used = 4,

            //Agent -> Activity / Agent -> Entity
            WasInfluencedBy = 8,

            //Agent -> Agent 
            ActedOnBehalfOf = 16,

            //Entity -> Activity
            WasGeneratedBy = 32,

            //Entity -> Agent 
            WasAttributedTo = 64,

            //Entity -> Entity
            WasDerivedFrom = 128
        }

        [Serializable]
        public enum EdgeRelation
        {
            None = 0,
            All = 1,
            Source = 2,
            Target = 4
        }

        [Serializable]
        public class GraphValuesHelper
        {
            public float GraphDuration = 0;
            public float CurrentGraphTime = 0;
            public float MinDisplayedTime = 0;
            public float MaxDisplayedTime = 0;
            public List<string> VertexLabels = new List<string>();
            public List<string> VertexTags = new List<string>();
            public List<string> VertexObjectsName = new List<string>();
            public List<string> EdgesLabels = new List<string>();
            public List<string> EdgesTypes = new List<string>();
        }

        [Serializable]
        public class StringSerializableDictionary : SerializableDictionaryBase<string, string>
        {

        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PinGUReplay.ProvenanceGraphViewerModule
{
    [CreateAssetMenu(fileName = "ProvenanceGraphFilterSettings", menuName = "PinGU Replay/ProvenanceGraphFilterSettings")]
    public class ProvenanceGraphFilterSettings : ScriptableObject
    {
        [Header("Visualization Time and Sync Settings")] 
        [SerializeField] private bool _autoSyncGraphReplay;
        public bool AutoSyncGraphReplay => _autoSyncGraphReplay;
        
        [SerializeField] private float _pastSecondsToShow;

        public float PastSecondsToShow => _pastSecondsToShow;
        [SerializeField] private float _futureSecondsToShow;
        public float FutureSecondsToShow => _futureSecondsToShow;

        [Header("Visualization Filters")] 
        [SerializeField] private VisualizationFilterMode _filterMode;
        public VisualizationFilterMode FilterMode => _filterMode;
        
        [Header("Only for Include Filter Mode")] 
        [SerializeField] private List<IncludeVisualizationIncludeFilter> _includeVisualizationFilters;
        public List<IncludeVisualizationIncludeFilter> IncludeVisualizationFilters => _includeVisualizationFilters;

        [Header("Only for Exclude Filter Mode")]
        [SerializeField] private ExcludeVisualizationRemoveVertexFilter _excludeRemoveVertexVisualizationFilters;
        public ExcludeVisualizationRemoveVertexFilter ExcludeRemoveVertexVisualizationFilters => _excludeRemoveVertexVisualizationFilters;
        
        [SerializeField] private ExcludeVisualizationRemoveEdgeFilter _excludeRemoveEdgesVisualizationFilters;
        public ExcludeVisualizationRemoveEdgeFilter ExcludeRemoveEdgesVisualizationFilters => _excludeRemoveEdgesVisualizationFilters;

         
        [Serializable]
        public enum VisualizationFilterMode
        {
            Include,
            Exclude
        }

        [Serializable]
        public struct ExcludeVisualizationRemoveVertexFilter
        {
            public List<string> VertexLabels;
            public List<ProvenanceGraphViewerController.VertexType> VertexTypes;
            public List<string> VertexObjectsName;
            public List<string> VertexObjectsTag;
        }
        
        [Serializable]
        public struct ExcludeVisualizationRemoveEdgeFilter
        {
            public ProvenanceGraphViewerController.EdgeRelation EdgeRelation;
            public List<string> EdgesLabels;
            public List<ProvenanceGraphViewerController.EdgeType> EdgesTypes;
            public List<string> VertexObjectsLabels;
            public List<ProvenanceGraphViewerController.VertexType> VertexObjectsType;
            public List<string> VertexObjectsName;
            public List<string> VertexObjectsTag;
        }
        
        [Serializable]
		public struct IncludeVisualizationIncludeFilter
		{
			[Header("Include Rules")]
			public string IncludeVertexLabel;
			public ProvenanceGraphViewerController.EdgeRelation IncludeEdgeRelation;
			public bool IgnoreThisFilter;

			[Header("Exclude Rules")]
			public List<ProvenanceGraphViewerController.VertexType> ExcludeVertexType;
			public List<string> ExcludeVertexObjectsName;
			public List<string> ExcludeVertexObjectsTag;
			public List<string> ExcludeEdgeLabels;
			public List<ProvenanceGraphViewerController.EdgeType> ExcludeEdgeTypes;
			public List<string> ExcludeEdgeVertexObjectsLabels;
			public List<ProvenanceGraphViewerController.VertexType> ExcludeEdgeVertexObjectsType;
			public List<string> ExcludeEdgeVertexObjectsName;
			public List<string> ExcludeEdgeVertexObjectsTag;
		}
    }
}


using PinGU;
using UnityEngine;

public class CollectableProvenance : MonoBehaviour
{
    [SerializeField] private ExtractProvenance _extractProvenance;
    
    private void Start()
    {
        _extractProvenance.AddAttribute(SampleProvenanceConstants.CollectableNameAttribute, gameObject.name);
        _extractProvenance.NewEntityVertex(SampleProvenanceConstants.SpawnCollectable);
    }
}

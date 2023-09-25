using PinGU;
using UnityEngine;

public class PlayerProvenance : MonoBehaviour
{
    [SerializeField] private ExtractProvenance _extractProvenance;
    
    [Header("Position - Event")] 
    [SerializeField] private bool _collectPositionEvent = true;
    [SerializeField] private float _positionEventCollectInterval = 3;
    private float _curCollectPositionIntervalTime = 0;
    
    [Header("Collect Item - Event")] 
    [SerializeField] private bool _collectCollectItemEvent = true;
    
    private void Start()
    {
        _extractProvenance.NewAgentVertex(gameObject.name);
        _curCollectPositionIntervalTime = _positionEventCollectInterval;
    }


    private void Update()
    {
        if(_collectPositionEvent)
            CollectPositionEvent();
    }

    private void CollectPositionEvent()
    {
        _curCollectPositionIntervalTime += Time.deltaTime;
        
        if(_curCollectPositionIntervalTime < _positionEventCollectInterval)
            return;

        _curCollectPositionIntervalTime = 0;
        _extractProvenance.NewActivityVertex(SampleProvenanceConstants.PlayerPosition, this.gameObject);
        //Need configure hasInfluenceID to consume previous GenerateInfluenceC, we append GetInstanceID
        //because if the game have two or more players the PlayerPosition Activity will be individual
        //of each player.
        _extractProvenance.HasInfluence_ID(SampleProvenanceConstants.PlayerPosition + GetInstanceID());
        _extractProvenance.GenerateInfluenceC(SampleProvenanceConstants.PlayerPosition, 
            SampleProvenanceConstants.PlayerPosition + this.GetInstanceID().ToString(), 
            SampleProvenanceConstants.PlayerPosition,
            "1", 1);    
    }
    
    
    public void OnCollectCollectable(GameObject collectableGameObject)
    {
        if(!_collectCollectItemEvent)
            return;

        _extractProvenance.AddAttribute(SampleProvenanceConstants.CollectableNameAttribute, collectableGameObject.gameObject.name);
        _extractProvenance.NewActivityVertex(SampleProvenanceConstants.CollectCollectable, this.gameObject);
    }
    
    
}

#pragma strict
public var prov : ExtractProvenance = null;

function Awake()
{
	// Load provenance pointers
	var provObj : GameObject = GameObject.Find("Provenance");
	prov = GetComponent(ExtractProvenance); 
	
	if(prov == null)
	{
		prov = GetComponentInParent(ExtractProvenance); 
	}
	
	prov.influenceContainer = provObj.GetComponent(InfluenceController); 
	prov.provenance = provObj.GetComponent(ProvenanceController); 
	
	Prov_EnviromentAgent();
	Prov_Idle();
	
	renderer.enabled = true;
}

public function Prov_EnviromentAgent()
{
	prov.NewAgentVertex("Coordinates", "");
}

public function Prov_Idle()
{
	prov.NewActivityVertex("Idle", "");
	prov.HasInfluence("Enemy");
}
#pragma strict
public var prov : ExtractProvenance = null;

function Awake()
{
	// Load provenance pointers
	prov = GetComponent(ExtractProvenance); 
	if(prov == null)	{
		prov = GetComponentInParent(ExtractProvenance); 
	}
	/*
	var provObj : GameObject = GameObject.Find("Provenance");
	
	
	if(prov == null)
	{
		prov = GetComponentInParent(ExtractProvenance); 
	}
	
	prov.influenceContainer = provObj.GetComponent(InfluenceController); 
	prov.provenance = provObj.GetComponent(ProvenanceController); 
	*/
	Prov_EnviromentAgent();
}

public function Prov_EnviromentAgent()
{
	prov.NewAgentVertex("Enviroment");
}

public function Prov_Enviroment(type : String, gameobject : GameObject)
{
	prov.NewEntityVertex(type, gameobject);
	var heroObj : GameObject = GameObject.FindGameObjectWithTag("Player");
	var player : PlayerProv = heroObj.GetComponent(PlayerProv);
	prov.GenerateInfluenceC("Player", this.GetInstanceID().ToString(), "Interacted", "1", 1);
	player.Prov_Interact();	
}

public function Prov_UnlockInfluence()
{
	prov.GenerateInfluenceC("Unlocked", this.GetInstanceID().ToString(), "Unlocked", "1", 1);
}

public function Prov_Unlock(gameobject : GameObject)
{
	prov.NewEntityVertex("LockedDoor", gameobject);
	prov.HasInfluence("Unlocked");
}
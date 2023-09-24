#pragma strict
public var xmlName : String;
public var countLaps : boolean = false;
private var exported : boolean = false;
private var lap : int = 0;

function OnTriggerEnter(other : Collider)
{
	if(!exported)
	{
		exported = true;
		// Provenance
		Prov_Export();
	}
}

function OnTriggerExit (other : Collider)
{
	exported = false;
}

function Prov_Export()
{
	var ProvObj : GameObject = GameObject.Find("Provenance");
	var provController : ProvenanceController = ProvObj.GetComponent(ProvenanceController); 
	if(!countLaps)
	{
		provController.Save(xmlName);
	}
	else
	{
		provController.Save(xmlName + lap);
		lap++;
	}
}
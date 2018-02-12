#pragma strict

private var exported : boolean = false;
public var provenaceGameObjectName : String = "Provenance";
public var xmlExportName : String = "";

function Start () {

}

function Update () {

}

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
	Debug.Log (provenaceGameObjectName);
	var ProvObj : GameObject = GameObject.Find(provenaceGameObjectName);
	var prov : ProvenanceController = ProvObj.GetComponent(ProvenanceController); 
	prov.Save(xmlExportName);
}
#pragma strict

private var exported : boolean = false;

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
	Debug.Log ("Exported");
	var ProvObj : GameObject = GameObject.Find("Provenance");
	var prov : ProvenanceController = ProvObj.GetComponent(ProvenanceController); 
	prov.Save("Angry_Robots");
}
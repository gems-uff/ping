#pragma strict

//=================================================================================================================
// Script for storing influence edges for the entire game
// Attach this script in an Empty GameObject that is never destroyed during the game 
//(In the same GameObject for ProvenanceGatherer)
// Link it to ProvenanceGatherer
//
// Uses ArrayList for influence edges
// All functions are automatically invoked and controlled by 'ExtractProvenance' script script
//
// If you desire to manually clean/erase the influence list, then invoke 'CleanInfluence' function
//=================================================================================================================

//=================================================================================================================
// *Declarations Section*
//=================================================================================================================
public var provenance : ProvenanceController;	
private var influenceList : List.<InfluenceEdge> = new List.<InfluenceEdge>();
private var consumableList : List.<InfluenceEdge> = new List.<InfluenceEdge>();

//=================================================================================================================
// *Functions Section*
//=================================================================================================================

//=================================================================================================================
// Create a new influence and add it to the influence list
// Function invoked at 'ExtractProvenance' to create a new influence
//=================================================================================================================
/*
public function CreateInfluence(tag : String, ID : String, source : String, influenceName : String, influenceValue : String, consumable : boolean, quantity : int)
{
	CreateInfluence(tag, ID, source, influenceName, influenceValue, consumable, quantity, -1)
}
public function CreateInfluence(tag : String, ID : String, source : String, influenceName : String, influenceValue : String, consumable : boolean, quantity : int, expirationTime : float)
{
	var newInfluence : InfluenceEdge = new InfluenceEdge(tag, ID, source, influenceName, influenceValue, consumable, quantity, expirationTime);
	
	if(consumable)
		consumableList.Add(newInfluence);
	else
		influenceList.Add(newInfluence);
}
public function CreateInfluenceWithMissable(tag : String, ID : String, source : String, influenceName : String, influenceValue : String, consumable : boolean, quantity : int, target : GameObject)
{
	CreateInfluenceWithMissable(tag, ID, source, influenceName, influenceValue, consumable, quantity, target, -1)
}
*/
public function CreateInfluence(tag : String, ID : String, source : String, influenceName : String, influenceValue : String, consumable : boolean, quantity : int, target : GameObject, expirationTime : float)
{
	var newInfluence : InfluenceEdge;
	if(target != null)
	{
		// Create Missable Influence and associate the edge with the GameObject
		var prov : ExtractProvenance = target.GetComponent(ExtractProvenance);
		var missableID : String = provenance.CreateInfluenceEdge(source, prov.GetCurrentVertex().ID, influenceName + " Missed", "0");
		
		// Create the normal Influence that will replace the missable edge when consumed
		newInfluence = new InfluenceEdge(tag, ID, source, influenceName, influenceValue, consumable, quantity, missableID, expirationTime);
	}
	else
		newInfluence = new InfluenceEdge(tag, ID, source, influenceName, influenceValue, consumable, quantity, expirationTime);
		
	if(consumable)
		consumableList.Add(newInfluence);
	else
		influenceList.Add(newInfluence);
}

//=================================================================================================================
// Remove all influences from the influence list
// Use this function to remove all influences in the influence list
//=================================================================================================================
public function CleanInfluence()
{
	CleanInfluenceConsumable();
	CleanInfluenceNotConsumable();
}

public function CleanInfluenceConsumable()
{
	influenceList = new List.<InfluenceEdge>();
}
public function CleanInfluenceNotConsumable()
{
	influenceList = new List.<InfluenceEdge>();
}

//=================================================================================================================
// Remove all influences with 'tag' from the influence list
// Function invoked at 'ExtractProvenance' to remove an existing influence because it expired
//=================================================================================================================
public function RemoveInfluenceByTag(tag : String)
{
	RemoveInfluenceByTag(tag, influenceList);
	RemoveInfluenceByTag(tag, consumableList);
}
public function RemoveInfluenceByTag(tag : String, list : List.<InfluenceEdge>)
{
	var i : int;
	//var currentInf : InfluenceEdge = new InfluenceEdge();
	for (i = 0; i < list.Count; i++)
	{
		//currentInf = list[i];
		if(list[i].tag == tag)
		{
			list.RemoveAt(i);
		}
	}
}

//=================================================================================================================
// Remove all influences with 'ID' from the influence list
// Function invoked at 'ExtractProvenance' to remove an existing influence because it expired
//=================================================================================================================
public function RemoveInfluenceByID(ID : String)
{
	RemoveInfluenceByID(ID, influenceList);
	RemoveInfluenceByID(ID, consumableList);
}
public function RemoveInfluenceByID(ID : String, list : List.<InfluenceEdge>)
{
	var i : int;
	for (i = 0; i < list.Count; i++)
	{
		if(list[i].ID == ID)
		{
			list.RemoveAt(i);
		}
	}
}

//=================================================================================================================
// Check if there are any influences for the current vertex
// Function invoked at 'ExtractProvenance' to check if the current action was influenced
//=================================================================================================================

// Check influence list by 'tag'
function WasInfluencedByTag(tag : String, targetID : String)
{
	WasInfluencedBy(tag, targetID, true);
}

// Check influence list by influence's 'ID'
function WasInfluencedByID(ID : String, targetID : String)
{
	WasInfluencedBy(ID, targetID, false);
}


// Check both influence lists
function WasInfluencedBy(type : String, targetID : String, isTag : boolean)
{
	// Normal influence List
	WasInfluencedBy(type, targetID, influenceList, isTag);
	// Consumable influence List
	WasInfluencedBy(type, targetID, consumableList, isTag);
}

function WasInfluencedBy(type : String, targetID : String, list : List.<InfluenceEdge>, isTag : boolean)
{
	var i : int;
	var edgeValue : String;

	for (i = 0; i < list.Count; i++)
	{
		// Determine if the search is by TAG or ID
		if(isTag)
			edgeValue = list[i].tag;
		else
			edgeValue = list[i].ID;
			
		if(edgeValue == type)
		{
			list[i].quantity--;
			// Check if this influence had expiration time
			if((list[i].expirationTime == -1) || (Time.time < (list[i].expirationTime)))
			{
				// Check if the influence had a missable placeholder
				if(list[i].missableID != null)
				{
					// This influence had a missable placeholder
					// Need to update the placeholder instead of adding a new edge
					provenance.UpdateInfluenceEdge(list[i].source, targetID, list[i].name, list[i].infValue, list[i].missableID);
				}
				else
					provenance.CreateInfluenceEdge(list[i].source, targetID, list[i].name, list[i].infValue);

				if((list[i].quantity <=  0) && (list[i].consumable))
				{
					list.RemoveAt(i);
				}
			}
			else	//Remove it since it expired
				list.RemoveAt(i);
		}
	}
}

/*
function Awake () {
	provenance = GetComponentInChildren(ProvenanceController);
}
*/
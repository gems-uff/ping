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
public function CreateInfluence(tag : String, ID : String, source : String, influenceName : String, influenceValue : String, consumable : boolean, quantity : int)
{
	var newInfluence : InfluenceEdge = new InfluenceEdge(tag, ID, source, influenceName, influenceValue, consumable, quantity);
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
	/*
	var i : int;
	//var removeList : List.<int> = new List.<int>();
	for (i = 0; i < influenceList.Count; i++)
	{
		if(influenceList[i].consumable == false)
		{
			influenceList.RemoveAt(i);
			//removeList.Add(i);
			i = 0;
		}
	}
	*/
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
	WasInfluencedByTag(tag, targetID, influenceList);
	WasInfluencedByTag(tag, targetID, consumableList);
}
function WasInfluencedByTag(tag : String, targetID : String, list : List.<InfluenceEdge>)
{
	var i : int;

	for (i = 0; i < list.Count; i++)
	{
		if(list[i].tag == tag)
		{
			if(list[i].consumable)
			{
				list[i].quantity--;
				provenance.CreateInfluenceEdge(list[i].source, targetID, list[i].name, list[i].infValue);
				if(list[i].quantity ==  0)
				{
					list.RemoveAt(i);
				}
								
			}
			else
			{
				provenance.CreateInfluenceEdge(list[i].source, targetID, list[i].name, list[i].infValue);
			}
		}
	}
}

// Check influence list by influence's 'ID'
function WasInfluencedByID(ID : String, targetID : String)
{
	WasInfluencedByID(ID, targetID, influenceList);
	WasInfluencedByID(ID, targetID, consumableList);
}
function WasInfluencedByID(ID : String, targetID : String, list : List.<InfluenceEdge>)
{
	var i : int;

	for (i = 0; i < list.Count; i++)
	{
		if(list[i].ID == ID)
		{
			if(list[i].consumable)
			{
				list[i].quantity--;
				provenance.CreateInfluenceEdge(list[i].source, targetID, list[i].name, list[i].infValue);
				if(list[i].quantity ==  0)
				{
					list.RemoveAt(i);
				}
								
			}
			else
			{
				provenance.CreateInfluenceEdge(list[i].source, targetID, list[i].name, list[i].infValue);
			}
		}
	}
}

/*
function Awake () {
	provenance = GetComponentInChildren(ProvenanceController);
}
*/
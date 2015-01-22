#pragma strict

//===================================================================================================================
// 'InfluenceEdge' Class Definition
// This script is to define the influenceEdge class
// Do not attach this script in any GameObject
// It is only necessary to be on your resources folder
// The 'InfluenceEdge' class is used for the Provenance-Scripts
//===================================================================================================================
class InfluenceEdge
{
	public var tag : String;			// This is the influence tag, which is used to designate actions that can be affected by it
										// or to group up various influences for the influence-check process
	public var ID : String;				// This is the influence's ID and is used to single out an influence from a 'type' group
	public var source : String;			// This contains the vertex ID that generated the influence
	public var name : String;			// This is the name of the influence Edge
	public var infValue : String;		// This is the value of the influence Edge
	public var consumable : boolean;	// This controls if the influence has a limit of usages
	public var quantity : int;			// This is how many times this influence can still be used
	public var missableID : String;		// This is used for missable influences
	public var expirationTime : float;
	
	//================================================================================================================
	// Empty Influence Constructor
	//================================================================================================================
	function InfluenceEdge()
	{
		this.tag = "";
		this.ID = "";
		this.source = "";
		this.name = "";
		this.infValue = "";
		this.consumable = false;
		this.quantity = 1;
		this.missableID = null;
		this.expirationTime = -1;
	}
	/*
	//================================================================================================================
	// Influence Constructor
	//================================================================================================================
	function InfluenceEdge(tag_ : String, ID_ : String, source_ : String, name_ : String, infValue_ : String, consumable_ : boolean, quantity_ : int)
	{
		this.tag = tag_;
		this.ID = ID_;
		this.source = source_;
		this.name = name_;
		this.infValue = infValue_;
		this.consumable = consumable_;
		this.quantity = quantity_;
		this.missableID = null;
		this.timeStamp = -1;
		this.duration = -1;
	}
	*/
	//================================================================================================================
	// Influence Constructor
	//================================================================================================================
	function InfluenceEdge(tag_ : String, ID_ : String, source_ : String, name_ : String, infValue_ : String, consumable_ : boolean, quantity_ : int, expirationTime_ : float)
	{
		this.tag = tag_;
		this.ID = ID_;
		this.source = source_;
		this.name = name_;
		this.infValue = infValue_;
		this.consumable = consumable_;
		this.quantity = quantity_;
		this.missableID = null;
		this.expirationTime = expirationTime_;
	}
	
	//================================================================================================================
	// Missable Influence Constructor
	//================================================================================================================
	function InfluenceEdge(tag_ : String, ID_ : String, source_ : String, name_ : String, infValue_ : String, consumable_ : boolean, quantity_ : int, _missableID : String, expirationTime_ : float)
	{
		this.tag = tag_;
		this.ID = ID_;
		this.source = source_;
		this.name = name_;
		this.infValue = infValue_;
		this.consumable = consumable_;
		this.quantity = quantity_;
		this.missableID = _missableID;
		this.expirationTime = expirationTime_;
	}
}
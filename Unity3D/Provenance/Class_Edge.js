#pragma strict

//===================================================================================================================
// 'Edge' Class Definition
// This script is to define the Edge class
// Do not attach this script in any GameObject
// It is only necessary to be on your resources folder
// The 'Edge' class is used for the Provenance-Scripts
//===================================================================================================================
class Edge
{
	public var ID : String;				// Edge's Unique ID
	public var type : String;			// Provenance tyé for this edge
	public var label : String;			// A human-readable label for this edge (i.e. damage, hit points)
	public var value : String;		// Value of this edge (i.e. +4)
	public var sourceID : String;		// Vertex Source of this edge
	public var targetID : String;		// Vertex Target of this edge
	
	//================================================================================================================
	// Empty Edge Constructor
	//================================================================================================================
	function Edge()
	{
		this.ID = "";
		this.type = "";
		this.label = "";
		this.value = "";
		this.sourceID = "";
		this.targetID = "";
	}
	
	//================================================================================================================
	// Edge Constructor
	//================================================================================================================
	function Edge(id_ : String, label_ : String, type_ : String, edge_value_ : String, sourceID_ : String, targetID_ : String)
	{
		this.ID = id_;
		this.type = type_;
		this.label = label_;
		this.value = edge_value_;
		this.sourceID = sourceID_;
		this.targetID = targetID_;
	}
}
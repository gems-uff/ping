#pragma strict

//===================================================================================================================
// 'Vertex' Class Definition
// This script is to define the Edge class
// Do not attach this script in any GameObject
// It is only necessary to be on your resources folder
// The 'Vertex' class is used for the Provenance-Scripts
//===================================================================================================================

import System.Collections.Generic;
import System.Xml.Serialization;

class Vertex
{
	public var ID : String;		// Vertex' unique ID
	public var type : String;	// Provenance Label for this vertex (Activity, Agent, Entity)
	public var label : String;	// A human-readable name for this vertex
	public var date : String;	// Game time when the vertex is created
	
	@XmlArray("attributes")
 	@XmlArrayItem("attribute")
	public var attributes : List.<Attribute>;	// A List representing each vertex' attributes 
													// Each attribute must have a String (Name) and a Number (att_value)

	//================================================================================================================
	// Empty Vertex Constructor
	//================================================================================================================
	public function Vertex()
	{
		this.ID = "";
		this.date = "";
		this.type = "";
		this.label = "";
		this.attributes = new List.<Attribute>();
	}
	
	//================================================================================================================
	// Vertex Constructor
	//================================================================================================================
	public function Vertex(id_ : String, date_ : String, type_ : String, label_ : String, attribute_ : List.<Attribute>)
	{
		this.ID = id_;
		this.date = date_;
		this.type = type_;
		this.label = label_;
		
		this.attributes = new List.<Attribute>();
		
		//Copy the attribute list
		for(var i = 0; i < attribute_.Count; i++)
		{
			this.attributes.Add(attribute_[i]);
		}
	}	
}

//===================================================================================================================
// AttributeType Class Definition
// This script is to define the AttributeType type used for Vertex' Attributes
//===================================================================================================================
class Attribute
{
	public var name : String;		// Name of the attribute (i.e. HitPoints)
	public var value : String;	// Value of the attribute (i.e. 100)
	
	//================================================================================================================
	// AttributeType Constructor
	//================================================================================================================
	public function Attribute()
	{
		this.name = "";
		this.value = "";
	}
	
	//================================================================================================================
	// AttributeType Constructor
	//================================================================================================================
	public function Attribute(name_ : String, att_value_ : String)
	{
		this.name = name_;
		this.value = att_value_;
	}
}
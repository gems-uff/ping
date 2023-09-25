#pragma strict

//=================================================================================================================
// Script for creating vertices for the attached GameObject
// Attach this script in the desired game object and invoke the functions described below to gather provenance data
//
// Link it to InfluenceController
// Link it to ProvenanceGatherer
//----------------------------------------------------------------------------------------------------------------
// Brief explanations of each function used to record provenance information:
//
//	NewActivityVertex(label, details): Creates an Activity type vertex. Custom game attributes must be inserted by 'AddAttribute' function first
//	NewAgentVertex(label, details): Creates an Agent type vertex. Custom game attributes must be inserted by 'AddAttribute' function first
//	NewEntityVertex(label, details): Creates an Entity type vertex. Custom game attributes must be inserted by 'AddAttribute' function first
//	NewVertex(): Creates an user-defined <type> vertex.
//  AddAttribute(name, value): Adds a new attribute to the attribute list. 
//                  The attribute's name and value are informed by the user and before invoking NewVertex or any of its variants.
//  PopulateAttributes(): Add unity-related attributes to the attribute list. Invoked from NewVertex or any of its variants.
//  ClearList(): Clean the attribute list for the next vertex. Invoked from NewVertex or any of its variants.
//  GenerateInfluence(tag, ID, name, value): Stores information about the current vertex that is used to influenciate other vertices
//  HasInfluence(tag): Checks if there is any influence instance of 'tag' for the current vertex and generates their appropriate edges
//  HasInfluence_ID(ID): Checks if there is any influence instance of 'ID' for the current vertex and generates their appropriate edges
//  RemoveInfluenceTag(tag): Removes all influences that belongs to the group 'tag' defined by the user
//  RemoveInfluenceTag(ID): Removes all influences of 'ID' defined by the user
//
//----------------------------------------------------------------------------------------------------------------
// How to use:
//
// 1) Invoke 'AddAttribute' to add any custom or game specific attributes that is desired to be stored
// 2) Invoke the any of the 'NewVertex' typed functions when an action is executed to store provenance information about the action
// 3) Then invoke 'HasInfluence' function for each desired 'tag' or 'ID' to check if there is anything stored that influenced the current action
// 4) If the current action can influence another action, then invoke 'GenerateInfluence' by defining its 'tag' and influence 'ID'
// 5) If any influence effect expired, then invoke 'RemoveInfluenceTag' or 'RemoveInfluenceID' to remove that influence
//=================================================================================================================

//=================================================================================================================
// *Declarations*
//=================================================================================================================
// Influence Controller Object pointer
private var influenceContainer : InfluenceController;	

// Provenance Export Object pointer	
private var provenance : ProvenanceController;	

// Last created vertex of this GameObject. It is used by Provenance Controller to link vertices
private var currentVertex : Vertex = null;	
// A list containing all attributes for the current vertex
private var attributeList : List.<Attribute> = new List.<Attribute>();

private var agentVertex : Vertex = null;

public var provenaceGameObjectName : String = "Provenance";
//=================================================================================================================
// *Functions Section*
//=================================================================================================================

//=================================================================================================================
// New Activity Vertex
// Creates a new vertex from the Activity Type
// Add the new vertex to the vertexList in the Provenance Controller
//=================================================================================================================

// Uses Time.time for the Vertex.date field and this gameobject
public function NewActivityVertex(label_ : String)
{
	NewActivityVertex((Time.time).ToString(), label_, this.gameObject);
}

// Uses Time.time for the Vertex.date field
public function NewActivityVertex(label_ : String, gameobject_ : GameObject)
{
	NewActivityVertex((Time.time).ToString(), label_, gameobject_);
}

// User defines the Vertex.date field
public function NewActivityVertex(date_ : String, label_ : String)
{
	NewActivityVertex(date_, label_, this.gameObject);
}
// User defines the Vertex.date field and the gameobject
public function NewActivityVertex(date_ : String, label_ : String, gameobject_ : GameObject)
{
	var oldTarget : String = currentVertex.type;
	PopulateAttributes(gameobject_);
	currentVertex = provenance.AddVertex(date_, "Activity", label_, attributeList, currentVertex);
	if((agentVertex != null) && (currentVertex.type != "Agent") && (oldTarget != "Agent"))
	{
		provenance.CreateProvenanceEdge(currentVertex, agentVertex);
	}
	ClearList();
}

//=================================================================================================================
// New Agent Vertex
// Creates a new vertex from the Agent Type
// Add the new vertex to the vertexList in the Provenance Controller
//=================================================================================================================
// Uses Time.time for the Vertex.date field
public function NewAgentVertex(label_ : String)
{
	findProvenanceManager();
	NewAgentVertex((Time.time).ToString(), label_, this.gameObject);
}

// Uses Time.time for the Vertex.date field
public function NewAgentVertex(label_ : String, gameobject_ : GameObject)
{
	findProvenanceManager();
	NewAgentVertex((Time.time).ToString(), label_, gameobject_);
}

// User defines the Vertex.date field
public function NewAgentVertex(date_ : String, label_ : String)
{
	findProvenanceManager();
	NewAgentVertex(date_, label_, this.gameObject);
}

// User defines the Vertex.date field
public function NewAgentVertex(date_ : String, label_ : String, gameobject_ : GameObject)
{
	findProvenanceManager();
	PopulateAttributes(gameobject_);
	currentVertex = provenance.AddVertex(date_, "Agent", label_, attributeList, null);
	agentVertex = currentVertex;
	ClearList();
}

//=================================================================================================================
// New Entity Vertex
// Creates a new vertex from the Entity Type
// Add the new vertex to the vertexList in the Provenance Controller
//=================================================================================================================
// Uses Time.time for the Vertex.date field
public function NewEntityVertex(label_ : String)
{
	NewEntityVertex((Time.time).ToString(), label_, this.gameObject);
}

// Uses Time.time for the Vertex.date field
public function NewEntityVertex(label_ : String, gameobject_ : GameObject)
{
	NewEntityVertex((Time.time).ToString(), label_, gameobject_);
}

// Uses Time.time for the Vertex.date field. Links Entity to the Agent that created it
public function NewEntityVertexFromAgent(label_ : String)
{
	NewEntityVertexFromAgent((Time.time).ToString(), label_, this.gameObject);
}

// Uses Time.time for the Vertex.date field. Links Entity to the Agent that created it
public function NewEntityVertexFromAgent(label_ : String, gameobject_ : GameObject)
{
	NewEntityVertexFromAgent((Time.time).ToString(), label_, gameobject_);
}

// Uses Time.time for the Vertex.date field
public function NewEntityVertex(date_ : String, label_ : String)
{
	NewEntityVertex(date_, label_, this.gameObject);
}

// User defines the Vertex.date field
public function NewEntityVertex(date_ : String, label_ : String, gameobject_ : GameObject)
{
	PopulateAttributes(gameobject_);
	currentVertex = provenance.AddVertex(date_, "Entity", label_, attributeList, currentVertex);
	ClearList();
}

// User defines the Vertex.date field. Links Entity to the Agent that created it
public function NewEntityVertexFromAgent(date_ : String, label_ : String)
{
	NewEntityVertexFromAgent(date_, label_, this.gameObject);
}

// User defines the Vertex.date field. Links Entity to the Agent that created it
public function NewEntityVertexFromAgent(date_ : String, label_ : String, gameobject_ : GameObject)
{
	PopulateAttributes(gameobject_);
	currentVertex = provenance.AddVertex(date_, "Entity", label_, attributeList, currentVertex);
	
	if((agentVertex != null) && (currentVertex != "Agent"))
	{
		provenance.CreateProvenanceEdge(currentVertex, agentVertex);
	}
	ClearList();
}

//=================================================================================================================
// New <Type> Vertex
// Creates a new vertex of the <Type> defined by user
// Add the new vertex to the vertexList in the Provenance Controller
//=================================================================================================================

// Uses Time.time for the Vertex.date field
public function NewVertex(type_ : String, label_ : String, gameobject_ : GameObject)
{
	NewVertex((Time.time).ToString(), type_, label_, gameobject_);
}

// User defines the Vertex.date field
public function NewVertex(date_ : String, type_ : String, label_ : String, gameobject : GameObject)
{
	PopulateAttributes(gameobject);
	currentVertex = provenance.AddVertex(date_, type_, label_, attributeList, currentVertex);
	ClearList();
}
//=================================================================================================================
// Create a new attribute for the vertex
// Attribute defined by the user
//=================================================================================================================
public function AddAttribute(name : String, att_value : String)
{
	var attribute : Attribute;
	
	attribute = new Attribute(name, att_value);
	
	this.attributeList.Add(attribute);
}

//=================================================================================================================
// Gather GameObject specific Attributes
// Add these attributes to the attributeList for the vertex
//=================================================================================================================
private function PopulateAttributes(gameobject : GameObject)
{
	var attribute : Attribute;
	
	attribute = new Attribute("ObjectName", gameobject.name.ToString());
	this.attributeList.Add(attribute);
	
	attribute = new Attribute("ObjectTag", gameobject.tag.ToString());
	this.attributeList.Add(attribute);
	
	attribute = new Attribute("ObjectID", gameobject.GetInstanceID().ToString());
	this.attributeList.Add(attribute);
	
	attribute = new Attribute("ObjectPosition_X", gameobject.transform.position.x.ToString());
	this.attributeList.Add(attribute);
	
	attribute = new Attribute("ObjectPosition_Y", gameobject.transform.position.y.ToString());
	this.attributeList.Add(attribute);
	
	attribute = new Attribute("ObjectPosition_Z", gameobject.transform.position.z.ToString());
	this.attributeList.Add(attribute);
}

//=================================================================================================================
// Clear the list of attributes for the next vertex
// Function invoked after current vertex is added to the vertex list
//=================================================================================================================
private function ClearList()
{
	this.attributeList = new List.<Attribute>();
}

//=================================================================================================================
// *Influence-Related Section*
//=================================================================================================================

//=================================================================================================================
// Generate an influence for this vertex
//=================================================================================================================
// Legend: M = Missable / C = Consumable / E = Expirable

public function GenerateInfluence(tag : String, ID : String, influenceName : String, influenceValue : String)
{
	GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, false, 10, null, -1);
}

public function GenerateInfluenceC(tag : String, ID : String, influenceName : String, influenceValue : String, quantity : int)
{
	GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, true, quantity, null, -1);
}

public function GenerateInfluenceM(tag : String, ID : String, influenceName : String, influenceValue : String, target : GameObject)
{
	GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, false, 10, target, -1);
}	

public function GenerateInfluenceE(tag : String, ID : String, influenceName : String, influenceValue : String, expiration : float)
{
	GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, false, 10, null, expiration);
}

public function GenerateInfluenceCE(tag : String, ID : String, influenceName : String, influenceValue : String, quantity : int, expiration : float)
{
	GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, true, quantity, null, expiration);
}

public function GenerateInfluenceMC(tag : String, ID : String, influenceName : String, influenceValue : String, quantity : int, target : GameObject)
{
	GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, true, quantity, target, -1);
}

public function GenerateInfluenceME(tag : String, ID : String, influenceName : String, influenceValue : String, target : GameObject, expiration : float)
{
	GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, false, 10, target, expiration);
}

public function GenerateInfluenceMCE(tag : String, ID : String, influenceName : String, influenceValue : String, consumable : boolean, quantity : int, target : GameObject, expiration : float)
{
	influenceContainer.CreateInfluence(tag, ID, currentVertex.ID, influenceName, influenceValue, true, quantity, target, expiration);
}
//=================================================================================================================
// Checks if current vertex was influenced by any other vertex
// If so, consume the influence and generate the appropriate edge connecting both vertices
// Need to check all influences, since it can have more than one at the same time
// Returns a list of all influence's ID used
//=================================================================================================================
// By 'tag'
public function HasInfluence(tag : String)
{
	if(currentVertex != null)
		influenceContainer.WasInfluencedByTag(tag, currentVertex.ID);
}

// By 'ID'
public function HasInfluence_ID(ID : String)
{
	if(currentVertex != null)
		influenceContainer.WasInfluencedByID(ID, currentVertex.ID);
}

//=================================================================================================================
// Remove all influences from 'tag'
//=================================================================================================================
public function RemoveInfluenceTag(tag : String)
{
	influenceContainer.RemoveInfluenceByTag(tag);
}

//=================================================================================================================
// Remove all influences with 'ID'
//=================================================================================================================
public function RemoveInfluenceID(ID : String)
{
	influenceContainer.RemoveInfluenceByID(ID);
}

//=================================================================================================================
// Gets and Sets
//=================================================================================================================
public function GetCurrentVertex()
{
	return currentVertex;
}

public function SetCurrentVertex(vertex : Vertex)
{
	currentVertex = vertex;
}

public function GetAgentVertex()
{
	return agentVertex;
}

public function SetAgentVertex(vertex : Vertex)
{
	agentVertex = vertex;
}

function Awake()
{
	var ProvObj : GameObject = GameObject.Find(provenaceGameObjectName);
	influenceContainer = ProvObj.GetComponent(InfluenceController);  
	provenance = ProvObj.GetComponent(ProvenanceController);  
	
}

function findProvenanceManager() {
	if ( provenance == null) {
		var ProvObj : GameObject = GameObject.Find(provenaceGameObjectName);
		influenceContainer = ProvObj.GetComponent(InfluenceController);  
		provenance = ProvObj.GetComponent(ProvenanceController);  
	}
}

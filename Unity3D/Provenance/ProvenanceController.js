#pragma strict

//=================================================================================================================
// Script for storing vertices and edges
// Attach this script in an Empty GameObject that is never destroyed during the game
//(In the same GameObject for InfluenceController)
//
// Uses one ArrayList for vertices and another for edges
// All functions are automatically invoked and controlled by 'ExtractProvenance' script or 'InfluenceController' script
//=================================================================================================================


//=================================================================================================================
// *Declarations Section*
//=================================================================================================================
private var vertexList : List.<Vertex> = new List.<Vertex>();    			
private var edgeList : List.<Edge> = new List.<Edge>();		
		

//=================================================================================================================
// *Functions Section*
//=================================================================================================================

//=================================================================================================================
// Add Vertex to the vertexList
// Create a new Edge connecting the new vertex with the last one
// Add edge to edgeList
// Return new vertex in order to update the caller
//=================================================================================================================
public function AddVertex(date_ : String, type_ : String, label_ : String, attribute_ : List.<Attribute>, details_ : String, target : Vertex) : Vertex
{
	var source : Vertex = new Vertex(NewVertexID(), date_, type_, label_, attribute_, details_);

	vertexList.Add(source);
	
	// If target is not null, then create an edge connecting both vertices
	if(target != null)
	{
		CreateProvenanceEdge(source, target);	// Create an edge using PROV definitions
	}
	
	return source;
}

//=================================================================================================================
// Add Edge to the edgeList
//=================================================================================================================
private function AddEdge(t : Edge)
{
	edgeList.Add(t);
}

//=================================================================================================================
// Generate a new ID for Edge
//=================================================================================================================
private function NewEdgeID() : String
{
	return "edge_" + edgeList.Count;
}

//=================================================================================================================
// Generate a new ID for vertex
//=================================================================================================================
private function NewVertexID() : String
{
	return "vertex_" + vertexList.Count;
}

//=================================================================================================================
// Create a new edge connecting Source to Target
// Defines the edge provenance label according to Source and Target types
// Uses PROV edge definitions for label
// Add the edge to the edgeList
//=================================================================================================================
public function CreateProvenanceEdge(source: Vertex, target : Vertex)
{
	// Default edge label
	var newEdge : Edge = new Edge(NewEdgeID(), "Neutral", "WasAssociatedTo", "", source.ID, target.ID);
	
	// Try to classify using PROV definitions
	if(source.type == "Activity")
	{
		if(target.type == "Activity")
		{
			newEdge = new Edge(NewEdgeID(), "Neutral", "WasInformedBy", "", source.ID, target.ID);
		}
		else if(target.type == "Agent")
		{
			newEdge = new Edge(NewEdgeID(), "Neutral", "WasAssociatedTo", "", source.ID, target.ID);
		}
		else if(target.type == "Entity")
		{
			newEdge = new Edge(NewEdgeID(), "Neutral", "Used", "", source.ID, target.ID);
		}
	}
	else if(source.type == "Agent")
	{
		if(target.type == "Activity")
		{
			newEdge = new Edge(NewEdgeID(), "Neutral", "WasInfluencedBy", "", source.ID, target.ID);
		}
		else if(target.type == "Agent")
		{
			newEdge = new Edge(NewEdgeID(), "Neutral", "ActedOnBehalfOf", "", source.ID, target.ID);
		}
		else if(target.type == "Entity")
		{
			newEdge = new Edge(NewEdgeID(), "Neutral", "WasInfluencedBy", "", source.ID, target.ID);
		}
	}
	else if(source.type == "Entity")
	{
		if(target.type == "Activity")
		{
			newEdge = new Edge(NewEdgeID(), "Neutral", "WasGeneratedBy", "", source.ID, target.ID);
		}
		else if(target.type == "Agent")
		{
			newEdge = new Edge(NewEdgeID(), "Neutral", "WasAttributedTo", "", source.ID, target.ID);
		}
		else if(target.type == "Entity")
		{
			newEdge = new Edge(NewEdgeID(), "Neutral", "WasDerivedFrom", "", source.ID, target.ID);
		}
	}

	// Add the edge to the edgeList
	AddEdge(newEdge);	
}

//=================================================================================================================
// Create a new edge connecting Source to Target with a value
// This edge is known as influence edge
// Defines the edge provenance label as "WasInfluencedBy"
// Add the edge to the edgeList
//=================================================================================================================
public function CreateInfluenceEdge(targetID : String, sourceID : String, infName : String, infValue : String) : String
{
	var newEdge : Edge = new Edge(NewEdgeID(), infName, "WasInfluencedBy", infValue, sourceID, targetID);
	AddEdge(newEdge);
	
	return newEdge.ID;
}

public function UpdateInfluenceEdge(targetID : String, sourceID : String, infName : String, infValue : String, edgeID : String)
{
	var i : int;
	// Search for edge in edgeList
	for (i = 0; i < edgeList.Count; i++)
	{
		if(edgeList[i].ID == edgeID)
		{
			edgeList[i] = new Edge(edgeID, infName, "WasInfluencedBy", infValue, sourceID, targetID);
			// Found. No need to keep searching
			i = edgeList.Count + 1;
		}
	}
}
//=================================================================================================================
// Export all Provenance information gathered to a XML file
//=================================================================================================================
public function Save(filename : String)
{
	var provContainer : ProvenanceContainer = new ProvenanceContainer(vertexList, edgeList);
	//provContainer.Save(Path.Combine(Application.persistentDataPath, "provenancedata.xml"));
	provContainer.Save(Path.Combine(Application.dataPath, filename + ".xml"));
	Debug.Log(Application.dataPath);
	//provContainer.Save("./Files/" + filename + ".xml");
}

//=================================================================================================================
// Load all previous Provenance information gathered from a XML file
//=================================================================================================================
public function Load(filename : String)
{
	//var provContainer : ProvenanceContainer = ProvenanceContainer.Load("./Files/" + filename + ".xml");
	var provContainer : ProvenanceContainer = ProvenanceContainer.Load(Path.Combine(Application.dataPath, filename + ".xml"));
	
}
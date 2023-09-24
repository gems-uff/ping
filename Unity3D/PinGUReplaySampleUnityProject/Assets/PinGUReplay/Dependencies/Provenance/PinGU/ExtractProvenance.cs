using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace PinGU { 
    public class ExtractProvenance : MonoBehaviour
    {
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
        public InfluenceController influenceContainer;

        // Provenance Export Object pointer	
        public ProvenanceController provenance;

        // Last created vertex of this GameObject. It is used by Provenance Controller to link vertices
        private Vertex currentVertex = null;
        // A list containing all attributes for the current vertex
        private List<Attribute> attributeList = new List<Attribute>();

        private Vertex agentVertex = null;

        //=================================================================================================================
        // *Functions Section*
        //=================================================================================================================

        //=================================================================================================================
        // New Activity Vertex
        // Creates a new vertex from the Activity Type
        // Add the new vertex to the vertexList in the Provenance Controller
        //=================================================================================================================

        // Uses Time.time for the Vertex.date field and this gameobject
        public void NewActivityVertex(string label_)
        {
            NewActivityVertex((Time.time).ToString(CultureInfo.InvariantCulture), label_, this.gameObject);
        }

        // Uses Time.time for the Vertex.date field
        public void NewActivityVertex(string label_, GameObject gameobject_)
        {
            NewActivityVertex((Time.time).ToString(CultureInfo.InvariantCulture), label_, gameobject_);
        }

        // User defines the Vertex.date field
        public void NewActivityVertex(string date_, string label_)
        {
            NewActivityVertex(date_, label_, this.gameObject);
        }
        // User defines the Vertex.date field and the gameobject
        public void NewActivityVertex(string date_, string label_, GameObject gameobject_)
        {
            string oldTarget = currentVertex.type;
            PopulateAttributes(gameobject_);
            currentVertex = provenance.AddVertex(date_, "Activity", label_, attributeList, currentVertex);
            if ((agentVertex != null) && (currentVertex.type != "Agent") && (oldTarget != "Agent"))
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
        public void NewAgentVertex(string label_)
        {
            NewAgentVertex((Time.time).ToString(CultureInfo.InvariantCulture), label_, this.gameObject);
        }

        // Uses Time.time for the Vertex.date field
        public void NewAgentVertex(string label_, GameObject gameobject_)
        {
            NewAgentVertex((Time.time).ToString(CultureInfo.InvariantCulture), label_, gameobject_);
        }

        // User defines the Vertex.date field
        public void NewAgentVertex(string date_, string label_)
        {
            NewAgentVertex(date_, label_, this.gameObject);
        }

        // User defines the Vertex.date field
        public void NewAgentVertex(string date_, string label_, GameObject gameobject_)
        {
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
        public void NewEntityVertex(string label_)
        {
            NewEntityVertex((Time.time).ToString(CultureInfo.InvariantCulture), label_, this.gameObject);
        }

        // Uses Time.time for the Vertex.date field
        public void NewEntityVertex(string label_, GameObject gameobject_)
        {
            NewEntityVertex((Time.time).ToString(CultureInfo.InvariantCulture), label_, gameobject_);
        }

        // Uses Time.time for the Vertex.date field. Links Entity to the Agent that created it
        public void NewEntityVertexFromAgent(string label_)
        {
            NewEntityVertexFromAgent((Time.time).ToString(CultureInfo.InvariantCulture), label_, this.gameObject);
        }

        // Uses Time.time for the Vertex.date field. Links Entity to the Agent that created it
        public void NewEntityVertexFromAgent(string label_, GameObject gameobject_)
        {
            NewEntityVertexFromAgent((Time.time).ToString(CultureInfo.InvariantCulture), label_, gameobject_);
        }

        // Uses Time.time for the Vertex.date field
        public void NewEntityVertex(string date_, string label_)
        {
            NewEntityVertex(date_, label_, this.gameObject);
        }

        // User defines the Vertex.date field
        public void NewEntityVertex(string date_, string label_, GameObject gameobject_)
        {
            PopulateAttributes(gameobject_);
            currentVertex = provenance.AddVertex(date_, "Entity", label_, attributeList, currentVertex);
            ClearList();
        }

        // User defines the Vertex.date field. Links Entity to the Agent that created it
        public void NewEntityVertexFromAgent(string date_, string label_)
        {
            NewEntityVertexFromAgent(date_, label_, this.gameObject);
        }

        // User defines the Vertex.date field. Links Entity to the Agent that created it
        public void NewEntityVertexFromAgent(string date_, string label_, GameObject gameobject_)
        {
            PopulateAttributes(gameobject_);
            currentVertex = provenance.AddVertex(date_, "Entity", label_, attributeList, currentVertex);

            if ((agentVertex != null) && (currentVertex.label != "Agent"))
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
        public void NewVertex(string type_, string label_, GameObject gameobject_)
        {
            NewVertex((Time.time).ToString(CultureInfo.InvariantCulture), type_, label_, gameobject_);
        }

        // User defines the Vertex.date field
        public void NewVertex(string date_, string type_, string label_, GameObject gameobject)
        {
            PopulateAttributes(gameobject);
            currentVertex = provenance.AddVertex(date_, type_, label_, attributeList, currentVertex);
            ClearList();
        }
        //=================================================================================================================
        // Create a new attribute for the vertex
        // Attribute defined by the user
        //=================================================================================================================
        public void AddAttribute(string name, string att_value)
        {
            Attribute attribute;

            attribute = new Attribute(name, att_value);

            this.attributeList.Add(attribute);
        }

        //=================================================================================================================
        // Gather GameObject specific Attributes
        // Add these attributes to the attributeList for the vertex
        //=================================================================================================================
        private void PopulateAttributes(GameObject gameobject)
        {
            Attribute attribute;

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
        private void ClearList()
        {
            this.attributeList = new List<Attribute>();
        }

        //=================================================================================================================
        // *Influence-Related Section*
        //=================================================================================================================

        //=================================================================================================================
        // Generate an influence for this vertex
        //=================================================================================================================
        // Legend: M = Missable / C = Consumable / E = Expirable

        public void GenerateInfluence(string tag, string ID, string influenceName, string influenceValue)
        {
            GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, false, 10, null, -1);
        }

        public void GenerateInfluenceC(string tag, string ID, string influenceName, string influenceValue, int quantity)
        {
            GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, true, quantity, null, -1);
        }

        public void GenerateInfluenceM(string tag, string ID, string influenceName, string influenceValue, GameObject target)
        {
            GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, false, 10, target, -1);
        }

        public void GenerateInfluenceE(string tag, string ID, string influenceName, string influenceValue, float expiration)
        {
            GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, false, 10, null, expiration);
        }

        public void GenerateInfluenceCE(string tag, string ID, string influenceName, string influenceValue, int quantity, float expiration)
        {
            GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, true, quantity, null, expiration);
        }

        public void GenerateInfluenceMC(string tag, string ID, string influenceName, string influenceValue, int quantity, GameObject target)
        {
            GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, true, quantity, target, -1);
        }

        public void GenerateInfluenceME(string tag, string ID, string influenceName, string influenceValue, GameObject target, float expiration)
        {
            GenerateInfluenceMCE(tag, ID, influenceName, influenceValue, false, 10, target, expiration);
        }

        public void GenerateInfluenceMCE(string tag, string ID, string influenceName, string influenceValue, bool consumable, int quantity, GameObject target, float expiration)
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
        public void HasInfluence(string tag)
        {
            if (currentVertex != null)
                influenceContainer.WasInfluencedByTag(tag, currentVertex.ID);
        }

        // By 'ID'
        public void HasInfluence_ID(string ID)
        {
            if (currentVertex != null)
                influenceContainer.WasInfluencedByID(ID, currentVertex.ID);
        }

        //=================================================================================================================
        // Remove all influences from 'tag'
        //=================================================================================================================
        public void RemoveInfluenceTag(string tag)
        {
            influenceContainer.RemoveInfluenceByTag(tag);
        }

        //=================================================================================================================
        // Remove all influences with 'ID'
        //=================================================================================================================
        public void RemoveInfluenceID(string ID)
        {
            influenceContainer.RemoveInfluenceByID(ID);
        }

        //=================================================================================================================
        // Gets and Sets
        //=================================================================================================================
        public Vertex GetCurrentVertex()
        {
            return currentVertex;
        }

        public void SetCurrentVertex(Vertex vertex)
        {
            currentVertex = vertex;
        }

        public Vertex GetAgentVertex()
        {
            return agentVertex;
        }

        public void SetAgentVertex(Vertex vertex)
        {
            agentVertex = vertex;
        }

        void Awake()
        {
            GameObject ProvObj = GameObject.FindGameObjectWithTag("ProvenanceCapture");
            influenceContainer = ProvObj.GetComponent<InfluenceController>();
            provenance = ProvObj.GetComponent<ProvenanceController>();

        }

    }
}
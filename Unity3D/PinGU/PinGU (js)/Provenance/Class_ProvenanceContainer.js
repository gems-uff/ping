#pragma strict

//===================================================================================================================
// 'ProvenanceContainer' Class Definition
// This script is to define the ProvenanceContainer class
// Do not attach this script in any GameObject
// It is only necessary to be on your resources folder
// The 'ProvenanceContainer' class is used for the Provenance-Scripts
// It is responsible for exporting provenance information into a XML file
//===================================================================================================================

import System.Collections.Generic;
import System.Xml.Serialization;
import System.Xml;
import System.IO;
 
 @XmlRoot("provenancedata")
 public class ProvenanceContainer
 {
 	@XmlArray("vertices")
 	@XmlArrayItem("vertex")
 	public var vertexList : List.<Vertex>; 
 	
 	@XmlArray("edges")
 	@XmlArrayItem("edge")   			
	public var edgeList : List.<Edge>; 
	
	//================================================================================================================
	// Empty Constructor
	//================================================================================================================
	public function ProvenanceContainer()
	{
		vertexList = new List.<Vertex>();    			
		edgeList = new List.<Edge>();	
	}
	
	//================================================================================================================
	// Constructor
	//================================================================================================================
	public function ProvenanceContainer(vList : List.<Vertex>, eList : List.<Edge>)
	{
		vertexList = vList;
		edgeList = eList;
	}
 
 	public function Save(path : String)
 	{
 		var serializer : XmlSerializer = new XmlSerializer(ProvenanceContainer);
 		var stream : Stream = new FileStream(path, FileMode.Create);
 		serializer.Serialize(stream, this);
 		stream.Close();
 	}
 
 	public static function Load(path : String):ProvenanceContainer 
 	{
 		var serializer : XmlSerializer = new XmlSerializer(ProvenanceContainer);
 		var stream : Stream = new FileStream(path, FileMode.Open);
 		var result : ProvenanceContainer = serializer.Deserialize(stream) as ProvenanceContainer;
 		stream.Close();
 		return result;
 	}
 
	//Loads the xml directly from the given string. Useful in combination with www.text.
	public static function LoadFromText(text : String):ProvenanceContainer
	{
		var serializer : XmlSerializer = new XmlSerializer(ProvenanceContainer);
		return serializer.Deserialize(new StringReader(text)) as ProvenanceContainer;
	}
 }
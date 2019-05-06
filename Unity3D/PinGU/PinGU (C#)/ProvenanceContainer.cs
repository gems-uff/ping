using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace PinGU { 
    //===================================================================================================================
    // 'ProvenanceContainer' Class Definition
    // This script is to define the ProvenanceContainer class
    // Do not attach this script in any GameObject
    // It is only necessary to be on your resources folder
    // The 'ProvenanceContainer' class is used for the Provenance-Scripts
    // It is responsible for exporting provenance information into a XML file
    //===================================================================================================================

    [XmlRoot("provenancedata")]
    public class ProvenanceContainer
    {
        [XmlArray("vertices")]
        [XmlArrayItem("vertex")]
        public List<Vertex> vertexList;

        [XmlArray("edges")]
        [XmlArrayItem("edge")]
        public List<Edge> edgeList;

        //================================================================================================================
        // Empty Constructor
        //================================================================================================================
        public ProvenanceContainer()
        {
            vertexList = new List<Vertex>();
            edgeList = new List<Edge>();
        }

        //================================================================================================================
        // Constructor
        //================================================================================================================
        public ProvenanceContainer(List<Vertex> vList, List<Edge> eList)
        {
            vertexList = vList;
            edgeList = eList;
        }

        public void Save(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ProvenanceContainer));
            Stream stream = new FileStream(path, FileMode.Create);
            serializer.Serialize(stream, this);
            stream.Close();
        }

        public static ProvenanceContainer Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ProvenanceContainer));
            Stream stream = new FileStream(path, FileMode.Open);
            ProvenanceContainer result = serializer.Deserialize(stream) as ProvenanceContainer;
            stream.Close();
            return result;
        }

        //Loads the xml directly from the given string. Useful in combination with www.text.
        public static ProvenanceContainer LoadFromText(string text)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ProvenanceContainer));
            return serializer.Deserialize(new StringReader(text)) as ProvenanceContainer;
        }
    }
}
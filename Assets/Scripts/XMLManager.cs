using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Xml;
using System.Xml.Serialization;
using System.IO;

public class XMLManager : MonoBehaviour {

	public static XMLManager ins;

	void Awake () {
		ins = this;
	} 

	public navMeshData meshData = new navMeshData ();

	//save function
	public void saveItems () {
		//open a new xml file
		XmlSerializer serializer = new XmlSerializer (typeof (navMeshData));
		FileStream stream = new FileStream (Application.dataPath + "/StreamingFiles/XML/DesertNavMeshData.xml", FileMode.Create);

		//add data
		serializer.Serialize (stream, meshData);

		stream.Close (); 
	}

	//load function
	public void loadData () {
		//open a new xml file
		XmlSerializer serializer = new XmlSerializer (typeof (navMeshData));
		FileStream stream = new FileStream (Application.dataPath + "/StreamingFiles/XML/DesertNavMeshData.xml", FileMode.Open);

		meshData = (navMeshData) serializer.Deserialize (stream);

		stream.Close (); 
	}
}

[System.Serializable]
public class navMeshData {
	public Vector2[][] mesh;
	public Segment[] portals;
	public Node[] nodes;
}

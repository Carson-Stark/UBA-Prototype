using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class NavMesh : MonoBehaviour {

	public bool generateNewNavMesh;
	public GameObject mapBounds;
	public GameObject[] holes;
	public GameObject[] buildings;
	public GameObject test;

	public Polygon mapPoly;
	public List<Polygon> holePolys = new List<Polygon> ();

	public Polygon[] mapPolygons;
	public Segment[] portals;
	public Node[] nodes;

	void Start () {
		if (generateNewNavMesh) {
			Vector2[] mapPoints = localToWorldPoints (mapBounds.GetComponent<PolygonCollider2D> ().GetPath (1), mapBounds.transform); 
			mapPoly = new Polygon (new LinkedList<Vector2> (mapPoints));
			mapPoly = Geometry.simplifyPolygon (mapPoly, 0.5f); 
			mapPoly = Geometry.inflatePolygon (mapPoly, 0.8f);

			foreach (GameObject hole in holes) {
				List<Vector2> holePoints = localToWorldPoints (hole.GetComponent<PolygonCollider2D> ().GetPath (0), hole.transform).ToList ();
				Polygon holePoly = new Polygon (new LinkedList<Vector2> (holePoints));
				holePoly = Geometry.simplifyPolygon (holePoly, 0.3f);
				holePoly = Geometry.inflatePolygon (holePoly, 0.8f); 
				holePolys.Add (holePoly); 
			}

			foreach (GameObject building in buildings) {
				for (int i = 0; i < building.GetComponent<PolygonCollider2D> ().pathCount; i++) { 
					List<Vector2> bPoints = localToWorldPoints (building.GetComponent<PolygonCollider2D> ().GetPath (i), building.transform).ToList (); 

					Polygon bPoly = new Polygon (new LinkedList<Vector2> (bPoints));
					bPoly = Geometry.simplifyPolygon (bPoly, 0.3f);
					bPoly = Geometry.inflatePolygon (bPoly, 0.7f); 
					holePolys.Add (bPoly); 
				}
			}
			
			Polygon[] triangles = Geometry.triangulate (mapPoly, holePolys.ToArray (), 60, test); 
			mapPolygons = Geometry.polygonPartion (triangles); 
			nodes = generateNodes (mapPolygons, out portals);

			XMLManager.ins.meshData.mesh = serializePolygons (mapPolygons);
			XMLManager.ins.meshData.portals = portals;
			XMLManager.ins.meshData.nodes = nodes;
			XMLManager.ins.saveItems ();
		}
		else {
			XMLManager.ins.loadData ();
			mapPolygons = deserializePolygons (XMLManager.ins.meshData.mesh);
			portals = XMLManager.ins.meshData.portals; 
			nodes = XMLManager.ins.meshData.nodes;
		}
	}

	Node[] generateNodes (Polygon[] mesh, out Segment[] segments) {
		List<Segment> segs = new List<Segment> ();
		List<Node> nds = new List<Node> ();

		foreach (Polygon p1 in mesh) {
			foreach (Polygon p2 in mesh) {
				if (p1 == p2)
					continue;

				//find shared edge
				List<Vector2> sharedVerts = new List<Vector2> ();
				foreach (Vector2 v in p1.verticies) {
					if (p2.verticies.Contains (v))
						sharedVerts.Add (v); 
				}

				//create a new node
				if (sharedVerts.Count > 1) {
					Segment segment = new Segment (sharedVerts [0], sharedVerts [1]);
					if (!segs.Contains (segment)) { 
						segs.Add (segment); 
						Node node = new Node (segment.midpoint, segs.Count - 1);
						node.polygons.AddRange (new List<int> { Array.IndexOf (mesh, p1), Array.IndexOf (mesh, p2)}); 
						nds.Add (node); 
					}
				}
			}
		}

		//assign neighbors
		foreach (Node n1 in nds) {
			foreach (Node n2 in nds) {
				if (!n1.Equals (n2) && (n1.polygons.Contains (n2.polygons[0]) || n1.polygons.Contains (n2.polygons[1])))
					n1.neighbors.Add (n2.segment); 
			}
		}

		segments = segs.ToArray ();
		return nds.ToArray ();
	}

	Vector2[][] serializePolygons (Polygon[] polygons) {
		List<Vector2[]> list = new List<Vector2[]> ();

		foreach (Polygon poly in polygons)
			list.Add (poly.verticies.ToArray()); 

		return list.ToArray (); 
	}

	Polygon[] deserializePolygons (Vector2[][] polygons) {
		List<Polygon> polys = new List<Polygon> ();

		foreach (Vector2[] poly in polygons)
			polys.Add (new Polygon (new LinkedList<Vector2> (poly))); 

		return polys.ToArray (); 
	}

	Vector2[] localToWorldPoints (Vector2[] points, Transform trans) {
		Vector2[] newPoints = new Vector2[points.Length];
		for (int i = 0; i < points.Length; i++) {
			newPoints [i] = trans.TransformPoint (points [i]); 
		}

		return newPoints;
	}

	void OnDrawGizmos () {
		try {
			foreach (Polygon p in mapPolygons) {
				Vector2 lastV = p.verticies.Last.Value;
				foreach (Vector2 v in p.verticies) {
					Gizmos.DrawLine (lastV, v); 
					lastV = v;
				}
			}
		}
		catch {
		} 
	}
}

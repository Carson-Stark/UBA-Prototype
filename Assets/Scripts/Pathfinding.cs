using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class Pathfinding : MonoBehaviour {

	public Transform seeker;
	public Transform target;

	NavMesh nvg;
	List<Node> nodes;

	Segment[] polygonPath; 
	Vector2[] path;

	void Start () {
		nvg = GetComponent<NavMesh> (); 
		nodes = nvg.nodes.ToList ();
	}

	void Update () {
		Vector2 mousePosition = Camera.main.ScreenToWorldPoint (Input.mousePosition);
		if (Input.GetMouseButton (0))
			seeker.position = mousePosition;

		target.position = mousePosition;

		findPath ((Vector2)seeker.position, (Vector2)target.position); 
	}

	public void findPath (Vector2 start, Vector2 goal) {
		nodes = new List<Node> (nvg.nodes);

		//A star:

		Node s = new Node (start);
		Node g = new Node (goal);
		int goalPoly = 0;

		foreach (Polygon p in nvg.mapPolygons) {
			if (Geometry.pointInsidePolygon (p, start)) {
				s.polygons.Add (Array.IndexOf (nvg.mapPolygons, p)); 
			}

			if (Geometry.pointInsidePolygon (p, goal)) {
				goalPoly = Array.IndexOf (nvg.mapPolygons, p);
				g.polygons.Add (Array.IndexOf (nvg.mapPolygons, p)); 
			}
		}

		if (s.polygons.Count < 1 || g.polygons.Count < 1) {
			return;
		}

		foreach (Node n in nodes) {
			//find start neighbors
			if (n.polygons.Except (s.polygons).ToArray ().Length < 2) {
				n.neighbors.Add (s.segment); 
				s.neighbors.Add (n.segment); 
			}

			//find goal neighbors
			if (n.polygons.Except (g.polygons).ToArray ().Length < 2) {
				n.neighbors.Add (g.segment); 
				g.neighbors.Add (n.segment);  
			}
		}

		nodes.Add (s); 

		s.H_cost = Vector2.Distance (s.point, goal);
		s.G_cost = 0;

		//nodes to evaluate 
		Heap<Node> open = new Heap<Node> (nodes.Count);
		//nodes already evaluated
		HashSet<Node> closed = new HashSet<Node> ();

		open.Add (s); 

		while (open.Count > 0) {
			//find the node with the lowest F cost
			Node current = open.RemoveFirst (); 
			closed.Add (current);

			//we reached the goal!
			if (current.polygons.Contains (goalPoly)) {
				polygonPath = RetracePath (s, current); 
				path = Funnel (start, goal, polygonPath.ToArray ()).ToArray (); 
				return;
			}
				
			foreach (int nIndex in current.neighbors) {
				Node n = nodes [nIndex];

				if (closed.Contains (n))
					continue;

				//if the new path to the neighbor is shorter or it is not in the open set :
				//assign/reassing the F_cost and add it to the open set if needed
				float newMovementCostToNeighbor = current.G_cost + Vector2.Distance (current.point, n.point);
				if (newMovementCostToNeighbor < n.G_cost || !open.Contains (n)) {
					n.G_cost = newMovementCostToNeighbor;
					n.H_cost = Vector2.Distance (n.point, goal);
					n.parent = current;

					if (!open.Contains (n))
						open.Add (n); 
				}
			}
		}
	}

	Segment[] RetracePath (Node start, Node goal) {
		List<Segment> path = new List<Segment> ();

		Node current = goal;
		while (current != start) {
			path.Add (nvg.portals[current.segment]);
			current = current.parent;
		} 

		path.Reverse (); 
		return path.ToArray ();
	}

	List<Vector2> Funnel (Vector2 start, Vector2 goal, Segment[] path) {
		List<Vector2> smoothedPath = new List<Vector2> ();

		List<Vector2> portalsLeft = new List<Vector2> ();
		List<Vector2> portalsRight = new List<Vector2> ();

		Vector2 lastMid = start;
		foreach (Segment seg in path) {
			if (Geometry.orientation (lastMid, seg.midpoint, seg.a) == 1)
				portalsRight.Add (seg.a);
			else 
				portalsLeft.Add (seg.a);  
			
			if (Geometry.orientation (lastMid, seg.midpoint, seg.b) == 1)
				portalsRight.Add (seg.b);
			else
				portalsLeft.Add (seg.b); 
			
			lastMid = seg.midpoint;
		}

		portalsLeft.Add (goal);
		portalsRight.Add (goal); 

		smoothedPath.Add (start); 
		Vector2 apex = start;
			
		Vector2 funnelRight = portalsRight [0];
		Vector2 funnelLeft = portalsLeft [0];
		for (int i = 1; i < portalsRight.Count; i++) {
			if (portalsLeft [i] != portalsLeft [i-1]) {
				//if the left side crossed the right side, add the turning point to the smoothed path
				if (Geometry.orientation (funnelRight, apex, portalsLeft [i]) == 2) {
					Debug.Log ("lc"); 
					smoothedPath.Add (funnelRight); 
					apex = funnelRight;  

					for (int j = portalsRight.IndexOf (funnelRight) + 1; j <= portalsRight.Count; j++) {
						if (portalsRight [j] != funnelRight) {
							funnelRight = portalsRight [j];
							i = j;
							break;
						}
					}
				}

				//if the funnel will shrink, move to the next point
				if (Vector2.Angle (funnelLeft - apex, funnelRight - apex) >= Vector2.Angle (portalsLeft [i] - apex, funnelRight - apex)) 
					funnelLeft = portalsLeft [i];
				else
					Debug.Log ("Before: " + Vector2.Angle (funnelLeft - apex, funnelRight - apex) + " After: " + Vector2.Angle (portalsLeft [i] - apex, funnelRight - apex)); 
			}

			if (portalsRight [i] != portalsRight [i-1]) {
				//if the right side crossed the left side, add the turning point to the smoothed path
				if (Geometry.orientation (funnelLeft, apex, portalsRight [i]) == 1) {
					Debug.Log ("rc"); 
					smoothedPath.Add (funnelLeft);
					apex = funnelLeft;

					for (int j = portalsLeft.IndexOf (funnelLeft) ; j <= portalsLeft.Count; j++) {
						if (portalsLeft [j] != funnelLeft) {
							funnelLeft = portalsLeft [j];
							i = j;
							break;
						}
					}
				}

				//if the funnel will shrink, move to the next point
				if (Vector2.Angle (funnelLeft - apex, funnelRight - apex) >= Vector2.Angle (funnelLeft - apex, portalsRight [i] - apex))
					funnelRight = portalsRight [i];
				else
					Debug.Log ("Before: " + Vector2.Angle (funnelLeft - apex, funnelRight - apex) + " After: " + Vector2.Angle (funnelLeft - apex, portalsRight [i] - apex)); 
			}

			Debug.Log ("FL: " + funnelLeft + " FR: " + funnelRight); 
			Debug.Log ("L: " + portalsLeft [i] + " R: " + portalsRight [i]); 
		}
	
		smoothedPath.Add (goal); 

		return smoothedPath;
	}

	void OnDrawGizmos () {
		try {
			Gizmos.color = Color.black;
			Vector2 last = path[0];
			foreach (Vector2 p in path) {
				if (p != path[0])
					Gizmos.DrawLine (last, p); 

				last = p;
			}

			Gizmos.color = Color.gray;
			Segment last1 = polygonPath[0];
			foreach (Segment p in polygonPath) {
				if (p != polygonPath[0])
					Gizmos.DrawLine (last1.midpoint, p.midpoint); 

				last1 = p;
			}
		}
		catch {

		}
	}
}

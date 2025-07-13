using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Geometry {
	
	#region simplifyPolygon

	//removes unessesary verticies from a polygon : Visvalingam’s algorithm
	public static Polygon simplifyPolygon (Polygon polygon, float minSignificance) {
		Polygon simplifiedPoly = new Polygon (polygon.verticies);
		LinkedList<vertice> verts = new LinkedList<vertice> ();

		//populate verts
		LinkedListNode<Vector2> current = polygon.verticies.First;
		while (current != null) {
			vertice v = new vertice (current.Value, areaOfTriangle (current.PreviousOrLast ().Value, current.Value, current.NextOrFirst ().Value));
			verts.AddFirst (v); 

			current = current.Next;
		}

		//recusively remove the least significant vertice
		bool noVertsLessThanMinSig = false;
		while (!noVertsLessThanMinSig) {
			//find the vertice with the least significance
			LinkedListNode<vertice> vertToRemove = verts.First;
			LinkedListNode<vertice> cur = verts.First;
			while (cur != null) {
				if (cur.Value.significance < vertToRemove.Value.significance)
					vertToRemove = cur; 

				cur = cur.Next;
			}

			//remove the vertice and update the significance of adjacient verticies
			if (vertToRemove.Value.significance < minSignificance && simplifiedPoly.verticies.Count > 1) {
				LinkedListNode<vertice> previous = vertToRemove.PreviousOrLast (); 
				LinkedListNode<vertice> next = vertToRemove.NextOrFirst (); 

				verts.Remove (vertToRemove); 
				simplifiedPoly.verticies.Remove (simplifiedPoly.verticies.Find (vertToRemove.Value.point)); 
			
				vertice newPVert = new vertice (previous.Value.point, areaOfTriangle (previous.PreviousOrLast ().Value.point, previous.Value.point, previous.NextOrFirst ().Value.point));
				previous.Value = newPVert;

				vertice newNVert = new vertice (next.Value.point, areaOfTriangle (next.PreviousOrLast ().Value.point, next.Value.point, next.NextOrFirst ().Value.point));
				next.Value = newNVert;
			} 
			else
				noVertsLessThanMinSig = true;
		}

		return simplifiedPoly;
	}
		
	//used to store signiicane of a vertice
	struct vertice {
		public Vector2 point;
		public float significance;

		public vertice (Vector2 pt, float sig) {
			point = pt;
			significance = sig;
		}
	}

	//find the area of three points
	static float areaOfTriangle (Vector2 a, Vector2 b, Vector2 c) {
		float area = Mathf.Abs ((a.x * (b.y-c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y)) / 2);
		return area; 
	}

	#endregion

	#region triangulate

	//group the verticies of a polygon into triangules : Ear Clipping
	public static Polygon[] triangulate (Polygon polygon, Polygon[] Holes, float ARThreshold, GameObject test) {
		List<Polygon> holes = new List<Polygon> (Holes);
		foreach (Polygon hole in Holes) {
			if (!holes.Contains (hole))
				continue;

			Polygon combPoly = hole; 

			Polygon[] hs = holes.ToArray ();
			foreach (Polygon h in hs) {
				if (hole.verticies == h.verticies || !holes.Contains (h))
					continue;

				Polygon newCombPoly = combinePolygons (combPoly, h, false);
				if (newCombPoly != null) {
					combPoly = newCombPoly;
					holes.Remove (h); 
				}
			}

			Polygon combBoundsPoly = combinePolygons (polygon, combPoly, true);
			if (combBoundsPoly != null) {
				polygon = combBoundsPoly;
				holes.Remove (hole); 
			} 
			else {
				holes.Remove (hole); 
				holes.Add (combPoly);
			}
		}

		List<Polygon> triangles = new List<Polygon> ();

		//cylincal list of the polygon verts ordered counter clockwise, including holes
		LinkedList<Vector2> polygonVerts;
		//list references to the polygon verticies
		LinkedList<int> polyVerts = new LinkedList<int> ();
		//list references to verticies with an interior angle less than 180 degrees
		List<int> convexVerts = new List<int> ();
		//list refrences to verticies with an interior angle greater than 180 degrees
		List<int> reflexVerts = new List<int> ();
		//cylincal list of references to ear tips
		List<int> earTips = new List<int> ();

		polygonVerts = new LinkedList<Vector2> (polygon.verticies);

		//add hole verts to polygon verts
		foreach (Polygon hole in holes) {
			//find possible bridges
			Heap<Segment> bridges = new Heap<Segment> (hole.verticies.Count * polygonVerts.Count);
			foreach (Vector2 v1 in polygonVerts) {
				foreach (Vector2 v2 in hole.verticies) {
					bridges.Add (new Segment (v1, v2)); 
				}
			}

			//choose best bridge
			Segment bridge = bridges.RemoveFirst (); 
			while (!bridgeIsValid (bridge, polygonVerts, holes.ToArray ()) && bridges.Count > 1) {
				bridge = bridges.RemoveFirst ();
			}

			//Debug.DrawLine (bridge.a, bridge.b, Color.cyan, 1000); 

			//Insert hole verts
			LinkedListNode<Vector2> bridgeA = polygonVerts.Find (bridge.a);
			LinkedListNode<Vector2> bridgeB = hole.verticies.Find (bridge.b);
			LinkedListNode<Vector2> last = bridgeA;
			for (LinkedListNode<Vector2> current = bridgeB; current != bridgeB || last == bridgeA; current = current.NextOrFirst ()) {
				polygonVerts.AddAfter (last, current.Value);
				last = polygonVerts.Find (current.Value);
			}

			polygonVerts.AddAfter (last, bridge.b);
			polygonVerts.AddAfter (polygonVerts.FindLast (bridge.b), bridge.a);
		}

		//find convex / reflex verts
		for (LinkedListNode<Vector2> current = polygonVerts.First; current != null; current = current.Next) {
			int index = current.GetIndex (); 
			polyVerts.AddLast (index); 

			Polygon triangle = new Polygon (LinkedListExt.NewLinkedList<Vector2> (current.PreviousOrLast ().Value, current.Value, current.NextOrFirst ().Value));

			//TextMesh tx = GameObject.Instantiate (test, new Vector3 (current.Value.x, current.Value.y, -1), Quaternion.identity).GetComponent<TextMesh> ();
			//tx.text = current.GetIndex ().ToString (); 

			if (isConvex (triangle)) {
				convexVerts.Add (index);
			} else {
				reflexVerts.Add (index);
			}
		}

		//find ears
		foreach (int vert in convexVerts) {
			LinkedListNode<int> current = polyVerts.Find (vert); 

			Polygon triangle = new Polygon (LinkedListExt.NewLinkedList<Vector2> (polygonVerts.ElementAt (current.PreviousOrLast ().Value), polygonVerts.ElementAt (current.Value), polygonVerts.ElementAt (current.NextOrFirst ().Value)));
			if (isEar (reflexVerts.ToArray (), polygonVerts.ToArray (), triangle))
				earTips.Add (vert);  
		}

		while (polyVerts.Count > 2) {
			//something went wrong!
			if (earTips.Count < 1) {
				Debug.LogError ("No more ear tips!");
				return triangles.ToArray ();
			}

			LinkedListNode<int> earTip = polyVerts.Find (earTips[0]); 

			//find ear with smallest angle
			float smallestAngle = Mathf.Infinity; 
			foreach (int ear in earTips) {
				LinkedListNode<int> earNode = polyVerts.Find (ear); 
				float angle = Vector2.Angle (polygonVerts.ElementAt (earNode.PreviousOrLast ().Value) - polygonVerts.ElementAt (earNode.Value), polygonVerts.ElementAt (earNode.NextOrFirst ().Value) - polygonVerts.ElementAt (earNode.Value)); 
				if (angle < smallestAngle) {
					earTip = earNode;
					smallestAngle = angle;
				}
			}

			LinkedListNode<int> previous = earTip.PreviousOrLast ();
			LinkedListNode<int> next = earTip.NextOrFirst ();

			//store trplet
			Vector2 a = polygonVerts.ElementAt (previous.Value);
			Vector2 b = polygonVerts.ElementAt (earTip.Value);
			Vector2 c = polygonVerts.ElementAt (next.Value);

			Polygon tri = new Polygon (LinkedListExt.NewLinkedList<Vector2> (a, b, c)); 

			//swap diagonal with adjacent triangle to improve triangle quality
			float originalAR = smallestAngle;
			if (originalAR < ARThreshold) { 
				foreach (Polygon triangle in triangles.ToArray ()) {
					//find longest edge
					Vector2[] longestEdge;
					float ab = Vector2.Distance (a, b); 
					float bc = Vector2.Distance (b, c); 
					float ca = Vector2.Distance (c, a);

					if (ab > bc && ab > ca)
						longestEdge = new Vector2[] { a, b };
					else if (bc > ab && bc > ca)
						longestEdge = new Vector2[] { b, c };
					else
						longestEdge = new Vector2[] { c, a };
				

					if (triangle.verticies.Contains (longestEdge [0]) && triangle.verticies.Contains (longestEdge [1])) {
						//find the verticies not part of the pair
						Vector2 d = tri.verticies.Except (longestEdge).ToArray () [0];
						Vector2 e = triangle.verticies.Except (longestEdge).ToArray () [0];

						//generate new triangles
						Polygon t1 = new Polygon (LinkedListExt.NewLinkedList<Vector2> (longestEdge [0], e, d)); 
						Polygon t2 = new Polygon (LinkedListExt.NewLinkedList<Vector2> (longestEdge [1], d, e));

						if (getTriAngle (longestEdge [0], e, d) > originalAR && getTriAngle (longestEdge [1], d, e) > originalAR) {
							Debug.DrawLine (longestEdge [1], longestEdge [0], Color.green, 3);  

							//ensure orientation of old triangle is still CC
							Vector2[] triVerts1 = t2.verticies.ToArray ();
							if (orientation (triVerts1 [0], triVerts1 [1], triVerts1 [2]) != 2) { 
								t2.verticies.First.Value = triVerts1 [2];
								t2.verticies.Last.Value = triVerts1 [0];

								//debug
								Vector2[] triVerts2 = t2.verticies.ToArray ();
								if (orientation (triVerts2 [0], triVerts2 [1], triVerts2 [2]) != 2)
									Debug.Log ("not fixed1");
								else
									Debug.Log ("fixed1");
							}
									
							//update triangles
							triangle.verticies = t2.verticies;
							tri = t1;
						}
					}
				}
			}

			//enusre the triangle is orientated CC
			Vector2[] triVerts = tri.verticies.ToArray ();
			if (orientation (triVerts [0], triVerts [1], triVerts [2]) != 2) { 
				Vector2 placeHolder = tri.verticies.First.Value;
				tri.verticies.First.Value = tri.verticies.Last.Value;
				tri.verticies.Last.Value = placeHolder;

				//debug
				Vector2[] triVerts2 = tri.verticies.ToArray ();
				if (orientation (triVerts2 [0], triVerts2 [1], triVerts2 [2]) != 2)
					Debug.Log ("not fixed");
				else
					Debug.Log ("fixed");
			}
				
			triangles.Add (tri); 

			//remove ear tip from lists
			earTips.Remove (earTip.Value);
			convexVerts.Remove (earTip.Value); 
			polyVerts.Remove (earTip.Value); 

			//update the statis of the adjacient verticies
			updateVertStat (previous, polygonVerts, earTips, convexVerts, reflexVerts); 
			updateVertStat (next, polygonVerts, earTips, convexVerts, reflexVerts); 

			/*string p = "P: [";
			polyVerts.ToList ().ForEach (i => p += i + ",");
			Debug.Log (p + "]"); 

			string C = "C: [";
			convexVerts.ForEach (i => C += i + ",");
			Debug.Log (C + "]"); 

			string E = "E: [";
			earTips.ForEach (i => E += i + ",");
			Debug.Log (E + "]"); 

			string R = "R: [";
			reflexVerts.ForEach (i => R += i + ",");
			Debug.Log (R + "]"); */
		}

		return triangles.ToArray ();
	}

	static bool bridgeIsValid (Segment bridge, LinkedList<Vector2> verts, Polygon[] holes) {
		//test the bridge for intersection with the polygon bounds
		for (LinkedListNode<Vector2> current = verts.First; current != null; current = current.Next) {
			if (doIntersect (current.Value, current.PreviousOrLast ().Value, bridge.a, bridge.b))
				return false;
		}

		//test the bridge for intersection with the polygon bounds
		foreach (Polygon h in holes) {
			for (LinkedListNode<Vector2> current = h.verticies.First; current != null; current = current.Next) {
				if (doIntersect (current.Value, current.PreviousOrLast ().Value, bridge.a, bridge.b))
					return false;
			}
		}

		return true;
	}

	//determine if the verticie is still convex / reflex / ear
	static void updateVertStat (LinkedListNode<int> vert, LinkedList<Vector2> verts, List<int> e, List<int> c, List<int> r) {
		Polygon triangle = new Polygon (LinkedListExt.NewLinkedList<Vector2> (verts.ElementAt (vert.PreviousOrLast ().Value), verts.ElementAt (vert.Value), verts.ElementAt (vert.NextOrFirst ().Value))); 

		bool convex = isConvex (triangle);

		//Debug.DrawLine (triangle.verticies.First.Value, triangle.verticies.Last.Value, Color.gray, 2); 

		if (convex && !c.Contains (vert.Value)) {
			c.Add (vert.Value); 
			r.Remove (vert.Value); 
		}

		bool ear = convex ? isEar (r.ToArray (), verts.ToArray (), triangle) : false;

		if (ear && !e.Contains (vert.Value))
			e.Add (vert.Value);
		else if (!ear && e.Contains (vert.Value))
			e.Remove (vert.Value); 
	}

	//find the triangles minimum angle
	static float getTriAngle (Vector2 a, Vector2 b, Vector2 c) {
		return Mathf.Min (Vector2.Angle (b - a, c - a), Vector2.Angle (a - b, c - b), Vector2.Angle (a - c, b - c));
	}

	//test wether any reflex verticies lie in the triangle
	static bool isEar (int[] reflexVerts, Vector2[] verts, Polygon triangle) {
		foreach (int r in reflexVerts) {
			if (triangle.verticies.Contains (verts [r]))
				continue;

			if (pointInsidePolygon (triangle, verts[r]))
				return false;
		}

		return true;
	}

	//test wether the orientation of the triplet is the same as the polygon (counter clockwise)
	static bool isConvex (Polygon triangle) {
		Vector2 a = triangle.verticies.First.Value;
		Vector2 b = triangle.verticies.First.Next.Value;
		Vector2 c = triangle.verticies.Last.Value;

		if (orientation (a, b, c) == 2)
			return false;
		
		return true;
	}

	#endregion

	#region inflatePolygon

	//adds a buffer around a polygon by finding intersections of parrallel lines
	public static Polygon inflatePolygon (Polygon polygon, float buffer) {
		Polygon newPoly = new Polygon (new LinkedList<Vector2> ()); 

		//replace each point with new point
		for (LinkedListNode<Vector2> current = polygon.verticies.First; current != null; current = current.Next) {
			Vector2 a = current.PreviousOrLast ().Value;
			Vector2 b = current.Value;
			Vector2 c = current.NextOrFirst ().Value;

			//find parrallel lines a certain distance away
			Vector2 ab = a - b;
			Vector2 p1 = b + new Vector2 (-ab.y, ab.x).normalized * buffer;
			Vector2 q1 = a + new Vector2 (-ab.y, ab.x).normalized * buffer;

			Vector2 cb = c - b;
			Vector2 p2 = b + new Vector2 (-cb.y, cb.x).normalized * -buffer;
			Vector2 q2 = c + new Vector2 (-cb.y, cb.x).normalized * -buffer;

			Vector2 intersection = findIntersection (p1, q1, p2, q2);

			//dull edge if needed
			float angle = Vector2.Angle (q1 - intersection, q2 - intersection);
			if (angle < 90 && !isConvex (new Polygon (LinkedListExt.NewLinkedList (a, b, c)))) {
				newPoly.verticies.AddLast (intersection + (q1 - intersection).normalized / (angle / 65)); 
				newPoly.verticies.AddLast (intersection + (q2 - intersection).normalized / (angle / 65)); 
			}
			else
				newPoly.verticies.AddLast (intersection); 
		}

		return newPoly;
	}

	//finds the intersection of two lines
	static Vector2 findIntersection (Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2) {
		//find the equations of the lines
		float m1 = (q1.y - p1.y) / (q1.x - p1.x);
		float m2 = (q2.y - p2.y) / (q2.x - p2.x);
		float b1 = p1.y - (m1 * p1.x);
		float b2 = p2.y - (m2 * p2.x);

		//find intersection by setting both sides equal
		Vector2 intersection;
		intersection.x = (b1 - b2) / (m2 - m1);
		intersection.y = m1 * intersection.x + b1; 
		return intersection;
	}

	#endregion

	#region combinePolygons

	public static Polygon combinePolygons (Polygon p1, Polygon p2, bool inverse) { 
		Polygon P1 = new Polygon (new LinkedList<Vector2> (p1.verticies));
		Polygon P2 = new Polygon (new LinkedList<Vector2> (p2.verticies)); 

		//find intersections
		List<Vector2> intersections = new List<Vector2> ();
		p1 = findIntersections (P1, P2, intersections, out intersections);
		p2 = findIntersections (P2, P1, intersections, out intersections); 

		//check if one polygon lies inside of the other
		if (intersections.Count < 2) {
			if (!inverse && pointInsidePolygon (P2, p1.center))
				return P2;
			else if ((!inverse && pointInsidePolygon (P1, p2.center)) || (inverse && !pointInsidePolygon (P1, p2.center)))
				return P1;
			else
				return null;
		}

		string p1String = "P1: [";
		//remove points from P1 that lie in P2
		foreach (Vector2 v in P1.verticies) {
			if (pointInsidePolygon (P2, v))
				p1.verticies.Remove (v);
			p1String += v + " ";
		}
		Debug.Log (p1String + "]"); 
			
		//remove points from P2 that lie in P1
		string p2String = "P2: [";
		foreach (Vector2 v in P2.verticies) {
			if ((inverse && !pointInsidePolygon (P1, v)) || (!inverse && pointInsidePolygon (P1, v)))
				p2.verticies.Remove (v);
			p2String += v + " ";
		}
		Debug.Log (p2String); 
			
		//create union polygon:
		//1.trace through P1 unitl we reached an intersection
		//2.trace through P2 unitl we reached an intersection
		//3.start where we left off in P1 and repeat
		Polygon newPoly = new Polygon (new LinkedList<Vector2> ());
		for (LinkedListNode<Vector2> current1 = p1.verticies.First; current1 != null; current1 = current1.Next) {
			newPoly.verticies.AddLast (current1.Value); 
			//Debug.Log ("1: " + current1.Value);

			//reached an intersection
			if (intersections.Contains (current1.Value) && current1 != p1.verticies.First) {  
				for (LinkedListNode<Vector2> current2 = p2.verticies.FindAproxVector2 (current1.Value).NextOrFirst (); !newPoly.verticies.Contains (current2.Value); current2 = current2.NextOrFirst ()) {
					newPoly.verticies.AddLast (current2.Value); 
					//Debug.Log ("2: " + current2.Value); 
					 
					if (intersections.Contains (current2.Value)) {
						current1 = p1.verticies.FindAproxVector2 (current2.Value);
						if (current1 == p1.verticies.First)
							current1 = p1.verticies.Last;
						break;
					}
				}
			} 
		}

		return newPoly;
	}

	static Polygon findIntersections (Polygon p1, Polygon p2, List<Vector2> intersections, out List<Vector2> newIntersects) {
		Polygon newPolygon = new Polygon (new LinkedList<Vector2> (p1.verticies)); 
		for (LinkedListNode<Vector2> current1 = p1.verticies.First; current1 != null; current1 = current1.Next) {
			List<Vector2> intersectionsOnEdge = new List<Vector2> ();
			for (LinkedListNode<Vector2> current2 = p2.verticies.First; current2 != null; current2 = current2.Next) {
				if (doIntersect (current1.Value, current1.PreviousOrLast ().Value, current2.Value, current2.PreviousOrLast ().Value)) {
					Vector2 intersection = findIntersection (current1.Value, current1.PreviousOrLast ().Value, current2.Value, current2.PreviousOrLast ().Value);
					intersectionsOnEdge.Add (intersection); 
				}
			}

			if (intersectionsOnEdge.Count > 0) {
				intersectionsOnEdge.Sort (delegate (Vector2 v1, Vector2 v2) { 
					return Vector2.Distance (v1, current1.PreviousOrLast ().Value).CompareTo (Vector2.Distance (v2, current1.PreviousOrLast ().Value)); 
				});

				LinkedListNode<Vector2> insert = newPolygon.verticies.Find (current1.Value);
				foreach (Vector2 intersection in intersectionsOnEdge) {
					newPolygon.verticies.AddBefore (insert, intersection);
					if (!intersections.Contains (intersection)) 
						intersections.Add (intersection); 
				}
			}
		}

		newIntersects = intersections;
		return newPolygon;
	}

	#endregion

	#region polygonize

	//removes unnessesary diagonals from a triangulation to create a convex polygonization
	public static Polygon[] polygonPartion (Polygon[] triangulation) {
		List<Segment> inessentialDiagonals = new List<Segment> ();

		//find all inessentialDiagonals
		foreach (Polygon p1 in triangulation) {
			foreach (Polygon p2 in triangulation) {
				if (p1 == p2)
					continue;

				//find shared edge if there is one
				List<Vector2> sharedEdge = new List<Vector2> ();
				foreach (Vector2 v in p1.verticies) {
					if (p2.verticies.Contains (v))
						sharedEdge.Add (v); 
				}

				if (sharedEdge.Count > 1) {
					Segment seg = new Segment (sharedEdge [0], sharedEdge [1]);

					//add edge to list of inessential diagonals if it is not essential
					if (!inessentialDiagonals.Contains (seg) && !isEssential (seg, p1, p2))
						inessentialDiagonals.Add (seg);
				}
			}
		}

		List<Polygon> polygons = new List<Polygon> (triangulation);
		while (inessentialDiagonals.Count > 0) {
			//find longest diagonal
			Segment segToRemove = inessentialDiagonals [0];
			foreach (Segment seg in inessentialDiagonals) {
				if (seg.length > segToRemove.length)
					segToRemove = seg;
			}

			//find the polygons the segment belongs to
			List<Polygon> polys = new List<Polygon>();
			foreach (Polygon p in polygons) {
				if (p.verticies.Contains (segToRemove.a) && p.verticies.Contains (segToRemove.b))
					polys.Add (p);
			}

			if (!inessentialDiagonals.Remove (segToRemove)) {
				Debug.Log ("Segment dosent exist"); 
			}

			Polygon newPoly = removeDiagonal (segToRemove, polys [0], polys [1]);
			polygons.Add (newPoly); 
			polygons.Remove (polys[0]);
			polygons.Remove (polys[1]); 

			//update the neighboring segments
			foreach (Polygon p in polygons) {
				List<Vector2> sharedEdge = new List<Vector2> ();
				foreach (Vector2 v in newPoly.verticies) {
					if (p.verticies.Contains (v))
						sharedEdge.Add (v); 
				}

				if (sharedEdge.Count > 1) {
					Segment seg = new Segment (sharedEdge [0], sharedEdge [1]);
					if (inessentialDiagonals.Contains (seg) && isEssential (seg, p, newPoly))
						inessentialDiagonals.Remove (seg); 
				}
			}
		}

		return polygons.ToArray ();
	}

	static Polygon removeDiagonal (Segment diagonal, Polygon P1, Polygon P2) {
		Polygon combPoly = new Polygon (P1.verticies); 
		Polygon p1 = new Polygon (P1.verticies);
		Polygon p2 = new Polygon (P2.verticies);

		/*string p1Verts = "1: ";
		p1.verticies.ToList ().ForEach (i => p1Verts += i); 
		Debug.Log (p1Verts); 

		string p2Verts = "2: ";
		p2.verticies.ToList ().ForEach (i => p2Verts += i); 
		Debug.Log (p2Verts);*/

		int ori = orientation (diagonal.a, diagonal.b, p2.center); 
		LinkedListNode<Vector2> a = ori == 1 ? p2.verticies.Find (diagonal.a) : p2.verticies.Find (diagonal.b);
		LinkedListNode<Vector2> b = ori == 1 ? p2.verticies.Find (diagonal.b) : p2.verticies.Find (diagonal.a);
		LinkedListNode<Vector2> last = combPoly.verticies.Find (a.Value);
		for (LinkedListNode<Vector2> current = a.NextOrFirst (); current != b; current = current.NextOrFirst ()) {
			//Debug.Log (current.Value); 
			combPoly.verticies.AddAfter (last, current.Value);
			last = combPoly.verticies.Find (current.Value);
		}

		/*string pVerts = "P: ";
		combPoly.verticies.ToList ().ForEach (i => pVerts += i); 
		Debug.Log (pVerts); */

		return combPoly;
	}

	static bool isEssential (Segment seg, Polygon p1, Polygon p2) {
		Polygon combPoly = removeDiagonal (seg, p1, p2);
		LinkedListNode<Vector2> a = combPoly.verticies.Find (seg.a);
		LinkedListNode<Vector2> b = combPoly.verticies.Find (seg.b);

		if (orientation (a.PreviousOrLast ().Value, a.Value, a.NextOrFirst ().Value) == 2 && orientation (b.PreviousOrLast ().Value, b.Value, b.NextOrFirst ().Value) == 2)
			return false;

		return true;
	}

	#endregion

	//finds points shared by both polygons
	public static Vector2[] sharedPoints (Polygon p1, Polygon p2) {
		List<Vector2> sharedPoints = new List<Vector2> ();

		foreach (Vector2 v in p1.verticies) {
			if (p2.verticies.Contains (v))
				sharedPoints.Add (v);
		}

		return sharedPoints.ToArray (); 
	}

	public static bool pointInsidePolygon (Polygon polygon, Vector2 point) {
		//find the polygons bounding box
		float MinX = polygon.verticies.First.Value.x;
		float MinY = polygon.verticies.First.Value.y; 
		float MaxX = polygon.verticies.First.Value.x;
		float MaxY = polygon.verticies.First.Value.y; 
		foreach (Vector2 vert in polygon.verticies) {
			MinX = Mathf.Min (MinX, vert.x);
			MinY = Mathf.Min (MinY, vert.y);
			MaxX = Mathf.Max (MaxX, vert.x);
			MaxY = Mathf.Max (MaxY, vert.y);
		}

		//test if the point is inside the polygon bounding box
		if (point.x <= MinX || point.y <= MinY || point.x >= MaxX || point.y >= MaxY)
			return false;

		//count the number of times that an ray to the right of the point intersects the polygon
		Vector2 ray = point + Vector2.right * (MaxX - MinX);

		int intersections = 0;
		for (LinkedListNode<Vector2> current = polygon.verticies.First; current != null; current = current.Next) {
			if (doIntersect (point, ray, current.Value, current.PreviousOrLast ().Value))
				intersections++;
		}

		//if intersections is even, we are outside the polygon
		return intersections % 2 == 0 ? false : true;
	}

	//returns true if line segment 'p1q1' and 'p2q2' intersect
	static bool doIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2) {
		// Find the four orientations needed for general and
		// special cases
		int o1 = orientation(p1, q1, p2);
		int o2 = orientation(p1, q1, q2);
		int o3 = orientation(p2, q2, p1);
		int o4 = orientation(p2, q2, q1);

		//the lines do not intersect if the endpoint of one line lies on the other
		if (onSegment (p1, p2, q2) || onSegment (q1, p2, q2) || onSegment (p2, p1, q1) || onSegment (q2, p1, q1))
			return false;

		// General case
		if (o1 != o2 && o3 != o4) {
			//Debug.DrawLine (p1, q1, Color.black, 3);
			//Debug.DrawLine (p2, q2, Color.gray, 3); 
			return true;
		}

		return false; // Doesn't fall in any of the above cases
	}

	// Given three colinear points p, q, r, the function checks if
	// point q lies on line segment 'pr'
	static bool onSegment(Vector2 checkPoint, Vector2 endpoint1, Vector2 endpoint2)
	{
		float ab = Vector2.Distance (endpoint1, endpoint2); 
		float ap = Vector2.Distance (endpoint1, checkPoint);
		float bp = Vector2.Distance (endpoint2, checkPoint);

		return ab == ap + bp;
	}

	// To find orientation of ordered triplet (p, q, r).
	// The function returns following values
	// 0 --> p, q and r are colinear
	// 1 --> Clockwise
	// 2 --> Counterclockwise
	public static int orientation(Vector2 p, Vector2 q, Vector2 r)
	{
		float val = (q.y - p.y) * (r.x - q.x) -
			(q.x - p.x) * (r.y - q.y);

		if (val == 0) return 0;  // colinear

		return (val > 0)? 1: 2; // clock or counterclock wise
	}

	//finds average of points
	public static Vector2 midpoint (params Vector2[] points) {
		Vector2 mid = new Vector2 ();

		foreach (Vector2 point in points) {
			mid.x += point.x;
			mid.y += point.y;
		}

		mid.x /= points.Length;
		mid.y /= points.Length;

		return mid;
	}
}

[System.Serializable]
public class Segment : IHeapItem<Segment>, System.IEquatable<Segment> {
	public Vector2 a;
	public Vector2 b;

	int heapIndex;

	public Segment (Vector2 endpoint1, Vector2 endpoint2) {
		a = endpoint1;
		b = endpoint2;
	}

	public Segment () {
		
	}

	public float length {
		get {
			return Vector2.Distance (a, b); 
		}
	}

	public Vector2 midpoint {
		get {
			return Geometry.midpoint (a, b);
		}
	}

	public int HeapIndex {
		get {
			return heapIndex;
		}
		set {
			heapIndex = value;
		}
	}

	public bool Equals (Segment segToCompare) {
		return (segToCompare.a == this.a && segToCompare.b == this.b) || (segToCompare.b == this.a && segToCompare.a == this.b);
	}

	public int CompareTo (Segment segToCompare) {
		int compare = length.CompareTo (segToCompare.length);
		if (compare == 0)
			compare = 1;  

		return -compare;
	}
}

public class Polygon {
	//ordered list of verts
	public LinkedList<Vector2> verticies;

	public Polygon (LinkedList<Vector2> verts) {
		verticies = new LinkedList<Vector2> (verts);
	}

	public Vector2 center {
		get {
			return Geometry.midpoint (verticies.ToArray ()); 
		}
	}
}



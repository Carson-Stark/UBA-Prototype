using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingTest : MonoBehaviour {

	NavMesh nvm;
	Pathfinding pf;

	float maxX = 0;
	float maxY = 0;
	float minX = Mathf.Infinity;
	float minY = Mathf.Infinity;

	void Start () {
		nvm = GetComponent<NavMesh> ();
		pf = GetComponent<Pathfinding> ();

		foreach (Vector2 v in nvm.mapPoly.verticies) {
			if (v.x > maxX)
				maxX = v.x;
			if (v.y > maxY)
				maxY = v.y;
			if (v.x < minX)
				minX = v.x;
			if (v.y < minY)
				minY = v.y;
		}

		InvokeRepeating ("SwitchPoints", 3, 3); 
	}

	void SwitchPoints () {
		pf.findPath (RandomPoint(), RandomPoint()); 
	}

	Vector2 RandomPoint () {
		bool valid;
		Vector2 point;
		do {
			point.x = Random.Range (minX, maxX);
			point.y = Random.Range (minY, maxY);

			valid = true;

			if (Geometry.pointInsidePolygon (nvm.mapPoly, point)){
				foreach (Polygon h in nvm.holePolys) {
					if (Geometry.pointInsidePolygon (h, point)) {
						valid = false;
						break;
					}
				}
			}
			else 
				valid = false;
		} 
		while (!valid);

		return point; 
	}
}

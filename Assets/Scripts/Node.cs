using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Node : IHeapItem<Node>, System.IEquatable<Node>{
	public Vector2 point;
	public int segment;
	public List<int> neighbors = new List<int> ();
	public List<int> polygons = new List<int> ();

	public Node parent;

	public float H_cost; //distance from the goal
	public float G_cost; //distance from the start

	int heapIndex;

	public float F_cost {
		get { 
			return H_cost + G_cost; 
		}
	}

	public Node (Vector2 pos, int seg) {
		point = pos;
		segment = seg;
	}

	public Node (Vector2 pos) {
		point = pos;
	}

	public Node () {

	}

	public int HeapIndex {
		get {
			return heapIndex;
		}
		set {
			heapIndex = value;
		}
	}

	public bool Equals (Node nodeToCompare) {
		return nodeToCompare.segment == this.segment;
	}

	public int CompareTo (Node nodeToCompare) {
		int compare = F_cost.CompareTo (nodeToCompare.F_cost);
		if (compare == 0) {
			compare = H_cost.CompareTo (nodeToCompare.H_cost); 
		}

		return -compare;
	}
}

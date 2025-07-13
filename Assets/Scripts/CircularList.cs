using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LinkedListExt {

	public static LinkedListNode<T> NextOrFirst<T> (this LinkedListNode<T> current) {
		return current.Next ?? current.List.First;
	}

	public static LinkedListNode<T> PreviousOrLast<T> (this LinkedListNode<T> current) {
		return current.Previous ?? current.List.Last;
	}

	public static LinkedList<T> NewLinkedList<T> (params T[] items) {
		return new LinkedList<T> (items);
	}

	public static LinkedListNode<Vector2> FindAproxVector2 (this LinkedList<Vector2> list, Vector2 value) {
		for (LinkedListNode<Vector2> node = list.First; node != null; node = node.Next) {
			if (Mathf.Abs (node.Value.x - value.x) < 0.001f && Mathf.Abs (node.Value.y - value.y) < 0.001f)
				return node;
		}

		return null;
	}

	public static int GetIndex<T> (this LinkedListNode<T> item) {
		int count = 0;
		for (LinkedListNode<T> node = item.List.First; node != null; node = node.Next, count++)
		{
			if (item == node)
				return count;
		}

		return -1;
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class building : MonoBehaviour {

	public SpriteRenderer roof;

	public Transform[] triggers;

	public float maxDistFromTrig;

	GameManager manager;
	GameObject localCharacter;

	void Start () {
		manager = Camera.main.GetComponent<GameManager>(); 
	}

	void Update () {
		if (manager.localPlayer == null)
			return;
		
		localCharacter = manager.localPlayer;

		float closestDist = Mathf.Infinity;
		Vector2 closestTrigger = transform.position;
		foreach (Transform trigger in triggers) {
			if (Vector2.Distance ((Vector2)trigger.position, localCharacter.transform.position) < closestDist) {
				closestTrigger = (Vector2)trigger.position;
				closestDist = Vector2.Distance (closestTrigger, localCharacter.transform.position);
			}
		}

		if (closestDist < maxDistFromTrig) {
			Color newColor = roof.color; 
			newColor.a = closestDist / maxDistFromTrig;

			roof.color = newColor;
		}
	}

	void OnTriggerEnter2D (Collider2D other) {
		if (other == localCharacter.GetComponent<Collider2D>())
			roof.enabled = false;
	}

	void OnTriggerExit2D (Collider2D other) {
		if (other == localCharacter.GetComponent<Collider2D>())
			roof.enabled = true;
	}
}

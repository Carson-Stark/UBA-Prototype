using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PlayerMovementOffline : MonoBehaviour {

	//local movement
	public GameObject head;
	public GameObject directionIndicator;
	public GameObject aimIndicator;

	public float turnSpeed;
	public float moveSpeed;
	public float diveSpeed;
	public float diveDist;
	public float minDisFromObj;
	public int stationaryObjLayer;

	public PlayerInputOffline player_input;

	float collisionRadius;
	bool aboutToCollide;

	[HideInInspector] public bool diving;
	Vector2 diveTarget;

	Vector2 targetPos;
	Quaternion targetRot;

	void Start () {
		diving = false;

		//bit shift to get a bit layer mask of only stationary objects
		stationaryObjLayer = 1 << stationaryObjLayer;
		collisionRadius = GetComponent<CircleCollider2D> ().radius; 
	}

	void Update () {
		//ensures that the player never flips
		Quaternion Rotation = transform.rotation;
		Rotation.x = 0;
		Rotation.y = 0;
		transform.rotation = Rotation;

		//prevents glitching through walls and jarring effect when colliding
		if (Physics2D.CircleCast (transform.position, collisionRadius / 1.5f, player_input.currentInput.moveDirection, collisionRadius / 2 + minDisFromObj, stationaryObjLayer))
			aboutToCollide = true;
		else
			aboutToCollide = false;

		transform.position = MovePos (transform.position, true, player_input.currentInput, moveSpeed * Time.deltaTime);
		transform.rotation = MoveRot (transform.rotation, true, player_input.currentInput, turnSpeed * Time.deltaTime);

		//check if we pressed dive
		CheckForDive (); 
	}

	public Vector2 MovePos (Vector2 startingPos, bool mine, input _input, float speed) {
		if (diving)
			return startingPos;

		if (_input.moveing) {
			//show direction
			directionIndicator.SetActive (true); 
			directionIndicator.transform.rotation = Quaternion.FromToRotation (Vector2.up, _input.moveDirection);
			directionIndicator.transform.position = transform.position;

			//move the player foward
			return (Vector2)startingPos + _input.moveDirection * speed;
		}

		return startingPos;
	}

	public Quaternion MoveRot (Quaternion startingRot, bool mine, input _input, float speed) {
		if (diving)
			return startingRot; 

		Quaternion TargetRot;

		if (_input.shooting) {
			TargetRot = Quaternion.FromToRotation (Vector2.up, _input.shootDirection);

			if (!player_input.currentInput.aimingGranade) {
				//show aim
				aimIndicator.SetActive (true); 
				aimIndicator.transform.rotation = TargetRot;
				aimIndicator.transform.position = transform.position;
			}
		}
		else {
			//rotate to the direction we're facing
			TargetRot = Quaternion.FromToRotation (Vector2.up, _input.moveDirection);

			//hide aim
			aimIndicator.SetActive (false); 
		}
		
		//smoothly rotates towards new rotation instead of snapping
		return Quaternion.Lerp (startingRot, TargetRot, speed);
	}

	void CheckForDive () {
		if (player_input.currentInput.readyToDive && player_input.currentInput.moveing && player_input.currentInput.moveDirection.magnitude > 0.8f && !diving && !aboutToCollide) {
			diving = true;
			player_input.diveButton.activated = false;
			player_input.currentInput.readyToDive = false;
			diveTarget = (Vector2)transform.position + player_input.currentInput.moveDirection * diveDist;
			transform.rotation = Quaternion.FromToRotation (Vector2.up, player_input.currentInput.moveDirection); 
		} 
		else if (diving) {
			transform.position = Vector2.MoveTowards (transform.position, diveTarget, diveSpeed * Time.deltaTime);

			if (Vector2.Distance (transform.position, diveTarget) < 0.01f || aboutToCollide)
				diving = false;
		}
	}
}

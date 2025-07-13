using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	public GameObject playerToFollow;
	public float cameraFollowSpeed;

	void Start () {
		Vector3 newPos = playerToFollow.transform.position;
		newPos.z = transform.position.z;

		transform.position = newPos;
	}

	void Update () {
		Vector3 newPos = playerToFollow.transform.position;
		newPos.z = transform.position.z;

		transform.position = Vector3.Lerp (transform.position, newPos, cameraFollowSpeed);
	}
}

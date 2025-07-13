using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class PlayerInput : MonoBehaviour {

	public JoyStick moveJoystick;
	public JoyStick shootJoystick;
	public button granadeButton;
	public button diveButton;

	[HideInInspector] public bool mine;
	PhotonView PV;
	PlayerMovement PM;

	[HideInInspector] public input currentInput;
	[HideInInspector] public List<request> requests = new List<request>();
	[HideInInspector] public int sequenceNumber;
	[HideInInspector] public float timeRequestSent;


	void Awake () {
		PV = GetComponent<PhotonView> (); 

		if (PV.isMine) {
			mine = true;
		}

		sequenceNumber = 0;
		currentInput = new input ();

		if (!PhotonNetwork.isMasterClient)
			InvokeRepeating ("fixed_update", 0, (float)((double)1 / PhotonNetwork.sendRate)); 
	}

	float lastTime;

	void Update () {
		//no point to sync input detection with send rate if we're not sending anything
		if (PhotonNetwork.isMasterClient)
			fixed_update (); 
	}

	void fixed_update () {
		if (PV.isMine) {
			//Debug.Log ("change input   " + PhotonNetwork.time); 
			currentInput = new input ();
			currentInput.moveDirection = moveJoystick.inputDirection;
			currentInput.moveing = moveJoystick.usingJoystick;

			currentInput.shootDirection = shootJoystick.inputDirection;
			currentInput.shooting = shootJoystick.usingJoystick;

			currentInput.aimingGranade = granadeButton.activated;
			currentInput.readyToDive = diveButton.activated;
		}
	}

	public void SerializeState (PhotonStream stream, PhotonMessageInfo info) { 
		//fixed_update (); 

		if (stream.isWriting) {
			sequenceNumber++;

			request currentRequest = new request ();
			currentRequest.id = sequenceNumber;
			currentRequest.timeStamp = (float)PhotonNetwork.time;
			currentRequest.inputRequested = currentInput;

			requests.Add (currentRequest); 

			stream.SendNext (sequenceNumber); 
			stream.SendNext (currentRequest.timeStamp); 

			stream.SendNext (currentInput.moveDirection); 
			stream.SendNext (currentInput.moveing); 

			stream.SendNext (currentInput.shootDirection); 
			stream.SendNext (currentInput.shooting);

			stream.SendNext (currentInput.aimingGranade);
			stream.SendNext (currentInput.readyToDive); 

			//Debug.Log ("sent input  " + PhotonNetwork.time); 
		} 
		else {
			int SN = (int)stream.ReceiveNext ();
			if (SN >= sequenceNumber) {
				timeRequestSent = (float)stream.ReceiveNext (); 

				currentInput.moveDirection = (Vector2)stream.ReceiveNext (); 
				currentInput.moveing = (bool)stream.ReceiveNext (); 

				currentInput.shootDirection = (Vector2)stream.ReceiveNext (); 
				currentInput.shooting = (bool)stream.ReceiveNext (); 

				currentInput.aimingGranade = (bool) stream.ReceiveNext (); 
				currentInput.readyToDive = (bool) stream.ReceiveNext (); 

				sequenceNumber = SN;
			}
			else {
				Debug.LogWarning ("lost " + (sequenceNumber - SN).ToString ()  + " packats"); 
			}
		}  
	}
}

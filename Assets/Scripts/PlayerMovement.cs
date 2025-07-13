using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PlayerMovement : MonoBehaviour {

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

	[HideInInspector] public PlayerInput player_input;

	float collisionRadius;
	bool aboutToCollide;

	[HideInInspector] public bool diving;
	Vector2 diveTarget;

	//networking
	public bool showServerPos;
	public bool clientSidePrediction;
	public bool serverReconsiliation;
	public bool rubberbanding;
	public bool trustMasterClient;

	public float rotCorrectionThreshold;
	public float posCorrectionThreshold;

	public float rotTrustThreshold;
	public float posTrustThreshold;
	public int votesNeeded;

	int index;

	PhotonView PV;
	GameManager manager;

	Quaternion serverRot;
	Vector2 serverPos;
	bool serverAboutToCollide;
	bool serverDiving;

	Vector2 targetPos;
	Quaternion targetRot;

	List<GameState> pastGameStates;

	int lastRecievedSequenceNumber;
	int lastValidatedRequest;
	double timeBetweenSerialization;

	List<bool> votes;
	bool waitingForVotes;

	//debuging
	Transform test;
	Text test2;
	Transform test3;
	public bool simulateMasterClientCheating;

	void Awake () {
		pastGameStates = new List<GameState> ();
	}

	void Start () {
		player_input = transform.parent.GetComponent<PlayerInput> (); 
		PV = GetComponent<PhotonView> ();
		manager = Camera.main.GetComponent<GameManager> ();

		diving = false;

		//bit shift to get a bit layer mask of only stationary objects
		stationaryObjLayer = 1 << stationaryObjLayer;
		collisionRadius = GetComponent<CircleCollider2D> ().radius; 
	
		lastValidatedRequest = 0;
		lastRecievedSequenceNumber = 0;
		timeBetweenSerialization = 1 / (double)PhotonNetwork.sendRateOnSerialize;

		votes = new List<bool> ();
		waitingForVotes = false;

		serverPos = transform.position;
		serverRot = transform.rotation;
		serverAboutToCollide = false;

		targetPos = transform.position;
		targetRot = transform.rotation;

		//test = GameObject.FindGameObjectWithTag ("debug").transform;
		//if (showServerPos)
			//test.gameObject.SetActive (true); 
		//test2 = GameObject.FindGameObjectWithTag ("test").GetComponent<Text>();
		//test3 = GameObject.FindGameObjectWithTag ("debug2").transform; 
	}

	void Update () {
		//debug purposes
		if (PhotonNetwork.isMasterClient && simulateMasterClientCheating && Input.GetKeyDown (KeyCode.Space))
			moveSpeed = 1;

		index = manager.charactersInGame.IndexOf (gameObject); 

		//ensures that the player never flips
		Quaternion Rotation = transform.rotation;
		Rotation.x = 0;
		Rotation.y = 0;
		transform.rotation = Rotation;

		//prevents glitching through walls and jarring effect when colliding
		if (Physics2D.CircleCast (transform.position, collisionRadius / 1.5f, player_input.currentInput.moveDirection, collisionRadius / 2 + minDisFromObj, stationaryObjLayer) || serverAboutToCollide)
			aboutToCollide = true;
		else
			aboutToCollide = false;

		if (PhotonNetwork.isMasterClient) {
			//test2.text = "Host";

			if (!aboutToCollide)
				transform.position = MovePos (transform.position, player_input.mine, player_input.currentInput, moveSpeed * Time.deltaTime);
			
			transform.rotation = MoveRot (transform.rotation, player_input.mine, player_input.currentInput, turnSpeed * Time.deltaTime); 

			CheckForDive (); 
		}
		else if (player_input.mine) {
			//test.position = serverPos;
			//test.rotation = serverRot;

			//test2.text = "Last Recieved S#: " + lastRecievedSequenceNumber + "     Last Val: " + lastValidatedRequest + "   S#: " + player_input.sequenceNumber + "    Inputs To Val: " + player_input.requests.Count;

			if (clientSidePrediction) {
				if (!aboutToCollide)
					transform.position = MovePos (transform.position, true, player_input.currentInput, moveSpeed * Time.deltaTime); //client-side prediciton
					
				transform.rotation = MoveRot (transform.rotation, true, player_input.currentInput, turnSpeed * Time.deltaTime); //^^ 

				CheckForDive ();
			} 
			else {
				MoveNetwork (); 
			}

			if (lastRecievedSequenceNumber - lastValidatedRequest > 0 && serverReconsiliation && !diving) { 
				if (!aboutToCollide)
					validatePrediction (lastRecievedSequenceNumber, serverPos, serverRot); //server reconciliation
				else if (serverAboutToCollide)
					targetPos = serverPos;

				if (rubberbanding) {
					//if (targetPos != serverPos)
						transform.position = Vector2.Lerp ((Vector2)transform.position, targetPos, 0.1f); //rubberbanding
					//else if (Vector2.Distance (transform.position, targetPos) > 0.1f)
						//transform.position = Vector2.Lerp ((Vector2)transform.position, targetPos, 0.3f);
						
					//transform.rotation = Quaternion.Lerp (transform.rotation, targetRot, 0.2f);
				} 
				else {
					transform.position = targetPos;
					transform.rotation = targetRot;
				}
			}

			if (waitingForVotes && !trustMasterClient) {
				if (votes.Count == PhotonNetwork.room.PlayerCount)
					tallyVotes (); 
			}
		}
		else
			MoveNetwork (); //interpolate other players
	}

	void tallyVotes () {
		waitingForVotes = false;
		int votesFalse = 0;

		foreach (bool vote in votes) {
			if (vote == false) {
				votesFalse++;
			}
		}

		if (votesFalse >= votesNeeded) {
			Debug.Log ("Migrating Host: " + votesFalse + " / " + PhotonNetwork.room.PlayerCount); 
			Camera.main.GetComponent<GameManager> ().MigrateHost ();
		}
		else {
			Debug.Log ("Not Migrating Host: " + votesFalse + " / " + PhotonNetwork.room.PlayerCount); 
		}

		votes.Clear (); 
	}
		
	void validatePrediction (int LRSN, Vector2 SP, Quaternion SR) { 
		//rewind
		Vector2 pos = SP;
		Quaternion rot = SR;
	
		request[] copyOfRequests = player_input.requests.ToArray ();
		foreach (request Request in copyOfRequests) {
			if (Request.id <= LRSN) {

				if (Request.id == LRSN) {
					//check master client
					GameState correspondingGameState = manager.FindGameState (FindRequest (LRSN + 1).timeStamp);  
					if (correspondingGameState != null && !trustMasterClient && !waitingForVotes && player_input.requests.Count > 1 && (Vector2.Distance (correspondingGameState.positions [index], SP) > posTrustThreshold || Quaternion.Angle (correspondingGameState.rotations [index], SR) > rotTrustThreshold)) {
						Debug.LogWarning ("Is the master client cheating: " + Vector2.Distance (correspondingGameState.positions [index], SP)); 
						Debug.Log (LRSN + " : " + player_input.sequenceNumber + " : " + player_input.requests.Count + " : " + player_input.requests[0].id); 

						waitingForVotes = true;

						PV.RpcSecure (
							"validateMasterClient", PhotonTargets.All, true, 
							PhotonNetwork.player,
							manager.FindGameState (Request.timeStamp).positions [index],
							manager.FindGameState (Request.timeStamp).rotations [index],
							SP,
							SR,
							Request.inputRequested.moveing,
							Request.inputRequested.moveDirection,
							Request.inputRequested.shooting,
							Request.inputRequested.shootDirection
						);  
					}
				}

				//clear out all the requests up to what the server has proccessed
				lastValidatedRequest = Request.id; 
				player_input.requests.Remove (Request);
			} 
			else {
				//add back unprocessed inputs
				pos = MovePos (pos, false, Request.inputRequested, turnSpeed * (float)timeBetweenSerialization); 
				rot = MoveRot (rot, false, Request.inputRequested, moveSpeed * (float)timeBetweenSerialization); 
			}
		}

		if (Vector2.Distance ((Vector2)transform.position, pos) > posCorrectionThreshold) 
			targetPos = pos;
		if (Quaternion.Angle (transform.rotation, rot) > rotCorrectionThreshold)
			targetRot = rot;

		//test3.position = pos;
		//test3.rotation = rot;
	}

	public Vector2 MovePos (Vector2 startingPos, bool mine, input _input, float speed) {
		if (diving)
			return startingPos;

		//move the player foward
		if (_input.moveing) {
			if (mine) {
				//show direction
				directionIndicator.SetActive (true); 
				directionIndicator.transform.rotation = Quaternion.FromToRotation (Vector2.up, _input.moveDirection);
				directionIndicator.transform.position = transform.position;
			}
			
			return (Vector2)startingPos + _input.moveDirection * speed;
		} 
		else if (mine)
			directionIndicator.SetActive (false);

		return startingPos;
	}

	public Quaternion MoveRot (Quaternion startingRot, bool mine, input _input, float speed) {
		if (diving)
			return startingRot; 

		Quaternion TargetRot;

		if (_input.shooting) {
			TargetRot = Quaternion.FromToRotation (Vector2.up, _input.shootDirection);

			if (mine && !player_input.currentInput.aimingGranade) {
				//show aim
				aimIndicator.SetActive (true); 
				aimIndicator.transform.rotation = TargetRot;
				aimIndicator.transform.position = transform.position;
			}
		}
		else {
			TargetRot = Quaternion.FromToRotation (Vector2.up, _input.moveDirection);

			if (mine)
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

	void MoveNetwork () {
		if (!serverDiving)
			transform.position = Vector2.MoveTowards ((Vector2)transform.position, serverPos, moveSpeed * Time.deltaTime);
		else
			transform.position = Vector2.MoveTowards ((Vector2)transform.position, serverPos, diveSpeed * Time.deltaTime);
		
		transform.rotation = Quaternion.Lerp (transform.rotation, serverRot, turnSpeed * Time.deltaTime * 2);
	}

	[PunRPC]
	void validateMasterClient (PhotonPlayer player, Vector2 startPos, Quaternion startRot, Vector2 serverPos, Quaternion serverRot, bool moveing, Vector2 moveDirection, bool shooting, Vector2 shootDirection) {
		input Input = new input ();
		Input.moveing = moveing;
		Input.moveDirection = moveDirection;
		Input.shooting = shooting;
		Input.shootDirection = shootDirection;

		bool trustServer = true;

		Vector2 pos = MovePos (startPos, false, Input, moveSpeed * (float)timeBetweenSerialization);
		Quaternion rot = MoveRot (startRot, false, Input, turnSpeed * (float)timeBetweenSerialization);

		if (Vector2.Distance (pos, serverPos) > posTrustThreshold || Quaternion.Angle (rot, serverRot) > rotTrustThreshold)
			trustServer = false;

		PV.RpcSecure ("voteAuthenticity", player, true, trustServer); 
	}

	[PunRPC]
	void voteAuthenticity (bool trustServer) {
		votes.Add (trustServer);
		Debug.Log ("vote recieved: " + trustServer); 
	}

	request FindRequest (int id) {
		foreach (request Request in player_input.requests) {
			if (Request.id == id)
				return Request;
		}

		Debug.LogError ("no request matches id: " + id); 
		return null;
	}

	public void SerializeState (PhotonStream stream, PhotonMessageInfo info) {
		if (stream.isWriting) {
			//this is a player on the master client : send transform
			stream.SendNext ((Vector2)transform.position);
			stream.SendNext (transform.rotation); 
			stream.SendNext (aboutToCollide); 
			stream.SendNext (diving); 
			stream.SendNext (player_input.sequenceNumber);  
		} 
		else {
			//this is the local player : recieve positions
			serverPos = (Vector2)stream.ReceiveNext (); 
			serverRot = (Quaternion)stream.ReceiveNext ();
			serverAboutToCollide = (bool)stream.ReceiveNext (); 
			serverDiving = (bool)stream.ReceiveNext () ;
			lastRecievedSequenceNumber = (int)stream.ReceiveNext ();  
		}
	}
}

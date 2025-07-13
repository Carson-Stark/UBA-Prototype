using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour {
	public GameObject respawning;

	GameManager manager;

	GameObject player;

	int team;

	void Start () {
		manager = Camera.main.GetComponent<GameManager> ();
		team = 0;

		if (PhotonNetwork.isMasterClient)
			Invoke ("spawnShip", Random.Range (1.01f, 2));

		if (PhotonView.Get (this).isMine) 
			transform.GetChild (0).gameObject.SetActive (true);  //enable UI
	}

	void Update () {
		if (player == null)
			respawning.SetActive (true);
	}

	public void spawnShip () {
		string name = "Soldier";
		int id = PhotonNetwork.AllocateSceneViewID ();
		Transform Sp = manager.AssignSP (team);

		PhotonView.Get(this).RPC ("spawnShipNetwork", PhotonTargets.AllBufferedViaServer, name, id, (Vector2)Sp.position, Sp.rotation);
	}

	[PunRPC]
	void spawnShipNetwork (string name, int id, Vector2 spawnPoint, Quaternion spawnRotation) {
		Object character = Resources.Load(name);
		player = (GameObject) Instantiate (character, spawnPoint, spawnRotation);
		player.GetComponent<PhotonView>().viewID = id; 

		player.GetComponent<PlayerMovement>().player_input = GetComponent<PlayerInput>(); 

		if (GetComponent<PhotonView> ().isMine) {
			player.GetComponent<PlayerMovement> ().directionIndicator = transform.GetChild (1).gameObject; 
			player.GetComponent<PlayerMovement> ().aimIndicator = transform.GetChild (2).gameObject; 
			manager.localPlayer = player;
		}

		manager.charactersInGame.Add (player);

		Debug.Log (PhotonView.Get (this).owner.NickName + " was spawned"); 

		respawning.SetActive (false); 

		player.transform.parent = transform;
	}

	void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info) {

		GetComponent<PlayerInput>().SerializeState (stream, info); 
	}
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerOffline : MonoBehaviour {
	public GameObject respawning;

	GameManager manager;
	public string nickname = "Carson";
	GameObject player;

	int team;

	void Start () {
		manager = Camera.main.GetComponent<GameManager> ();
		team = 0;

		Invoke ("spawnCharacter", Random.Range (1.01f, 2));

		transform.GetChild (0).gameObject.SetActive (true);  //enable UI
	}

	void Update () {
		if (player == null)
			respawning.SetActive (true);
	}

	public void spawnCharacter () {
		string name = "Soldier_off";
		Transform Sp = manager.AssignSP (team);
		player = (GameObject)Instantiate (Resources.Load (name), (Vector2)Sp.position, Sp.rotation); 

		player.GetComponent<PlayerMovementOffline> ().player_input = GetComponent<PlayerInputOffline> ();
		player.GetComponent<PlayerMovementOffline> ().directionIndicator = transform.GetChild (1).gameObject; 
		player.GetComponent<PlayerMovementOffline> ().aimIndicator = transform.GetChild (2).gameObject; 
		manager.localPlayer = player;

		manager.charactersInGame.Add (player);

		Debug.Log (nickname + " was spawned"); 

		respawning.SetActive (false); 

		player.transform.parent = transform;
	}
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour {

	public bool online;

	public AudioClip sound;
	public float coolDown;
	public int damage; 
	public int maxAmmoInClip;
	public float reloadTime;
	public Transform shotOrgin;
	public GameObject blood;
	public LineRenderer line;
	[HideInInspector] public PlayerMovement movement;
	[HideInInspector]public PlayerMovementOffline movement_off;
	public bool lagCompensation;

	PlayerInput player_input;
	PlayerInputOffline player_input_off;
	PhotonView PV;
	GameManager manager;
	SpriteManager rend;

	float coolDownCount;
	float currentAmmoInClip;
	float reloadCount;

	bool reloading;

	GameObject test;
	GameObject test2;

	void Start () {
		PV = GetComponent<PhotonView> ();

		if (online) {
			movement = GetComponent<PlayerMovement> ();
			player_input = movement.player_input; 
		} 
		else {
			movement_off = GetComponent<PlayerMovementOffline> ();
			player_input_off = movement_off.player_input; 
		}
		
		manager = Camera.main.GetComponent<GameManager> (); 
		rend = GetComponent<SpriteManager> (); 

		coolDownCount = coolDown;
		currentAmmoInClip = maxAmmoInClip;
		reloadCount = reloadTime;

		reloading = false;

		test = GameObject.FindGameObjectWithTag ("debug"); 
		test2 = GameObject.FindGameObjectWithTag ("debug2");
	}

	void Update () {
		input currentIn;
		if (online)
			currentIn = player_input.currentInput;
		else
			currentIn = player_input_off.currentInput;

		bool diving;
		if (online)
			diving = movement.diving;
		else
			diving = movement_off.diving;

		if (reloading) {
			reloadCount -= Time.deltaTime;

			if (reloadCount < 0) {
				reloading = false;
				reloadCount = reloadTime;
				currentAmmoInClip = maxAmmoInClip;
			}
		} 
		else if (currentIn.shooting && !currentIn.aimingGranade && !diving) {
			coolDownCount -= Time.deltaTime;

			if (coolDownCount <= 0) {
				checkForHit ();

				if (online && PhotonNetwork.isMasterClient)
					PV.RPC ("FireGraphics", PhotonTargets.All, false);
				else {
					FireGraphics (true); //prediciton
				}

				currentAmmoInClip--;
				if (currentAmmoInClip < 0 && !reloading) {
					reloading = true;
					Debug.Log ("reloading");  
				}

				coolDownCount = coolDown;
			}
		}
	}

	[PunRPC]
	void FireGraphics (bool prediciton) {
		if (!online || !player_input.mine || PhotonNetwork.isMasterClient || prediciton) {
			rend.StartCoroutine ("DisplayShootSprite");
			AudioSource.PlayClipAtPoint (sound, transform.position); 

			RaycastHit2D hit = Physics2D.Raycast ((Vector2)shotOrgin.position, (Vector2)transform.up, 100);
			if (hit) {
				line.SetPosition (0, shotOrgin.position); 
				line.SetPosition (1, hit.point); 
				line.enabled = true;

				Invoke ("disableLine", 0.05f); 
			}
		}
	}

	void disableLine () {
		line.enabled = false;
	}

	void checkForHit () {
		if (online) {
			if (PhotonNetwork.isMasterClient && !player_input.mine) {
				//rewind all players positions
				if (lagCompensation) {
					foreach (GameObject player in manager.charactersInGame) {
						Debug.Log ("rewind: " + player_input.timeRequestSent); 
						manager.rewindGame (player_input.timeRequestSent); 
					}

					//test.transform.position = manager.charactersInGame [0].transform.position;
					//test2.transform.position = manager.charactersInGame [1].transform.position;
				}
			}
		}

		//check for hit
		RaycastHit2D hit = Physics2D.Raycast ((Vector2)shotOrgin.position, (Vector2)transform.up, 100);
		if (hit) {
			if (hit.transform.tag == "Player" && !hit.transform.GetComponent<PlayerMovement> ().diving) {
				if (online) {
					//if we are the server : we are in charge of damaging the player
					if (PhotonNetwork.isMasterClient)
						hit.transform.GetComponent<PhotonView> ().RpcSecure ("Damage", PhotonTargets.AllViaServer, true, damage, transform.parent.GetComponent<PhotonView> ().owner);
				} 
				//else {
			//		hit.transform.GetComponent<Bot> (); 
				//}

				//predict and show instant feedback (dosen't affect gameplay)
				Instantiate (blood, hit.transform.position, transform.rotation); 
			} 
			else if (PhotonNetwork.isMasterClient && hit.transform.tag == "Damageable") {
				if (online)
					hit.transform.GetComponent<PhotonView> ().RpcSecure ("Damage", PhotonTargets.AllViaServer, true, damage, transform.parent.GetComponent<PhotonView> ().owner);
			}
		}

		if (online) {
			if (PhotonNetwork.isMasterClient && !player_input.mine) {
				if (lagCompensation) {
					//revert all players positions back to present
					foreach (GameObject player in manager.charactersInGame) {
						Debug.Log ("return: " + PhotonNetwork.time); 
						manager.rewindGame (PhotonNetwork.time); 
					}
				}
			}
		}
	}
}

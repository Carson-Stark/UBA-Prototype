using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviour {

	public int startingHealth;
	public GameObject nametag;
	public AudioClip hitSound;

	PhotonPlayer lastHitFrom;

	PhotonView PV;

	int health;
	GameObject tag;
	Text health_tx;

	void Start () {
		PV = PhotonView.Get (this); 

		health = startingHealth;

		if (GetComponent<PlayerMovement> ().player_input.mine) {
			Camera.main.GetComponent<CameraFollow> ().playerToFollow = gameObject; 
			Camera.main.GetComponent<CameraFollow> ().enabled = true;

			health_tx = GameObject.FindGameObjectWithTag ("health").GetComponent<Text> ();
			health_tx.text = "Health: " + health;
		}

		tag = (GameObject)Instantiate (nametag, transform.position, Quaternion.identity); 
		tag.GetComponent<TextMesh> ().text = transform.parent.GetComponent<PhotonView>().owner.NickName;
	}

	[PunRPC]
	void Damage (int damage, PhotonPlayer player) {
		lastHitFrom = player;
		GetComponent<SpriteManager> ().StartCoroutine ("DisplayHitSprite"); 
		AudioSource.PlayClipAtPoint (hitSound, transform.position); 
		health -= damage;

		if (GetComponent<PlayerMovement> ().player_input.mine)
			health_tx.text = "Health: " + health;
	}

	void Update () {
		Vector3 tagPos = transform.position;
		if (Mathf.Abs (transform.rotation.eulerAngles.z) < 90 || Mathf.Abs (transform.rotation.eulerAngles.z) > 270)
			tagPos.y -= 1;
		else
			tagPos.y += 1;
		tagPos.z = -1;
		tag.transform.position = tagPos;

		if (health <= 0 && PhotonNetwork.isMasterClient)
			PV.RpcSecure ("Die", PhotonTargets.All, true);
	}

	[PunRPC]
	void Die () {
		if (PhotonNetwork.isMasterClient) {
			transform.parent.GetComponent<Player> ().Invoke ("spawnShip", Random.Range (1.01f, 2));
			lastHitFrom.AddScore (1); 
		}
		if (GetComponent<PlayerMovement> ().player_input.mine) {
			Camera.main.GetComponent<CameraFollow> ().enabled = false;
			Camera.main.GetComponent<MiniMap> ().enabled = false;
		}
		
		Camera.main.GetComponent<GameManager>().charactersInGame.Remove (gameObject);  
		Destroy (tag); 
		Destroy (gameObject); 
	}

	void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info) {

		GetComponent<PlayerMovement>().SerializeState (stream, info);
	}
}

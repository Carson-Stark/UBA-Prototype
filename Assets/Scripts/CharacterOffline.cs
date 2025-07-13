using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterOffline : MonoBehaviour {
	public int startingHealth;
	public GameObject nametag;
	public AudioClip hitSound;

	int health;
	GameObject Tag;
	Text health_tx;

	Bot lastHitFrom;

	void Start () {
		health = startingHealth;

		//Camera.main.GetComponent<CameraFollow> ().playerToFollow = gameObject; 
		//Camera.main.GetComponent<CameraFollow> ().enabled = true;

		//health_tx = GameObject.FindGameObjectWithTag ("health").GetComponent<Text> ();
		//health_tx.text = "Health: " + health;

		Tag = (GameObject)Instantiate (nametag, transform.position, Quaternion.identity); 
		Tag.GetComponent<TextMesh> ().text = transform.parent.GetComponent<PlayerOffline> ().nickname;
	}

	void Damage (int damage, Bot bot) {
		lastHitFrom = bot;
		GetComponent<SpriteManager> ().StartCoroutine ("DisplayHitSprite"); 
		AudioSource.PlayClipAtPoint (hitSound, transform.position); 
		health -= damage;

		health_tx.text = "Health: " + health;
	}

	void Update () {
		Vector3 tagPos = transform.position;
		if (Mathf.Abs (transform.rotation.eulerAngles.z) < 90 || Mathf.Abs (transform.rotation.eulerAngles.z) > 270)
			tagPos.y -= 1;
		else
			tagPos.y += 1;
		tagPos.z = -1;

		if (Tag != null)
			Tag.transform.position = tagPos;

		if (health <= 0)
			Die (); 
	}
		
	void Die () {
		transform.parent.GetComponent<PlayerOffline> ().Invoke ("spawnCharacter", Random.Range (1.01f, 2));
		lastHitFrom.score += 1; 

		Camera.main.GetComponent<CameraFollow> ().enabled = false;

		Camera.main.GetComponent<OfflineGameManager>().charactersInGame.Remove (gameObject);  
		Destroy (Tag); 
		Destroy (gameObject); 
	}
}
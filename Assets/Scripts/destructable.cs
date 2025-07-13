using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class destructable : MonoBehaviour {

	public int health;
	public bool explosive;
	public int blastRadius;
	public float blastForce;
	public int damage;
	public int damageableLayer;
	public AudioClip sound;
	public GameObject explosion;

	PhotonPlayer lastHitBy;
	bool exploding;

	[PunRPC]
	void Damage (int damage, PhotonPlayer player) {
		if (exploding)
			return;
		
		GetComponent<SpriteManager> ().StartCoroutine ("DisplayHitSprite"); 
		lastHitBy = player;
		health -= damage;

		if (health <= 0) {
			if (explosive)
				Invoke ("explode", 0.3f); 
			else
				Destroy (gameObject); 
		}
	}

	void explode () {
		exploding = true;

		if (PhotonNetwork.isMasterClient)
			checkForHits ();

		explodeGraphics ();
	}

	void checkForHits () {
		Collider2D[] objectsInRaduis = Physics2D.OverlapCircleAll (transform.position, blastRadius, 1 << damageableLayer);

		foreach (Collider2D coll in objectsInRaduis) {
			if (coll == GetComponent<Collider2D> ())
				continue;

			//if (coll.tag != "Player")
				//coll.GetComponent<Rigidbody2D>().AddForce (-blastForce * (transform.position - coll.transform.position), ForceMode2D.Impulse); 
			
			coll.GetComponent<PhotonView> ().RPC ("Damage", PhotonTargets.All, damage, lastHitBy); 
		}
	}
		
	void explodeGraphics () {
		GameObject explo = (GameObject)Instantiate (explosion, transform.position, transform.rotation);
		Destroy (explo, 0.3f); 
		AudioSource.PlayClipAtPoint (sound, transform.position); 
		Destroy (gameObject); 
	}
}

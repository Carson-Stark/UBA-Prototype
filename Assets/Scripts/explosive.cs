using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class explosive : MonoBehaviour {

	public int blastRadius;
	public int damage;
	public int damageableLayer;
	public AudioClip sound;
	public GameObject explosion;

	PhotonPlayer lastHitBy;
	bool exploding;

	public void explode () {
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class granadeObj : MonoBehaviour {

	[HideInInspector] public bool mine;
	[HideInInspector] public PhotonPlayer owner;
	[HideInInspector] public Vector2 target;
	public GameObject explosion;
	public GameObject blastHole;
	public AudioClip sound;
	public float moveSpeed;
	public float rotateSpeed;
	public float explosionDelay;
	public float blastRaduis;
	public int damage;
	public int damageableLayer;

	bool countingDown;

	void Update () {
		if (target == null)
			return;
		
		if (Vector2.Distance (transform.position, target) > 0.01f) {
			transform.position = Vector2.MoveTowards (transform.position, target, moveSpeed * Time.deltaTime);
			transform.Rotate (0, 0, rotateSpeed * Time.deltaTime); 
		}
		else if (!countingDown) {
			countingDown = true;
			transform.GetChild (0).GetComponent<Animator>().enabled = true;

			Invoke ("explode", explosionDelay);
		}
	}

	void explode () {
		if (PhotonNetwork.isMasterClient)
			checkForHits ();

		explodeGraphics ();
		Debug.Log ("HI"); 
	}

	void checkForHits () {
		Collider2D[] objectsInRaduis = Physics2D.OverlapCircleAll (transform.position, blastRaduis, 1 << damageableLayer);

		foreach (Collider2D coll in objectsInRaduis) {
			coll.GetComponent<PhotonView> ().RpcSecure ("Damage", PhotonTargets.All, true, damage, owner); 
		}
	}
		
	void explodeGraphics () {
		GameObject explo = (GameObject)Instantiate (explosion, transform.position, transform.rotation);
		Destroy (explo, 0.3f); 
		Instantiate (blastHole, transform.position, transform.rotation); 
		AudioSource.PlayClipAtPoint (sound, transform.position); 
		Destroy (gameObject); 
	}
}

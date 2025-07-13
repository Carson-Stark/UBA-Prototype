using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteManager : MonoBehaviour {

	public SpriteRenderer rend;

	public Sprite shootingSprite;
	public float shootSpriteDisplayTime;
	public Sprite hitSprite;
	public float hitSpriteDisplayTime;

	Sprite defaultSprite;

	void Start () {
		defaultSprite = rend.sprite;
	}

	IEnumerator DisplayShootSprite () {
		rend.sprite = shootingSprite;

		yield return new WaitForSeconds (shootSpriteDisplayTime); 

		rend.sprite = defaultSprite;
	}

	IEnumerator DisplayHitSprite () {
		rend.sprite = hitSprite;

		yield return new WaitForSeconds (hitSpriteDisplayTime); 

		rend.sprite = defaultSprite;
	}
}

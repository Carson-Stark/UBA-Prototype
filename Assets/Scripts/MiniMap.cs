using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMap : MonoBehaviour {

	public GameObject map;
	public RectTransform miniMap;
	public RectTransform playerIcon;

	[HideInInspector] public PlayerInput inputs;

	float miniMapSize, mapSize;
	float ratio;

	void Start () {

		miniMapSize = miniMap.rect.width;
		mapSize = map.GetComponent<SpriteRenderer>().bounds.size.x;

		ratio = miniMapSize / mapSize;
	}

	void Update () {
		playerIcon.rotation = Quaternion.FromToRotation (Vector2.up, inputs.currentInput.moveDirection);

		miniMap.anchoredPosition = transform.position * -ratio;
	}
}

using UnityEngine;
using System.Collections;

public class WaterAnimation : MonoBehaviour {
	
	public float waveScale = 0.1f;
	public float colorScale = 20f;
	public bool useCoordOffset;

	float baseScaleY;
	float baseY;
	float randomOffset;

	Color baseColour;

	Renderer ren;

	void Awake() {
		ren = GetComponent<Renderer>();
	}

	void Start() {
		baseScaleY = transform.localScale.y;
		baseY = transform.position.y;
		randomOffset = Random.Range(0f, 2 * Mathf.PI);

		baseColour = ren.material.color;

		if(useCoordOffset) {
			randomOffset = transform.position.x + transform.position.z;
		}
	}

	void Update() {
		float waveHeight = Mathf.Sin(Time.time + randomOffset) * waveScale;
		transform.localScale = new Vector3(transform.localScale.x, baseScaleY + waveHeight, transform.localScale.z);
		transform.position = new Vector3(transform.position.x, baseY + waveHeight / 2, transform.position.z);
		ren.material.color = baseColour + new Color(0.05f, 0.05f, 0.05f) * Mathf.Sin(Time.time + randomOffset);
	}

}

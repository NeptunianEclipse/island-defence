using UnityEngine;
using System.Collections;

public class ScaleToOrthographicSize : MonoBehaviour {

	Vector3 baseScale;
	static float baseOrthoSize;

	void Start() {
		baseScale = transform.localScale;
		if(baseOrthoSize == 0) {
			baseOrthoSize = Camera.main.orthographicSize;
		}
	}

	void Update() {
		transform.localScale = baseScale * (Camera.main.orthographicSize / baseOrthoSize);
	}

}

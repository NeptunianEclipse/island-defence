using UnityEngine;
using System.Collections;

public class ParticleDestruct : MonoBehaviour {

	ParticleSystem system;
	float waitTime;

	void Awake() {
		system = GetComponentInChildren<ParticleSystem>();
		waitTime = system.startLifetime;

		StartCoroutine(WaitForDestruction());
	}

	IEnumerator WaitForDestruction() {
		yield return new WaitForSeconds(waitTime);
		Destroy(gameObject);
	}

}

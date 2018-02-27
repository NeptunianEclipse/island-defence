using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PopupText : MonoBehaviour {

	public float duration;
	public float riseSpeed;

	Text risingText;

	void Awake() {
		risingText = transform.Find("PopupCanvas/Text").GetComponent<Text>();
		StartCoroutine(RiseAndFade());
	}

	IEnumerator RiseAndFade() {
		float startTime = Time.time;
		while(startTime + duration > Time.time) {
			transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);
			risingText.color = new Color(risingText.color.r, risingText.color.g, risingText.color.b, Mathf.Lerp(1, 0, (Time.time - startTime) / (duration)));
			yield return null;
		}
		Destroy(gameObject);
	}

}

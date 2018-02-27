using UnityEngine;
using System.Collections;

// Component that stores and manages player resources
public class ResourceManager : MonoBehaviour {

	public int initialLava;
	public int initialLifePower;

	int lava;
	public int Lava {
		get { return lava; }
		set { 
			lava = value;
			GameController.instance.lavaText.text = lava.ToString();
		}
	}

	int lifePower;
	public int LifePower {
		get { return lifePower; }
		set { 
			lifePower = value;
			GameController.instance.lifeText.text = lifePower.ToString();
		}
	}

	void Awake() {
		lava = initialLava;
		lifePower = initialLifePower;
	}

	void Start() {
		GameController.instance.lavaText.text = initialLava.ToString();
		GameController.instance.lifeText.text = initialLifePower.ToString();
	}

}
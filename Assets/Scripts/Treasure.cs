using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Treasure : MonoBehaviour {

	public int startingHealth;
	public int healRate;
	public int healLavaCost;
	public float unitCarryHeight;

	public PirateUnit controllingUnit;
	public bool hasBeenDugUp;

	public int x;
	public int y;
	public Tile CurrentTile {
		get { return mapController.map.GetTileAt(x, y); }
	}

	public delegate void TreasureEventHandler();
	public event TreasureEventHandler onTreasurePickup;
	public event TreasureEventHandler onTreasureDrop;

	MapController mapController;
	PirateAIController AIController;
	ResourceManager resourceManager;
	Transform volcano;
	Image volcanoHealthBar;
	Text volcanoHealthText;

	int health;
	public int Health {
		get { return health; }
	}

	public Treasure Dig(int damage) {
		health = Mathf.Clamp(health - damage, 0, startingHealth);

		if(health < startingHealth) {
			volcanoHealthBar.transform.parent.gameObject.SetActive(true);
			volcanoHealthBar.fillAmount = (float)health / (float)startingHealth;
			volcanoHealthText.text = Mathf.Clamp(health, 0, startingHealth).ToString();
		} else {
			volcanoHealthBar.transform.parent.gameObject.SetActive(false);
		}

		if(health <= 0) {
			hasBeenDugUp = true;
			transform.Find("Treasure").gameObject.SetActive(true);
			transform.localPosition = new Vector3(transform.position.x, unitCarryHeight, transform.position.z);
			if(onTreasurePickup != null) {
				onTreasurePickup();
			}
			return this;
		}
		return null;
	}

	public void Heal() {
		resourceManager.Lava -= healLavaCost;
		GameController.instance.CreatePopupText("-" + healLavaCost + " Lava", Color.red, new Vector3(mapController.map.VolcanoX + 0.5f, 1, mapController.map.VolcanoY + 0.5f));
		Dig(-healRate);
	}

	public void Drop(int x, int y) {
		controllingUnit = null;
		transform.parent = null;
		if(onTreasureDrop != null)
			onTreasureDrop();
		StartCoroutine(mapController.AnimateObjRiseFall(gameObject, CurrentTile.Type.surfaceHeight, false));
	}

	void Awake() {
		mapController = FindObjectOfType<MapController>();
		AIController = FindObjectOfType<PirateAIController>();
		resourceManager = FindObjectOfType<ResourceManager>();
	}

	void Start() {
		health = startingHealth;
		volcano = GameObject.FindGameObjectWithTag("Volcano").transform;
		volcanoHealthBar = volcano.Find("Offset/VolcanoCanvas/BG/HealthBar").GetComponent<Image>();
		volcanoHealthText = volcano.Find("Offset/VolcanoCanvas/BG/HealthText").GetComponent<Text>();
	}

}

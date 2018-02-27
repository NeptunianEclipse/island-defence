using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

// Controls the overall flow of the game
public class GameController : MonoBehaviour {

	public static GameController instance;

	public enum GameState {
		MainMenu,
		Game
	}

	GameState currentGameState = GameState.MainMenu;
	public GameState CurrentGameState {
		get { return currentGameState; }
		set {
			currentGameState = value;
			switch(currentGameState) {
			case GameState.MainMenu:

				break;
			case GameState.Game:

				break;
			}
		}
	}

	public enum MenuState {
		MainMenu,
		Help
	}
	public MenuState currentMenuState;

	public delegate void TurnEventHandler(int turnNumber, Turn currentTurn);
	public event TurnEventHandler onTurnChange;

	public int playerActionsPerTurn = 1;

	public Transform highlight;
	public Material highlightMat;
	public Material highlightDisabledMat;

	public GameObject popupTextPrefab;
	public float UIHeight;

	public Text turnNumberText;
	public Text currentTurnText;
	public Text lifeText;
	public Text lavaText;

	public GameObject gameUI;
	public GameObject buttonPanel;
	public GameObject helpPanel;
	public GameObject title;
	public GameObject whiteFade;
	public GameObject gameOverPanel;

	public Text survivedTurnsText;

	public RectTransform lifeTooltipPanel;
	public Text lifeTooltipText;
	public RectTransform lavaTooltipPanel;
	public Text lavaTooltipText;

	public RectTransform attackTextPanel;

	public float fadeSpeed;

	public bool canInteract;

	RectTransform highlightTextPanel;
	Text highlightText;

	Renderer highlightRenderer;

	int lastTileX;
	int lastTileY;

	int playerActions;
	public int PlayerActions {
		get { return playerActions; }
		set {
			playerActions = value;
			canInteract = true;
			if(playerActions <= 0) {
				EndPlayersTurn();
			}
		}
	}

	int turnNumber = 1;
	public int TurnNumber {
		get { return turnNumber; }
	}

	public enum Turn {
		Player,
		Pirates
	}

	Turn currentTurn = Turn.Player;
	public Turn CurrentTurn {
		get { return currentTurn; }
	}

	MapController mapController;
	Pathfinding pathfinding;
	PirateAIController AIController;
	Treasure treasure;

	public void AdvanceTurn() {
		
		if(currentTurn == Turn.Player) {
			currentTurn = Turn.Pirates;
		} else {
			currentTurn = Turn.Player;
			turnNumber++;
		}

		turnNumberText.text = turnNumber.ToString();
		currentTurnText.text = currentTurn == Turn.Player ? "YOUR TURN" : "PIRATES TURN";

		lastTileX = 0;
		lastTileY = 0;

		if(onTurnChange != null)
			onTurnChange(turnNumber, currentTurn);

		if(currentTurn == Turn.Player)
			PlayersTurn();
	}

	public void GameOver() {
		currentGameState = GameState.MainMenu;
		GameObject.FindGameObjectWithTag("Volcano").transform.Find("Offset/VolcanoCanvas/BG").gameObject.SetActive(false);
		gameUI.SetActive(false);
		gameOverPanel.SetActive(true);
		survivedTurnsText.text = "Survived Turns: " + turnNumber;
		CurrentGameState = GameState.MainMenu;
	}

	public void CreatePopupText(string displayText, Color colour, Vector3 location) {
		Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));
		GameObject popupText = Instantiate(popupTextPrefab, location + randomOffset, Quaternion.identity) as GameObject;
		popupText.transform.Find("PopupCanvas/Text").LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
		Text text = popupText.transform.Find("PopupCanvas/Text").GetComponent<Text>();
		text.text = displayText;
		text.color = colour;
	}

	public void EndlessMode() {
		buttonPanel.gameObject.SetActive(false);
		title.SetActive(false);
		helpPanel.SetActive(true);
		canInteract = false;
		CurrentGameState = GameState.Game;
		NewGame();
	}

	public void ShowHelp() {
		currentMenuState = MenuState.Help;
		buttonPanel.SetActive(false);
		helpPanel.SetActive(true);
		title.SetActive(false);
	}

	public void HideHelp() {
		if(CurrentGameState == GameState.MainMenu) {
			currentMenuState = MenuState.MainMenu;
			buttonPanel.SetActive(true);
			helpPanel.SetActive(false);
			title.SetActive(true);
		} else {
			helpPanel.SetActive(false);
			gameUI.SetActive(true);
			canInteract = true;
		}
	}

	public void QuitGame() {
		Application.Quit();
	}

	public void CloseGO() {
		currentMenuState = MenuState.MainMenu;
		buttonPanel.SetActive(true);
		helpPanel.SetActive(false);
		title.SetActive(true);
		gameOverPanel.SetActive(false);
		Application.LoadLevel(0);
	}

	public void EndTurn() {
		if(currentTurn == Turn.Player && canInteract) {
			AdvanceTurn();
		}
	}

	public void PirateAttackBegin() {
		attackTextPanel.gameObject.SetActive(true);
	}

	public void PirateAttackEnd() {
		attackTextPanel.gameObject.SetActive(false);
	}

	void NewGame() {
		turnNumber = 0;
		currentTurn = Turn.Player;

		//gameUI.SetActive(true);
	}

	void PlayersTurn() {
		playerActions = playerActionsPerTurn;
		canInteract = true;
	}

	void EndPlayersTurn() {
		AdvanceTurn();
	}

	void Awake() {
		if(instance != null) {
			Debug.LogError("Multiple game controllers!");
		} else {
			instance = this;
		}

		mapController = FindObjectOfType<MapController>();
		pathfinding = FindObjectOfType<Pathfinding>();
		AIController = FindObjectOfType<PirateAIController>();

		highlightRenderer = highlight.GetComponentInChildren<Renderer>();
		highlightTextPanel = highlight.Find("HighlightCanvas/BG").GetComponent<RectTransform>();
		highlightText = highlight.Find("HighlightCanvas/BG/HealthText").GetComponent<Text>();

		AIController.onPirateAttackBegin += PirateAttackBegin;
		AIController.onPirateAttackEnd += PirateAttackEnd;
	}

	void Start() {
		mapController.map.GenerateOceanMap();
		mapController.GenerateTileGOs();

		pathfinding.CreateNodeGrid();
		AIController.Init();

		treasure = FindObjectOfType<Treasure>();

		turnNumberText.text = "1";
		currentTurnText.text = "YOUR TURN";

		buttonPanel.SetActive(true);
		helpPanel.SetActive(false);
		title.SetActive(true);
		gameUI.SetActive(false);
		gameOverPanel.SetActive(false);

		canInteract = true;
	}

	void UpdateTooltip(Tile tile, List<PirateUnit> unitsInTile) {
		if(unitsInTile.Count > 0) {
			if(Vector3.Distance(new Vector3(tile.X, 0, tile.Y), new Vector3(mapController.map.VolcanoX, 0, mapController.map.VolcanoY)) < mapController.lavaShootRange) {
				lavaTooltipText.text = "Fire at pirates (" + mapController.lavaAttackDamage + " dam) -" + mapController.lavaAttackCost + " Lava";
				lifeTooltipText.text = "Ensnare pirates with vines -" + mapController.vineCost + " Life";
			} else {
				lavaTooltipText.text = "Out of range";
				lifeTooltipText.text = "Out of range";
			}
		} else if(tile.Type.name == "Volcano"){
			lavaTooltipText.text = "Restore volcano (+1 health) -" + treasure.healLavaCost + " Lava";
			lifeTooltipText.text = "N/A";
		} else {
			Tile.TileType lavaUpgrade = Tile.GetTypeByName(tile.Type.lavaUpgrade);
			Tile.TileType growUpgrade = Tile.GetTypeByName(tile.Type.growUpgrade);
			if(lavaUpgrade.name != "Water") {
				if(lavaUpgrade.name == "Mountain" && (Mathf.Abs(tile.X - mapController.map.VolcanoX) == 1 || Mathf.Abs(tile.X - mapController.map.VolcanoX) == 0) && (Mathf.Abs(tile.Y - mapController.map.VolcanoY) == 1 || Mathf.Abs(tile.Y - mapController.map.VolcanoY) == 0)) {
					lavaTooltipText.text = "Upgrade volcano -" + (mapController.baseVolcanoUpgradeCost + Mathf.FloorToInt(Mathf.Pow(mapController.volcanoUpgradeCostRate, mapController.volcanoAdjacentMountains.Count)));
				} else {
					lavaTooltipText.text = "Build " + lavaUpgrade.name + " -" + lavaUpgrade.lavaCost + " Lava";
				}
			} else {
				lavaTooltipText.text = "N/A";
			}
			if(growUpgrade.name != "Water") {
				lifeTooltipText.text = "Build " + growUpgrade.name + " -" + growUpgrade.lifeCost + " Life";
			} else {
				lifeTooltipText.text = "N/A";
			}
		}
	}

	void Update() {
		if(currentGameState == GameState.Game) {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			if(Input.mousePosition.y > UIHeight && Physics.Raycast(ray, out hit)) {
				int tileX = Mathf.FloorToInt(hit.point.x + 0.25f);
				int tileY = Mathf.FloorToInt(hit.point.z + 0.25f);

				if(tileX >= 0 && tileX < mapController.mapSizeX && tileY >= 0 && tileY < mapController.mapSizeY) {

					List<PirateUnit> unitsInTile = AIController.GetUnitsInTile(mapController.map.GetTileAt(tileX, tileY));
					if(tileX != lastTileX || tileY != lastTileY)
						UpdateTooltip(mapController.map.GetTileAt(tileX, tileY), unitsInTile);

					if(unitsInTile.Count > 1) {
						highlightTextPanel.sizeDelta = new Vector2(15 * unitsInTile.Count, highlightTextPanel.rect.height);
						
						string text = unitsInTile.Count + " units:\n" + unitsInTile[0].NumPirates.ToString();
						for(int i = 1; i < unitsInTile.Count; i++) {
							text += " | " + unitsInTile[i].NumPirates.ToString();
						}
						highlightText.text = text;
						
						highlightTextPanel.gameObject.SetActive(true);
					} else {
						highlightTextPanel.gameObject.SetActive(false);
					}

					if(currentTurn == Turn.Player && canInteract) {
						highlightRenderer.material = highlightMat;

						if(Input.GetMouseButtonDown(0)) {
							canInteract = false;
							mapController.Lava(tileX, tileY);
						} else if(Input.GetMouseButtonDown(1)) {
							canInteract = false;
							mapController.Grow(tileX, tileY);
						}
					} else {
						highlightRenderer.material = highlightDisabledMat;
					}

					highlight.gameObject.SetActive(true);
					highlight.position = new Vector3(tileX, highlight.position.y, tileY);
				} else {
					highlight.gameObject.SetActive(false);
				}

				lastTileX = tileX;
				lastTileY = tileY;
			} else {
				highlight.gameObject.SetActive(false);
			}
		}
	}

}

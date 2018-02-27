using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Component that controls the actions of all pirates, as well as overall "strategy," spawning, path management, and rendering
public class PirateAIController : MonoBehaviour {

	// The base number of turns until a pirate attack (i.e. turns since the beginning of the game)
	public int baseAttackTurnDelay;

	// Rate at which the turn delay between attacks decreases
	public float attackTurnDelayRate;

	// The base number of units that will spawn in a pirate attack (first attack)
	public int baseUnitNumber;

	// Rate at which the number of units in successive attacks increases
	public float unitNumberRate;

	// The base strength of units that will spawn in a pirate attack (first attack) 
	public int baseUnitStrength;

	// Rate at which the strength of units in successive attacks increases
	public float unitStrengthRate;

	// The amount by which a units strength can vary from the attacks default
	public int unitStrengthVariance;

	// The maximum strength of a unit
	public int maxUnitStrength;

	// The number of turns between units of each attack spawning
	public int turnsBetweenUnitSpawns;

	public float unitHeight;

	public GameObject unitPrefab;

	public enum PirateState {
		Waiting,
		Attacking
	}
	public PirateState state;

	public enum AttackState {
		Seeking,
		Escaping
	}
	public AttackState attackState;

	public delegate void PirateEventHandler();
	public event PirateEventHandler onPirateAttackBegin;
	public event PirateEventHandler onPirateAttackEnd;
	
	MapController.Direction attackDirection;

	Pathfinding pathfinding;
	MapController mapController;
	CameraController camController;

	List<PirateUnit> activeUnits;

	int currentAttack;

	int turnsUntilNextAttack;
	int lastAttackEnd;

	int totalAttackUnits;
	int unitsYetToSpawn;
	int lastUnitSpawn;
	int unitStrength;

	public Tile TreasureTile {
		get {
			if(treasure.CurrentTile == mapController.Volcano) {
				if(mapController.map.GetTileAt(mapController.map.VolcanoX, mapController.map.VolcanoY + 1).Type.name == "Mountain") {
					if(mapController.map.GetTileAt(mapController.map.VolcanoX, mapController.map.VolcanoY - 1).Type.name == "Mountain") {
						if(mapController.map.GetTileAt(mapController.map.VolcanoX + 1, mapController.map.VolcanoY).Type.name == "Mountain") {
							if(mapController.map.GetTileAt(mapController.map.VolcanoX - 1, mapController.map.VolcanoY).Type.name == "Mountain") {
								int x = Random.Range(0, 1) * 2 - 1;
								int y = x == 0 ? Random.Range(0, 1) * 2 - 1 : 0;
								return mapController.map.GetTileAt(mapController.map.VolcanoX + x, mapController.map.VolcanoY + y);
							} else {
								return mapController.map.GetTileAt(mapController.map.VolcanoX - 1, mapController.map.VolcanoY);
							}
						} else {
							return mapController.map.GetTileAt(mapController.map.VolcanoX + 1, mapController.map.VolcanoY);
						}
					} else {
						return mapController.map.GetTileAt(mapController.map.VolcanoX, mapController.map.VolcanoY - 1);
					}
				} else {
					return mapController.map.GetTileAt(mapController.map.VolcanoX, mapController.map.VolcanoY + 1);
				}
			}
			return treasure.CurrentTile;
		}
	}

	Treasure treasure;

	public void TurnChanged(int turnNumber, GameController.Turn currentTurn) {
		if(currentTurn == GameController.Turn.Pirates) {
			StartCoroutine(PiratesTurn(turnNumber));
		} else {
			PlayersTurn(turnNumber);
		}
	}

	public void UnitDied(PirateUnit unit) {
		activeUnits.Remove(unit);
		Destroy(unit.gameObject);
	}

	public void NodeChanged(Pathfinding.Node node) {
		if(state == PirateState.Attacking) {
			foreach(PirateUnit unit in activeUnits) {
				List<Tile> path = pathfinding.FindPathTiles(unit.CurrentTile, unit.targetTile);
				unit.assignedPath = path;
			}
		}
	}

	public void TreasurePickedUp() {
		attackState = AttackState.Escaping;
		foreach(PirateUnit unit in activeUnits) {
			unit.targetTile = mapController.GetRandomEdgeTile();
			unit.assignedPath = pathfinding.FindPathTiles(unit.CurrentTile, unit.targetTile);
		}
	}

	public void TreasureDropped() {
		attackState = AttackState.Seeking;
		foreach(PirateUnit unit in activeUnits) {
			unit.targetTile = TreasureTile;
			unit.assignedPath = pathfinding.FindPathTiles(unit.CurrentTile, unit.targetTile);
		}
	}

	public void Init() {
		turnsUntilNextAttack = baseAttackTurnDelay;
		lastAttackEnd = 0;
		treasure = FindObjectOfType<Treasure>();
		
		pathfinding.onNodeChange += NodeChanged;
		treasure.onTreasurePickup += TreasurePickedUp;
		treasure.onTreasureDrop += TreasureDropped;
	}

	public List<PirateUnit> GetUnitsInTile(Tile tile) {
		List<PirateUnit> units = new List<PirateUnit>();
		foreach(PirateUnit unit in activeUnits) {
			if(unit.CurrentTile == tile) {
				units.Add(unit);
			}
		}
		return units;
	}

	IEnumerator PiratesTurn(int turnNumber) {
		if(state == PirateState.Waiting && turnNumber - lastAttackEnd >= turnsUntilNextAttack) {
			state = PirateState.Attacking;

			BeginAttack();
			if(onPirateAttackBegin != null)
				onPirateAttackBegin();
		}
		if(state == PirateState.Attacking) {
			if(unitsYetToSpawn > 0 && turnNumber - lastUnitSpawn > turnsBetweenUnitSpawns) {
				SpawnUnit();
				unitsYetToSpawn--;
				lastUnitSpawn = turnNumber;
			}

			if(activeUnits.Count > 0) {
				foreach(PirateUnit unit in activeUnits) {
					if(unit.assignedPath == null) {
						List<Tile> path = pathfinding.FindPathTiles(unit.CurrentTile, unit.targetTile);
						unit.assignedPath = path;
					}

					yield return StartCoroutine(unit.Move());
				}
			} else {
				state = PirateState.Waiting;
				lastAttackEnd = turnNumber;
				if(onPirateAttackEnd != null)
					onPirateAttackEnd();
			}
		}
	
		GameController.instance.AdvanceTurn();
	}

	void SpawnUnit() {

		int maxEdgeSpawnPos = attackDirection == MapController.Direction.East || attackDirection == MapController.Direction.West 
							? mapController.mapSizeY 
							: mapController.mapSizeX;
		int edgeSpawnPos = Random.Range(0, maxEdgeSpawnPos);

		Vector3 spawnPos = Vector3.zero;
		switch(attackDirection) {
		case MapController.Direction.North:
			spawnPos = new Vector3(edgeSpawnPos, unitHeight, mapController.mapSizeY - 1);
			break;
		case MapController.Direction.East:
			spawnPos = new Vector3(mapController.mapSizeX - 1, unitHeight, edgeSpawnPos);
			break;
		case MapController.Direction.South:
			spawnPos = new Vector3(edgeSpawnPos, unitHeight, 0);
			break;
		case MapController.Direction.West:
			spawnPos = new Vector3(0, unitHeight, edgeSpawnPos);
			break;
		}

		GameObject newUnitGO = Instantiate(unitPrefab, spawnPos, Quaternion.identity) as GameObject;
		PirateUnit newUnit = newUnitGO.GetComponent<PirateUnit>();

		newUnit.x = (int)spawnPos.x;
		newUnit.y = (int)spawnPos.z;
		newUnit.NumPirates = Random.Range(Mathf.Clamp(unitStrength - unitStrengthVariance, 0, maxUnitStrength), Mathf.Clamp(unitStrength + unitStrengthVariance, 0, maxUnitStrength));
		newUnit.targetTile = TreasureTile;

		activeUnits.Add(newUnit);
	}

	void BeginAttack() {
		currentAttack++;

		totalAttackUnits = baseUnitNumber + Mathf.FloorToInt(unitNumberRate * (currentAttack - 1));
		unitsYetToSpawn = totalAttackUnits;
		unitStrength = baseUnitStrength + Mathf.FloorToInt(unitStrengthRate * (currentAttack - 1));

		attackDirection = (MapController.Direction)Random.Range(0, 4);
	}

	void PlayersTurn(int turnNumber) {
		if(activeUnits.Count > 0) {
			for(int i = activeUnits.Count - 1; i > -1; i--) {
				activeUnits[i].ApplyEnvironment();
			}
		}
	}

	void Awake() {
		mapController = FindObjectOfType<MapController>();
		pathfinding = FindObjectOfType<Pathfinding>();
		camController = FindObjectOfType<CameraController>();
		activeUnits = new List<PirateUnit>();

		GameController.instance.onTurnChange += TurnChanged;

	}

	void Update() {

	}

}

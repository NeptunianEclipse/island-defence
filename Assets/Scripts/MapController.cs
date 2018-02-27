using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Component controller that "renders" the Map data to the game world (through the creation, destruction and modification of game objects)
public class MapController : MonoBehaviour {

	public GameObject[] tilePrefabs;

	public int mapSizeX;
	public int mapSizeY;

	public Transform tileContainer;
	public GameObject steamParticles;
	public GameObject treasurePrefab;
	public GameObject volcanicRockPrefab;

	public float riseAnimationMinDistance;
	public float riseAnimationSpeed;
	public float parabolaHeight;
	public float parabolaSpeed;

	public int vineCost;
	public int lavaAttackCost;
	public int lavaAttackDamage;

	public float lavaRockHeight;
	public float lavaShootRange;

	public int baseVolcanoUpgradeCost;
	public float volcanoUpgradeCostRate;

	public Map map;

	public Tile Volcano {
		get { return map.GetTileAt(map.VolcanoX, map.VolcanoY); }
	}

	public enum Direction {
		North,
		East,
		South,
		West
	}
	
	GameObject[,] tileGOs;

	List<Tile> forests;
	public List<Tile> volcanoAdjacentMountains; // Mountains that are in the 8 tiles surrounding the volcano

	ResourceManager resourceManager;
	PirateAIController AIController;

	GameObject treasureGO;
	Treasure treasure;

	public void GenerateTileGOs() {
		ClearTileGOs();
		for(int x = 0; x < mapSizeX; x++) {
			for(int y = 0; y < mapSizeY; y++) {
				Tile currentTile = map.GetTileAt(x, y);
				SetTileGO(currentTile, x, y, null);
			}
		}
		treasureGO = Instantiate(treasurePrefab, new Vector3(map.VolcanoX, 2, map.VolcanoY), Quaternion.identity) as GameObject;
		treasure = treasureGO.GetComponent<Treasure>();
		treasure.x = map.VolcanoX;
		treasure.y = map.VolcanoY;
	}

	public Vector3 TileCoordToWorldCoord(int tileX, int tileY) {
		return new Vector3(tileX, 0, tileY);
	}

	public void ClearTileGOs() {
		foreach(GameObject obj in tileGOs) {
			Destroy(obj);
		}
		tileGOs = new GameObject[mapSizeX, mapSizeY];
	}

	public void Grow(int x, int y) {
		Tile tile = map.GetTileAt(x, y);
		List<PirateUnit> unitsInTile = AIController.GetUnitsInTile(tile);
		if(unitsInTile.Count > 0) {
			if(resourceManager.LifePower >= vineCost) {
				foreach(PirateUnit unit in unitsInTile) {
					if(!unit.haveTreasure)
						unit.vined = true;
				}
			} else {
				GameController.instance.canInteract = true;
				return;
			}
		} else {
			Tile.TileType type = tile.Type;
			Tile.TileType upgradeType = Tile.GetTypeByName(type.growUpgrade);
			if(upgradeType.name != "Water") {
				int upgradeCost = upgradeType.lifeCost;
				if(resourceManager.LifePower >= upgradeCost) {
					resourceManager.LifePower -= upgradeCost;
					GameController.instance.CreatePopupText("-" + upgradeCost + " Life", Color.red, new Vector3(x + 0.5f, 1, y + 0.5f));
					map.Grow(tile);
				} else {
					GameController.instance.canInteract = true;
					return;
				}
			} else {
				GameController.instance.canInteract = true;
				return;
			}
		}
		GameController.instance.PlayerActions--;
	}

	public void Lava(int x, int y) {
		Tile tile = map.GetTileAt(x, y);
		List<PirateUnit> unitsInTile = AIController.GetUnitsInTile(tile);
		if(unitsInTile.Count > 0) {
			if(resourceManager.Lava >= lavaAttackCost && Vector3.Distance(new Vector3(map.VolcanoX, 0, map.VolcanoY), new Vector3(tile.X, 0, tile.Y)) < lavaShootRange) {
				resourceManager.Lava -= lavaAttackCost;
				StartCoroutine(ShootVolcanicRock(tile, unitsInTile));
			} else {
				GameController.instance.canInteract = true;
				return;
			}
		} else {
			Tile.TileType type = tile.Type;
			Tile.TileType upgradeType = Tile.GetTypeByName(type.lavaUpgrade);
			if(upgradeType.name != Tile.TileTypes[0].name || tile.Type.name == "Volcano") {
				if(tile.Type.name == "Volcano") {
					if(treasure.Health < treasure.startingHealth && resourceManager.Lava >= treasure.healLavaCost) {
						treasure.Heal();
						GameController.instance.PlayerActions--;
					} else {
						GameController.instance.canInteract = true;
						return;
					}
				} else {
					int upgradeCost = upgradeType.lavaCost;
					if(upgradeType.name == "Mountain" && (Mathf.Abs(x - map.VolcanoX) == 1 || Mathf.Abs(x - map.VolcanoX) == 0) && (Mathf.Abs(y - map.VolcanoY) == 1 || Mathf.Abs(y - map.VolcanoY) == 0)) {
						upgradeCost = baseVolcanoUpgradeCost + Mathf.FloorToInt(Mathf.Pow(volcanoUpgradeCostRate, volcanoAdjacentMountains.Count));
					}

					if(resourceManager.Lava >= upgradeCost) {
						resourceManager.Lava -= upgradeCost;
						GameController.instance.CreatePopupText("-" + upgradeCost + " Lava", Color.red, new Vector3(x + 0.5f, 1, y + 0.5f));
						StartCoroutine(ShootVolcanicRock(tile, new List<PirateUnit>()));
					} else {
						GameController.instance.canInteract = true;
						return;
					}
				}
			} else {
				GameController.instance.canInteract = true;
				return;
			}
		}
	}

	public void TileChanged(Tile tile, Tile oldTile) {
		SetTileGO(tile, tile.X, tile.Y, oldTile);
	}

	public void TurnChanged(int turnNumber, GameController.Turn currentTurn) {
		if(currentTurn == GameController.Turn.Player) {
			int lifeProduction = Tile.GetTypeByName("Forest").lifeProduction;
			foreach(Tile forest in forests) {
				resourceManager.LifePower += lifeProduction;
				GameController.instance.CreatePopupText("+" + lifeProduction + " Life", Color.green, new Vector3(forest.X + 0.5f, 1, forest.Y + 0.5f));
			}

			resourceManager.Lava++;
			GameController.instance.CreatePopupText("+1 Lava", Color.green, new Vector3(map.VolcanoX + 0.5f, 1, map.VolcanoY + 0.5f));
			int lavaProduction = Tile.GetTypeByName("Mountain").lavaProduction;
			foreach(Tile mountain in volcanoAdjacentMountains) {
				resourceManager.Lava += lavaProduction;
				GameController.instance.CreatePopupText("+" + lavaProduction + " Lava", Color.green, new Vector3(mountain.X + 0.5f, 1, mountain.Y + 0.5f));
			}
		}
	}

	public Tile GetRandomEdgeTile(Direction direction) {
		int maxEdgePos = direction == MapController.Direction.East || direction == MapController.Direction.West ? mapSizeY : mapSizeX;
		int edgePos = Random.Range(0, maxEdgePos);
		
		Tile edgeTile = map.GetTileAt(0, 0);
		switch(direction) {
		case MapController.Direction.North:
			edgeTile = map.GetTileAt(edgePos, mapSizeY - 1);
			break;
		case MapController.Direction.East:
			edgeTile = map.GetTileAt(mapSizeX - 1, edgePos);
			break;
		case MapController.Direction.South:
			edgeTile = map.GetTileAt(edgePos, 0);
			break;
		case MapController.Direction.West:
			edgeTile = map.GetTileAt(0, edgePos);
			break;
		}

		return edgeTile;
	}

	public Tile GetRandomEdgeTile() {
		Direction randomDirection = (Direction)Random.Range(0, 4);
		return GetRandomEdgeTile(randomDirection);
	}

	public bool IsEdgeTile(Tile tile) {
		return tile.X == 0 || tile.Y == 0 || tile.X == mapSizeX - 1 || tile.Y == mapSizeY - 1;
	}

	void ShootPirates(Tile tile, List<PirateUnit> unitsInTile) {
		foreach(PirateUnit unit in unitsInTile) {
			unit.NumPirates -= lavaAttackDamage;
		}
		GameController.instance.PlayerActions--;
	}

	void ShootTile(Tile tile) {
		map.Lava(tile);
		GameController.instance.PlayerActions--;
	}

	void SetTileGO(Tile tile, int x, int y, Tile oldTile) {
		if(tileGOs[x, y] != null) {
			if(oldTile.Type.name == "Forest") {
				for(int i = 0; i < forests.Count; i++) {
					if(forests[i].X == oldTile.X && forests[i].Y == oldTile.Y) {
						forests.Remove(forests[i]);
						break;
					}
				}
			} else if(oldTile.Type.name == "Mountain") {
				if(Mathf.Abs(oldTile.X - map.VolcanoX) <= 1 && Mathf.Abs(oldTile.Y - map.VolcanoY) <= 1) {
					for(int i = 0; i < volcanoAdjacentMountains.Count; i++) {
						if(volcanoAdjacentMountains[i].X == oldTile.X && volcanoAdjacentMountains[i].Y == oldTile.Y) {
							volcanoAdjacentMountains.Remove(volcanoAdjacentMountains[i]);
							break;
						}
					}
				}
			}
			if(oldTile.Type.name == "Water") {
				Instantiate(steamParticles, new Vector3(x + 0.5f, tileGOs[x, y].transform.position.y, y + 0.5f), Quaternion.identity);
				StartCoroutine(AnimateObjRiseFall(tileGOs[x, y], -1, true));
			} else {
				Destroy(tileGOs[x, y]);
			}
		}
		GameObject tilePrefab = tilePrefabs[System.Array.IndexOf (Tile.TileTypes, tile.Type)];
		GameObject tileGO = Instantiate(tilePrefab, TileCoordToWorldCoord(x, y), Quaternion.identity) as GameObject;
		tileGO.transform.SetParent(tileContainer);
		tileGO.name += "_" + x + "," + y;
		tileGOs[x, y] = tileGO;
		if(tile.Type.name == "Forest") {
			forests.Add(tile);
			foreach(Transform tree in tileGO.transform.Find("Offset/Grass")) {
				StartCoroutine(AnimateObjExpand(tree.gameObject, 10, 0.02f));
			}
		} else if(tile.Type.name == "Jungle") {
			foreach(Transform tree in tileGO.transform.Find("Offset/Grass")) {
				StartCoroutine(AnimateObjExpand(tree.gameObject, 10, 0.02f));
			}
		} else if(tile.Type.name == "Grass") {
			StartCoroutine(AnimateObjExpand(tileGO.transform.Find("Offset/Grass").gameObject, 10, 0.02f));
		} if(tile.Type.name == "Mountain") {
			if(Mathf.Abs(tile.X - map.VolcanoX) <= 1 && Mathf.Abs(tile.Y - map.VolcanoY) <= 1) {
				volcanoAdjacentMountains.Add(tile);
			}
			tileGO.transform.Translate(Vector3.down * 2);
			StartCoroutine(AnimateObjRiseFall(tileGO, 0, false));
		} else if(tile.Type.name == "Rock") {
			tileGO.transform.Translate(Vector3.down * 2);
			StartCoroutine(AnimateObjRiseFall(tileGO, 0, false));
		}
	}

	IEnumerator ShootVolcanicRock(Tile tile, List<PirateUnit> unitsInTile) {
		GameObject volcanicRock = Instantiate(volcanicRockPrefab, new Vector3(map.VolcanoX + 0.5f, lavaRockHeight, map.VolcanoY + 0.5f), Quaternion.identity) as GameObject;
		yield return StartCoroutine(AnimateProjectile(volcanicRock, new Vector3(tile.X + 0.5f, -1, tile.Y + 0.5f), parabolaHeight, parabolaSpeed, true));

		if(unitsInTile.Count > 0) {
			ShootPirates(tile, unitsInTile);
		} else {
			ShootTile(tile);
		}
	}

	public IEnumerator AnimateObjRiseFall(GameObject obj, float targetY, bool destroyAfter) {
		while(Mathf.Abs(obj.transform.position.y - targetY) > riseAnimationMinDistance) {
			obj.transform.Translate(Vector3.up * (targetY - obj.transform.position.y) * riseAnimationSpeed * Time.deltaTime);
			yield return null;
		}
		obj.transform.position = new Vector3(obj.transform.position.x, targetY, obj.transform.position.z);
		if(destroyAfter) {
			Destroy(obj);
		}
	}

	IEnumerator AnimateObjExpand(GameObject obj, float speed, float minDistance) {
		float startTime = Time.time;
		Vector3 originalScale = obj.transform.localScale;
		obj.transform.localScale = new Vector3(0, 0, 0);
		while(Mathf.Abs(obj.transform.localScale.x - originalScale.x) > minDistance) {
			float delta = Time.deltaTime * speed * (originalScale.x - obj.transform.localScale.x);
			obj.transform.localScale = new Vector3(obj.transform.localScale.x + delta * originalScale.x, 
			                                       obj.transform.localScale.y + delta * originalScale.y, 
			                                       obj.transform.localScale.z + delta * originalScale.z);
			yield return null;
		}
		obj.transform.localScale = originalScale;
	}

	IEnumerator AnimateProjectile(GameObject obj, Vector3 targetPos, float height, float volcanicRockSpeed, bool destroyAfter) {
		Vector3 initialPos = obj.transform.position;
		float horzDistance = Vector3.Distance(initialPos, targetPos);
		float x = 0;
		while(x < horzDistance) {
			obj.transform.position = (targetPos - initialPos).normalized * x + initialPos + Vector3.up * Parabola(initialPos, targetPos, height, x);
			x += Time.deltaTime * (volcanicRockSpeed * horzDistance);
			yield return null;
		}
		obj.transform.position = targetPos;

		if(destroyAfter) {
			StartCoroutine(DelayedDestruction(obj, 1));
		}
	}

	IEnumerator DelayedDestruction(GameObject obj, float delay) {
		yield return new WaitForSeconds(delay);
		Destroy(obj);
	}

	float Parabola(Vector3 initialPos, Vector3 targetPos, float height, float x) { // x is between 0 and horzDistance, y = 0 for both vector3s
		float horzDistance = Vector3.Distance(initialPos, targetPos);
		float A = 0;
		float B = horzDistance;
		float C = (A + B) / 2;
		float D = height / (0.5f * A * B - 0.25f * Mathf.Pow(B, 2) - 0.25f * Mathf.Pow(A, 2));
		float heightAtX = D * Mathf.Pow(x, 2) - D * A * x - D * B * x + D * A * B;
		return heightAtX;
	}
	
	void Awake() {
		map = new Map(mapSizeX, mapSizeY);

		tileGOs = new GameObject[mapSizeX, mapSizeY];

		resourceManager = FindObjectOfType<ResourceManager>();
		AIController = FindObjectOfType<PirateAIController>();

		forests = new List<Tile>();
		volcanoAdjacentMountains = new List<Tile>();

		map.onTileChange += TileChanged;
		GameController.instance.onTurnChange += TurnChanged;
	}

}

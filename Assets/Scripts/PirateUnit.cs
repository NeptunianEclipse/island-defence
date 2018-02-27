using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

// Defines a single unit of pirates that can move through the map under the control of the PirateAIController
public class PirateUnit : MonoBehaviour {

	public int x;
	public int y;

	public bool alive = true;

	public List<Tile> assignedPath;

	public int availableMovePoints;

	public float unitMoveSpeed;
	public float minDistanceToTarget;
	public bool drawPath;

	public int digDamage;
	
	public Vector3 treasurePosition;

	public Tile CurrentTile {
		get { return mapController.map.GetTileAt(x, y); }
	}

	public Tile targetTile;
	public bool haveTreasure;
	public bool vined;

	public enum PirateMode {
		Attacking,
		Digging
	}
	PirateMode mode = PirateMode.Attacking;

	PirateAIController AIController;
	MapController mapController;
	Treasure treasure;

	int movePoints;

	Color pathColour;

	RectTransform healthRect;
	Text healthText;

	int numPirates = 10;
	public int NumPirates {
		get { return numPirates; }
		set {
			int difference = value - numPirates;
			if(Mathf.Abs(difference) > 0 && value > 0) {
				GameController.instance.CreatePopupText(difference.ToString(), Color.red, new Vector3(x + 0.5f, 1, y + 0.5f));
			}
			numPirates = value;
			
			if(value <= 0) {
				Die();
			}

			int total = value;
			List<PirateUnit> otherUnits = GetOtherUnitsInCurrentTile();
			if(otherUnits.Count > 0) {
				foreach(PirateUnit unit in otherUnits) {
					total += unit.NumPirates;
				}
			}
			healthText.text = total.ToString();
		}
	}

	public List<PirateUnit> GetOtherUnitsInCurrentTile() {
		List<PirateUnit> unitList = AIController.GetUnitsInTile(CurrentTile);
		unitList.Remove(this);
		return unitList;
	}

	public IEnumerator Move() {
		if(AIController.attackState == PirateAIController.AttackState.Seeking || AIController.attackState == PirateAIController.AttackState.Escaping) {
			if(CurrentTile == targetTile) {
				if(AIController.attackState == PirateAIController.AttackState.Seeking) {
					if(treasure.hasBeenDugUp) {
						DigTreasure();
					} else {
						mode = PirateMode.Digging;
					}
				}
			} else {
				if(!vined) {
					List<Vector3> localPath = new List<Vector3>();
					localPath.Clear();
					if(alive) {
						movePoints = availableMovePoints;
						while(movePoints > 0 && assignedPath.Count > 0) {
							float height = assignedPath[0].Type.surfaceHeight;
							localPath.Add(new Vector3(assignedPath[0].X, height, assignedPath[0].Y));
							movePoints -= assignedPath[0].Type.movementCost;

							assignedPath.RemoveAt(0);
						}
						x = (int)localPath[localPath.Count - 1].x;
						y = (int)localPath[localPath.Count - 1].z;
						yield return StartCoroutine(MoveGOAlongPath(localPath));
					}
				} else {
					vined = false;
				}
			}
		}
		if(AIController.attackState != PirateAIController.AttackState.Escaping && mode == PirateMode.Digging && CurrentTile == targetTile) {
			DigTreasure();
		}

		if(haveTreasure) {
			treasure.x = x;
			treasure.y = y;
		}

		if(AIController.attackState == PirateAIController.AttackState.Escaping && haveTreasure && mapController.IsEdgeTile(CurrentTile)) {
			GameController.instance.GameOver();
		}
	}

	public void ApplyEnvironment() {
		NumPirates -= CurrentTile.Type.damage;
	}

	IEnumerator MoveGOAlongPath(List<Vector3> path) {
		for(int i = 0; i < path.Count; i++) {
			yield return StartCoroutine(MoveGOTo(path[i]));
		}
	}

	IEnumerator MoveGOTo(Vector3 targetPos) {
		while((targetPos - transform.position).magnitude > minDistanceToTarget) {
			transform.Translate((targetPos - transform.position).normalized * unitMoveSpeed * Time.deltaTime);
			yield return null;
		}
		transform.position = targetPos;
	}

	void DigTreasure() {
		Treasure potentialTreasure = treasure.Dig(digDamage);
		if(potentialTreasure != null) {
			haveTreasure = true;
			mode = PirateMode.Attacking;
			potentialTreasure.controllingUnit = this;
			potentialTreasure.transform.SetParent(transform);
			potentialTreasure.transform.localPosition = treasurePosition + Vector3.up * potentialTreasure.unitCarryHeight;
		}
	}

	void Die() {
		GameController.instance.CreatePopupText("Pirate unit defeated!", Color.red, new Vector3(x + 0.5f, 1, y + 0.5f));
		if(haveTreasure) {
			haveTreasure = false;
			treasure.Drop(x, y);
		}
		alive = false;
		AIController.UnitDied(this);
	}

	void Awake() {
		AIController = FindObjectOfType<PirateAIController>();
		mapController = FindObjectOfType<MapController>();
		treasure = FindObjectOfType<Treasure>();

		healthRect = transform.Find("UnitCanvas/BG").GetComponent<RectTransform>();
		healthText = transform.Find("UnitCanvas/BG/HealthText").GetComponent<Text>();

		pathColour = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
	}

	void OnDrawGizmos() {
		if(drawPath && assignedPath != null && assignedPath.Count > 0) {
			for(int i = 0; i < assignedPath.Count; i++) {
				Gizmos.color = pathColour;
				Gizmos.DrawWireCube(new Vector3(assignedPath[i].X + 0.5f, transform.position.y, assignedPath[i].Y + 0.5f), Vector3.one / 2);
			}
		}
	}


}

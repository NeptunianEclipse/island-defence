using UnityEngine;
using System.Collections;

// Base class that defines a single tile in the game world (independent of rendering)
public class Tile {

	public struct TileType {
		public string name;
		public int lavaCost;
		public int lifeCost;

		public string lavaUpgrade;
		public string growUpgrade;

		public int lavaProduction;
		public int lifeProduction;

		public bool traversable;
		public int pathingCost;
		public int movementCost;

		public int damage;

		public float surfaceHeight;

		public TileType(string name, int lavaCost, int lifeCost, string lavaUpgrade, string growUpgrade, int lavaProduction, int lifeProduction, bool traversable, int pathingCost, int movementCost, int damage, float surfaceHeight) {
			this.name = name;
			this.lavaCost = lavaCost;
			this.lifeCost = lifeCost;
			this.lavaUpgrade = lavaUpgrade;
			this.growUpgrade = growUpgrade;
			this.lavaProduction = lavaProduction;
			this.lifeProduction = lifeProduction;
			this.traversable = traversable;
			this.pathingCost = pathingCost;
			this.movementCost = movementCost;
			this.damage = damage;
			this.surfaceHeight = surfaceHeight;
		}
	}

	static TileType[] tileTypes = new TileType[7] {
		new TileType("Water", 0, 0, "Rock", "", 0, 0, true, 5, 1, 0, 0.2f),
		new TileType("Rock", 3, 0, "Mountain", "Grass", 0, 0, true, 1, 1, 0, 0.65f),
		new TileType("Grass", 0, 5, "Mountain", "Forest", 0, 0, true, 1, 1, 0, 0.65f),
		new TileType("Forest", 0, 10, "Mountain", "Jungle", 0, 1, true, 2, 2, 0, 0.65f),
		new TileType("Jungle", 0, 25, "Mountain", "", 0, 0, true, 2, 2, 2, 0.65f),
		new TileType("Mountain", 7, 0, "", "", 1, 0, true, 10000, 4, 1, 1.75f),
		new TileType("Volcano", 0, 0, "", "", 1, 0, false, 0, 0, 0, 1.75f)
	};

	public static TileType GetTypeByName(string name) {
		foreach(TileType type in tileTypes) {
			if(name == type.name) {
				return type;
			}
		}
		return tileTypes[0];
	}

	public static TileType[] TileTypes {
		get {
			return tileTypes;
		}
	}

	TileType type;
	public TileType Type {
		get { return type; }
		set { type = value; }
	}
	
	Map map;

	int x;
	public int X {
		get { return x; }
	}

	int y;
	public int Y {
		get { return y; }
	}

	// Constructor for a new tile
	public Tile(int x, int y, Map map, TileType tileType) {
		this.x = x;
		this.y = y;
		this.map = map;
		this.type = tileType;
	}

}

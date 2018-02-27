using UnityEngine;
using System.Collections;

// Base class that stores and manages a map of tiles (independent of rendering)
public class Map  {

	public delegate void TileEventHandler(Tile tile, Tile oldTile);
	public event TileEventHandler onTileChange;
	
	public Map(int mapSizeX, int mapSizeY) {
		this.mapSizeX = mapSizeX;
		this.mapSizeY = mapSizeY;
	}

	int mapSizeX;
	public int MapSizeX {
		get { return mapSizeX; }
	}

	int mapSizeY;
	public int MapSizeY {
		get { return mapSizeY; }
	}

	Tile[,] tileMap;

	int volcanoX;
	public int VolcanoX {
		get { return volcanoX; }
	}
	int volcanoY;
	public int VolcanoY {
		get { return volcanoY; }
	}

	public Tile GetTileAt(int x, int y) {
		if(x >= 0 & x < mapSizeX && y >= 0 && y < mapSizeY) {
			return tileMap[x, y];
		} else {
			Debug.LogError("Tile (" + x + ", " + y + ") is out of map bounds.");
			return tileMap[0, 0];
		}
	}

	public void GenerateOceanMap() {
		volcanoX = Mathf.FloorToInt(mapSizeX / 2);
		volcanoY = Mathf.FloorToInt(mapSizeY / 2);

		tileMap = new Tile[mapSizeX, mapSizeY];
		Tile newTile;
		for(int x = 0; x < mapSizeX; x++) {
			for(int y = 0; y < mapSizeY; y++) {
				if(x == volcanoX && y == volcanoY) {
					newTile = new Tile(x, y, this, Tile.GetTypeByName("Volcano"));
				} else {
					newTile = new Tile(x, y, this, Tile.TileTypes[0]);
				}
				tileMap[x, y] = newTile;
			}
		}
	}

	public void Grow(Tile tile) {
		Tile.TileType upgradeType = Tile.GetTypeByName(tile.Type.growUpgrade);
		ChangeTileType(tile, upgradeType);
	}

	public void Lava(Tile tile) {
		Tile.TileType upgradeType = Tile.GetTypeByName(tile.Type.lavaUpgrade);
		ChangeTileType(tile, upgradeType);
	}

	void ChangeTileType(Tile tile, Tile.TileType type) {
		Tile oldTile = new Tile(tile.X, tile.Y, this, tile.Type);
		tile.Type = type;

		if(onTileChange != null)
			onTileChange(tile, oldTile);
	}


}

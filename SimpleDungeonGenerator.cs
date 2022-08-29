using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class SimpleDungeonGenerator : MonoBehaviour
{
    [SerializeField]
    private Vector2Int roomSize = new Vector2Int(10, 10);

    [SerializeField]
    private Tilemap roomMap, colliderMap;
    [SerializeField]
    private TileBase roomFloorTile, pathFloorTile;

    [SerializeField]
    private InputActionReference generate;

    public UnityEvent OnFinishedRoomGeneration;

    public static List<Vector2Int> fourDirections = new()
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    private DungeonData dungeonData;

    private void Awake()
    {
        dungeonData = FindObjectOfType<DungeonData>();
        if (dungeonData == null)
            dungeonData = gameObject.AddComponent<DungeonData>();

        generate.action.performed += Generate;
    }

    private void Generate(InputAction.CallbackContext obj)
    {
        dungeonData.Reset();

        dungeonData.Rooms.Add(
            GenerateRectangularRoomAt(Vector2.zero, roomSize));
        dungeonData.Rooms.Add(
            GenerateRectangularRoomAt(Vector2Int.zero + Vector2Int.right * 15, roomSize));
        dungeonData.Rooms.Add(
            GenerateRectangularRoomAt(Vector2Int.zero + Vector2Int.down * 15, roomSize));

        dungeonData.Path.UnionWith(
            CreateStraightCorridor(Vector2Int.zero, Vector2Int.zero + Vector2Int.right * 15));
        dungeonData.Path.UnionWith(
            CreateStraightCorridor(Vector2Int.zero, Vector2Int.zero + Vector2Int.down * 15));

        GenerateDungeonCollider();

        OnFinishedRoomGeneration?.Invoke();
    }

    private void GenerateDungeonCollider()
    {
        //create a hahset that contains all the tiles that represent the dungeon
        HashSet<Vector2Int> dungeonTiles = new HashSet<Vector2Int>();
        foreach (Room room in dungeonData.Rooms)
        {
            dungeonTiles.UnionWith(room.FloorTiles);
        }
        dungeonTiles.UnionWith(dungeonData.Path);

        //Find the outline of the dungeon that will be our walls / collider aound the dungeon
        HashSet<Vector2Int> colliderTiles = new HashSet<Vector2Int>();
        foreach (Vector2Int tilePosition in dungeonTiles)
        {
            foreach (Vector2Int direction in fourDirections)
            {
                Vector2Int newPosition = tilePosition + direction;
                if(dungeonTiles.Contains(newPosition) == false)
                {
                    colliderTiles.Add(newPosition);
                    continue;
                }
            }
        }

        foreach (Vector2Int pos in colliderTiles)
        {
            colliderMap.SetTile((Vector3Int)pos, roomFloorTile);
        }
    }

    private Room GenerateRectangularRoomAt(Vector2 roomCenterPosition, Vector2Int roomSize)
    {
        Vector2Int half = roomSize / 2;

        HashSet<Vector2Int> roomTiles = new();

        //Generate the room around the roomCenterposition
        for (int x = -half.x; x < half.x; x++)
        {
            for (int y = -half.y; y < half.y; y++)
            {
                Vector2 position = roomCenterPosition + new Vector2(x, y);
                Vector3Int positionInt = roomMap.WorldToCell(position);
                roomTiles.Add((Vector2Int)positionInt);
                roomMap.SetTile(positionInt, roomFloorTile);
            }
        }
        return new Room(roomCenterPosition, roomTiles);
    }

    private HashSet<Vector2Int> CreateStraightCorridor(Vector2Int startPostion, 
        Vector2Int endPosition)
    {
        //Create a hashset and add start and end positions to it
        HashSet<Vector2Int> corridorTiles = new();
        corridorTiles.Add(startPostion);
        roomMap.SetTile((Vector3Int)startPostion, pathFloorTile);
        corridorTiles.Add(endPosition);
        roomMap.SetTile((Vector3Int)endPosition, pathFloorTile);

        //Find the direction of the straight line
        Vector2Int direction 
            = Vector2Int.CeilToInt(((Vector2)endPosition - startPostion).normalized);
        Vector2Int currentPosition = startPostion;

        //Add all tiles until we reach the end position
        while (Vector2.Distance(currentPosition, endPosition) > 1)
        {
            currentPosition += direction;
            corridorTiles.Add(currentPosition);
            roomMap.SetTile((Vector3Int)currentPosition, pathFloorTile);
        }

        return corridorTiles;
    }
}


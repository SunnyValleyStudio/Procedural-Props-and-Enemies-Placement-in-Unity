using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;

public class AgentPlacer : MonoBehaviour
{
    [SerializeField]
    private GameObject enemyPrefab, playerPrefab;

    [SerializeField]
    private int playerRoomIndex;
    [SerializeField]
    private CinemachineVirtualCamera vCamera;

    [SerializeField]
    private List<int> roomEnemiesCount;

    DungeonData dungeonData;

    [SerializeField]
    private bool showGizmo = false;

    private void Awake()
    {
        dungeonData = FindObjectOfType<DungeonData>();
    }

    public void PlaceAgents()
    {
        if (dungeonData == null)
            return;

        //Loop for each room
        for (int i = 0; i < dungeonData.Rooms.Count; i++)
        {
            //TO place eneies we need to analyze the room tiles to find those accesible from the path
            Room room = dungeonData.Rooms[i];
            RoomGraph roomGraph = new RoomGraph(room.FloorTiles);

            //Find the Path inside this specific room
            HashSet<Vector2Int> roomFloor = new HashSet<Vector2Int>(room.FloorTiles);
            //Find the tiles belonging to both the path and the room
            roomFloor.IntersectWith(dungeonData.Path);

            //Run the BFS to find all the tiles in the room accessible from the path
            Dictionary<Vector2Int, Vector2Int> roomMap = roomGraph.RunBFS(roomFloor.First(), room.PropPositions);

            //Positions that we can reach + path == positions where we can place enemies
            room.PositionsAccessibleFromPath = roomMap.Keys.OrderBy(x => Guid.NewGuid()).ToList();

            //did we add this room to the roomEnemiesCount list?
            if(roomEnemiesCount.Count > i)
            {
                PlaceEnemies(room, roomEnemiesCount[i]);
            }

            //Place the player
            if(i==playerRoomIndex)
            {
                GameObject player = Instantiate(playerPrefab);
                player.transform.localPosition = dungeonData.Rooms[i].RoomCenterPos + Vector2.one*0.5f;
                //Make the camera follow the player
                vCamera.Follow = player.transform;
                vCamera.LookAt = player.transform;
                dungeonData.PlayerReference = player;
            }
        }
    }

    /// <summary>
    /// Places enemies in the positions accessible from the path
    /// </summary>
    /// <param name="room"></param>
    /// <param name="enemysCount"></param>
    private void PlaceEnemies(Room room, int enemysCount)
    {
        for (int k = 0; k < enemysCount; k++)
        {
            if (room.PositionsAccessibleFromPath.Count <= k)
            {
                return;
            }
            GameObject enemy = Instantiate(enemyPrefab);
            enemy.transform.localPosition = (Vector2)room.PositionsAccessibleFromPath[k] + Vector2.one*0.5f;
            room.EnemiesInTheRoom.Add(enemy);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (dungeonData == null || showGizmo == false)
            return;
        foreach (Room room in dungeonData.Rooms)
        {
            Color color = Color.green;
            color.a = 0.3f;
            Gizmos.color = color;

            foreach (Vector2Int pos in room.PositionsAccessibleFromPath)
            {
                Gizmos.DrawCube((Vector2)pos + Vector2.one * 0.5f, Vector2.one);
            }
        }
    }
}

public class RoomGraph
{
    public static List<Vector2Int> fourDirections = new()
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    Dictionary<Vector2Int, List<Vector2Int>> graph = new Dictionary<Vector2Int, List<Vector2Int>>();

    public RoomGraph(HashSet<Vector2Int> roomFloor)
    {
        foreach (Vector2Int pos in roomFloor)
        {
            List<Vector2Int> neighbours = new List<Vector2Int>();
            foreach (Vector2Int direction in fourDirections)
            {
                Vector2Int newPos = pos + direction;
                if (roomFloor.Contains(newPos))
                {
                    neighbours.Add(newPos);
                }
            }
            graph.Add(pos, neighbours);
        }
    }

    /// <summary>
    /// Creates a map of reachable tiles in our dungeon.
    /// </summary>
    /// <param name="startPos">Door position or tile position on the path between rooms inside this room</param>
    /// <param name="occupiedNodes"></param>
    /// <returns></returns>
    public Dictionary<Vector2Int, Vector2Int> RunBFS(Vector2Int startPos, HashSet<Vector2Int> occupiedNodes)
    {
        //BFS related variuables
        Queue<Vector2Int> nodesToVisit = new Queue<Vector2Int>();
        nodesToVisit.Enqueue(startPos);

        HashSet<Vector2Int> visitedNodes = new HashSet<Vector2Int>();
        visitedNodes.Add(startPos);

        //The dictionary that we will return 
        Dictionary<Vector2Int, Vector2Int> map = new Dictionary<Vector2Int, Vector2Int>();
        map.Add(startPos, startPos);

        while (nodesToVisit.Count > 0)
        {
            //get the data about specific position
            Vector2Int node = nodesToVisit.Dequeue();
            List<Vector2Int> neighbours = graph[node];

            //loop through each neighbour position
            foreach (Vector2Int neighbourPosition in neighbours)
            {
                //add the neighbour position to our map if it is valid
                if (visitedNodes.Contains(neighbourPosition) == false &&
                    occupiedNodes.Contains(neighbourPosition) == false)
                {
                    nodesToVisit.Enqueue(neighbourPosition);
                    visitedNodes.Add(neighbourPosition);
                    map[neighbourPosition] = node;
                }
            }
        }

        return map;
    }
}

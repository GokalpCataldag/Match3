using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public class Match3 : MonoBehaviour
{
    [SerializeField]
    public Match3Game game;

    Grid2D<Tile> tiles;

    float2 tileOffset;

    [SerializeField]
    Tile[] tilePrefabs;

    float busyDuration;
    public bool IsPlaying => true;

    public bool IsBusy => busyDuration > 0f;

    void Update()
    {
        if (busyDuration > 0f)
        {
            busyDuration -= Time.deltaTime;
            if (busyDuration < 0f)
                busyDuration = 0f;
        }
    }
    public void StartNewGame()
    {
        game.StartNewGame();
        tileOffset = -0.5f * (float2)(game.Size - 1);

        if (tiles.IsUndefined)
        {
            tiles = new(game.Size);
        }
        else
        {
            for (int y = 0; y < tiles.SizeY; y++)
            {
                for (int x = 0; x < tiles.SizeX; x++)
                {
                    tiles[x, y]?.Despawn();
                    tiles[x, y] = null;
                }
            }
        }

        for (int y = 0; y < tiles.SizeY; y++)
        {
            for (int x = 0; x < tiles.SizeX; x++)
            {
                tiles[x, y] = SpawnTile(game[x, y], x, y);
                UpdateGroupIcons(5, 8, 10);
            }
        }

        //  Yeni eklenen kýsým
        if (!game.HasAnyValidMove())
        {
            Debug.Log("[StartNewGame] No moves found — shuffling...");
            game.ShuffleBoard();

            // Spawn yeniden
            for (int y = 0; y < tiles.SizeY; y++)
            {
                for (int x = 0; x < tiles.SizeX; x++)
                {
                    tiles[x, y]?.Despawn();
                    tiles[x, y] = SpawnTile(game[x, y], x, y);
                }
            }
        }
        UpdateGroupIcons(5, 8, 10);
    }
    Tile SpawnTile(TileState t, float x, float y)
    {
        if (t == TileState.None)
        {
            return null;
        }

        int index = (int)t - 1;

        if (index < 0 || index >= tilePrefabs.Length)
        {
            return null;
        }
        //Debug.Log("tile uretildi");
        return tilePrefabs[index].Spawn(new Vector3(x + tileOffset.x, y + tileOffset.y));
    }
    public void DoWork()
    {
        if (game.NeedsFilling)
        {
            DropTiles();
            UpdateGroupIcons(5, 8, 10);
            if (!game.HasAnyValidMove())
            {
                Debug.Log("[DoWork] Deadlock detected — shuffling...");
                game.ShuffleBoard();

                // Spawn sonrasý:
                for (int y = 0; y < tiles.SizeY; y++)
                {
                    for (int x = 0; x < tiles.SizeX; x++)
                    {
                        tiles[x, y]?.Despawn();
                        tiles[x, y] = SpawnTile(game[x, y], x, y);

                        //  Yeni eklenen shuffle efekti
                        tiles[x, y]?.PlayShuffleEffect();
                    }
                }
                UpdateGroupIcons(5, 8, 10);
            }
        }
    }
    void DropTiles()
    {
        game.DropTiles();

        for (int i = 0; i < game.DroppedTiles.Count; i++)
        {
            TileDrop drop = game.DroppedTiles[i];
            Tile tile;
            Vector3 newPosition = new Vector3(
                drop.coordinates.x + tileOffset.x,
                drop.coordinates.y + tileOffset.y
            );

            if (drop.fromY < tiles.SizeY)
            {
                // Mevcut tile'ý kayarak düþür
                tile = tiles[drop.coordinates.x, drop.fromY];
                busyDuration = Mathf.Max(tile.DropTo(newPosition), busyDuration);
            }
            else
            {
                // Yeni tile spawn et (üstten geliyormuþ gibi)
                Vector3 spawnPosition = new Vector3(
                    drop.coordinates.x + tileOffset.x,
                    tiles.SizeY + tileOffset.y // Ekranýn üstünden baþla
                );
                tile = SpawnTile(game[drop.coordinates], spawnPosition.x, spawnPosition.y);
                tile.SetPositionImmediate(spawnPosition); // Baþlangýç pozisyonunu ayarla
                busyDuration = Mathf.Max(tile.DropTo(newPosition), busyDuration);
            }

            tiles[drop.coordinates] = tile;
        }
    }
    public Vector2 TileOffset => tileOffset;
    List<int2> FindConnectedGroup(int2 start, TileState target)
    {
        List<int2> group = new List<int2>();
        Queue<int2> queue = new Queue<int2>();
        HashSet<int2> visited = new HashSet<int2>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            int2 current = queue.Dequeue();
            group.Add(current);

            foreach (int2 dir in new int2[]
            {
            new int2(1, 0), new int2(-1, 0),
            new int2(0, 1), new int2(0, -1)
            })
            {
                int2 neighbor = current + dir;
                if (tiles.AreValidCoordinates(neighbor) &&
                    !visited.Contains(neighbor) &&
                    game[neighbor] == target)
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }
        return group;
    }
    public void TrySelectTile(int x, int y)
    {
        int2 clicked = new int2(x, y);

        if (!tiles.AreValidCoordinates(clicked))
            return;

        TileState target = game[clicked];
        if (target == TileState.None)
            return;

        List<int2> group = FindConnectedGroup(clicked, target);

        if (group.Count < 2)
            return;

        foreach (int2 c in group)
        {
            game[c] = TileState.None;
            busyDuration = Mathf.Max(tiles[c].Disappear(), busyDuration);
            tiles[c] = null;
        }

        game.NeedsFilling = true;
   
    }
    void UpdateGroupIcons(int a, int b, int c)
    {
        HashSet<int2> visited = new HashSet<int2>();

        for (int y = 0; y < tiles.SizeY; y++)
        {
            for (int x = 0; x < tiles.SizeX; x++)
            {
                int2 coord = new int2(x, y);
                if (visited.Contains(coord) || game[coord] == TileState.None)
                    continue;

                List<int2> group = FindConnectedGroup(coord, game[coord]);

                foreach (int2 cPos in group)
                {
                    Tile tile = tiles[cPos];
                    tile?.SetIconByGroupSize(group.Count, a, b, c);
                    visited.Add(cPos);
                }
            }
        }
    }
}
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using Random = UnityEngine.Random;

using static Unity.Mathematics.math;
using System.Linq;

public class Match3Game : MonoBehaviour
{
    [SerializeField]
    int2 size = 8;

    Grid2D<TileState> grid;

    public TileState this[int x, int y]
    {
        get
        {
            return grid[x, y];
        }
    }

    public TileState this[int2 c]
    {
        get 
        {
            return grid[c]; 
        }
        set 
        { 
            grid[c] = value;
        }
    }
    List<Match> matches;
    public bool NeedsFilling
    { get; set; }
    public List<int2> ClearedTileCoordinates
    { get; private set; }
    public List<TileDrop> DroppedTiles
    { get; private set; }

    public int2 Size => size;
    public void StartNewGame()
    {
        if (grid.IsUndefined)
        {
            grid = new(size);
            matches = new();
            ClearedTileCoordinates = new();
            DroppedTiles = new();
        }
        do
        {
            FillGrid(); // mevcut grid�i rastgele doldur
        }
        while (!HasAnyValidMove());
    }

    void FillGrid()
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                // Random tile directly (2 dahil, 8 hari�) ��nk� 1=None atlan�yor
                grid[x, y] = (TileState)Random.Range(2, 8);
            }
        }
    }
    public bool HasMatches() 
    {
        return matches.Count > 0; 
    }

    public void DropTiles()
    {
        if (size.x == 2 && size.y == 2)
        {
            DropTilesWithMatchingGuarantee2x2();
        }
        else
        {
            // Mevcut DropTiles sistemin (senin zaten yazm�� oldu�un kod)
            DroppedTiles.Clear();

            for (int x = 0; x < size.x; x++)
            {
                int holeCount = 0;
                for (int y = 0; y < size.y; y++)
                {
                    if (grid[x, y] == TileState.None)
                    {
                        holeCount += 1;
                    }
                    else if (holeCount > 0)
                    {
                        grid[x, y - holeCount] = grid[x, y];
                        grid[x, y] = TileState.None;
                        DroppedTiles.Add(new TileDrop(x, y - holeCount, holeCount));
                    }
                }

                for (int h = 1; h <= holeCount; h++)
                {
                    grid[x, size.y - h] = (TileState)Random.Range(2, 8);
                    DroppedTiles.Add(new TileDrop(x, size.y - h, holeCount));
                }
            }

            NeedsFilling = false;
        }
    }
    public bool HasAnyValidMove()
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                TileState current = grid[x, y];
                if (current == TileState.None)
                    continue;

                int2[] directions = new int2[]
                {
                new int2(1, 0), // sa�
                new int2(0, 1)  // a�a��
                };

                foreach (int2 dir in directions)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;

                    if (nx < size.x && ny < size.y && grid[nx, ny] == current)
                    {
                        return true; // E�le�me bulunabilir
                    }
                }
            }
        }

        return false; // Deadlock
    }
    public void ShuffleBoard()
    {
        List<TileState> states = new List<TileState>();

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if (grid[x, y] != TileState.None)
                    states.Add(grid[x, y]);
            }
        }

        for (int attempt = 0; attempt < 50; attempt++)
        {
            for (int i = states.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (states[i], states[j]) = (states[j], states[i]);
            }

            int index = 0;
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    if (grid[x, y] != TileState.None)
                        grid[x, y] = states[index++];
                }
            }
            Debug.Log($"[Shuffle] Resolved after {attempt + 1} tries.");
            if (HasAnyValidMove())
            {
                Debug.Log($"[Shuffle] Resolved after {attempt + 1} tries.");
                return;
            }
        }
        Debug.LogWarning("[Shuffle] Deadlock persists after max attempts.");
    }
    void DropTilesWithMatchingGuarantee2x2()
    {
        DroppedTiles.Clear();
        Debug.Log("Garanti calisti");
        // 1. Grid'de h�l� kalan tile t�rlerini topla
        HashSet<TileState> existingTypes = new HashSet<TileState>();
        bool gridEmpty = true;

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if (grid[x, y] != TileState.None)
                {
                    existingTypes.Add(grid[x, y]);
                }
                if (grid[x, y] != TileState.None)
                {
                    existingTypes.Add(grid[x, y]);
                    gridEmpty = false;
                }
            }

        }
        string logText = "[2x2 Drop] Existing Tile Types in Grid: ";
        foreach (TileState t in existingTypes)
        {
            logText += t.ToString() + " ";
        }
        Debug.Log(logText);
        // 2. D��mesi gereken ta�lar� s�rayla �ret
        for (int x = 0; x < size.x; x++)
        {
            int holeCount = 0;

            // Y�kselerek gelen ta�lar� kayd�r
            for (int y = 0; y < size.y; y++)
            {
                if (grid[x, y] == TileState.None)
                {
                    holeCount += 1;
                }
                else if (holeCount > 0)
                {
                    grid[x, y - holeCount] = grid[x, y];
                    grid[x, y] = TileState.None;
                    DroppedTiles.Add(new TileDrop(x, y - holeCount, holeCount));
                }
            }

            // 3. Yeni ta�lar� �stten �ret
            for (int h = 1; h <= holeCount; h++)
            {
                TileState newTile;

                // En son ta� i�in: garanti e�le�me
                if (h == holeCount && !gridEmpty && existingTypes.Count > 0)
                {
                    newTile = GetRandomFromSet(existingTypes);
                    Debug.Log($"[2x2 Grid E�le�me Garantisi] Var olan t�rlerden se�ildi: {newTile}");
                }
                else if (gridEmpty)
                {
                    // �zel �retim: 2 tane ayn� t�r tile �ret
                    TileState forced = (TileState)Random.Range(2, 8);
                    newTile = (h % 2 == 0) ? forced : (TileState)Random.Range(2, 8);
                    Debug.Log("bos gride mudahale");
                    Debug.Log($"[2x2 Grid Bo�] �retilen tile: {newTile}, zorunlu e�le�me t�r�: {forced}");
                }
                else
                {
                    newTile = (TileState)Random.Range(2, 8);
                }

                grid[x, size.y - h] = newTile;
                DroppedTiles.Add(new TileDrop(x, size.y - h, holeCount));
            }
        }

        NeedsFilling = false;
    }
    TileState GetRandomFromSet(HashSet<TileState> set)
    {
        int index = Random.Range(0, set.Count); // 0 dahil, Count hari�
        int i = 0;

        foreach (TileState t in set)
        {
            if (i == index)
                return t;
            i++;
        }

        return TileState.A; // yedek (asla olmaz)
    }

}
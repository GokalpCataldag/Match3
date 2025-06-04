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

    private int? shuffleCounter;
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
        if ((size.x == 2 && size.y <= 3) || (size.x == 3 && size.y == 2))
        {
            DropTilesWithMatchingGuarantee();
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
        Debug.Log("[Smart Shuffle] Ak�ll� shuffle ba�l�yor...");

        // 1. Renkleri say
        Dictionary<TileState, List<int2>> colorPositions = new Dictionary<TileState, List<int2>>();

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if (grid[x, y] != TileState.None)
                {
                    if (!colorPositions.ContainsKey(grid[x, y]))
                        colorPositions[grid[x, y]] = new List<int2>();

                    colorPositions[grid[x, y]].Add(new int2(x, y));
                }
            }
        }

        // 2. En �ok olan� bul
        var mostCommon = colorPositions.OrderByDescending(kvp => kvp.Value.Count).First();
        int halfCount = Mathf.Max(2, mostCommon.Value.Count / 2);

        // 3. K��e belirle
        if (!shuffleCounter.HasValue) shuffleCounter = 0;
        int corner = shuffleCounter.Value % 4;
        shuffleCounter++;

        int2[] corners = {
        new int2(0, size.y - 1),           // Sol �st
        new int2(size.x - 1, size.y - 1),  // Sa� �st
        new int2(0, 0),                     // Sol alt
        new int2(size.x - 1, 0)            // Sa� alt
    };

        // 4. Tile'lar� topla
        List<TileState> tiles = new List<TileState>();
        foreach (var kvp in colorPositions)
        {
            foreach (var poas in kvp.Value)
            {
                tiles.Add(grid[poas.x, poas.y]);  //  D�zeltildi
                grid[poas.x, poas.y] = TileState.None;  //  D�zeltildi
            }
        }

        // 5. Grubu yerle�tir
        int2 pos = corners[corner];
        int dir = (corner == 1 || corner == 3) ? -1 : 1;

        for (int i = 0; i < halfCount; i++)
        {
            if (grid.AreValidCoordinates(pos))
            {
                grid[pos] = mostCommon.Key;
                tiles.Remove(mostCommon.Key);

                pos.x += dir;
                if (!grid.AreValidCoordinates(pos))
                {
                    pos.x = corners[corner].x;
                    pos.y += (corner <= 1) ? -1 : 1;
                }
            }
        }

        // 6. Kalanlar� kar��t�r ve yerle�tir
        tiles = tiles.OrderBy(x => Random.value).ToList();

        int index = 0;
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if (grid[x, y] == TileState.None && index < tiles.Count)
                    grid[x, y] = tiles[index++];
            }
        }

        Debug.Log($"[Smart Shuffle] {halfCount} adet {mostCommon.Key} gruplanm��");
    }
    void DropTilesWithMatchingGuarantee()
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

        // ��Z�M: Sadece 1 kez ayn� renk �retmek i�in flag
        bool matchingTileCreated = false;

        // Bo� grid i�in garanti renk
        TileState guaranteedColor = TileState.None;
        if (gridEmpty)
        {
            guaranteedColor = (TileState)Random.Range(2, 8);
            Debug.Log($"[2x2 Bo� Grid] Se�ilen garanti renk: {guaranteedColor}");
        }

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

                // En son ta� i�in: garanti e�le�me (ama sadece 1 kez!)
                if (h == holeCount && !gridEmpty && existingTypes.Count > 0 && !matchingTileCreated)
                {
                    newTile = GetRandomFromSet(existingTypes);
                    matchingTileCreated = true; // Flag'i i�aretle
                    Debug.Log($"[2x2 E�le�me Garantisi] 1 tile var olan t�rden se�ildi: {newTile}");
                }
                else if (gridEmpty)
                {
                    // �st s�radaki 2 tile ayn� renk olsun
                    if ((x == 0 && h == 2) || (x == 1 && h == 2))
                    {
                        newTile = guaranteedColor;
                        Debug.Log($"[2x2 Bo� Grid] �st s�ra garanti renk: {guaranteedColor}");
                    }
                    else
                    {
                        newTile = (TileState)Random.Range(2, 8);
                        Debug.Log($"[2x2 Bo� Grid] Rastgele tile: {newTile}");
                    }
                }
                else
                {
                    newTile = (TileState)Random.Range(2, 8);
                    Debug.Log($"[2x2] Rastgele tile �retildi: {newTile}");
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
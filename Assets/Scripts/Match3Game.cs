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
            FillGrid(); // mevcut grid’i rastgele doldur
        }
        while (!HasAnyValidMove());
    }

    void FillGrid()
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                // Random tile directly (2 dahil, 8 hariç) çünkü 1=None atlanýyor
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
            // Mevcut DropTiles sistemin (senin zaten yazmýþ olduðun kod)
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
                new int2(1, 0), // sað
                new int2(0, 1)  // aþaðý
                };

                foreach (int2 dir in directions)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;

                    if (nx < size.x && ny < size.y && grid[nx, ny] == current)
                    {
                        return true; // Eþleþme bulunabilir
                    }
                }
            }
        }

        return false; // Deadlock
    }
    public void ShuffleBoard()
    {
        Debug.Log("[Smart Shuffle] Akýllý shuffle baþlýyor...");

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

        // 2. En çok olaný bul
        var mostCommon = colorPositions.OrderByDescending(kvp => kvp.Value.Count).First();
        int halfCount = Mathf.Max(2, mostCommon.Value.Count / 2);

        // 3. Köþe belirle
        if (!shuffleCounter.HasValue) shuffleCounter = 0;
        int corner = shuffleCounter.Value % 4;
        shuffleCounter++;

        int2[] corners = {
        new int2(0, size.y - 1),           // Sol üst
        new int2(size.x - 1, size.y - 1),  // Sað üst
        new int2(0, 0),                     // Sol alt
        new int2(size.x - 1, 0)            // Sað alt
    };

        // 4. Tile'larý topla
        List<TileState> tiles = new List<TileState>();
        foreach (var kvp in colorPositions)
        {
            foreach (var poas in kvp.Value)
            {
                tiles.Add(grid[poas.x, poas.y]);  //  Düzeltildi
                grid[poas.x, poas.y] = TileState.None;  //  Düzeltildi
            }
        }

        // 5. Grubu yerleþtir
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

        // 6. Kalanlarý karýþtýr ve yerleþtir
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

        Debug.Log($"[Smart Shuffle] {halfCount} adet {mostCommon.Key} gruplanmýþ");
    }
    void DropTilesWithMatchingGuarantee()
    {
        DroppedTiles.Clear();
        Debug.Log("Garanti calisti");

        // 1. Grid'de hâlâ kalan tile türlerini topla
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

        // ÇÖZÜM: Sadece 1 kez ayný renk üretmek için flag
        bool matchingTileCreated = false;

        // Boþ grid için garanti renk
        TileState guaranteedColor = TileState.None;
        if (gridEmpty)
        {
            guaranteedColor = (TileState)Random.Range(2, 8);
            Debug.Log($"[2x2 Boþ Grid] Seçilen garanti renk: {guaranteedColor}");
        }

        // 2. Düþmesi gereken taþlarý sýrayla üret
        for (int x = 0; x < size.x; x++)
        {
            int holeCount = 0;

            // Yükselerek gelen taþlarý kaydýr
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

            // 3. Yeni taþlarý üstten üret
            for (int h = 1; h <= holeCount; h++)
            {
                TileState newTile;

                // En son taþ için: garanti eþleþme (ama sadece 1 kez!)
                if (h == holeCount && !gridEmpty && existingTypes.Count > 0 && !matchingTileCreated)
                {
                    newTile = GetRandomFromSet(existingTypes);
                    matchingTileCreated = true; // Flag'i iþaretle
                    Debug.Log($"[2x2 Eþleþme Garantisi] 1 tile var olan türden seçildi: {newTile}");
                }
                else if (gridEmpty)
                {
                    // Üst sýradaki 2 tile ayný renk olsun
                    if ((x == 0 && h == 2) || (x == 1 && h == 2))
                    {
                        newTile = guaranteedColor;
                        Debug.Log($"[2x2 Boþ Grid] Üst sýra garanti renk: {guaranteedColor}");
                    }
                    else
                    {
                        newTile = (TileState)Random.Range(2, 8);
                        Debug.Log($"[2x2 Boþ Grid] Rastgele tile: {newTile}");
                    }
                }
                else
                {
                    newTile = (TileState)Random.Range(2, 8);
                    Debug.Log($"[2x2] Rastgele tile üretildi: {newTile}");
                }

                grid[x, size.y - h] = newTile;
                DroppedTiles.Add(new TileDrop(x, size.y - h, holeCount));
            }
        }

        NeedsFilling = false;
    }
    TileState GetRandomFromSet(HashSet<TileState> set)
    {
        int index = Random.Range(0, set.Count); // 0 dahil, Count hariç
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
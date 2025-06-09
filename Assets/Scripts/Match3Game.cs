using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using Random = UnityEngine.Random;

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
    public bool NeedsFilling
    { get; set; }
    public List<TileDrop> DroppedTiles
    { get; private set; }

    public int2 Size 
    {
        get { return size; }
    } 
    public void StartNewGame()
    {
        if (grid.IsUndefined)
        {
            grid = new(size);
            DroppedTiles = new();
        }
        do
        {
            FillGrid();
        }
        while (!HasAnyValidMove());
    }

    /// <summary>
    /// Grid'i tile'larla doldurur.
    /// </summary>
    void FillGrid()
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                grid[x, y] = (TileState)Random.Range(2, 8);
            }
        }
    }

    /// <summary>
    /// Tile'lar patladýktan sonra yeni tile ile doldurur.
    /// </summary>
    public void DropTiles()
    {
        // 2x2 - 3x2 - 2x3 gibi kucuk tile'lar icin ayri bir fonksiyon gelistirdim.
        if ((size.x == 2 && size.y <= 3) || (size.x == 3 && size.y == 2))
        {
            DropTilesWithMatchingGuarantee();
        }
        else
        {
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

    /// <summary>
    /// Oynanabilir hamle var mi onu kontrol eder.
    /// </summary>
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
                new int2(1, 0),
                new int2(0, 1) 
                };

                foreach (int2 dir in directions)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;

                    if (nx < size.x && ny < size.y && grid[nx, ny] == current)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Deadlock durumunda taslari karistirmaya yarar.
    /// Grid'deki tile'larý renklerine gore gruplar. 
    /// En cok bulunan tile'ý secer.
    /// Secilen tile'ýn sayýsýnýn yarisi kadarini her shuffle'da farkli kose olacak sekilde yerlestirir.
    /// Kalan tile'larý grid'e rastgele dagitir.
    /// </summary>
    public void ShuffleBoard()
    {

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

        var mostCommon = colorPositions.OrderByDescending(kvp => kvp.Value.Count).First();
        int halfCount = Mathf.Max(2, mostCommon.Value.Count / 2);

        if (!shuffleCounter.HasValue) shuffleCounter = 0;
        int corner = shuffleCounter.Value % 4;
        shuffleCounter++;

        int2[] corners = {
        new int2(0, size.y - 1),          
        new int2(size.x - 1, size.y - 1),  
        new int2(0, 0),                     
        new int2(size.x - 1, 0)            
    };

        List<TileState> tiles = new List<TileState>();
        foreach (var kvp in colorPositions)
        {
            foreach (var poas in kvp.Value)
            {
                tiles.Add(grid[poas.x, poas.y]);
                grid[poas.x, poas.y] = TileState.None; 
            }
        }

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
    }

    /// <summary>
    /// Kucuk grid oldugu icin uretilen tile'larda en az bir eslesme olmasýný saglar.
    /// Grid'de bulunan tile ile uretilen tile'lardan birinin ayni olmasi saglandi.
    /// Eger patlamadan sonra grid'de tile yok ise uretilen tile'lardan ikisi ayni olacak sekilde uretilmesi saglandi.
    /// </summary>
    void DropTilesWithMatchingGuarantee()
    {
        DroppedTiles.Clear();

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

        bool matchingTileCreated = false;

        TileState guaranteedColor = TileState.None;
        if (gridEmpty)
        {
            guaranteedColor = (TileState)Random.Range(2, 8);
        }

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
                TileState newTile;

                if (h == holeCount && !gridEmpty && existingTypes.Count > 0 && !matchingTileCreated)
                {
                    newTile = GetRandomFromSet(existingTypes);
                    matchingTileCreated = true; 
                }
                else if (gridEmpty)
                {
                    if ((x == 0 && h == 2) || (x == 1 && h == 2))
                    {
                        newTile = guaranteedColor;
                    }
                    else
                    {
                        newTile = (TileState)Random.Range(2, 8);
                    }
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
        int index = Random.Range(0, set.Count);
        int i = 0;

        foreach (TileState t in set)
        {
            if (i == index)
                return t;
            i++;
        }

        return TileState.A;
    }
   
}
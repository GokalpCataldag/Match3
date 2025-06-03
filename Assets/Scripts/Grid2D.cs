using Unity.Mathematics;

[System.Serializable]
public struct Grid2D<T>
{
    T[] cells;
    int2 size;

    public Grid2D(int2 size)
    {
        this.size = size;
        cells = new T[size.x * size.y];
    }

    public T this[int x, int y]
    {
        get
        {
            return cells[y * size.x + x];
        }
        set
        {
            cells[y * size.x + x] = value;
        }
    }

    public T this[int2 c]
    {
        get
        {
            return this[c.x, c.y];
        }
        set
        {
            this[c.x, c.y] = value;
        }
    }

    public int2 Size
    {
        get
        {
            return size;
        }
    }

    public bool IsUndefined
    {
        get
        {
            return cells == null || cells.Length == 0;
        }
    }

    public bool AreValidCoordinates(int2 c)
    {
        return 0 <= c.x && c.x < size.x && 0 <= c.y && c.y < size.y;
    }
    public int SizeX
    {
        get { return size.x; }
    }

    public int SizeY
    {
        get { return size.y; }
    }
}

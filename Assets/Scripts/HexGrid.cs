using UnityEngine;


/// <summary>
/// 管理整个六边形网格地图
/// </summary>
public class HexGrid : MonoBehaviour
{
    public HexGridChunk chunkPrefab;                        // 块预制体

    public HexCell cellPrefab;                              // 六边形预制体

    public Color defaultColor = Color.white;

    public Texture2D noiseSource;

    public int chunkCountX = 4, chunkCountZ = 3;            // 网格块数目

    public int seed;                                        // 随机数种子

    int cellCountX, cellCountZ;                             // 六边形数目


    HexGridChunk[] chunks;

    HexCell[] cells;
    void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    public HexCell GetCell(Vector3 position)
    {
        // 世界坐标转换相对坐标
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ)
        {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }
        return cells[x + z * cellCountX];
    }

    /// <summary>
    /// 创建单个六边形
    /// </summary>
    /// <param name="x">宽（第几列）</param>
    /// <param name="z">高（第几行）</param>
    /// <param name="i">index</param>
    void CreateCell(int x, int z, int i)
    {
        // 转换成U3D里的坐标
        Vector3 position;
        position.x = x * (HexMetrics.innerRadius * 2f) + HexMetrics.innerRadius * (z % 2);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        //cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Color = defaultColor;

        #region 设置邻里关系
        // 第二列开始往西边连接
        if (x > 0)
        {
            // 连接东、西方向邻居，因为是双向连接，连接西边的时候，西边的格子也会自动连接自己
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        
        // 第二行开始往南边连接
        if (z > 0)
        {
            if ((z & 1) == 0) // & 按位与，判断是否是偶数行，和（z%2 == 0）效果一样
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                // 第0行、第2行等偶数行的第一个格子西南方向没有格子，见右边注释                                 // 3行： ······
                if (x > 0)                                                                                      // 2行：······
                {                                                                                               // 1行： ······
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);                               // 0行：······
                }
            }
            else {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                // 第1行、第3行等奇数行的最后一个格子东南方向没有格子
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }
        #endregion
        
        AddCellToChunk(x, z, cell);
    }

    /// <summary>
    /// 把六边形添加到对应的网格块下
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="cell"></param>
    void AddCellToChunk(int x, int z, HexCell cell)
    {
        // 计算出这个六边形属于哪个网格块
        int chunkX = x / HexMetrics.chunkSizeX;     
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        // 计算出这个六边形是该网格块中的第几个
        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }
}
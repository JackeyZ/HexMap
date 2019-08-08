using UnityEngine;
using System.IO;

/// <summary>
/// 管理整个六边形网格地图
/// </summary>
public class HexGrid : MonoBehaviour
{
    public HexGridChunk chunkPrefab;                        // 块预制体

    public HexCell cellPrefab;                              // 六边形预制体

    public Texture2D noiseSource;

    int chunkCountX, chunkCountZ;                           // 网格块数目（把所有六边形分成多少块）

    public int seed;                                        // 随机数种子

    public int cellCountX = 20, cellCountZ = 15;                             // 六边形总数目

    public Color[] colors;                                  // 不同地形的颜色

    HexGridChunk[] chunks;                                  // 网格块数组

    HexCell[] cells;
    void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexMetrics.colors = colors;
        }
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexMetrics.colors = colors;
        CreateMap(cellCountX, cellCountZ);
    }
    public bool CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.chunkSizeX != 0 || z <= 0 || z % HexMetrics.chunkSizeZ != 0)
        {
            Debug.LogError("不支持的大小！横向必须是" + HexMetrics.chunkSizeX + "的倍数，纵向必须是" + HexMetrics.chunkSizeZ + "的整数。");
            return false;
        }

        // 如果已经创建了地图，就先把场景上旧的地图销毁掉
        if (chunks != null)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                Destroy(chunks[i].gameObject);
            }
            chunks = null;
        }

        // 生成地图
        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
        CreateChunks();
        CreateCells();
        return true;
    }
    /// <summary>
    /// 创建所有网络快
    /// </summary>
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

    /// <summary>
    /// 刷新所有网路块的三角面
    /// </summary>
    public void RefreshChunks()
    {
        if(chunks != null)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i].Refresh();
            }
        }
    }

    /// <summary>
    /// 创建所有六边形
    /// </summary>
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

    #region 地图储存和加载相关
    /// <summary>
    /// 保存所有六边形数据到文件
    /// </summary>
    /// <param name="writer"></param>
    public void Save(BinaryWriter writer)
    {
        // 储存地图大小
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }
    }

    /// <summary>
    /// 从文件加载所有六边形数据
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="header">地图版本号，可根据不同版本号作对应操作</param>
    public void Load(BinaryReader reader, int header)
    {
        // 读取地图大小并创建对应大小的地图
        int x = reader.ReadInt32();
        int z = reader.ReadInt32();

        // 当加载的地图与当前场景的地图大小不一致的时候才重新创建地图
        if (x != cellCountX || z != cellCountZ)
        {
            if (!CreateMap(x, z))
            {
                return;
            }
        }
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }
    }
    #endregion
}
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 管理整个六边形网格地图
/// </summary>
public class HexGrid : MonoBehaviour
{
    public HexGridChunk chunkPrefab;                         // 块预制体

    public HexCell cellPrefab;                               // 六边形预制体

    public Texture2D noiseSource;

    int chunkCountX, chunkCountZ;                            // 网格块数目（把所有六边形分成多少块）

    public int seed;                                         // 随机数种子

    public int cellCountX = 20, cellCountZ = 15;             // 六边形总数目

    public HexUnit unitPrefab;

    HexGridChunk[] chunks;                                   // 网格块数组

    HexCell[] cells;

    HexCellPriorityQueue searchFrontier;                     // 寻路的边界队列，用于存储未访问的边界格子

    int searchFrontierPhase;                                 // 搜索进程

    HexCell currentPathFrom, currentPathTo;                  // 用于记录寻路起点与终点

    bool currentPathExists;                                  // 寻路结果

    List<HexUnit> units = new List<HexUnit>();               // 储存地图上所有移动单位

    HexCellShaderData cellShaderData;                        // 格子数据纹理组件（用于战争迷雾）

    void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
        }
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexUnit.unitPrefab = unitPrefab;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>(); 
        CreateMap(cellCountX, cellCountZ);
    }
    public bool CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.chunkSizeX != 0 || z <= 0 || z % HexMetrics.chunkSizeZ != 0)
        {
            Debug.LogError("不支持的大小！横向必须是" + HexMetrics.chunkSizeX + "的倍数，纵向必须是" + HexMetrics.chunkSizeZ + "的整数。");
            return false;
        }

        // 清除之前的寻路路径
        ClearPath();

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
        cellShaderData.Initialize(cellCountX, cellCountZ);
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

    public void ShowUI(bool showUI)
    {
        foreach (var item in cells)
        {
            item.ShowUI = showUI;
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
        if (index < cells.Length)
        {
            return cells[index];
        }
        return null;
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
    /// 传入一条射线，获取射线触碰到的六边形
    /// </summary>
    /// <param name="ray"></param>
    /// <returns></returns>
    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }
        return null;
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
        cell.Elevation = 0;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.ShaderData = cellShaderData;
        cell.Index = i;

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

        // 储存移动单位数据
        writer.Write(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
        }
    }

    /// <summary>
    /// 从文件加载所有六边形数据
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="header">地图版本号，可根据不同版本号作对应操作</param>
    public void Load(BinaryReader reader, int header)
    {
        // 清理之前的寻路路径
        ClearPath();

        // 清理移动单位
        ClearUnits();

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

        // 存档版本2以上才有移动单位数据
        if (header >= 2)
        {
            // 读取移动单位
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }
    }
    #endregion

    #region 寻路
    /// <summary>
    /// 是否已经找到有效的寻路路径
    /// </summary>
    public bool HasPath
    {
        get
        {
            return currentPathExists;
        }
    }

    /// <summary>
    /// 两个格子之间的距离
    /// </summary>
    /// <param name="fromCell">当前所在格子</param>
    /// <param name="toCell">目标格子</param>
    /// <param name="speed">单回合移动预算</param>
    public void FindPath(HexCell fromCell, HexCell toCell, int speed)
    {
        //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        //sw.Start();

        ClearPath();                                                    // 清除上一次的路径
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, speed);
        ShowPath(speed);                                                // 展示路径

        //sw.Stop();
        //Debug.Log(sw.ElapsedMilliseconds);
    }

    bool Search(HexCell fromCell, HexCell toCell, int speed)
    {
        searchFrontierPhase += 2;
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else {
            searchFrontier.Clear();
        }
        
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        // 遍历未访问的边界格子
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            // 判断是否找到目标格子
            if (current == toCell)
            {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;                       // 到达当前格子所需的回合数

            // 把当前格子未访问的可达邻居全部加到待访问格子里
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);

                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase) // 跳过对应方向没有邻居的或已经找到最短路径的单元格
                {
                    continue;
                }

                // 跳过在水下的格子
                if (neighbor.IsUnderwater)
                {
                    continue;
                }

                // 跳过已经有单位占据的格子
                if (neighbor.Unit)
                {
                    continue;
                }

                HexEdgeType edgeType = current.GetEdgeType(neighbor);

                // 跳过陡坡
                if (current.GetEdgeType(neighbor) == HexEdgeType.Cliff)
                {
                    continue;
                }

                int moveCost;

                // 道路行走成本为1
                if (current.HasRoadThroughEdge(d))
                {
                    moveCost = 1;
                }
                // 跳过没有道路连通的围墙
                else if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
                    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                }

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;    // 用刚得到的邻居距离，算出起点出到达邻居所需的回合数
                
                // 判断该邻居是否在下一个回合才能到达
                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;         // 不直接使用上面distance的原因是，当前回合剩余行动点在下一回合会清零，所以到达该邻居实际上的移动成本更高
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.Coordinates.DistanceTo(toCell.Coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 显示搜索出来的路径
    /// </summary>
    /// <param name="speed"></param>
    void ShowPath(int speed)
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
            currentPathTo.EnableHighlight(Color.red);
        }
        currentPathFrom.EnableHighlight(Color.blue);
    }

    /// <summary>
    /// 隐藏搜索出来的路径
    /// </summary>
    /// <param name="speed"></param>
    public void ClearPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            current.DisableHighlight();
            currentPathExists = false;
        }
        currentPathFrom = currentPathTo = null;
    }

    /// <summary>
    /// 获取寻路找到的路径点列表
    /// </summary>
    /// <returns></returns>
    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
        {
            return null;
        }
        List<HexCell> path = ListPool<HexCell>.Get();
        
        // 遍历终点到起点
        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }
        path.Add(currentPathFrom);

        path.Reverse();             // 反转列表
        return path;
    }
    #endregion

    #region 移动单位相关
    /// <summary>
    /// 删除所有移动单位
    /// </summary>
    void ClearUnits()
    {
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }

    /// <summary>
    /// 添加移动单位
    /// </summary>
    /// <param name="unit">移动单位</param>
    /// <param name="location">单位所在六边形</param>
    /// <param name="orientation">旋转</param>
    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.Grid = this;
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    /// <summary>
    /// 移除移动单位
    /// </summary>
    /// <param name="unit"></param>
    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }
    #endregion

    #region 单位视野
    /// <summary>
    /// 找到一个格子附近的可视格子
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range">视野</param>
    /// <returns></returns>
    List<HexCell> GetVisibleCells(HexCell fromCell, int range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

        searchFrontierPhase += 2;
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else {
            searchFrontier.Clear();
        }

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            visibleCells.Add(current);
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase)
                {
                    continue;
                }

                int distance = current.Distance + 1;    
                if (distance > range)
                {
                    continue;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return visibleCells;
    }

    /// <summary>
    /// 增加格子可见性
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range"></param>
    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    /// <summary>
    /// 减少格子可见性
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range"></param>
    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].DecreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }
    #endregion
}
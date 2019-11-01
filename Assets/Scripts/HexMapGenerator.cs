using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：地图生成器
/// 作用：用于随机生成六边形地图
/// </summary>
public class HexMapGenerator : MonoBehaviour
{
    public HexGrid grid;                                    // 通过拖拽初始化

    public bool useFixedSeed;                               // 是否使用固定的随机数种子

    public int seed;                                        // 地图随机数种子

    int cellCount;                                          // 用于记录地图格子总数目

    HexCellPriorityQueue searchFrontier;                    // 待访问队列
        
    int searchFrontierPhase;                                // 用于记录访问阶段

    int xMin, xMax, zMin, zMax;                             // 记录横向、纵向边界六边形格子的index，在随机获取格子的时候保证格子在边界以内

    List<MapRegion> regions;

    [Tooltip("块的不规则程度")]
    [Range(0f, 0.5f)]
    public float jitterProbability = 0.25f;           

    [Tooltip("每块大陆最小数目")]
    [Range(20, 200)]
    public int chunkSizeMin = 30;              

    [Tooltip("每块大陆最大数目")]
    [Range(20, 200)]
    public int chunkSizeMax = 100;               

    [Tooltip("陆地百分比")]
    [Range(5, 95)]
    public int landPercentage = 50;                

    [Tooltip("水平面高度")]
    [Range(1, 5)]
    public int waterLevel = 3;           

    [Tooltip("高低差概率")]
    [Range(0f, 1f)]
    public float highRiseProbability = 0.25f; 

    [Tooltip("陆地下沉概率")]
    [Range(0f, 0.4f)]
    public float sinkProbability = 0.2f; 

    [Tooltip("地形最小高度")]
    [Range(-4, 0)]
    public int elevationMinimum = -2; 

    [Tooltip("地形最大高度")]
    [Range(6, 10)]
    public int elevationMaximum = 8;

    [Tooltip("横向地图边缘水域宽度")]
    [Range(0, 10)]
    public int mapBorderX = 5;

    [Tooltip("纵向地图边缘水域宽度")]
    [Range(0, 10)]
    public int mapBorderZ = 5;

    [Tooltip("区域边界水域宽度")]
    [Range(0, 10)]
    public int regionBorder = 5;

    [Tooltip("区域数目")]
    [Range(1, 4)]
    public int regionCount = 1;

    [Tooltip("侵蚀百分比")]
    [Range(0, 100)]
    public int erosionPercentage = 50;

    /// <summary>
    /// 生成随机地图
    /// </summary>
    /// <param name="x">横向格子数目</param>
    /// <param name="z">纵向格子数目</param>
    public void GenerateMap(int x, int z)
    {
        Random.State originalRandomState = Random.state;
        if (!useFixedSeed)
        {
            seed = Random.Range(0, int.MaxValue);               // 初始化随机数种子
            seed ^= (int)System.DateTime.Now.Ticks;             // ^按位异或，二者，只有一个1的时候结果为1，0 ^ 0 = 0, 1 ^ 1 = 0, 1 ^ 0 = 1 
            seed ^= (int)Time.unscaledTime;
            seed &= int.MaxValue;                               // 负数的符号位是1，int.MaxValue是正数，符号位是0，按位与把符号位设为0，使用负数转换成正数
        }
        Random.InitState(seed);

        cellCount = x * z;

        // 创建默认地图
        grid.CreateMap(x, z);

        // 初始化待访问队列
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }

        // 设置水平面
        for (int i = 0; i < cellCount; i++)
        {
            grid.GetCell(i).WaterLevel = waterLevel;
        }

        CreateRegions();        // 划分区域
        CreateLand();           // 生成陆地
        ErodeLand();            // 侵蚀
        SetTerrainType();       // 设置地形类型

        // 重置格子搜索阶段，以免影响寻路（因为后续HexGrid中也会用到，而且二者的searchFrontierPhase步长不一样）
        for (int i = 0; i < cellCount; i++)
        {
            grid.GetCell(i).SearchPhase = 0;
        }

        Random.state = originalRandomState;
    }

    /// <summary>
    /// 升起地形
    /// </summary>
    /// <param name="chunkSize">本次调用需要升起地形的格子数目</param>
    /// <param name="budget">全部需下沉格子预算</param>
    /// <param name="region">区域</param>
    int RaiseTerrain(int chunkSize, int budget, MapRegion region)
    {
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell(region);
        firstCell.SearchPhase = searchFrontierPhase;                                                        // 表示当前阶段已访问该格子
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.Coordinates;

        int rise = Random.value < highRiseProbability ? 2 : 1;                                              // 根据高低差概率随机当前格子是否升得更高
        int size = 0;                                                                                       // 用于记录已访问格子的数目
        while (size < chunkSize && searchFrontier.Count > 0)                                                // 访问数目不够并且还有待访问格子的时候进行循环
        {
            HexCell current = searchFrontier.Dequeue();
            int originalElevation = current.Elevation;                                                      // 原来的高度
            int newElevation = originalElevation + rise;                                                    // 新高度
            if (newElevation > elevationMaximum)
            {
                continue;
            }
            current.Elevation = newElevation;                                                               // 得到现在的高度

            // 如果高度上升到水平面以上，则表明有新的陆地生成，则预算-1，如果预算已经到0则跳出循环
            if (originalElevation < waterLevel && current.Elevation >= waterLevel && --budget == 0)
            {
                break;
            }
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                // 如果邻居存在并且未被访问过
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase) 
                {
                    neighbor.SearchPhase = searchFrontierPhase;                                             // 表示当前阶段已访问该格子
                    neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;                    // 用于微扰优先级
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        searchFrontier.Clear();
        return budget;
    }

    /// <summary>
    /// 下沉地形
    /// </summary>
    /// <param name="chunkSize">本次调用需要下沉地形的格子数目</param>
    /// <param name="budget">全部需上升格子的预算</param>
    int SinkTerrain(int chunkSize, int budget, MapRegion region)
    {
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell(region);
        firstCell.SearchPhase = searchFrontierPhase;                                                        // 表示当前阶段已访问该格子
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.Coordinates;

        int sink = Random.value < highRiseProbability ? 2 : 1;                                              // 根据高低差概率随机当前格子是否下降的更多
        int size = 0;                                                                                       // 用于记录已访问格子的数目
        while (size < chunkSize && searchFrontier.Count > 0)                                                // 访问数目不够并且还有待访问格子的时候进行循环
        {
            HexCell current = searchFrontier.Dequeue();
            int originalElevation = current.Elevation;                                                      // 原来的高度
            int newElevation = originalElevation - sink;                                                    // 新高度
            if (newElevation < elevationMinimum)
            {
                continue;
            }
            current.Elevation = newElevation;                                                   // 得到现在的高度

            // 如果高度从水平面以上下降到水平面一下，则预算+1，表明有一个陆地变成了水，需要生成的陆地数目+1
            if (originalElevation >= waterLevel && current.Elevation < waterLevel)
            {
                budget++;
            }
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                // 如果邻居存在并且未被访问过
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;                                             // 表示当前阶段已访问该格子
                    neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;                    // 用于微扰优先级
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        searchFrontier.Clear();
        return budget;
    }

    /// <summary>
    /// 生成陆地
    /// </summary>
    void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);      // 利用陆地率计算得到所需生成的陆地格子数目     // ps：半径为r的全六边形包含3r ^ 2 + 3r + 1个单元格

        // 循环至陆地格子预算用完
        for (int guard = 0; guard < 10000; guard++)                                 // 以防地图边缘水域过宽，而边界内部六边形数目不足以生成所有陆地格子预算，所以当循环达到10000的时候，就算预算没用完也停止生成陆地
        {
            bool sink = Random.value < sinkProbability;                             // 根据下沉概率随机获得当次改变是上升还是下沉
            for (int i = 0; i < regions.Count; i++)
            {
                MapRegion region = regions[i];
                int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax + 1);
                if (sink)
                {
                    landBudget = SinkTerrain(chunkSize, landBudget, region);
                }
                else
                {
                    landBudget = RaiseTerrain(chunkSize, landBudget, region);
                    if (landBudget == 0)
                    {
                        return;
                    }
                }
            }
        }
        if (landBudget > 0)
        {
            Debug.LogWarning("陆地生成预算没用完请检查 地图边缘大小 以及 陆地率参数： " + landBudget);
        }
    }

    /// <summary>
    /// 侵蚀地形
    /// </summary>
    void ErodeLand()
    {
        // 待侵蚀列表，用于储存所有需要侵蚀的格子
        List<HexCell> erodibleCells = ListPool<HexCell>.Get();
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            if (IsErodible(cell))
            {
                erodibleCells.Add(cell);
            }
        }
        int targetErodibleCount = (int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);   // 根据侵蚀百分比，计算出不需要侵蚀的格子数量
        while (erodibleCells.Count > targetErodibleCount)
        {
            int index = Random.Range(0, erodibleCells.Count - 1);
            HexCell cell = erodibleCells[index];                                                    // 当前需侵蚀的格子
            HexCell targetCell = GetErosionTarget(cell);                                            // 悬崖邻居

            cell.Elevation -= 1;            // 侵蚀格子
            targetCell.Elevation += 1;      // 把侵蚀掉的泥土填到悬崖邻居上

            // 直到不可再侵蚀才移除出待侵蚀列表
            if (!IsErodible(cell))
            {
                // 用最后一个格子覆盖当前格子，然后移出调最后一个格子
                erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
                erodibleCells.RemoveAt(erodibleCells.Count - 1);
            }

            // 高度下降之后检查格子的邻居是否产生了新的悬崖，如果有则加入待侵蚀列表
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                if ( neighbor && neighbor.Elevation == cell.Elevation + 2 && !erodibleCells.Contains(neighbor))
                {
                    erodibleCells.Add(neighbor);
                }
            }

            // 悬崖邻居由于高度提升，可能导致自身变成悬崖
            if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
            {
                erodibleCells.Add(targetCell);
            }

            // 由于悬崖邻居的高度增加了，可能导致悬崖邻居的邻居变成不是悬崖，所以检查所有悬崖邻居的邻居，如果由可侵蚀变成不可侵蚀则移除出待侵蚀列表
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = targetCell.GetNeighbor(d);
                if (neighbor && neighbor != cell && neighbor.Elevation == targetCell.Elevation + 1 && !IsErodible(neighbor))
                {
                    erodibleCells.Remove(neighbor);
                }
            }
        }


        // 归还列表池
        ListPool<HexCell>.Add(erodibleCells);
    }

    /// <summary>
    /// 用于判断一个六边形是否可被侵蚀
    /// 当前只有悬崖六边形可被侵蚀
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    bool IsErodible(HexCell cell)
    {
        int erodibleElevation = cell.Elevation - 2;
        // 判断是否存在一个邻居比自己低2或以上的高度
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            // 判断邻居高度是否比自己低2或以上高度
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取一个格子附近是悬崖的随机邻居
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    HexCell GetErosionTarget(HexCell cell)
    {
        // 用于储存比自己低2高度以上的邻居
        List<HexCell> candidates = ListPool<HexCell>.Get();

        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                candidates.Add(neighbor);
            }
        }

        // 随机选出一个悬崖邻居
        HexCell target = candidates[Random.Range(0, candidates.Count)];

        ListPool<HexCell>.Add(candidates);
        return target;
    }

    /// <summary>
    /// 设置地形类型
    /// </summary>
    void SetTerrainType()
    {
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            if (!cell.IsUnderwater)
            {
                cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
            }
        }
    }

    /// <summary>
    /// 随机从地图中获取一个六边形格子
    /// </summary>
    /// <returns></returns>
    HexCell GetRandomCell(MapRegion region)
    {
        Debug.Log(Random.Range(7, 5));
        return grid.GetCell(Random.Range(region.xMin, region.xMax), Random.Range(region.zMin, region.zMax));
    }

    /// <summary>
    /// 划分区域
    /// </summary>
    void CreateRegions()
    {
        if (regions == null)
        {
            regions = new List<MapRegion>();
        }
        else
        {
            regions.Clear();
        }

        MapRegion region;
        switch (regionCount)
        {
            default:
                region.xMin = mapBorderX;
                region.xMax = grid.cellCountX - mapBorderX;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                break;
            case 2:                                                                         // 两个区域（大陆）
                if (Random.value < 0.5f)
                {
                    // 左右两个
                    region.xMin = mapBorderX;
                    region.xMax = grid.cellCountX / 2 - regionBorder;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(region);
                    region.xMin = grid.cellCountX / 2 + regionBorder;
                    region.xMax = grid.cellCountX - mapBorderX;
                    regions.Add(region);
                }
                else {
                    // 上下两个
                    region.xMin = mapBorderX;
                    region.xMax = grid.cellCountX - mapBorderX;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ / 2 - regionBorder;
                    regions.Add(region);
                    region.zMin = grid.cellCountZ / 2 + regionBorder;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(region);
                }
                break;
            case 3:                                                                         // 三个区域
                // 左中右三个
                region.xMin = mapBorderX;
                region.xMax = grid.cellCountX / 3 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                region.xMin = grid.cellCountX / 3 + regionBorder;
                region.xMax = grid.cellCountX * 2 / 3 - regionBorder;
                regions.Add(region);
                region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
                region.xMax = grid.cellCountX - mapBorderX;
                regions.Add(region);
                break;
            case 4:                                                                         // 四个区域
                region.xMin = mapBorderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ / 2 - regionBorder;
                regions.Add(region);
                region.xMin = grid.cellCountX / 2 + regionBorder;
                region.xMax = grid.cellCountX - mapBorderX;
                regions.Add(region);
                region.zMin = grid.cellCountZ / 2 + regionBorder;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
                region.xMin = mapBorderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                regions.Add(region);
                break;
        }
    }

    /// <summary>
    /// 地区
    /// </summary>
    struct MapRegion
    {
        public int xMin, xMax, zMin, zMax;      // 表示区域左右下上四个边界
    }
}

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

    [Tooltip("半球")]
    public HemisphereMode hemisphere;                       // 半球

    int cellCount;                                          // 用于记录地图格子总数目

    int landCells;                                          // 用于记录陆地生成数目

    HexCellPriorityQueue searchFrontier;                    // 待访问队列
        
    int searchFrontierPhase;                                // 用于记录访问阶段

    int xMin, xMax, zMin, zMax;                             // 记录横向、纵向边界六边形格子的index，在随机获取格子的时候保证格子在边界以内

    int temperatureJitterChannel;                           // 用于储存用哪个噪声通道（x、y、z、w）对温度进行抖动

    List<MapRegion> regions;                                // 用于记录划分的区域
            
    List<ClimateData> climate = new List<ClimateData>();    // 用于记录每个格子的气候
    List<ClimateData> nextClimate = new List<ClimateData>();// 用于记录每个格子下一个演变周期的气候

    static float[] temperatureBands = { 0.1f, 0.3f, 0.6f }; // 温度带划分

    static float[] moistureBands = { 0.12f, 0.28f, 0.85f }; // 湿度带划分

    static Biome[] biomes = {                               // 根据湿度带和温度带确定的十六种组合的地形特征（x轴是湿度（干→湿）、y轴是温度（高↑低））
        new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
        new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
        new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
        new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
    };
  
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

    [Tooltip("每周期蒸发，0表示没有蒸发，1代表大量蒸发")]
    [Range(0f, 1f)]
    public float evaporationFactor = 0.5f;

    [Tooltip("每周期降水量, 0表示没有降水，云不会减少，1表示一周期所有云会因降水消失")]
    [Range(0f, 1f)]
    public float precipitationFactor = 0.25f;
    
    [Tooltip("每周期流走的水分, 0表示没有流失，1表示一周期所有水都会流走")]
    [Range(0f, 1f)]
    public float runoffFactor = 0.25f;

    [Tooltip("每周期渗透走的水分， 用于平滑等高度格子中的水分")]
    [Range(0f, 1f)]
    public float seepageFactor = 0.125f;

    [Tooltip("风向，风从哪个方向吹来")]
    public HexDirection windDirection = HexDirection.NW;

    [Tooltip("风力，1级表示没有风")]
    [Range(1f, 10f)]
    public float windStrength = 4f;

    [Tooltip("初始湿度")]
    [Range(0f, 1f)]
    public float startingMoisture = 0.1f;

    [Tooltip("陆地上河流百分比, 河流格子数目=百分比*陆地数目")]
    [Range(0, 20)]
    public int riverPercentage = 10;

    [Tooltip("河流生成湖泊的概率")]
    [Range(0f, 1f)]
    public float extraLakeProbability = 0.25f;

    [Tooltip("最低温度，默认是南半球，但如果最低温度比最高温度高，则表示北半球")]
    [Range(0f, 1f)]
    public float lowTemperature = 0f;

    [Tooltip("最高温度")]
    [Range(0f, 1f)]
    public float highTemperature = 1f;

    [Tooltip("温度波动性")]
    [Range(0f, 1f)]
    public float temperatureJitter = 0.1f;

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
        CreateClimate();        // 气候
        CreateRivers();         // 河流
        SetTerrainType();       // 设置地形类型

        // 重置格子搜索阶段，以免影响寻路（因为后续HexGrid中也会用到，而且二者的searchFrontierPhase步长不一样）
        for (int i = 0; i < cellCount; i++)
        {
            grid.GetCell(i).SearchPhase = 0;
        }

        Random.state = originalRandomState;
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
    /// 生成陆地
    /// </summary>
    void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);      // 利用陆地率计算得到所需生成的陆地格子数目     // ps：半径为r的全六边形包含3r ^ 2 + 3r + 1个单元格
        landCells = landBudget;

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
            landCells -= landBudget;
        }
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
    /// 创建每个格子的气候
    /// </summary>
    void CreateClimate()
    {
        climate.Clear();
        nextClimate.Clear();
        ClimateData initialData = new ClimateData();
        initialData.moisture = startingMoisture;                        // 每个格子初始化一个湿度
        ClimateData clearData = new ClimateData();
        for (int i = 0; i < cellCount; i++)
        {
            climate.Add(initialData);
            nextClimate.Add(clearData);
        }

        // 暂定四十个周期
        for (int cycle = 0; cycle < 40; cycle++)
        {
            for (int i = 0; i < cellCount; i++)
            {
                EvolveClimate(i);
            }

            // 完成一周期的演变之后把最新的数据赋值给当前列表。
            List<ClimateData> swap = climate;
            climate = nextClimate;
            nextClimate = swap;
        }
    }

    /// <summary>
    /// 单个格子气候
    /// </summary>
    /// <param name="cellIndex"></param>
    void EvolveClimate(int cellIndex)
    {
        HexCell cell = grid.GetCell(cellIndex);
        ClimateData cellClimate = climate[cellIndex];

        // 蒸发阶段
        if (cell.IsUnderwater)                                                          // 水下
        {
            cellClimate.moisture = 1f;                                                  // 水下格子，湿度设为1
            cellClimate.clouds += evaporationFactor;                                    // 增加云量
        }
        else                                                                            // 陆地
        {                                                                          
            float evaporation = cellClimate.moisture * evaporationFactor;               // 格子湿度*单周期蒸发量 得到当前格子蒸发量
            cellClimate.clouds += evaporation;                                          // 蒸发量转化成云量
            cellClimate.moisture -= evaporation;                                        // 由于蒸发了，所以湿度也下降了
        }
        

        // 降雨阶段
        float precipitation = cellClimate.clouds * precipitationFactor;                 // 降雨量
        cellClimate.clouds -= precipitation;                                            // 去掉由于降雨消耗掉的云
        cellClimate.moisture += precipitation;                                          // 由于降水，所以湿度增加

        float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);         // 当前海拔可容纳的最大云量（海拔越高云量越低）
        if (cellClimate.clouds > cloudMaximum)                                          // 如果当前云量超过了云最大容纳量，则强迫降雨（雨影效应）
        {
            cellClimate.moisture += cellClimate.clouds - cloudMaximum;
            cellClimate.clouds = cloudMaximum;
        }


        // 扩散阶段
        HexDirection mainDispersalDirection = windDirection.Opposite();
        float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));         // 云扩散量
        float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);                 // 河流流走的水分
        float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);               // 通过泥土渗透流失的水分
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)               // 把自身的云均匀的扩散到邻居
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (!neighbor)
            {
                continue;
            }
            ClimateData neighborClimate = nextClimate[neighbor.Index];
            // 云的扩散
            if (d == mainDispersalDirection)                                            // 判断是否顺风
            {
                neighborClimate.clouds += cloudDispersal * windStrength;
            }
            else
            {
                neighborClimate.clouds += cloudDispersal;
            }                                

            // 水分（湿度）的扩散
            int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
            if (elevationDelta < 0)                                                     // 判断邻居是否比自己矮
            {
                cellClimate.moisture -= runoff;                                         // 自身湿度减少（河流）
                neighborClimate.moisture += runoff;                                     // 邻居湿度增加（河流）
            }
            else if (elevationDelta == 0)                                               // 判断是否与自己有相同高度
            {
                cellClimate.moisture -= seepage;                                        // 自身湿度减少（渗透）
                neighborClimate.moisture += seepage;                                    // 邻居湿度增加（渗透）
            }
            
            nextClimate[neighbor.Index] = neighborClimate;
        }
        // 扩散之后把自身云量设为0
        //cellClimate.clouds = 0f;

        // 把当前格子的数据存到下一周期数据中，其中因为云在扩散后是0，所以不用叠加到下一个周期
        ClimateData nextCellClimate = nextClimate[cellIndex];
        nextCellClimate.moisture += cellClimate.moisture;                               // 叠加湿度
        if (nextCellClimate.moisture > 1f)
        {
            nextCellClimate.moisture = 1f;
        }
        nextClimate[cellIndex] = nextCellClimate;
        climate[cellIndex] = new ClimateData();
    }


    /// <summary>
    /// 生成河流
    /// </summary>
    void CreateRivers()
    {
        List<HexCell> riverOrigins = ListPool<HexCell>.Get();                                                   // 河流源头列表，用于储存可作为河流源头的六边形格子
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            if (cell.IsUnderwater)
            {
                continue;
            }
            ClimateData data = climate[i];
            float weight = data.moisture * (cell.Elevation - waterLevel) / (elevationMaximum - waterLevel);     // 湿度（0~1）* 海拔百分比（0~1）得到权重
            if (weight > 0.75f)
            {
                // 添加两次增加选中的概率
                riverOrigins.Add(cell);
                riverOrigins.Add(cell);
            }
            if (weight > 0.5f)
            {
                riverOrigins.Add(cell);
            }
            if (weight > 0.25f)
            {
                riverOrigins.Add(cell);
            }
        }

        int riverBudget = Mathf.RoundToInt(landCells * riverPercentage * 0.01f);                                // 计算得到河流格子数目预算
        while (riverBudget > 0 && riverOrigins.Count > 0)
        {
            // 随机选出河流源头
            int index = Random.Range(0, riverOrigins.Count);
            int lastIndex = riverOrigins.Count - 1;
            HexCell origin = riverOrigins[index];

            bool isValidOrigin = true;
            // 检查附近是否有河流或者水，如果有，则不允许作为河流源头（避免河流源头集中在一起）
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = origin.GetNeighbor(d);
                if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater))
                {
                    isValidOrigin = false;
                    break;
                }
            }
            if (isValidOrigin)
            {
                // 创建河流
                riverBudget -= CreateRiver(origin);
            }

            // 把选出来的河流源头从列表移除
            riverOrigins[index] = riverOrigins[lastIndex];
            riverOrigins.RemoveAt(lastIndex);
        }

        if (riverBudget > 0)
        {
            Debug.LogWarning("河流预算没有用完。");
        }

        ListPool<HexCell>.Add(riverOrigins);
    }

    /// <summary>
    /// 根据源头创建一条河流
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    int CreateRiver(HexCell origin)
    {
        int length = 1;
        HexCell cell = origin;
        List<HexDirection> flowDirections = new List<HexDirection>();                       // 用于记录可以自身河流可流向的邻居方向
        HexDirection direction = HexDirection.NE;
        while (!cell.IsUnderwater)
        {
            int minNeighborElevation = int.MaxValue;                                        // 用于记录最矮的邻居高度
            flowDirections.Clear();
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                // 没有邻居, 则跳过
                if (!neighbor)
                {
                    continue;
                }

                if (neighbor.Elevation < minNeighborElevation)
                {
                    minNeighborElevation = neighbor.Elevation;
                }

                // 邻居是自己河流的源头或者邻居有流入河流, 则跳过
                if (neighbor == origin || neighbor.HasIncomingRiver)
                {
                    continue;
                }

                // 如果邻居高度比自己高, 则跳过
                int delta = neighbor.Elevation - cell.Elevation;
                if (delta > 0)
                {
                    continue;
                }

                // 如果邻居只有流出河流(表示邻居是之前河流的源头), 则把自己合并到邻居所在的河流
                if (neighbor.HasOutgoingRiver)
                {
                    cell.SetOutgoingRiver(d);
                    return length;
                }

                // 如果邻居比自己低, 增加三次, 以增加被选中的概率
                if (delta < 0)
                {
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                }

                // 如果流向不是急转弯则增加被选中的概率
                if ( length == 1 || (d != direction.Next2() && d != direction.Previous2()))
                {
                    flowDirections.Add(d);
                }

                flowDirections.Add(d);
            }

            // 判断是否已经无法再流动（河流尽头）
            if (flowDirections.Count == 0)
            {
                // 如果还在源头, 则返回, 不生成当前河流
                if (length == 1)
                {
                    return 0;
                }

                // 如果邻居都不比自身格子矮, 则生成湖泊
                if (minNeighborElevation >= cell.Elevation)
                {
                    cell.WaterLevel = minNeighborElevation;
                    if (minNeighborElevation == cell.Elevation)
                    {
                        cell.Elevation = minNeighborElevation - 1;
                    }
                }
                break;
            }

            // 随机选择一个可流的方向
            direction = flowDirections[Random.Range(0, flowDirections.Count)];
            cell.SetOutgoingRiver(direction);
            length += 1;

            // 如果附近邻居都不比自己矮，则生成湖泊
            if (minNeighborElevation >= cell.Elevation && Random.value <= extraLakeProbability)
            {
                cell.WaterLevel = cell.Elevation;
                cell.Elevation -= 1;
            }

            cell = cell.GetNeighbor(direction);
        }
        return length;
    }

    /// <summary>
    /// 设置地形类型
    /// </summary>
    void SetTerrainType()
    {
        temperatureJitterChannel = Random.Range(0, 4);                                      // 0~3里随机获得一个数，在获取温度函数里会用到
        int rockDesertElevation = elevationMaximum - (elevationMaximum - waterLevel) / 2;   // 计算岩漠高度线（位于水平面和最大高度的中间）
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            float temperature = DetermineTemperature(cell);
            float moisture = climate[i].moisture;
            if (!cell.IsUnderwater)
            {
                // 根据温度确定格子处于哪个温度带
                int t = 0;
                for (; t < temperatureBands.Length; t++)
                {
                    if (temperature < temperatureBands[t])
                    {
                        break;
                    }
                }

                // 根据湿度确定格子处于哪个湿度带
                int m = 0;
                for (; m < moistureBands.Length; m++)
                {
                    if (moisture < moistureBands[m])
                    {
                        break;
                    }
                }

                // 根据温度带和湿度带获得地形特征
                Biome cellBiome = biomes[t * 4 + m];

                // 如果是沙漠地形
                if (cellBiome.terrain == 0)
                {
                    // 判断海拔是否到了岩漠高度线，如果是则改变地形为岩石
                    if (cell.Elevation >= rockDesertElevation)
                    {
                        cellBiome.terrain = 3;
                    }
                }
                // 强制不是沙漠地形的最高海拔格子变成雪地
                else if (cell.Elevation == elevationMaximum)
                {
                    cellBiome.terrain = 4;
                }

                // 雪地的植被等级变为0
                if (cellBiome.terrain == 4)
                {
                    cellBiome.plant = 0;
                }
                // 增加河流附近的植被等级
                else if (cellBiome.plant < 3 && cell.HasRiver)
                {
                    cellBiome.plant += 1;
                }

                cell.TerrainTypeIndex = cellBiome.terrain;
                cell.PlantLevel = cellBiome.plant;
            }
            else {
                int terrain = 1;
                // 浅海区
                if (cell.Elevation == waterLevel - 1)
                {
                    int cliffs = 0, slopes = 0;
                    for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                    {
                        HexCell neighbor = cell.GetNeighbor(d);
                        if (!neighbor)
                        {
                            continue;
                        }
                        int delta = neighbor.Elevation - cell.WaterLevel;
                        if (delta == 0)
                        {
                            slopes += 1;
                        }
                        else if (delta > 0)
                        {
                            cliffs += 1;
                        }

                        // 如果有一半以上的邻居是陆地，那么这个是海湾或者湖泊，用草地
                        if (cliffs + slopes > 3)
                        {
                            terrain = 1;
                        }
                        // 如果有悬崖，则用石头
                        else if (cliffs > 0)
                        {
                            terrain = 3;
                        }
                        // 如果没有悬崖，只有斜坡，则用沙子，表示沙滩
                        else if (slopes > 0)
                        {
                            terrain = 0;
                        }
                        // 其他原理海岸的浅水区，用草地
                        else
                        {
                            terrain = 1;
                        }
                    }
                }
                // 湖泊用草地做海底
                else if (cell.Elevation >= waterLevel)
                {
                    terrain = 1;
                }
                // 深海用岩石做海底
                else if (cell.Elevation < 0)
                {
                    terrain = 3;
                }
                // 其他用淤泥做海底
                else {
                    terrain = 2;
                }

                // 低温的地方用淤泥代替草地
                if (terrain == 1 && temperature < temperatureBands[0])
                {
                    terrain = 2;
                }
                cell.TerrainTypeIndex = terrain;
            }
        }
    }

    #region 工具函数

    /// <summary>
    /// 随机从地图中获取一个六边形格子
    /// </summary>
    /// <returns></returns>
    HexCell GetRandomCell(MapRegion region)
    {
        return grid.GetCell(Random.Range(region.xMin, region.xMax), Random.Range(region.zMin, region.zMax));
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
    /// 获得一个六边形格子的温度(0~1)
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    float DetermineTemperature(HexCell cell)
    {
        float latitude = (float)cell.Coordinates.Z / grid.cellCountZ;                           // 纬度（0~1）
        // 如果地图既包括北半球也包括南半球
        if (hemisphere == HemisphereMode.Both)
        {
            // 纬度*2，0~1表示在南半球，1~2表示在北半球
            latitude *= 2f;
            // 根据纬度判断是否在北半球，如果是，则把温度反转
            if (latitude > 1f)
            {
                latitude = 2f - latitude;
            }
        }
        // 如果是北半球，则把温度反转
        else if (hemisphere == HemisphereMode.North)
        {
            latitude = 1f - latitude;
        }

        float temperature = Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);                 // 根据最高温度和最低温度，等比缩放温度

        temperature = temperature * 0.3f + temperature * 0.7f * (1f - (cell.ViewElevation - waterLevel) / (elevationMaximum - waterLevel + 1f));   // 海拔越高温度越低

        float jitter = HexMetrics.SampleNoise(cell.Position * 0.1f, false)[temperatureJitterChannel];

        temperature += (jitter * 2f - 1f) * temperatureJitter;      // 温度波动（*2-1吧0~1转换成-1~1）
        
        return temperature;
    }

    #endregion

    #region 结构体
    /// <summary>
    /// 地区结构体
    /// </summary>
    struct MapRegion
    {
        public int xMin, xMax, zMin, zMax;      // 表示区域左右下上四个边界
    }

    /// <summary>
    /// 气候结构体
    /// </summary>
    struct ClimateData
    {
        public float clouds;                    // 云
        public float moisture;                  // 湿度
    }

    /// <summary>
    /// 生物群系（地形特征）
    /// </summary>
    struct Biome
    {
        public int terrain;                     // 地形
        public int plant;                       // 植物等级

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="terrain">地形类型： 0:沙 1:草地 2:淤泥 3:石头 4:雪</param>
        /// <param name="plant">植物浓密等级</param>
        public Biome(int terrain, int plant)
        {
            this.terrain = terrain;
            this.plant = plant;
        }
    }
    #endregion

    #region 枚举
    /// <summary>
    /// 北半球还是南半球
    /// </summary>
    public enum HemisphereMode
    {
        Both,
        North,                  // 北半球
        South                   // 南半球
    }
    #endregion
}

using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// 单个六边形的脚本
/// </summary>
public class HexCell : MonoBehaviour
{
    public Text label;                          // 坐标Text

    public Image highlight;                     // 网格选中高亮

    private int terrainTypeIndex = 0;           // 地形类型下标

    public HexGridChunk chunk;                  // 所属网格块的引用


    /// <summary>
    /// 格子上的移动单位（暂时一个格子只允许有一个单位）
    /// </summary>
    public HexUnit Unit { get; set; }

    bool hasIncomingRiver, hasOutgoingRiver;

    HexDirection incomingRiver, outgoingRiver;

    int waterLevel;                 // 水平线

    int urbanLevel;                 // 城市等级

    int farmLevel;                  // 农场等级

    int plantLevel;                 // 植物等级

    bool walled;                    // 是否有围墙

    int specialIndex = 0;           // 特殊特征物体下标，对应HexFeatureManager里的special， 0表示没有特殊特征物体

    [SerializeField]
    bool[] roads;                   // 六个方向是否有道路

    bool showUI = false;            // 是否显示六边形内的UI

    private int elevation = int.MinValue;      // 高度，在HexGrid里create的时候初始化为0

    int distance = int.MaxValue;    // 自己与选中六边形之间的距离

    int searchPhase;

    /// <summary>
    /// 偏移坐标（X,Y,Z）
    /// </summary>
    [SerializeField]
    private HexCoordinates coordinates;

    /// <summary>
    /// 邻居数组
    /// </summary>
    [SerializeField]
    private HexCell[] neighbors = null;

    /// <summary>
    /// 偏移坐标（X,Y,Z）
    /// </summary>
    public HexCoordinates Coordinates
    {
        get
        {
            return coordinates;
        }

        set
        {
            coordinates = value;
        }
    }

    /// <summary>
    /// 高度
    /// </summary>
    public int Elevation
    {
        get
        {
            return elevation;
        }

        set
        {
            if (elevation != value)
            {
                elevation = value;

                // 刷新六边形GameObject的高度
                RefreshPosition();      

                // 检查河流有效性
                ValidateRivers();

                // 高度改变判断六个方向的高度差是否太大，是否需要移除道路，
                for (int i = 0; i < roads.Length; i++)
                {
                    if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                    {
                        SetRoad(i, false);
                    }
                }

                Refresh();
            }
        }
    }

    /// <summary>
    /// 当前格子寻路搜索进程
    /// </summary>
    public int SearchPhase
    {
        get
        {
            return searchPhase;
        }

        set
        {
            searchPhase = value;
        }
    }

    /// <summary>
    /// 六边形中心本地坐标
    /// </summary>
    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    /// <summary>
    /// 地形类型下标
    /// </summary>
    public int TerrainTypeIndex
    {
        get
        {
            return terrainTypeIndex;
        }
        set
        {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    #region 寻路相关属性
    /// <summary>
    /// 寻路时的父六边形
    /// </summary>
    public HexCell PathFrom { get; set; }

    /// <summary>
    /// 用于存储寻路时到终点的估计距离，若该值为0，则说明是一个普通的广度优先搜索寻路（非启发式寻路）
    /// </summary>
    public int SearchHeuristic { get; set; }

    /// <summary>
    /// 用于指向在寻路中与自己同优先级的邻接链表的下一个格子
    /// </summary>
    public HexCell NextWithSamePriority { get; set; } 

    /// <summary>
    /// 用于寻路中未访问边界格子的访问优先级
    /// </summary>
    public int SearchPriority
    {
        get
        {
            return distance + SearchHeuristic;
        }
    }
    public int Distance
    {
        get
        {
            return distance;
        }

        set
        {
            distance = value;
        }
    }

    #endregion

    #region 河流属性
    /// <summary>
    /// 是否有流入河流
    /// </summary>
    public bool HasIncomingRiver
    {
        get
        {
            return hasIncomingRiver;
        }
    }

    /// <summary>
    /// 是否有河流流出
    /// </summary>
    public bool HasOutgoingRiver
    {
        get
        {
            return hasOutgoingRiver;
        }
    }

    /// <summary>
    /// 是否有河流
    /// </summary>
    public bool HasRiver
    {
        get
        {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }

    /// <summary>
    /// 六边形是否是河流的开端或者末端
    /// </summary>
    public bool HasRiverBeginOrEnd
    {
        get
        {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }

    /// <summary>
    /// 河流流入方向
    /// </summary>
    public HexDirection IncomingRiver
    {
        get
        {
            return incomingRiver;
        }
    }

    /// <summary>
    /// 河流流出方向
    /// </summary>
    public HexDirection OutgoingRiver
    {
        get
        {
            return outgoingRiver;
        }
    }

    /// <summary>
    /// 河流是否流过指定的方向（六边形的边）
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return hasIncomingRiver && incomingRiver == direction || hasOutgoingRiver && outgoingRiver == direction;
    }

    /// <summary>
    /// 河床高度（世界坐标Y轴）
    /// </summary>
    public float StreamBedY
    {
        get
        {
            return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
        }
    }

    /// <summary>
    /// 河流水平面高度,坐标Y轴
    /// </summary>
    public float RiverSurfaceY
    {
        get
        {
            return (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        }
    }

    /// <summary>
    /// 获得河流的流入方向或流出方向（流入方向优先级更高）
    /// </summary>
    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

    /// <summary>
    /// 判断是否可以设置一条外流河
    /// </summary>
    /// <param name="neighbor"></param>
    /// <returns></returns>
    bool IsValidRiverDestination(HexCell neighbor)
    {
        // 如果自身高度大于等于邻居高度，如果自身是在水平面以下的，则表明水平面比邻居高，则可以有外流河
        // 如果自身小于邻居高度，但自身水平面等于邻居高度，则可以有外流河
        // 如果自身小于邻居高度，但自身水平面高于邻居高度，由于自然界不会出现这种特征，所以不允许产生外流河
        return neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
    }
    #endregion

    #region 道路属性
    /// <summary>
    /// 对应方向是否有道路
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }

    /// <summary>
    /// 本六边形是否至少有一条道路
    /// </summary>
    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                {
                    return true;
                }
            }
            return false;
        }
    }
    #endregion

    #region 水平线属性
    /// <summary>
    /// 水平面高度
    /// </summary>
    public int WaterLevel
    {
        get
        {
            return waterLevel;
        }

        set
        {
            if (waterLevel == value)
            {
                return;
            }
            waterLevel = value;

            // 检查河流有效性
            ValidateRivers();

            Refresh();
        }
    }

    /// <summary>
    /// 是否在水下
    /// </summary>
    public bool IsUnderwater
    {
        get
        {
            return waterLevel > elevation;
        }
    }

    /// <summary>
    /// 水平面坐标Y轴高度
    /// </summary>
    public float WaterSurfaceY
    {
        get
        {
            return (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        }
    }
    #endregion

    #region 特征物体等级属性
    /// <summary>
    /// 城市等级
    /// </summary>
    public int UrbanLevel
    {
        get
        {
            return urbanLevel;
        }
        set
        {
            if (urbanLevel != value)
            {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// 农场等级
    /// </summary>
    public int FarmLevel
    {
        get
        {
            return farmLevel;
        }
        set
        {
            if (farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// 植物等级
    /// </summary>
    public int PlantLevel
    {
        get
        {
            return plantLevel;
        }
        set
        {
            if (plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// 是否有围墙
    /// </summary>
    public bool Walled
    {
        get
        {
            return walled;
        }

        set
        {
            if (walled != value)
            {
                walled = value;
                Refresh();
            }
        }
    }

    /// <summary>
    /// 特殊特征物体下标，对应HexFeatureManager里的special
    /// </summary>
    public int SpecialIndex
    {
        get
        {
            return specialIndex;
        }

        set
        {
            if (specialIndex != value && !HasRiver)     // 没有河流才允许设置特殊特征物体
            {
                specialIndex = value;
                RemoveRoads();                          // 有特殊特征物体的时候去除道路
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// 是否有特殊特征物体
    /// </summary>
    public bool IsSpecial
    {
        get
        {
            return specialIndex > 0;
        }
    }
    #endregion


    /// <summary>
    /// 根据高度，刷新六边形GameObject的高度
    /// </summary>
    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength; // 高度噪声偏移
        transform.localPosition = position;
    }

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    /// <summary>
    /// 根据方向，获取斜坡类型
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }

    /// <summary>
    /// 根据另一个cell，获取自身与该cell之间斜坡类型
    /// </summary>
    /// <param name="otherCell"></param>
    /// <returns></returns>
    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    /// <summary>
    /// 获得自身与对应方向邻居的高度差
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    #region 河流相关
    /// <summary>
    /// 移除流出河流
    /// </summary>
    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
        {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    ///  移除流入河流
    /// </summary>
    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
        {
            return;
        }

        // 自身的是否有流入河流置为false
        hasIncomingRiver = false;
        // 刷新自身所在的网络快
        RefreshSelfOnly();


        HexCell neighbor = GetNeighbor(incomingRiver);
        // 把河流流入方向邻居的是否有河流流出置为false
        neighbor.hasOutgoingRiver = false;
        // 刷新邻居所在网格
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// 移除六边形所有河流（流入、流出）
    /// </summary>
    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    /// <summary>
    /// 设置流出河流（会自动设置流出方向邻居的流入河流，所以不需要有设置流入河流的方法）
    /// </summary>
    /// <param name="direction"></param>
    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction)
        {
            return;
        }
        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
        {
            return;
        }
        // 邻居不存在或者邻居比自己高则不生成河流
        //if (!neighbor || elevation < neighbor.elevation)
        //{
        //    return;
        //}

        // 移除之前的流出河流
        RemoveOutgoingRiver();
        // 如果要设置的流出河流方向和之前的流入河流方向一样，则需要清理掉之前的流入河流
        if (hasIncomingRiver && incomingRiver == direction)
        {
            RemoveIncomingRiver();
        }
        // 重新设置流出河流
        hasOutgoingRiver = true;
        outgoingRiver = direction;

        // 清除特殊特征物体, 有河流不允许有特殊特征物体
        specialIndex = 0;

        // 移除邻居的流入河流
        neighbor.RemoveIncomingRiver();
        // 重新设置邻居流入河流
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        //neighbor.RefreshSelfOnly();

        // 清空对应方向的道路，并且刷新网格
        SetRoad((int)direction, false);
    }

    /// <summary>
    /// 改变高度或水平面高度的时候需要用此方法检测河流有效性
    /// </summary>
    void ValidateRivers()
    {
        if(hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver)))
        {
            RemoveOutgoingRiver();
        }
        if(hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }
    #endregion

    #region 道路相关
    /// <summary>
    /// 添加道路
    /// </summary>
    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction) &&        // 判断对应方向是否已经有道路或者是否有河流
            !IsSpecial && !GetNeighbor(direction).IsSpecial &&                  // 没有特殊特征物体
            GetElevationDifference(direction) <= 1)                             // 自身与邻居的高度差
        {
            SetRoad((int)direction, true);
        }
    }

    /// <summary>
    /// 移除道路
    /// </summary>
    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                SetRoad(i, false);
            }
        }
    }

    /// <summary>
    /// 设置道路状态
    /// </summary>
    /// <param name="index"></param>
    /// <param name="state">道路状态，有道路/无道路</param>
    void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }
    #endregion

    /// <summary>
    /// 只刷新本六边形所在的网格块
    /// </summary>
    void RefreshSelfOnly()
    {
        chunk.Refresh();

        // 矫正移动单位坐标
        if (Unit)
        {
            Unit.ValidateLocation();
        }
    }

    /// <summary>
    /// 刷新本六边形所在的网格块，以及邻居所在网格块
    /// </summary>
    void Refresh()
    {
        if (chunk)
        {
            // 刷新邻居所在的块
            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
            chunk.Refresh();
            // 矫正移动单位坐标
            if (Unit)
            {
                Unit.ValidateLocation();
            }
        }
    }

    #region 地图储存和加载相关
    public void Save(BinaryWriter writer)
    {
        // 整形数据（把0~255的整形转换成byte以节省空间）
        writer.Write((byte)terrainTypeIndex);                   // 地形类型
        writer.Write((byte)(elevation + 100));                  // 高度 +100是为了把负数高度偏移成整数，转换成byte才不会出错
        writer.Write((byte)waterLevel);                         // 水平面高度
        writer.Write((byte)urbanLevel);                         // 城市等级
        writer.Write((byte)farmLevel);                          // 农场等级
        writer.Write((byte)plantLevel);                         // 植物等级
        writer.Write((byte)specialIndex);                       // 特殊特征物体引索

        writer.Write(walled);                                   // 是否被围墙围起

        // 储存河流信息
        // 一个byte（字节）八位， 把河流流入方向有六个方向，储存在低位的三个位里0000 0000~0000 0101
        //                        把是否有河流储存在高位的第一个位 0000 0000表示没有河流，1000 0000表示有河流
        if (hasIncomingRiver)
        {
            writer.Write((byte)(incomingRiver + 128)); // +128表示在最高位加1
        }                                   
        else
        {
            writer.Write((byte)0);
        }

        if (hasOutgoingRiver)
        {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        // 储存道路数据
        // 六个道路方向放在一个字节内，六个方向分别对应低六位， 0000 0000 ~ 0011 1111
        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                roadFlags |= 1 << i;        // 左移之后按位或， 例如：010 | 001 = 011
            }
        }
        writer.Write((byte)roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        // 按照保存顺序读取整形数据
        terrainTypeIndex = reader.ReadByte();
        elevation = reader.ReadByte() - 100;
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        RefreshPosition();                                      // 刷新六边形gameobject高度
        
        walled = reader.ReadBoolean();

        // 读取流入河流数据
        byte incomingRiverData = reader.ReadByte();
        if (incomingRiverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(incomingRiverData - 128);
        }
        else
        {
            hasIncomingRiver = false;
        }
        // 读取流出河流数据
        byte ougoingRiverData = reader.ReadByte();
        if (ougoingRiverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(ougoingRiverData - 128);
        }
        else
        {
            hasOutgoingRiver = false;
        }

        // 道路数据
        byte roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;         // 1左移i位，然后按位与，提取roadFlags对应位的数据。按位与： 0010 0110 & 0000 0010 = 0000 0010
        }                                                                                                              //0010 0110 & 0000 1000 = 0000 0000
    }
    #endregion

    #region UI
    /// <summary>
    /// 是否显示六边形内的UI
    /// </summary>
    public bool ShowUI
    {
        get
        {
            return showUI;
        }

        set
        {
            if (value != showUI)
            {
                showUI = value;
                UpdateDistanceLabel();
            }
        }
    }

    /// <summary>
    /// 距离文字
    /// </summary>
    void UpdateDistanceLabel()
    {
        label.gameObject.SetActive(showUI);
    }

    public void SetLabel(string str = "")
    {
        label.text = str;
    }

    /// <summary>
    /// 隐藏选中高亮
    /// </summary>
    public void DisableHighlight()
    {
        highlight.enabled = false;
    }

    /// <summary>
    /// 显示选中高亮
    /// </summary>
    public void EnableHighlight(Color color)
    {
        highlight.color = color;
        highlight.enabled = true;
    }
    /// <summary>
    /// 显示选中高亮
    /// </summary>
    public void EnableHighlight()
    {
        highlight.color = Color.white;
        highlight.enabled = true;
    }

    #endregion

}
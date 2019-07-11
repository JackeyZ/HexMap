using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个六边形的脚本
/// </summary>
public class HexCell : MonoBehaviour
{
    public Text label;              // 坐标Text

    private Color color;            // 颜色

    public HexGridChunk chunk;      // 所属网格块的引用

    bool hasIncomingRiver, hasOutgoingRiver;

    HexDirection incomingRiver, outgoingRiver;

    int waterLevel;                 // 水平线

    [SerializeField]
    bool[] roads;                   // 六个方向是否有道路

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

    private int elevation;       // 高度

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
            UpdateText();
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
                Vector3 position = transform.localPosition;
                position.y = value * HexMetrics.elevationStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength; // 高度噪声偏移
                transform.localPosition = position;

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
    /// 纯色区颜色
    /// </summary>
    public Color Color
    {
        get
        {
            return color;
        }

        set
        {
            if(color != value)
            {
                color = value;
                Refresh();
            }
        }
    }

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

    public void UpdateText()
    {
        //label.text = Coordinates.ToString(); // 坐标文字
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
        //RefreshSelfOnly();      

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
        // 判断对应方向是否已经有道路或者是否有河流，最后判断自身与邻居的高度差
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction) && GetElevationDifference(direction) <= 1)
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
        }
    }
}
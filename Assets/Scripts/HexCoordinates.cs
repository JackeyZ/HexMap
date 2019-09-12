using UnityEngine;

/// <summary>
/// 单个六边形的偏移坐标，X、Y、Z，分别代表该六边形相对于地图原点在某个轴向上偏移了多少个单位，由于对称的关系所以有：X + Y + Z = 0
/// </summary>
[System.Serializable]
public struct HexCoordinates
{
    [SerializeField]
    private int x, z;

    /// <summary>
    /// 逻辑偏移坐标X，右下偏移，正方向为右下
    /// </summary>
    public int X { get { return x; } }

    /// <summary>
    /// 逻辑偏移坐标Z，纵向偏移，正方向为正上方
    /// </summary>
    public int Z { get { return z; } }

    /// <summary>
    /// 逻辑偏移坐标Y,左下偏移，正方向为左下
    /// </summary>
    public int Y
    {
        get
        {
            return -X - Z;
        }
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    public HexCoordinates(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    /// <summary>
    /// 正交逻辑坐标转换成偏移逻辑坐标
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        //return new HexCoordinates(x, z);
        return new HexCoordinates(x - z / 2, z);
    }

    /// <summary>
    /// 世界坐标转换成Hex地图的三维度偏移坐标
    /// </summary>
    /// <param name="position">鼠标点击发出射线所触碰到的世界坐标相对于Hex地图原点的坐标</param>
    /// <returns></returns>
    public static HexCoordinates FromPosition(Vector3 position)
    {
        // Z轴改变所造成的实际偏移
        float offset = position.z * Mathf.Tan(Mathf.PI / 6);

        // 根据实际坐标，计算实际偏移坐标
        float posX = position.x - offset;
        float posY = -position.x - offset;  // Y轴与X轴对称

        // 实际偏移坐标转换成逻辑偏移坐标（单位化）
        float x = posX / (HexMetrics.innerRadius * 2f);
        float y = posY / (HexMetrics.innerRadius * 2f);

        // 四舍五入取整
        int iX = Mathf.RoundToInt(x);   
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        // 如果iX + iY + iZ != 0表示点击的点不在六边形的内接圆内，
        // 因为在单位化的时候使用的是innerRadius（内径），所以不在内接圆内的点在单位化之后，某一个轴向偏移会大于0.5，取整会出现问题
        // 所以需要摒弃取整幅度最大的坐标，然后从另外两个轴向偏移重新计算它
        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);
            if (dX > dY && dX > dZ)                 // 如果X取整幅度最大，则重建X
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)                       // 如果Z取整幅度最大，则重建Z
            {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public override string ToString()
    {
        return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }


    /// <summary>
    /// 根据坐标计算自己到这个坐标的距离
    /// </summary>
    /// <param name="coordinates"></param>
    /// <returns></returns>
    public int DistanceTo(HexCoordinates otherCoordinates)
    {
        int[] dis = { Mathf.Abs(X - otherCoordinates.X), Mathf.Abs(Y - otherCoordinates.Y), Mathf.Abs(Z - otherCoordinates.Z) }; // 各坐标的差值绝对值
        return Mathf.Max(dis);  // 取最大的绝对值，就是距离
    }
}
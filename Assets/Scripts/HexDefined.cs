using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 六边形格子六个扇形方向
/// </summary>
public enum HexDirection
{
    /// <summary>
    /// 东北
    /// </summary>
    NE,

    /// <summary>
    /// 东
    /// </summary>
    E,

    /// <summary>
    /// 东南
    /// </summary>
    SE,

    /// <summary>
    /// 西南
    /// </summary>
    SW,

    /// <summary>
    /// 西
    /// </summary>
    W,

    /// <summary>
    /// 西北
    /// </summary>
    NW
}

/// <summary>
/// 邻居方向枚举扩展
/// </summary>
public static class HexDirectionExtensions
{
    /// <summary>
    /// 获取相反的邻居方向
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    /// <summary>
    /// 获得上一个邻居方向，顺时针
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static HexDirection Previous(this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    /// <summary>
    /// 获得上上个邻居方向，顺时针
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }

    /// <summary>
    /// 获得下一个邻居方向，顺时针
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static HexDirection Next(this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }

    /// <summary>
    /// 获得下下个邻居方向，顺时针
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }
}

/// <summary>
/// 斜坡（阶梯）类型枚举
/// </summary>
public enum HexEdgeType
{
    /// <summary>
    /// 平面
    /// </summary>
    Flat,

    /// <summary>
    /// 斜坡（阶梯）
    /// </summary>
    Slope,

    /// <summary>
    /// 陡坡
    /// </summary>
    Cliff
}

/// <summary>
/// 五个顶点的边，用于描述有 五个顶点的边，通过两个Vector3插值计算出中间的另外三个顶点
/// </summary>
public struct EdgeVertices
{
    public Vector3 v1, v2, v3, v4, v5;

    /// <summary>
    /// 普通构造器，会平均分布每个顶点
    /// </summary>
    /// <param name="corner1"></param>
    /// <param name="corner2"></param>
    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, 1f / 4f);
        v3 = Vector3.Lerp(corner1, corner2, 2f / 4f);
        v4 = Vector3.Lerp(corner1, corner2, 3f / 4f);
        v5 = corner2;
    }

    /// <summary>
    /// 特殊构造器，根据outerStep参数分布内部顶点
    /// </summary>
    /// <param name="corner1">开始顶点</param>
    /// <param name="corner2">结束顶点</param>
    /// <param name="outerStep">插值调整参数</param>
    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, outerStep);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
        v5 = corner2;
    }
}

/// <summary>
/// 河流Editor状态
/// </summary>
enum OptionalToggle
{
    /// <summary>
    /// 忽略河流
    /// </summary>
    Ignore,

    /// <summary>
    /// 添加河流
    /// </summary>
    Yes,

    /// <summary>
    /// 移除河流
    /// </summary>
    No
}

/// <summary>
/// 散列随机网格元素（随机数）
/// </summary>
public struct HexHash
{
    public float a, b, c, d, e;

    public static HexHash Create()
    {
        HexHash hash;
        hash.a = Random.value * 0.999f;
        hash.b = Random.value * 0.999f;
        hash.c = Random.value * 0.999f;
        hash.d = Random.value * 0.999f;
        hash.e = Random.value * 0.999f;
        return hash;
    }
}

[System.Serializable]
public struct HexFeatureCollection
{
    public Transform[] Prefabs;             //同等级下不同的建筑

    public Transform Pick(float choice)
    {
        return Prefabs[(int)(choice * Prefabs.Length)];
    }
}
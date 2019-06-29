﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：储存地图常量
/// 作用：
/// </summary>
public static class HexMetrics
{
    /// <summary>
    /// 外接圆半径
    /// </summary>
    public const float outerRadius = 10f;

    /// <summary>
    /// 内接圆半径比外接圆半径
    /// </summary>
    public const float outerToInner = 0.866025404f;

    /// <summary>
    /// 外接圆半径比内接圆半径
    /// </summary>
    public const float innerToOuter = 1f / outerToInner;

    /// <summary>
    /// 内接圆半径
    /// </summary>
    public const float innerRadius = outerRadius * outerToInner;

    /// <summary>
    /// 六边形内部纯色区域百分比
    /// </summary>
    public const float solidFactor = 0.8f;

    /// <summary>
    /// 水平面六边形内部纯色区域百分比
    /// </summary>
    public const float waterFactor = 0.6f;

    /// <summary>
    /// 颜色混合区域（桥）百分比
    /// </summary>
    public const float blendFactor = 1f - solidFactor;

    /// <summary>
    /// 水平面六边形的桥所占百分比
    /// </summary>
    public const float waterBlendFactor = 1f - waterFactor;

    /// <summary>
    /// 相对中心的顶点坐标，即六边形六个角的相对坐标
    /// </summary>
    static Vector3[] corners = {
        new Vector3(0f, 0f, outerRadius),                       
        new Vector3(innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(0f, 0f, -outerRadius),
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0f, 0.5f * outerRadius)
    };

    public const float elevationStep = 3f;                                      // 单位高度，六边形之间高度每相差一个单位，则真实高度相差3

    public const int terracesPerSlope = 2;                                      // 每个斜坡的平阶梯数目
    public const int terraceSteps = terracesPerSlope * 2 + 1;                   // 平阶梯和斜阶梯的总数
    public const float horizontalTerraceStepSize = 1f / terraceSteps;           // 阶梯x、z轴偏移占比
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);   // 阶梯y轴偏移占比

    public static Texture2D noiseSource;                                        // 使用柏林噪声生成的纹理
    public const float cellPerturbStrength = 0f;                                // 扰乱幅度
    public const float elevationPerturbStrength = 0.2f;                         // 高度(y)微扰
    public const float noiseScale = 0.003f;                                     // 用于对坐标进行缩放，以适应UV坐标，世界坐标->UV坐标（0~1）

    public const int chunkSizeX = 5, chunkSizeZ = 5;                            // 单个网格块内六边形数目 

    public const float streamBedElevationOffset = -1.75f;                       // 河床偏移高度
    public const float waterElevationOffset = -0.5f;                            // 河水（海水）平面偏移高度（由于高度微扰为0.5f,所以这里设置为-0.5f刚好，-0.6也可以）

    /// <summary>
    /// 根据方向获取三角面的第一个顶点
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int)direction];
    }

    /// <summary>
    /// 根据方向获取三角面的第二个顶点
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        int index = (int)direction;
        return corners[index + 1 == 6 ? 0 : index + 1];
    }

    /// <summary>
    /// 根据方向获取六边形内部纯色区域的第一个顶点
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }

    /// <summary>
    /// 根据方向获取六边形内部纯色区域的第二个顶点
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        int index = (int)direction;
        return corners[index + 1 == 6 ? 0 : index + 1] * solidFactor;
    }

    /// <summary>
    /// 根据方向获取水平面六边形内部纯色区域的第一个顶点
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return corners[(int)direction] * waterFactor;
    }

    /// <summary>
    /// 根据方向获取水平面六边形内部纯色区域的第二个顶点
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        int index = (int)direction;
        return corners[index + 1 == 6 ? 0 : index + 1] * waterFactor;
    }

    /// <summary>
    /// 根据方向获取六边形内部纯色区，扇形边缘中点
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        int index = (int)direction;
        return (corners[index] + corners[index + 1 == 6 ? 0 : index + 1]) * (0.5f * solidFactor);
    }

    /// <summary>
    /// 获得混合区长方形的宽（高）（偏移向量）,根据内侧顶点求长方形的外侧顶点所需向量
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetBridge(HexDirection direction)
    {
        return (GetFirstCorner(direction) + GetSecondCorner(direction)) * blendFactor;
    }

    /// <summary>
    /// 获得水平面混合区长方形的宽（高）（偏移向量）,根据内侧顶点求长方形的外侧顶点所需向量
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (GetFirstCorner(direction) + GetSecondCorner(direction)) * waterBlendFactor;
    }

    /// <summary>
    /// 斜坡插值计算
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;

        // 偶数阶梯才需要y轴插值
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize; // 偶数代表是斜阶梯，斜阶梯才进行y轴插值
        a.y += (b.y - a.y) * v;
        return a;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
    {
        EdgeVertices result;
        result.v1 = TerraceLerp(a.v1, b.v1, step);
        result.v2 = TerraceLerp(a.v2, b.v2, step);
        result.v3 = TerraceLerp(a.v3, b.v3, step);
        result.v4 = TerraceLerp(a.v4, b.v4, step);
        result.v5 = TerraceLerp(a.v5, b.v5, step);
        return result;
    }

    /// <summary>
    /// 斜坡颜色插值
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    /// <summary>
    /// 获取斜坡类型
    /// </summary>
    /// <param name="elevation1"></param>
    /// <param name="elevation2"></param>
    /// <returns></returns>
    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        // 高度一样为平面
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }
        int delta = elevation2 - elevation1;
        // 高度相差1，生成阶梯斜坡
        if (delta == 1 || delta == -1)
        {
            return HexEdgeType.Slope;
        }
        // 其他情况，生成斜面
        return HexEdgeType.Cliff;
    }

    public static Vector4 SampleNoise(Vector3 position)
    {
        // 参数为0~1之间（UV坐标）
        return noiseSource.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale) * cellPerturbStrength;
    }

    /// <summary>
    /// 噪声扰乱顶点位置， 只对x、z进行扰乱
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += sample.x * 2 - 1;
        //position.y += sample.y * 2 - 1;
        position.z += sample.z * 2 - 1;
        return position;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：贝赛尔曲线
/// 作用：
/// </summary>
public static class Bezier
{
    /// <summary>
    /// 二阶贝赛尔曲线
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Vector3 GetPoint(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        t = Mathf.Clamp01(t);
        float r = 1f - t;
        return r * r * a + 2f * r * t * b + t * t * c;
    }

    /// <summary>
    /// 二阶贝赛尔曲线导数
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Vector3 GetDerivative(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        t = Mathf.Clamp01(t);
        return 2f * ((1f - t) * (b - a) + t * (c - b));                     // 对二阶贝赛尔公式的t进行求导
    }
}
using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 管理整个六边形网格里面的mesh
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    public bool useCollider;                                    // 是否拥有碰撞盒（河流没有碰撞盒，地形有）
    public bool useColors;                                      // 是否需要改变颜色
    public bool useUVCoordinates;                               // 是否需要UV坐标
    public bool useUV2Coordinates;                              // 是否需要第二个UV坐标

    Mesh hexMesh;

    [NonSerialized]
    List<Vector3> vertices;                                     // 网格顶点

    [NonSerialized]
    List<Color> colors;                                         // 顶点颜色列表，在shader中读取

    [NonSerialized]
    List<Vector2> uvs;                                          // UV坐标

    [NonSerialized]
    List<Vector2> uv2s;                                         // UV坐标2

    [NonSerialized]
    List<int> triangles;                                        // 三角面对应顶点引索

    MeshCollider meshCollider;                                  // mesh碰撞器

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        hexMesh.name = "Hex Mesh";
    }

    /// <summary>
    /// 清理网格数据
    /// </summary>
    public void Clear()
    {
        hexMesh.Clear();
        vertices = ListPool<Vector3>.Get();
        if (useColors)
        {
            colors = ListPool<Color>.Get();
        }
        if (useUVCoordinates)
        {
            uvs = ListPool<Vector2>.Get();
        }
        if (useUV2Coordinates)
        {
            uv2s = ListPool<Vector2>.Get();
        }
        triangles = ListPool<int>.Get();
    }

    /// <summary>
    /// 应用网格数据
    /// </summary>
    public void Apply()
    {
        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);        // 归还给列表池
        
        if (useColors)
        {
            hexMesh.SetColors(colors);
            ListPool<Color>.Add(colors);        // 归还给列表池
        }
        if (useUVCoordinates)
        {
            hexMesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);         // 归还给列表池
        }
        if (useUV2Coordinates)
        {
            hexMesh.SetUVs(1, uv2s);
            ListPool<Vector2>.Add(uv2s);        // 归还给列表池
        }

        hexMesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);           // 归还给列表池

        hexMesh.RecalculateNormals();
        meshCollider.sharedMesh = hexMesh;
    }

    /// <summary>
    /// 给三角形添加顶点和三角面引索
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    /// <summary>
    /// 给三角形添加顶点和三角面引索（不对顶点进行噪声干扰）
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    /// <summary>
    /// 增加三角面三个顶点的颜色
    /// </summary>
    /// <param name="color"></param>
    public void AddTriangleColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    /// <summary>
    /// 增加三角面三个顶点的颜色
    /// </summary>
    /// <param name="color1"></param>
    /// <param name="color2"></param>
    /// <param name="color3"></param>
    public void AddTriangleColor(Color color1, Color color2, Color color3)
    {
        colors.Add(color1);
        colors.Add(color2);
        colors.Add(color3);
    }

    /// <summary>
    /// 给长方形添加顶点和三角面引索
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        // 混色区长方形四个顶点
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        vertices.Add(HexMetrics.Perturb(v4));

        // 两个三角面组成混色区的长方形                       //长方形顶点位置：  v3----v4
        // v1 -> v3 -> v2                                     //                  v1----v2
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);

        // v2 -> v3 -> v4
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    /// <summary>
    /// 给长方形添加顶点和三角面引索（不对顶点进行噪声干扰）
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);

        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    /// <summary>
    /// 给长方形顶点添加颜色
    /// </summary>
    /// <param name="c1">内侧顶点颜色</param>
    /// <param name="c2">外侧顶点颜色</param>
    public void AddQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }

    /// <summary>
    /// 给长方形顶点添加颜色
    /// </summary>
    /// <param name="c1">内侧顶点颜色</param>
    /// <param name="c2">外侧顶点颜色</param>
    public void AddQuadColor(Color c)
    {
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
    }

    /// <summary>
    /// 给长方形顶点添加颜色
    /// </summary>
    /// <param name="c1"></param>
    /// <param name="c2"></param>
    /// <param name="c3"></param>
    /// <param name="c4"></param>
    public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }


    #region UV相关
    /// <summary>
    /// 给三角形三个顶点添加uv坐标
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    /// <summary>
    /// 给四边形四个顶点添加uv坐标
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }

    /// <summary>
    /// 给四边形四个顶点添加uv坐标
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }

    /// <summary>
    /// 给三角形三个顶点添加uv坐标2
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    /// <summary>
    /// 给四边形四个顶点添加uv坐标2
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }

    /// <summary>
    /// 给四边形四个顶点添加uv坐标2
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }
    #endregion
}

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
    //public bool useColors;                                      // 是否需要改变颜色
    public bool useUVCoordinates;                               // 是否需要UV坐标
    public bool useUV2Coordinates;                              // 是否需要第二个UV坐标
    //public bool useTerrainTypes;                                // 是否需要地形类型
    public bool useCellData;                                    // 是否需要单元数据纹理

    Mesh hexMesh;

    [NonSerialized]
    List<Vector3> vertices;                                    // 网格顶点

    [NonSerialized]
    List<Vector3> cellIndices;                                 // 用于记录每个顶点的地形类型

    [NonSerialized]
    List<Color> cellWeights;                                   // 每个顶点的地形权重

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
        if (useUVCoordinates)
        {
            uvs = ListPool<Vector2>.Get();
        }
        if (useUV2Coordinates)
        {
            uv2s = ListPool<Vector2>.Get();
        }
        if (useCellData)
        {
            cellWeights = ListPool<Color>.Get();
            cellIndices = ListPool<Vector3>.Get();
        }
        triangles = ListPool<int>.Get();
    }

    /// <summary>
    /// 应用网格数据
    /// </summary>
    public void Apply()
    {
        if (cellIndices != null && cellIndices.Count != vertices.Count)
        {
            Debug.Log(vertices.Count + ":" + cellIndices.Count);
        }
        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);        // 归还给列表池
        
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
        if (useCellData)
        {
            hexMesh.SetColors(cellWeights);
            ListPool<Color>.Add(cellWeights);
            hexMesh.SetUVs(2, cellIndices);     // 存储在texcoord2中
            ListPool<Vector3>.Add(cellIndices);
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
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
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
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
        uv2s.Add(uv4);
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
        uv2s.Add(new Vector2(uMin, vMin));
        uv2s.Add(new Vector2(uMax, vMin));
        uv2s.Add(new Vector2(uMin, vMax));
        uv2s.Add(new Vector2(uMax, vMax));
    }
    #endregion

    #region 给三角面或四边面的顶点增加格子数据
    /// <summary>
    /// 增加三角面三个顶点的数据。地形、地形占比等
    /// </summary>
    /// <param name="indices">顶点附近的三个格子索引（x, y, z）</param>
    /// <param name="weights1">第一个顶点的三种地形占比（r, g, b）</param>
    /// <param name="weights2">第二个顶点的三种地形占比（r, g, b）</param>
    /// <param name="weights3">第三个顶点的三种地形占比（r, g, b）</param>
    public void AddTriangleCellData(Vector3 indices, Color weights1, Color weights2, Color weights3)
    {
        cellIndices.Add(indices);       // 地形引索（x, y, z分别是周边三个格子的索引）
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);      // 顶点地形占比（r,g,b分别对应三个格子的占比）
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
    }

    /// <summary>
    /// 增加三角面三个顶点的数据。附近格子、格子占比等
    /// </summary>
    /// <param name="indices"></param>
    /// <param name="weights"></param>
    public void AddTriangleCellData(Vector3 indices, Color weights)
    {
        AddTriangleCellData(indices, weights, weights, weights);
    }

    /// <summary>
    /// 增加四边面四个顶点的数据。地形、地形占比等
    /// </summary>
    /// <param name="indices">x,y,z储存顶点附近三个六边形格子的索引</param>
    /// <param name="weights1">第一个顶点附近格子占比，x,y,z分别对应附近三个格子的影响占比</param>
    /// <param name="weights2">第二个顶点附近格子占比，x,y,z分别对应附近三个格子的影响占比</param>
    /// <param name="weights3">第三个顶点附近格子占比，x,y,z分别对应附近三个格子的影响占比</param>
    /// <param name="weights4">第四个顶点附近格子占比，x,y,z分别对应附近三个格子的影响占比</param>
    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2, Color weights3, Color weights4)
    {
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
        cellWeights.Add(weights4);
    }

    /// <summary>
    /// 增加四边面四个顶点的数据。地形、地形占比等
    /// </summary>
    /// <param name="indices"></param>
    /// <param name="weights1"></param>
    /// <param name="weights2"></param>
    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2)
    {
        AddQuadCellData(indices, weights1, weights1, weights2, weights2);
    }

    /// <summary>
    /// 增加四边面四个顶点的数据。地形、地形占比等
    /// </summary>
    /// <param name="indices"></param>
    /// <param name="weights"></param>
    public void AddQuadCellData(Vector3 indices, Color weights)
    {
        AddQuadCellData(indices, weights, weights, weights, weights);
    }
    #endregion
}

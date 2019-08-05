using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 名称：网格块
/// 作用：用于管理单个网格块
/// </summary>
public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;

    public HexMesh terrain;             // 地形
    public HexMesh rivers;              // 河流
    public HexMesh roads;               // 道路
    public HexMesh water;               // 开阔水面
    public HexMesh waterShore;          // 沿岸水面
    public HexMesh estuaries;           // 河口（瀑布下方，河海交接处）

    public HexFeatureManager features;  // 地貌特征管理器，用于给六边形增加特征

    HexMesh hexMesh;
    Canvas gridCanvas;

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }

    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
    }

    public void Refresh()
    {
        enabled = true;
    }

    void LateUpdate()
    {
        // 刷新三角面
        Triangulate();
        enabled = false;
    }


    /// <summary>
    /// 三角化网格
    /// </summary>
    /// <param name="cells"></param>
    public void Triangulate()
    {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        features.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
        features.Apply();
    }

    void Triangulate(HexCell cell)
    {
        // 对六个方向进行三角化
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }

        if (!cell.IsUnderwater)
        {
            if (!cell.HasRiver && !cell.HasRoads)
            {
                features.AddFeature(cell, cell.Position);
            }

            if (cell.IsSpecial)
            {
                features.AddSpecialFeature(cell, cell.Position);
            }
        }
    }

    /// <summary>
    /// 三角面化对应方向的扇形（一个六边形有六个方向的扇形）
    /// </summary>
    /// <param name="direction">方向</param>
    /// <param name="cell">六边形</param>
    void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);        // 纯色区三角面第一个顶点
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);       // 纯色区三角面第二个顶点

        EdgeVertices e = new EdgeVertices(v1, v2);

        // 如果六边形内有河流则使用另外的方法三角化
        if (cell.HasRiver)
        {
            // 如果对应方向有河流则调整高度为河床高度
            if (cell.HasRiverThroughEdge(direction))
            {
                e.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, center, e);
                }
            }
            else
            {
                // 三角化有河流的六边形内的非河流扇形以及道路
                TriangulateAdjacentToRiver(direction, cell, center, e);

                if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
                {
                    features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
                }
            }
        }
        // 如果六边形内没有河流则进行普通的扇形三角化，以及三角化道路
        else
        {
            TriangulateWithoutRiver(direction, cell, center, e);

            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
            {
                //放置一个特征物体
                features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
            }
        }

        // 每两个六边形共享一个桥，所以每个六边形只需要生成三个桥（东北桥、东桥、东南桥）
        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, e);
        }

        // 如果六边形在水下，则三角化水平面
        if (cell.IsUnderwater)
        {
            TriangulateWater(direction, cell, center);
        }
    }

    /// <summary>
    /// 三角面化桥、角落以及桥中间的河水
    /// </summary>
    /// <param name="direction">生成方向</param>
    /// <param name="cell">自身六边形</param>
    /// <param name="e1">双色混合区靠近圆心的边</param>
    void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        // 对应方向上的邻居
        HexCell neighbor = cell.GetNeighbor(direction);         // "??"表示如果前者为空则使用后者 ，a ?? b是 a != null ? a : b的简写

        // 如果没有邻居则不需要连接桥
        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        Vector3 e2_v1 = e1.v1 + bridge;
        Vector3 e2_v5 = e1.v5 + bridge;
        e2_v1.y = e2_v5.y = neighbor.Position.y; // 由于对y方向进行了噪声扰乱，所以这里直接取高度，不能在用neighbor.Elevation * HexMetrics.elevationStep计算了
        EdgeVertices e2 = new EdgeVertices(e2_v1, e2_v5);

        bool hasRiver = cell.HasRiverThroughEdge(direction);    // 对应方向是否有河流
        bool hasRoad = cell.HasRoadThroughEdge(direction);      // 对应方向是否有路

        // 如果六边形对应方向有河流，则调整桥邻居一侧的中心顶点的高度（自身在三角化六边形三角面的时候已经调整过了（Triangulate方法里调整的））
        if (hasRiver)
        {
            e2.v3.y = neighbor.StreamBedY;
        }

        ///// 桥混合区
        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            // 生成阶梯
            TriangulateEdgeTerraces(e1, cell, e2, neighbor, hasRoad);
        }
        else
        {
            // 生成斜面
            TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color, hasRoad);
        }
        ///// 桥混合区end

        // 围墙
        features.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);

        //// 三角混合区（角落）
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        // 每个六边形只需要生成两个三角（东北桥右侧三角、东桥下侧三角），因为三个六边形共享一个三角混合区，
        // 如果没有下一个邻居则不需要生成三角
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 nextDirectionV3 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            nextDirectionV3.y = nextNeighbor.Position.y; //nextNeighbor.Elevation * HexMetrics.elevationStep;

            // 找出最矮的六边形，赋值不同的参数，以便于分类处理所有情况
            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(e1.v5, cell, e2.v5, neighbor, nextDirectionV3, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(nextDirectionV3, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(e2.v5, neighbor, nextDirectionV3, nextNeighbor, e1.v5, cell);
            }
            else
            {
                TriangulateCorner(nextDirectionV3, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
            }
        }
        //// 三角混合区（角落）End

        // 判断是否有河流流过，如果有则三角化桥中间的河水
        if (cell.HasRiverThroughEdge(direction))
        {
            e2.v3.y = neighbor.StreamBedY;

            // 判断自己是否是陆地
            if (!cell.IsUnderwater)
            {
                // 判断邻居是否是陆地
                if (!neighbor.IsUnderwater)
                {
                    TriangulateRiverQuad(e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        0.8f,
                        cell.HasIncomingRiver && cell.IncomingRiver == direction
                    );
                }
                else
                {
                    // 三角化瀑布
                    TriangulateWaterfallInWater(e1.v2, e1.v4, e2.v2, e2.v4, cell.RiverSurfaceY, neighbor.RiverSurfaceY, neighbor.WaterSurfaceY);
                }
            }
            // 如果自己在水下，邻居是陆地
            else if (!neighbor.IsUnderwater && neighbor.Elevation > cell.WaterLevel)
            {
                // 三角化瀑布
                TriangulateWaterfallInWater(e2.v4, e2.v2, e1.v4, e1.v2, neighbor.RiverSurfaceY, cell.RiverSurfaceY, cell.WaterSurfaceY);
            }
        }
    }

    /// <summary>
    /// 三角化六边形之间的阶梯
    /// </summary>
    /// <param name="e1"></param>
    /// <param name="beginCell"></param>
    /// <param name="e2"></param>
    /// <param name="endCell"></param>
    void TriangulateEdgeTerraces(EdgeVertices e1, HexCell beginCell, EdgeVertices e2, HexCell endCell, bool hasRoad)
    {
        EdgeVertices lastEdgeVertices = e1;
        EdgeVertices curEdgeVertices;
        Color lastColor = beginCell.Color;
        Color curColor;
        for (int step = 1; step <= HexMetrics.terraceSteps; step++)
        {
            curEdgeVertices = HexMetrics.TerraceLerp(e1, e2, step);
            curEdgeVertices = HexMetrics.TerraceLerp(e1, e2, step);
            curColor = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, step);

            TriangulateEdgeStrip(lastEdgeVertices, lastColor, curEdgeVertices, curColor, hasRoad);

            lastEdgeVertices = curEdgeVertices;
            lastColor = curColor;
        }
    }

    /// <summary>
    /// 三角面化角落（三色混合区）
    /// </summary>
    /// <param name="bottom"></param>
    /// <param name="bottomCell">最矮的六边形</param>
    /// <param name="left"></param>
    /// <param name="leftCell">最矮六边形左边的六边形</param>
    /// <param name="right"></param>
    /// <param name="rightCell">最矮六边形右边的六边形</param>
    void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                // bottom的左边是阶梯，右边也是阶梯
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                // 左边是阶梯，右边是平面
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            else
            {
                // 左边是阶梯，右边是陡坡
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                // 左边是平面，右边是阶梯
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                // 左边是陡坡，右边是阶梯
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                // 左右都是陡坡，且左边比右边矮，且左边与右边连接的桥是阶梯
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                // 左右都是陡坡，且右边比左边矮，且左边与右边连接的桥是阶梯
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        }
        else {
            // 三个六边形之间的桥都是陡坡的情况
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }

        // 给三色混合区添加围墙
        features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
    }

    /// <summary>
    /// 三角化角落阶梯，用于三角化三边分别是：平、阶、阶的角落
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell">三角形阶梯所在六边形</param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        // 先生成第一个三角形
        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(beginCell.Color, c3, c4);

        // 后面的阶梯生成四边形
        for (int i = 2; i <= HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }
    }

    /// <summary>
    /// 三角化角落阶梯，处理底部左边是阶梯，右边是陡坡的情况（移动参数可适应其他一边阶梯一边陡坡的情况）
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = (float)(leftCell.Elevation - beginCell.Elevation) / (rightCell.Elevation - beginCell.Elevation); // 高度差占比
        // 得到右侧边与左侧六边形相交的点
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        // 如果左边六边形与右边六边形之间是阶梯
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    /// <summary>
    /// 三角化角落阶梯，处理底部左边是陡坡，右边是阶梯的情况（移动参数可适应其他一边阶梯一边陡坡的情况）
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = (float)(rightCell.Elevation - beginCell.Elevation) / (leftCell.Elevation - beginCell.Elevation);
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

        // 下方三角
        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        // 上方三角，先判断上方的边是否是阶梯
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    /// <summary>
    /// 三角化三色混合区角落里的阶梯
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="boundary"></param>
    /// <param name="boundaryColor"></param>
    void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor)
    {
        Vector3 v1 = begin;
        Vector3 v2;
        Color c1 = beginCell.Color;
        Color c2;
        for (int i = 1; i <= HexMetrics.terraceSteps; i++)
        {
            v2 = HexMetrics.TerraceLerp(begin, left, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(v1), HexMetrics.Perturb(v2), boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
            v1 = v2;
            c1 = c2;
        }
    }

    /// <summary>
    /// 三角化五顶点扇形
    /// </summary>
    /// <param name="center"></param>
    /// <param name="edge"></param>
    /// <param name="color"></param>
    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v4, edge.v5);
        terrain.AddTriangleColor(color);
    }

    /// <summary>
    /// 三角化长有五个顶点的桥（长方形）
    /// </summary>
    /// <param name="e1"></param>
    /// <param name="c1"></param>
    /// <param name="e2"></param>
    /// <param name="c2"></param>
    void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2, bool hasRoad = false)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);

        // 判断该桥是否有道路
        if (hasRoad)
        {
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
        }
    }

    #region 河流相关
    /// <summary>
    /// 三角化是河流开端或者结尾（只有流出或者只有流入）的六边形内部的有河流的扇形
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        // 计算出六边形中心点与外部边缘的中线
        //   外部边缘：   ————
        //   中线：        -——-
        //   六边形中心点：   .
        EdgeVertices m = new EdgeVertices(Vector3.Lerp(center, e.v1, 0.5f), Vector3.Lerp(center, e.v5, 0.5f));
        // 调整中线的中心顶点的高度为河床高度
        m.v3.y = e.v3.y;

        // 三角化中线到外部边缘之间的长方形
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

        // 三角化六边形中心店到中线的扇形
        TriangulateEdgeFan(center, m, cell.Color);

        // 三角化河水
        if (!cell.IsUnderwater)                    // 检查是否在陆地
        {
            bool reversed = cell.HasIncomingRiver; // 流入河水需要翻转UV
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed)
            {
                rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            }
            else
            {
                rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
            }
        }
    }

    /// <summary>
    /// 三角化有河流流过（有流出也有流入）的六边形内部的有河流的扇形
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL;    //六边形中心线的左边顶点
        Vector3 centerR;    //六边形中心线的左边顶点

        // 如果流入和流出方向刚好相反（河道是一条直线）
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            // 把六边形中心点扩展成一条垂直于河道的线（中心线）
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;        // 计算出中心线的左边顶点
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;           // 计算出中心线的右边顶点
        }
        // 如果流入方向和流出方向相邻
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2 / 3f);     // 把中心线右顶点往外部移动，让中心河道宽一点
        }
        // 如果流入方向和流出方向相邻
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, e.v1, 2 / 3f);     // 把中心线左顶点往外部移动，让中心河道宽一点
            centerR = center;
        }
        // 如果流入方向和流出方向相隔一个扇形
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);    // 调整中心线右顶点，使河道变宽
        }
        // 如果流入方向和流出方向相隔一个扇形
        else
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);// 调整中心线右顶点，使河道变宽
            centerR = centerR = center;
        }


        // 计算出中心线与外部边缘的中线
        // 梯形：
        //   外部边缘：   ————
        //   中线：        -——-
        //   六边形中心线： ——
        EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, 0.5f), Vector3.Lerp(centerR, e.v5, 0.5f), 1 / 6f);

        center = Vector3.Lerp(centerL, centerR, 0.5f);   // 中心点重新设置为中心线两顶点的中点
        m.v3.y = center.y = e.v3.y;                      // 调整中线的中心顶点的高度为河床高度

        // 三角化中线到外部边缘之间的长方形
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

        // 三角化六边形中心线到梯形中线的梯形（两个三角形，两个长方形）
        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddTriangleColor(cell.Color);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuadColor(cell.Color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddQuadColor(cell.Color);
        terrain.AddTriangle(centerR, m.v4, m.v5);
        terrain.AddTriangleColor(cell.Color);

        // 三角化河水
        if (!cell.IsUnderwater)                                 // 检查是否在陆地，如果在陆地则三角化河水
        {
            bool reversed = cell.IncomingRiver == direction;    // 流入河水需要翻转UV
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    /// <summary>
    /// 三角化有河流的六边形内部的非河流扇形
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        // 如果六边形内有道路
        if (cell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);
        }
        // 如果下一个相邻的扇形有河流
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            // 如果上一个相邻的扇形有河流
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.innerToOuter * 0.5f); // 调整中心点到中心线对应方向的顶点上
            }
            // 如果上上一个相邻的扇形有河流
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;    // 调整中心点到中心线对应方向的顶点上
            }
        }
        // 如果上一个相邻的扇形有河流并且下下个扇形有河流
        else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;       // 调整中心点到中心线对应方向的顶点上
        }

        EdgeVertices m = new EdgeVertices(Vector3.Lerp(center, e.v1, 0.5f), Vector3.Lerp(center, e.v5, 0.5f));

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);
    }


    /// <summary>
    /// 三角化河流四边形
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="y">高度（河水平面）</param>
    /// <param name="v">UV坐标的V</param>
    /// <param name="reversed"></param>
    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

    /// <summary>
    /// 三角化河流四边形
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="y1">自身高度（河水平面）</param>
    /// <param name="y1">邻居高度（河水平面）</param>
    /// <param name="v">UV坐标的V</param>
    /// <param name="reversed"></param>
    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reversed)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        // 添加顶点
        rivers.AddQuad(v1, v2, v3, v4);

        // 添加UV坐标（把一个六边形内的河水（六边形内四个四边形，加上桥，总共五个四边形）
        // UV坐标的v分为五等分：0~0.2， 0.2~0.4， 0.4~0.6， 0.6~0.8， 0.8~1）
        if (reversed)
        {
            rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);   // 流入河流
        }
        else
        {
            rivers.AddQuadUV(0f, 1f, v, v + 0.2f);          // 流出河流
        }
    }
    #endregion

    #region 道路相关
    /// <summary>
    /// 三角化有河流的六边形内的道路
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center">六边形的中心</param>
    /// <param name="e"></param>
    void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);           // 扇形是否有道路
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous()); // 上一个方向是否有河流
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());         // 下一个方向是否有河流

        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;                                            // 把道路中心初始化成六边形中心

        // 如果六边形是河流的源头或者尽头
        if (cell.HasRiverBeginOrEnd)
        {
            // 道路中点往河流的反方向推三分之一
            roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }
        // 如果六边形里河流流入方向和流出方向成一直线
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
        {
            Vector3 corner;             // 位于河流垂线上的点
            if (previousHasRiver)
            {
                // 如果扇形内没有河流且下一个方向的扇形也没有河流，则返回不进行道路三角化
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
                {
                    return;
                }
                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else
            {
                // 如果扇形内没有河流且上一个方向的扇形也没有河流，则返回不进行道路三角化
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
                {
                    return;
                }
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            // 需要把道路中心两个点沿着河流的垂线往外推
            roadCenter += corner * 0.5f;

            // 添加桥梁
            if ( cell.IncomingRiver == direction.Next() &&               // 由于有多个没有河流经过的扇形，所以只选取一个方向的扇形添加桥梁，保证只实例化一次桥梁
               ( cell.HasRoadThroughEdge(direction.Next2()) || cell.HasRoadThroughEdge(direction.Opposite()) ) ) // 河流对面的扇形也有道路才添加桥梁
            {
                features.AddBridge(roadCenter, center - corner * 0.5f); // 沿着河流垂线找到桥的第二个端点
            }

            // 六边形中心也往外推
            center += corner * 0.25f;
        }
        // 如果流入河流与流出河流相邻
        else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
        {
            // 道路中心往流入流出河流的相交尖端方向推
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        }
        // 如果流入河流与流出河流相邻
        else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
        {
            // 道路中心往流入流出河流的相交尖端方向推
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        }
        // 如果扇形上一个方向有河流，下一个方向也有河流（流出流入河流相隔一个扇形，当前扇形位于河流弧度的内侧）
        else if (previousHasRiver && nextHasRiver)
        {
            // 如果扇形里没有道路，则返回不进行道路三角化
            if (!hasRoadThroughEdge)
            {
                return;
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;    // 该参数与三角化河流那里一样用0.5，可以刚好移到河流边缘（TriangulateAdjacentToRiver方法内）
        }
        // 流出流入河流相隔一个扇形,且当前扇形位于河流弧度的外侧
        else
        {
            HexDirection middle; // 中间扇形的方向
            if (previousHasRiver)
            {
                middle = direction.Next();
            }
            else if (nextHasRiver)
            {
                middle = direction.Previous();
            }
            else
            {
                middle = direction;
            }
            // 如果河流弧度外侧三个扇形都没有河流，则不需要对道路进行三角化
            if ( !cell.HasRoadThroughEdge(middle) 
                && !cell.HasRoadThroughEdge(middle.Previous()) 
                && !cell.HasRoadThroughEdge(middle.Next()))
            {
                return;
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
            roadCenter += offset * 0.25f;

            
            if (direction == middle &&                              // 避免重复生成桥梁，只在创建河流弧度外侧的中间扇形道路的时候添加桥梁
                cell.HasRoadThroughEdge(direction.Opposite()))      // 河对岸也要有道路
            {
                features.AddBridge(roadCenter, center - offset * (HexMetrics.innerToOuter * 0.7f));     // 这里的第二个参数为道路在河流弧度内侧时的道路中心
            }
        }


        // 三角化道路
        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        // 如果上一个方向有河流，则三角化一个道路边缘填补空隙
        if (previousHasRiver)
        {
            TriangulateRoadEdge(roadCenter, center, mL);
        }
        // 如果下一个方向有河流，则三角化一个道路边缘填补空隙
        if (nextHasRiver)
        {
            TriangulateRoadEdge(roadCenter, mR, center);
        }
    }

    /// <summary>
    /// 三角化没有河流六边形内的扇形以及扇形内的道路
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateWithoutRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        // 三角化扇形
        TriangulateEdgeFan(center, e, cell.Color);

        // 判断所在六边形是否有道路，如果有则进行三角化
        if (cell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(direction, cell); // 获得左右中点的插值
            TriangulateRoad(
                center,
                Vector3.Lerp(center, e.v1, interpolators.x),        // 计算中点与扇形边缘第一个顶点之间连线的对应插值的点
                Vector3.Lerp(center, e.v5, interpolators.y),        // 计算中点与扇形边缘最后一个顶点之间连线的对应插值的点
                e,                                                  // 扇形边缘（弧）
                cell.HasRoadThroughEdge(direction)                  // 该扇形方向是否有道路
            );
        }
    }

    /// <summary>
    /// 三角化扇形内的道路
    ///    ----e
    ///   mL--mR
    ///      .
    /// </summary>
    /// <param name="center">六边形中点</param>
    /// <param name="mL">e.v1与center的中点</param>
    /// <param name="mR">e.v5与center的中点</param>
    /// <param name="e">扇形的弧(五顶点边)</param>
    /// <param name="hasRoadThroughCellEdge">该扇形是否有道路</param>
    void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e,bool hasRoadThroughCellEdge)
    {
        if (hasRoadThroughCellEdge)
        {
            // 外侧长方形
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);

            // 内侧两个三角形
            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    /// <summary>
    /// 获得六边形中心与扇形弧左右顶点之间连线的插值
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <returns></returns>
    Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
    {
        Vector2 interpolators;
        // 如果对应方向扇形就有道路，则使用0.5插值
        if (cell.HasRoadThroughEdge(direction))
        {
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            // 如果上一个方向扇形有道路，则使用0.5插值，否则使用0.25
            interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            // 如果下一个方向扇形有道路，则使用0.5插值，否则使用0.25
            interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }

    /// <summary>
    /// 用于三角化一个三角形
    /// 常用于三角化有道路的六边形内的非道路所在扇形的道路边缘
    /// </summary>
    /// <param name="center">六边形中心</param>
    /// <param name="mL">中心与扇形边缘第一个顶点之间的中点</param>
    /// <param name="mR">中心与扇形边缘最后一个顶点之间的中点</param>
    void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
    }

    /// <summary>
    /// 三角化道路长方形（长三个顶点，宽两个顶点）
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="v5"></param>
    /// <param name="v6"></param>
    void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6)
    {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
    }
    #endregion

    #region 水平面相关

    /// <summary>
    /// 三角化水平面六边形
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;

        HexCell neighbor = cell.GetNeighbor(direction);
        if(neighbor != null)
        {
            // 判断扇形对应方向是否沿岸
            if (!neighbor.IsUnderwater)
            {
                TriangulateWaterShore(direction, cell, neighbor, center);
            }
            else
            {
                TriangulateOpenWater(direction, cell, neighbor, center);
            }
        }
    }

    /// <summary>
    /// 三角化开阔水平面扇形（非边缘）
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        // 三角化扇形
        water.AddTriangle(center, c1, c2);

        // 三角化水平面桥
        if (direction <= HexDirection.SE)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction); // 获得桥的宽（高）
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);

            // 三角化角落
            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                {
                    return;
                }
                water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
            }
        }
    }

    /// <summary>
    /// 三角化沿岸水面扇形
    /// </summary>
    /// <param name="direction">当前处理的扇形方向</param>
    /// <param name="cell">自身六边形</param>
    /// <param name="neighbor">邻居六边形</param>
    /// <param name="center">自身六边形中点</param>
    void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        EdgeVertices e1 = new EdgeVertices(c1, c2);

        // 三角化沿岸扇形
        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);

        // 三角化沿岸水平面桥
        Vector3 neighbor_center = neighbor.Position;
        neighbor_center.y = center.y;                   // 保持y与自身等高
        // 得到邻居纯色区的弧（边）
        EdgeVertices e2 = new EdgeVertices(neighbor_center + HexMetrics.GetSecondSolidCorner(direction.Opposite()), 
                            neighbor_center + HexMetrics.GetFirstSolidCorner(direction.Opposite()));

        if (cell.HasRiverThroughEdge(direction))
        {
            TriangulateEstuary(e1, e2, cell.IncomingRiver == direction);
        }
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0, 0, 0, 1);                           // 陆地一侧v坐标为1
            waterShore.AddQuadUV(0, 0, 0, 1);
            waterShore.AddQuadUV(0, 0, 0, 1);
            waterShore.AddQuadUV(0, 0, 0, 1);
        }

        // 三角化角落
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());  // 岸边一定是一个桥一个角落的，所以不会重复创建角落
        if (nextNeighbor != null)
        {
            Vector3 v3 = nextNeighbor.Position + 
                (nextNeighbor.IsUnderwater ? HexMetrics.GetFirstWaterCorner(direction.Previous()) : HexMetrics.GetFirstSolidCorner(direction.Previous())); // 判断是水面还是陆地，二者纯色区域占比不一样
            v3.y = center.y;        // 保持y与自身等高

            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUV(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)); // 第三个顶点需要判断是在水面还是陆地，水面v为0，陆地用v为1
        }
    }

    /// <summary>
    /// 三角化瀑布
    /// </summary>
    /// <param name="v1">从瀑布上方，自上而下看，上方左边的顶点</param>
    /// <param name="v2">上右顶点</param>
    /// <param name="v3">下左顶点</param>
    /// <param name="v4">下右顶点</param>
    /// <param name="y1">上方所在六边形河流高度</param>
    /// <param name="y2">下方所在六边形河流高度</param>
    /// <param name="waterY">下方水平面高度</param>
    void TriangulateWaterfallInWater(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,float y1, float y2, float waterY)
    {
        // 让桥边线顶点高度变成河流高度
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        // 由于底部顶点的位置已经改变了，它们和原始顶点受微扰的程度不一样。
        // 这意味着最后的结果和原始瀑布不相符。为了解决这个问题，我们需要在插值前手动微扰顶点，然后加上一个未被微扰的四边形。
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);

        // 用插值的方法获得v3->v1向量、v4->v2向量与水平面的交点
        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);

        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }

    /// <summary>
    /// 三角化河口
    /// </summary>
    /// <param name="e1">自身纯色区扇形边（弧）</param>
    /// <param name="e2">邻居纯色区扇形边（弧）</param>
    /// <param name="incomingRiver">是否是流入河</param>
    void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver)
    {
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        waterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        // 河口顶点 : e2:     v1----v5
        //            e1:      v2--v4
        //
        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3); 
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        // 该UV的v用于判断离岸远近，u用于匹配瀑布水流消失插值（瀑布下方为1，扩散外围为0）
        estuaries.AddQuadUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        estuaries.AddTriangleUV(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        estuaries.AddQuadUV(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

        // 如果是流入河流的河口
        if (incomingRiver)
        {

            // uv2 用于匹配河水流动, v为河水流动方向
            // 由于是河口位于桥的下面，所以v坐标是0.8~1， 
            // 由于水平面纯色区占比只有0.6，陆地水面交接处的桥是0.2+0.4，比陆地与陆地交接处的桥（0.2+0.2）大了50%,所以v坐标扩大50%，变成0.8~1.1
            estuaries.AddQuadUV2(new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
            estuaries.AddTriangleUV2( new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f),new Vector2(0f, 0.8f));
            estuaries.AddQuadUV2(new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f), new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
        }
        // 如果是流出河流的河口（翻转uv）
        else
        {
            estuaries.AddQuadUV2(new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f), new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
            estuaries.AddTriangleUV2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            estuaries.AddQuadUV2(new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f), new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
        }
    }

    #endregion
}

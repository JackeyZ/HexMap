using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：特征物体管理器
/// 作用：管理建筑、农场、植物、围墙等特征物体
/// </summary>
public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] urbanCollections;                 // 建筑预制体（等级从高到低排列）
    public HexFeatureCollection[] farmCollections;                  // 农场预制体（等级从高到低排列）
    public HexFeatureCollection[] plantCollections;                 // 植物预制体（等级从高到低排列）

    public HexMesh walls;                                           // 围墙Mesh

    public Transform wallTower;                                     // 围墙塔楼的预制体

    public Transform bridge;                                        // 桥梁的预制体

    public Transform[] special;                                     // 特殊特征物体预制体

    private Transform container;                                    // 特征物体父物体

    public void Clear() {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
        walls.Clear();
    }
    public void Apply() {
        walls.Apply();
    }

    /// <summary>
    /// 增加特征物体
    /// </summary>
    /// <param name="position">所需添加的坐标（未被微扰的坐标）</param>
    public void AddFeature(HexCell hexCell ,Vector3 position)
    {
        // 如果有特殊特征物体，则不生成普通特征物体
        if (hexCell.IsSpecial)
        {
            return;
        }
        HexHash hash = HexMetrics.SampleHashGrid(position);
        Transform prefab = PickPrefab(urbanCollections, hexCell.UrbanLevel, hash.a, hash.d);
        Transform otherPrefab = PickPrefab(farmCollections, hexCell.FarmLevel, hash.b, hash.d);

        float usedHash = hash.a;
        if (prefab)
        {
            if (otherPrefab && hash.b < hash.a)
            {
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
            usedHash = hash.b;
        }
        otherPrefab = PickPrefab(plantCollections, hexCell.PlantLevel, hash.c, hash.d);
        if (prefab)
        {
            if (otherPrefab && hash.c < usedHash)
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else {
            return;
        }

        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);    // 随机朝向
        instance.SetParent(container, false);
    }

    /// <summary>
    /// 根据等级和随机值来选取建筑预制体
    /// </summary>
    /// <param name="level"></param>
    /// <param name="hash"></param>
    /// <returns></returns>
    Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
    {
        if (level > 0)
        {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                {
                    return collection[i].Pick(choice);
                }
            }
        }
        return null;
    }

    #region 围墙相关
    /// <summary>
    /// 给两个六边形交界处（双色混合区）添加围墙
    /// </summary>
    /// <param name="near">混合区靠近圆心的边</param>
    /// <param name="nearCell">自身所在的六边形</param>
    /// <param name="far">混合区外侧边</param>
    /// <param name="farCell">邻居六边形</param>
    public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell, bool hasRiver, bool hasRoal)
    {
        //如果自己和邻居，其中一个有围墙
        if (nearCell.Walled != farCell.Walled 
            && !nearCell.IsUnderwater && !farCell.IsUnderwater 
            && nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
        {
            AddWallSegment(near.v1, far.v1, near.v2, far.v2);
            if (hasRiver || hasRoal)
            {
                // 添加墙壁左右两侧四边形
                AddWallCap(near.v2, far.v2);
                AddWallCap(far.v4, near.v4);
            }
            else
            {
                AddWallSegment(near.v2, far.v2, near.v3, far.v3);
                AddWallSegment(near.v3, far.v3, near.v4, far.v4);
            }
            AddWallSegment(near.v4, far.v4, near.v5, far.v5);
        }
    }

    /// <summary>
    /// 给三个六边形交界处的三角形（三色混合区）添加围墙
    /// </summary>
    /// <param name="c1">第一个六边形与三角形交接的顶点</param>
    /// <param name="cell1"></param>
    /// <param name="c2">第二个六边形与三角形交接的顶点</param>
    /// <param name="cell2"></param>
    /// <param name="c3">第三个六边形与三角形交接的顶点</param>
    /// <param name="cell3"></param>
    public void AddWall(Vector3 c1, HexCell cell1, Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3)
    {
        if (cell1.Walled)
        {
            if (cell2.Walled)
            {
                if (!cell3.Walled)
                {
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }
            }
            else if (cell3.Walled)
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if (cell2.Walled)
        {
            if (cell3.Walled)
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if (cell3.Walled)
        {
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
        }
    }

    /// <summary>
    /// 创建一段围墙
    /// </summary>
    /// <param name="nearLeft">内侧边的左顶点</param>
    /// <param name="farLeft">外侧边左顶点</param>
    /// <param name="nearRight">内侧点的右顶点</param>
    /// <param name="farRight">外侧边右顶点</param>
    /// <param name="addTower">是否在本段围墙添加塔楼</param>
    void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight, bool addTower = false)
    {
        // 先对六边形的顶点进行微扰再计算围墙，以免用算出围墙之后再被微扰导致围墙厚薄不一
        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

        Vector3 leftOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);         // 左侧顶点近处指向远处的向量，向量长度为墙壁厚度的一半
        Vector3 rightOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);      // 右侧顶点近处指向远处的向量，向量长度为墙壁厚度的一半

        float leftTop = left.y + HexMetrics.wallHeight;     // 计算左上方顶点坐标
        float rightTop = right.y + HexMetrics.wallHeight;   // 计算右上方顶点坐标

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftOffset;
        v2 = v4 = right - rightOffset;
        v3.y = leftTop;                                     // 左顶点高度
        v4.y = rightTop;                                    // 右顶点高度

        walls.AddQuadUnperturbed(v1, v2, v3, v4);           // 生成围墙内侧四边形

        Vector3 t1 = v3, t2 = v4;                           // 记录内侧围墙四边形顶部两个顶点

        v1 = v3 = left + leftOffset;
        v2 = v4 = right + rightOffset;
        v3.y = leftTop;                                     // 左顶点高度
        v4.y = rightTop;                                    // 右顶点高度

        walls.AddQuadUnperturbed(v2, v1, v4, v3);           // 生成围墙外侧四边形

        walls.AddQuadUnperturbed(t1, t2, v3, v4);           // 生成围墙顶部四边形

        // 实例化塔楼
        if (addTower)
        {
            Transform towerInstance = Instantiate(wallTower);
            towerInstance.transform.localPosition = (left + right) * 0.5f;  // 坐标设为围墙中心线的中点
            Vector3 rightDirection = right - left;
            rightDirection.y = 0f;
            towerInstance.transform.right = rightDirection;                 // 设置旋转
            towerInstance.SetParent(container, false);
        }
    }

    /// <summary>
    /// 创建一段围墙(角落围墙)
    /// 当第一个六边形没有围墙时，第二第三个六边形必须要有围墙
    /// 当第一个六边形有围墙时，第二第三个六边形必须没有围墙
    /// </summary>
    /// <param name="pivot">与自身距离最近的三角形顶点</param>
    /// <param name="pivotCell">自身六边形</param>
    /// <param name="left">三角形左边顶点</param>
    /// <param name="leftCell">角落左边的邻居</param>
    /// <param name="right">三角形右边顶点</param>
    /// <param name="rightCell">角落右边的邻居</param>
    void AddWallSegment(Vector3 pivot, HexCell pivotCell,Vector3 left, HexCell leftCell,Vector3 right, HexCell rightCell)   
    {
        // 如果中枢六边形在水下
        if (pivotCell.IsUnderwater)
        {
            return;
        }
        
        // 左边是否要有围墙（左侧六边形不在水下并且与中枢六边形之间不是陡坡）
        bool hasLeftWall = !leftCell.IsUnderwater && pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
        // 右边是否要有围墙（右侧六边形不在水下并且与中枢六边形之间不是陡坡）
        bool hasRighWall = !rightCell.IsUnderwater && pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall)
        {
            if (hasRighWall)
            {
                bool hasTower = false;
                // 判断两边高度， 斜坡不生成塔楼
                if (leftCell.Elevation == rightCell.Elevation)
                {
                    HexHash hash = HexMetrics.SampleHashGrid((pivot + left + right) * (1f / 3f)); // 用角落中点对散列网络进行取样，得到坐标对应的随机值
                    hasTower = hash.e < HexMetrics.wallTowerThreshold;
                }
                AddWallSegment(pivot, left, pivot, right, hasTower);
            }
            // 右边没有墙壁且右侧六边形比左侧高，则表明右侧一个陡坡
            else if (leftCell.Elevation < rightCell.Elevation)  
            {
                AddWallWedge(pivot, left, right);
            }
            else {
                AddWallCap(pivot, left);         // 有一边没有围墙的时候两侧墙边需要添加四边形
            }
        }
        else if (hasRighWall)
        {
            // 左边没有墙壁且左侧六边形比右侧高，则表明左侧一个陡坡
            if (rightCell.Elevation < leftCell.Elevation)
            {
                AddWallWedge(right, pivot, left);
            }
            {
                AddWallCap(right, pivot);        // 有一边没有围墙的时候两侧墙边需要添加四边形
            }
        }
    }

    /// <summary>
    /// 给墙壁的两侧添加四边形
    /// </summary>
    /// <param name="near"></param>
    /// <param name="far"></param>
    void AddWallCap(Vector3 near, Vector3 far)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);

        Vector3 center = HexMetrics.WallLerp(near, far);                // 计算围墙底部中心线其中一端的顶点             
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);  // 厚度偏移向量

        Vector3 v1, v2, v3, v4;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = center.y + HexMetrics.wallHeight;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);
    }

    /// <summary>
    /// 在围墙与陡坡之间的角落添加墙壁（三色混合去内）
    /// </summary>
    /// <param name="near"></param>
    /// <param name="far"></param>
    /// <param name="point">陡坡一侧的顶点</param>
    void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        Vector3 pointTop = point;
        point.y = center.y;             // 修正陡坡一侧顶点的高度（陡坡的高度）

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;
        
        walls.AddQuadUnperturbed(v1, point, v3, pointTop);
        walls.AddQuadUnperturbed(point, v2, pointTop, v4);
        walls.AddTriangleUnperturbed(pointTop, v3, v4);
    }
    #endregion

    #region 河流上的桥梁
    /// <summary>
    /// 添加桥梁
    /// </summary>
    /// <param name="roadCenter1">桥一端道路的中心点</param>
    /// <param name="roadCenter2">桥另一端道路的中心点</param>
    public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
    {
        // 微扰之后再计算桥梁位置
        roadCenter1 = HexMetrics.Perturb(roadCenter1);
        roadCenter2 = HexMetrics.Perturb(roadCenter2);

        Transform instance = Instantiate(bridge);
        instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;            // 取中点
        instance.forward = roadCenter2 - roadCenter1;                           // 设置桥梁旋转方向

        // 设置桥梁长度
        float lenght = Vector3.Distance(roadCenter1, roadCenter2);
        instance.localScale = new Vector3(1f, 1f, lenght * (1 / HexMetrics.bridgeDesignLength)); // bridgeDesignLength是桥梁默认长度

        instance.SetParent(container, false);
    }
    #endregion

    #region 特殊特征物体
    public void AddSpecialFeature(HexCell cell, Vector3 position)
    {
        Transform instance = Instantiate(special[cell.SpecialIndex - 1]);   // 因为0表明没有特殊特征物体, 但是数组从0开始，所以这里-1
        instance.localPosition = HexMetrics.Perturb(position);
        HexHash hash = HexMetrics.SampleHashGrid(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        instance.SetParent(container, false);
    }
    #endregion
}

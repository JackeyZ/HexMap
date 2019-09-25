using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 名称：地图单位
/// 作用：用于控制六边形地图上的移动单位
/// </summary>
public class HexUnit : MonoBehaviour
{
    public static HexUnit unitPrefab;                                           // 用于储存单位预制体，在HexGrid.Awake里初始化

    List<HexCell> pathToTravel;                                                 // 移动路径

    const float travelSpeed = 4f;                                               // 移动速度

    const float rotationSpeed = 180f;                                           // 旋转速度

    HexCell location;                                                           // 位于哪一个六边形上
    /// <summary>
    /// 移动单位位于哪一个六边形上
    /// </summary>
    public HexCell Location
    {
        get
        {
            return location;
        }
        set
        {
            if (location)
            {
                // 先清除旧格子对自己的引用
                location.Unit = null;
            }
            location = value;
            transform.localPosition = value.Position;
            value.Unit = this;
        }
    }

    float orientation;                                                          // y轴旋转
    /// <summary>
    /// y轴旋转
    /// </summary>
    public float Orientation
    {
        get
        {
            return orientation;
        }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    void OnEnable()
    {
        if (location)
        {
            //transform.localPosition = location.Position;
        }
    }

    /// <summary>
    /// 矫正坐标
    /// </summary>
    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }

    public void Die()
    {
        location.Unit = null;
        Destroy(gameObject);
    }

    /// <summary>
    /// 判断格子是否是有效的目的地
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && !cell.Unit;              // 目标格子不是水下格子并且没有移动单位
    }

    #region 移动表现
    /// <summary>
    /// 沿着给出的路径移动
    /// </summary>
    /// <param name="path"></param>
    public void Travel(List<HexCell> path)
    {
        Location = path[path.Count - 1];
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    /// <summary>
    /// 沿着路径移动协程
    /// </summary>
    IEnumerator TravelPath()
    {
        Vector3 a, b, c = pathToTravel[0].Position;
        transform.localPosition = c;                            // Travel()方法里设置Location的时候把单位设置到了目的地，在这里初始化单位位置
        yield return LookAt(pathToTravel[1].Position);
        float t = Time.deltaTime * travelSpeed;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * 0.5f;
            for (; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 dir = Bezier.GetDerivative(a, b, c, t);                 // 通过求导得到曲线切线方向
                dir.y = 0f;
                transform.localRotation = Quaternion.LookRotation(dir);
                yield return null;
            }
            t -= 1;
        }
        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (; t < 1f; t += Time.deltaTime * travelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 dir = Bezier.GetDerivative(a, b, c, t);                     // 通过求导得到曲线切线方向
            dir.y = 0f;
            transform.localRotation = Quaternion.LookRotation(dir);
            yield return null;
        }
        transform.localPosition = location.Position;                            // 确保精确的移动到目标点
        pathToTravel = null;                                                    // 移动完毕的时候释放寻路路径
    }

    IEnumerator LookAt(Vector3 point)
    {
        point.y = transform.localPosition.y;

        Quaternion fromRotation = transform.localRotation;                                      // 当前旋转值
        Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);       // 目标旋转值

        float angle = Quaternion.Angle(fromRotation, toRotation);
        if (angle > 0)
        {
            float speed = rotationSpeed / angle;
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
            {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }

    /// <summary>
    /// 绘制Gizmos球，方便观察路径, 只会在Scene窗口下显示
    /// </summary>
    void OnDrawGizmos()
    {
        if (pathToTravel == null || pathToTravel.Count == 0)
        {
            return;
        }
        
        Vector3 a, b, c = pathToTravel[0].Position;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * 0.5f;
            for (float t = 0f; t < 1f; t += 0.1f)
            {
                Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
            }

        }
        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (float t = 0f; t < 1f; t += 0.1f)
        {
            Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
        }
    }

    #endregion

    #region 储存与加载
    public void Save(BinaryWriter writer)
    {
        location.Coordinates.Save(writer);      // 储存坐标
        writer.Write(orientation);              // 储存旋转
    }

    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
    }
    #endregion
}

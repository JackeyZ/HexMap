using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：
/// 作用：
/// </summary>
public class NewMapMenu : MonoBehaviour
{
    public HexGrid hexGrid;

    public void Open()
    {
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }
    /// <summary>
    /// 创建小型地图
    /// </summary>
    public void CreateSmallMap()
    {
        CreateMap(20, 15);
    }

    /// <summary>
    /// 创建中型地图
    /// </summary>
    public void CreateMediumMap()
    {
        CreateMap(40, 30);
    }

    /// <summary>
    /// 创建大型地图
    /// </summary>
    public void CreateLargeMap()
    {
        CreateMap(80, 60);
    }

    /// <summary>
    /// 创建地图
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    void CreateMap(int x, int z)
    {
        hexGrid.CreateMap(x, z);
        HexMapCamera.ValidatePosition();    // 验证地图边界，矫正摄像机位置
        Close();
    }
}

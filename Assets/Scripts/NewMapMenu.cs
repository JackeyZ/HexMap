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

    public HexMapGenerator mapGenerator;

    private bool generateMaps = true;

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
        if (generateMaps)
        {
            mapGenerator.GenerateMap(x, z); // 生成随机地图
        }
        else
        {
            hexGrid.CreateMap(x, z);        // 生成默认地图
        }
        HexMapCamera.ValidatePosition();    // 验证地图边界，矫正摄像机位置
        Close();
    }
    
    /// <summary>
    /// 设置是否随机生成地图
    /// </summary>
    /// <param name="toggle"></param>
    public void ToggleMapGeneration(bool toggle)
    {
        generateMaps = toggle;
    }
}

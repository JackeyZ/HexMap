using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：地图数据纹理
/// 作用：用于管理包含单元格数据的纹理，让整个hex地图的数据保存在一个纹理里，以便于shader进行采样获取地图数据
/// </summary>
public class HexCellShaderData : MonoBehaviour
{
    Texture2D cellTexture;                                                                          // 储存地图格子数据的纹理
    Color32[] cellTextureData;                                                                      // 地图上每个格子的数据，下标是六边形格子索引
    bool needsVisibilityReset = false;                                                              // 当前帧是否需要重置可见性                                      

    List<HexCell> transitioningCells = new List<HexCell>();                                         // 平滑转换中的格子
    const float transitionSpeed = 255f;                                                             // 平滑过渡的速度


    /// <summary>
    /// 是否立即转换可见与不可见，true表示立即转换、false表示平滑过渡转换
    /// </summary>
    public bool ImmediateMode { get; set; }

    /// <summary>
    /// 六边形网格地图引用，在HexGrid.Awake中初始化
    /// </summary>
    public HexGrid Grid { get; set; }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="x">宽</param>
    /// <param name="z">高</param>
    public void Initialize(int x, int z)
    {
        if (cellTexture)
        {
            cellTexture.Resize(x, z);
        }
        else
        {
            cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true);
            cellTexture.filterMode = FilterMode.Point;
            cellTexture.wrapMode = TextureWrapMode.Clamp;
            Shader.SetGlobalTexture("_HexCellData", cellTexture);
        }
        Shader.SetGlobalVector("_HexCellData_TexelSize", new Vector4(1f / x, 1f / z, x, z));  // 命名规则“纹理名称” + “TextSize”，在SetGlobalTexture之后Unity会随后自动设置GlobalVector，但在这里自己立即设置，不再等unity设置


        if (cellTextureData == null || cellTextureData.Length != x * z)
        {
            cellTextureData = new Color32[x * z];
        }
        else {
            for (int i = 0; i < cellTextureData.Length; i++)
            {
                cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        enabled = true;
    }

    /// <summary>
    /// 刷新某个单元格的地形数据到纹理
    /// </summary>
    /// <param name="cell"></param>
    public void RefreshTerrain(HexCell cell)
    {
        cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
        enabled = true;
    }

    /// <summary>
    /// 视野高度改变了，需要重置地图六边形网格的可见性
    /// </summary>
    public void ViewElevationChanged()
    {
        needsVisibilityReset = true;
        enabled = true;
    }

    /// <summary>
    /// 刷新某个单元格的可见性数据到纹理
    /// </summary>
    /// <param name="cell"></param>
    public void RefreshVisibility(HexCell cell)
    {
        int index = cell.Index;
        if (ImmediateMode)
        {
            cellTextureData[index].r = cell.IsVisible ? (byte)255 : (byte)0;           // 储存是否被单位看见
            cellTextureData[index].g = cell.IsExplored ? (byte)255 : (byte)0;          // 储存是否被单位探索过
        }
        else if (cellTextureData[index].b != 255)
        {
            // 设置b值为255，表示正在过渡中（节省性能，避免每次Add都遍历列表查找是否重复）
            cellTextureData[index].b = 255; 
            transitioningCells.Add(cell);
        }
        enabled = true;
    }

    void LateUpdate()
    {
        if (needsVisibilityReset)
        {
            needsVisibilityReset = false;
            Grid.ResetVisibility();
        }

        int delta = (int)(Time.deltaTime * transitionSpeed);
        if (delta == 0)
        {
            delta = 1;
        }
        // 遍历所有转换中的单元格
        for (int i = 0; i < transitioningCells.Count; i++)
        {
            if (!UpdateCellData(transitioningCells[i], delta))
            {
                // 如果转换完成则移出列表
                // 先把列表最后一个格子赋值到当前转换完成的格子的位置, 然后删除最后一个格子以节省性能
                transitioningCells[i--] = transitioningCells[transitioningCells.Count - 1];
                transitioningCells.RemoveAt(transitioningCells.Count - 1);
            }
        }
        cellTexture.SetPixels32(cellTextureData);       // 放lateUpdate是为了先让单元格数据更新完再应用到纹理上，保证每一帧只调一次节省性能
        cellTexture.Apply();
        enabled = transitioningCells.Count > 0;
    }

    bool UpdateCellData(HexCell cell, int delta)
    {
        int index = cell.Index;
        Color32 data = cellTextureData[index];
        bool stillUpdating = false;

        // 探索状态可见性
        if (cell.IsExplored)
        {
            if(data.g < 255)    // 平滑变亮
            {
                stillUpdating = true;
                int t = data.g + delta;
                data.g = t >= 255 ? (byte)255 : (byte)t;
            }
        }
        else if (data.g > 0)    // 平滑变暗
        {
            stillUpdating = true;
            int t = data.g - delta;
            data.g = t < 0 ? (byte)0 : (byte)t;
        }

        // 视野可见性
        if (cell.IsVisible)
        {
            if(data.r < 255)    // 平滑变亮
            {
                stillUpdating = true;
                int t = data.r + delta;
                data.r = t >= 255 ? (byte)255 : (byte)t;
            }
        }
        else if (data.r > 0)    // 平滑变暗
        {
            stillUpdating = true;
            int t = data.r - delta;
            data.r = t < 0 ? (byte)0 : (byte)t;
        }

        if (!stillUpdating)
        {
            data.b = 0;         // b值恢复0，表示已经不在过渡中
        }
        cellTextureData[index] = data;
        return stillUpdating;
    }
}

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
    /// 刷新某个单元格的可见性数据到纹理
    /// </summary>
    /// <param name="cell"></param>
    public void RefreshVisibility(HexCell cell)
    {
        cellTextureData[cell.Index].r = cell.IsVisible ? (byte)255 : (byte)0;
        enabled = true;
    }

    void LateUpdate()
    {
        cellTexture.SetPixels32(cellTextureData);       // 放lateUpdate是为了先让单元格数据更新完再应用到纹理上，保证每一帧只调一次
        cellTexture.Apply();
        enabled = false;
    }
}

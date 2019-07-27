using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：特征物体管理器
/// 作用：
/// </summary>
public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] urbanCollections;                 // 建筑预制体（等级从高到低排列）
    public HexFeatureCollection[] farmCollections;                  // 农场预制体（等级从高到低排列）
    public HexFeatureCollection[] plantCollections;                 // 植物预制体（等级从高到低排列）

    private Transform container;                                // 特征物体父物体

    public void Clear() {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }
    public void Apply() { }

    /// <summary>
    /// 增加特征物体
    /// </summary>
    /// <param name="position">所需添加的坐标（未被微扰的坐标）</param>
    public void AddFeature(HexCell hexCell ,Vector3 position)
    {
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

}

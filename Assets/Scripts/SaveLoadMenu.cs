using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

/// <summary>
/// 名称：
/// 作用：
/// </summary>
public class SaveLoadMenu : MonoBehaviour
{
    public HexGrid hexGrid;
    private bool saveMode;

    public Text menuLabel, actionButtonLabel;

    public InputField nameInput;                                // 地图文件名

    public RectTransform listContent;

    public SaveLoadItem itemPrefab;                             // fileList子项预制体

    public void Open(bool saveMode)
    {
        FillList();                                 //加载地图列表子项
        this.saveMode = saveMode;
        if (saveMode)
        {
            menuLabel.text = "保存地图";
            actionButtonLabel.text = "保存";
        }
        else {
            menuLabel.text = "加载地图";
            actionButtonLabel.text = "加载";
        }
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    /// <summary>
    /// 加载地图列表子项
    /// </summary>
    void FillList()
    {
        // 删除之前的子项
        for (int i = 0; i < listContent.childCount; i++)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }

        string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");       // 查找所有.map文件
        Array.Sort(paths);                                                                  // 首字母排序
        for (int i = 0; i < paths.Length; i++)
        {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);                      // 根据路径获取文件名，赋值给列表子项
            item.transform.SetParent(listContent, false);
        }
    }

    /// <summary>
    /// 设置input地图名称
    /// </summary>
    /// <param name="name"></param>
    public void SelectItem(string name)
    {
        nameInput.text = name;
    }

    /// <summary>
    /// 根据输入的地图名称的到地图储存路径
    /// </summary>
    /// <returns>储存或加载路径</returns>
    string GetSelectedPath()
    {
        string mapName = nameInput.text;    // 地图文件名
        if (mapName.Length == 0)
        {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");  // 返回路径，Combine可根据不同平台连接两个路径
    }

    /// <summary>
    /// 删除地图文件
    /// </summary>
    public void Delete()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        File.Delete(path);
        nameInput.text = "";
        FillList();                         // 刷新地图列表
    }

    /// <summary>
    /// 保存或加载地图
    /// </summary>
    public void Action()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (saveMode)
        {
            Save(path);
        }
        else
        {
            Load(path);
        }
        Close();
    }

    /// <summary>
    /// 储存地图到文件
    /// </summary>
    public void Save(string path)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))    // 使用using可在作用域结束的时候自动调用 writer.Close();
        {                                                                                   // 因为writer类和文件流类都实现了IDisposable接口。这些对象具有Dispose方法，它在退出使用范围的时候会被隐式调用。
            writer.Write(1);        // 预留一个空的整形作为地图版本号
            hexGrid.Save(writer);
        }
    }

    /// <summary>
    /// 从文件加载地图
    /// </summary>
    public void Load(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("文件不存在： " + path);
            return;
        }
        Debug.Log("加载开始：" + path);
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))                 // File.OpenRead(path) 相当于 File.Open(path, FileMode.Open)
        {
            int header = reader.ReadInt32();
            if (header == 1)
            {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();    // 矫正摄像机位置
                hexGrid.RefreshChunks();
            }
            else
            {
                Debug.LogError("地图文件格式有误！无法加载！ " + header);
            }
        }
    }
}

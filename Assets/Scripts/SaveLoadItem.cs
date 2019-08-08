using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 名称：
/// 作用：
/// </summary>
public class SaveLoadItem : MonoBehaviour
{

    public SaveLoadMenu menu;

    public string MapName
    {
        get
        {
            return mapName;
        }
        set
        {
            mapName = value;
            transform.GetChild(0).GetComponent<Text>().text = value;
        }
    }

    string mapName;

    public void Select()
    {
        menu.SelectItem(mapName);
    }
}

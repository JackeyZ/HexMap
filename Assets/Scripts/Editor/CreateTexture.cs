using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 名称：
/// 作用：
/// </summary>
public class CreateTexture
{
    [MenuItem("Tools/CreatePerlinTexture")]
    public static void CreatePerlinTexture()
    {
         Texture2D result = new Texture2D(512, 512, TextureFormat.ARGB32, false);
 
         for (int i = 0; i < result.height; ++i)
         {
             for (int j = 0; j < result.width; ++j)
            {
                float perlinRandom = Mathf.PerlinNoise(i /25f, j / 25f);
                Color newColor = new Color(perlinRandom, perlinRandom, perlinRandom);
                result.SetPixel(j, i, newColor);
             }
         }
         result.Apply();
        string savePath = Application.streamingAssetsPath + "/temp.jpg";
        File.WriteAllBytes(savePath, result.EncodeToJPG());
        AssetDatabase.Refresh();
    }
}

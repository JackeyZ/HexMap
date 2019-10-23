using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：
/// 作用：
/// </summary>
public class AutoRotaX : MonoBehaviour
{
    public float speed = 1;

    void Update() {
        transform.Rotate(Vector3.right, speed);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：
/// 作用：
/// </summary>
public class PlayerController : MonoBehaviour
{
    public float speed = 0.3f;
    CharacterController _CC;

    void Awake()
    {
        _CC = GetComponent<CharacterController>();
    }
    void FixedUpdate()
    {

        _CC.Move(new Vector3(Input.GetAxis("Horizontal") * speed, 0 ,Input.GetAxis("Vertical") * speed));
    }
}

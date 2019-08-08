using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 名称：
/// 作用：
/// </summary>
public class HexMapCamera : MonoBehaviour
{
    static HexMapCamera instance;

    Transform swivel, stick;
    float zoom = 1f;                                // 变焦程度，0表示最远，1表示最近
    public float stickMinZoom, stickMaxZoom;        // 最小、最大焦距
    public float swivelMinZoom, swivelMaxZoom;      // 最小、最大旋转角度
    public float moveSpeedMinZoom, moveSpeedMaxZoom;// 移动最小、最大速度
    public float rotationSpeed;                     // 旋转速度
    float rotationAngle;                            // 用于记录旋转角度
    public HexGrid grid;

    public static bool Locked
    {
        set
        {
            instance.enabled = !value;
        }
    }

    void Awake()
    {
        instance = this;
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }
        
        // 相机旋转
        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta);
        }

        // 相机位移
        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
    }

    // 调整焦距
    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta); // 限制零到一    
        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    // 调整位移
    void AdjustPosition(float xDelta, float zDelta)
    {
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        float distance = Mathf.Lerp(moveSpeedMaxZoom, moveSpeedMinZoom, zoom) * damping * Time.deltaTime;
        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);
    }

    Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (grid.cellCountX - 0.5f) * (2f * HexMetrics.innerRadius);
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        return position;
    }

    void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if (rotationAngle < 0f)
        {
            rotationAngle += 360f;
        }
        else if (rotationAngle >= 360f)
        {
            rotationAngle -= 360f;
        }
        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
    }

    /// <summary>
    /// 验证地图边界
    /// </summary>
    public static void ValidatePosition()
    {
        instance.AdjustPosition(0f, 0f);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 摄像机状态：普通、收缩、舒展
/// </summary>
public enum CameraStatus
{   
    Normal,                 // 普通
    Shrink,                 // 收缩
    Stretch,                // 舒展
}

/// <summary>
/// 名称：跟随摄像机
/// 作用：用于跟随目标
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public GameObject followTarget;                 // 跟随目标
    public float baseDistance = 10;                 // 基础距离
    private float originalDistance = 10;            // 最初设置的距离
    private float distance = 10;                    // 当前距离
    public float baseMoveSpeed = 5;                 // 基础移动速度
    private float moveSpeed = 5;                    // 当前速度
    public float rotaSpeed = 5;                     // 旋转速度
    [Range(0, 90)]
    public float angle = 45;                        // 水平角度

    public float shakeOffsetX = 10;                 // 抖动幅度X
    public float shakeOffsetY = 10;                 // 抖动幅度Y

    [Range(0, 1)]
    public float shrinkDistanceRatio = 0.7f;        // 收缩距离比例
    private float shrinkDistance = 7;               // 收缩距离

    public float scrollWheelSpeed = 30f;            // 鼠标滚轮缩放
    public float angleChangeSpeed = 2f;             // 鼠标拖动，更改水平角度速度

    public CameraStatus cameraStatus = CameraStatus.Normal;         // 摄像机状态

    private Camera cameraComponent = null;
    private float lastMousePosY = 0;

    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        distance = baseDistance;
        moveSpeed = baseMoveSpeed;
        shrinkDistance = shrinkDistanceRatio * baseDistance;
        originalDistance = baseDistance;
    }
    
    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    lastMousePosY = Input.mousePosition.y;
        //}

        //if (Input.GetMouseButton(0))
        //{
        //    float distance = Input.mousePosition.y - lastMousePosY;
        //    angle -= distance * Time.deltaTime * angleChangeSpeed;
        //    if(angle < 0)
        //    {
        //        angle = 0;
        //    }
        //    else if(angle > 90)
        //    {
        //        angle = 90;
        //    }
        //    lastMousePosY = Input.mousePosition.y;
        //}

        if(Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            if (baseDistance > originalDistance * 0.5)
            {
                baseDistance -= Time.deltaTime * scrollWheelSpeed;
                shrinkDistance = shrinkDistanceRatio * baseDistance;
            }
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            if (baseDistance < originalDistance * 1.5)
            {
                baseDistance += Time.deltaTime * scrollWheelSpeed;
                shrinkDistance = shrinkDistanceRatio * baseDistance;
            }
        }
    }

     void FixedUpdate()
    {
        if(followTarget != null)
        {
            // 收缩
            if (cameraStatus == CameraStatus.Shrink)
            {
                if(distance > shrinkDistance)
                {
                    distance -= Time.deltaTime * (distance - shrinkDistance) * 2;
                    if(distance <= shrinkDistance)
                    {
                        distance = shrinkDistance;
                    }
                }
                moveSpeed = 100;
            }
            // 舒展
            else if(cameraStatus == CameraStatus.Stretch)
            {
                if (distance < baseDistance)
                {
                    distance += Time.deltaTime * (baseDistance - distance) * 2;
                    if (distance >= baseDistance * 0.95)
                    {
                        cameraStatus = CameraStatus.Normal;
                    }
                }
                moveSpeed = 100;
            }
            // 普通
            else
            {
                distance = baseDistance;
                moveSpeed = baseMoveSpeed;
            }

            // 计算移动目标
            Vector3 targetPos = followTarget.transform.position;
            float offsetY = distance * Mathf.Sin(angle * Mathf.PI / 180);
            float offsetZ = - distance * Mathf.Cos(angle * Mathf.PI / 180);
            Vector3 cameraTargetPos = new Vector3(targetPos.x, targetPos.y + offsetY, targetPos.z + offsetZ);

            // 移动
            Vector3 viewportPoint = cameraComponent.WorldToViewportPoint(targetPos);
            float viewportParam = Mathf.Abs(viewportPoint.x - 0.5f) + Mathf.Abs(viewportPoint.y - 0.5f) + Mathf.Abs(viewportPoint.z - distance);
            float distanceParam = (viewportParam + 1) * moveSpeed;
            Vector3 moveDic = cameraTargetPos - transform.position;
            transform.Translate(moveDic * Mathf.Min(Time.deltaTime * distanceParam, 1), Space.World);

            if(cameraStatus == CameraStatus.Stretch)
            {
                // 抖动
                transform.Translate(Time.deltaTime * transform.up * Random.Range(-shakeOffsetY, shakeOffsetY));
                transform.Translate(Time.deltaTime * transform.right * Random.Range(-shakeOffsetX, shakeOffsetX));
            }
            else
            {
                // 旋转
                Vector3 dir = targetPos - transform.position;
                if (dir != Vector3.zero)
                {
                    Quaternion target_rotation = Quaternion.LookRotation(dir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, Time.deltaTime * rotaSpeed);
                    transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, 0, 0);  // 不旋转y和z
                }
            }

            //transform.LookAt(targetPos);
        }
    }

    /// <summary>
    /// 转换成收缩状态
    /// </summary>
    public void Shrink()
    {
        cameraStatus = CameraStatus.Shrink; 
    }
    
    /// <summary>
    /// 转换成舒展状态
    /// </summary>
    public void Stretch()
    {
        cameraStatus = CameraStatus.Stretch;
    }
}

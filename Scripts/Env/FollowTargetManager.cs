using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using JetBrains.Annotations;
using static Config;
using static GameManage;

public class FollowTargetManager : MonoBehaviour
{
    
    private const float CameraWidth = 0.2f; // 画中画摄像头的宽度
    private const float CameraHeight = 0.2f; // 画中画摄像头的高度
    private List<Camera> pipCameras = new(); // 存储所有画中画摄像头的列表

    private int SUM_RED;
    private int SUM_BLUE;
  

    public void CamerInit(Config config, List<Individual> individualsBlue, List<Individual> individualsRed)
    {
    
        SUM_BLUE = config.TeamBlue.num;
        SUM_RED = config.TeamRed.num;

        // 为红色队伍设置摄像头
        for (int i = 0; i < SUM_RED; i++)
        {
            string cameraName = "Red" + (i + 1);
            Camera camera = GameObject.Find(cameraName).GetComponent<Camera>();
            pipCameras.Add(camera);
            SetCameraViewport(camera, i, 0); // 设置在左侧
            SetCameraTarget(camera, individualsRed[i].TankgameObject.transform);
            camera.targetDisplay = 0;
            camera.enabled = false; // 初始时禁用摄像头
        }

        // 为蓝色队伍设置摄像头
        for (int i = 0; i < SUM_BLUE; i++)
        {
            string cameraName = "Blue" + (i + 1);
            Camera camera = GameObject.Find(cameraName).GetComponent<Camera>();
            pipCameras.Add(camera);
            SetCameraViewport(camera, i, 0.8f); // 设置在右侧
            SetCameraTarget(camera, individualsBlue[i].TankgameObject.transform);
            camera.targetDisplay = 0;
            camera.enabled = false; // 初始时禁用摄像头
        }
    }


    private void Update()
    {
        // 检测按键C是否被按下
        if (Input.GetKeyDown(KeyCode.C))
        {
            TogglePiPCameras();
        }
    }

    private void TogglePiPCameras()
    {
        foreach (var cam in pipCameras)
        {
            cam.enabled = !cam.enabled; // 切换每个摄像头的激活状态
        }
    }

    private void SetCameraViewport(Camera camera, int index, float startX)
    {
        camera.rect = new Rect(startX, 1 - (index + 1) * CameraHeight, CameraWidth, CameraHeight);
    }

    private void SetCameraTarget(Camera camera, Transform target)
    {
        if (!camera.gameObject.TryGetComponent<FollowTarget>(out var followTarget))
        {
            followTarget = camera.gameObject.AddComponent<FollowTarget>();
        }

        followTarget.target = target;
        // 假设你的坦克对象有一个子对象是炮塔的Transform
        //followTarget.firePoint = target.Find("TankRenderers/TankFree_Tower");
        // 假设你的无人机对象有一个子对象是无人机主框架
        followTarget.firePoint = target.Find("TankRenderers/Quadcopter/Frame");
    }  
}

public class FollowTarget : MonoBehaviour
{
    public Transform target; // 目标个体
    public Transform firePoint; // 炮台的Transform
    public Vector3 offset = new(0f, 4f, -9f); // 与目标的相对位置

    private void Update()
    {
        if (target)
        {
            Vector3 adjustedOffset = offset;
            if (firePoint)
            {
                adjustedOffset = Quaternion.Euler(0, firePoint.rotation.eulerAngles.y, 0) * offset;
            }
            else
            {
                UnityEngine.Debug.LogWarning("No fire point found!");
            }
            transform.position = target.position + adjustedOffset;
            transform.LookAt(target.position);
        }
    }
}

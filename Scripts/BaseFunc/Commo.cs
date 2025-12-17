using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.MLAgents;
using UnityEngine;
using static Commo;

/// <summary>
/// 改脚本记录一些公共的方法，比如移动，旋转，获得伤害代码等。
/// </summary>
public class Commo : MonoBehaviour
{
    public ShellControl shell;
    public struct TankAttributes
    {
        //tank个体信息
        public float PH;
        public float PHFULL;
        

        //抑制tank连发
        public int cooldowntime;
        public int firetime;
        public float MaxSpeed;
        public float hMaxSpeed;
        public float rotateSpeed;

        //定义每一个个体编号,用于个体识别
        public int TankNum;
        public int EnemyNum;
    }

    /// <summary>
    /// 定义敌人信息结构体，个体与敌方的距离、敌方个体的信息
    /// </summary>
    public struct EnemyInfo
    {
        public float Distance;
        public Individual Enemy;

        public EnemyInfo(float distance, Individual enemy)
        {
            Distance = distance;
            Enemy = enemy;
        }
    }
    
    /// <summary>
    /// NR存储对手结构体
    /// </summary>
    public struct OpponentInfo
    {       
        public float Distance;
        public Vector3 Position;

        public OpponentInfo(float distance, Vector3 position)
        {          
            Distance = distance;
            Position = position;
        }
    }


    /// <summary>
    /// 定义移动代码：
    /// gameobject:移动的物体 v_value:前进后退速度 r_value:旋转速度 h_value:Y轴方向速度
    /// </summary>
    public void Move(GameObject gameobject, Rigidbody _rigidbody, float v_value, float r_value, float maxSpeed, Vector3 targetPos)
    {


        Vector3 forceDirection = gameobject.transform.forward * v_value;

        _rigidbody.AddForce(forceDirection, ForceMode.VelocityChange);

        float dis = Vector3.Distance(gameobject.transform.position, targetPos);
        float dis_y = gameobject.transform.position.y - targetPos.y;
        float ratio = Mathf.Abs(dis_y) / Mathf.Sqrt(Mathf.Pow(dis, 2) - Mathf.Pow(dis_y, 2));
        if (Mathf.Abs(dis_y) >= 30)
        {
            if (dis_y > 0)
            {
                _rigidbody.velocity += ratio * 6f * Vector3.down;
                //Debug.Log(agentrb.velocity.y);
            }
            else
                _rigidbody.velocity += ratio * 6f * Vector3.up;
        }
        else if(gameobject.transform.position.y > 30)
        {
            _rigidbody.velocity += UnityEngine.Random.Range(-20, 20) * Vector3.up;
        }
        else
            _rigidbody.velocity += 4 * Vector3.up;

        if (Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up).magnitude > maxSpeed)
        {
            float h_speed = _rigidbody.velocity.y;
            _rigidbody.velocity = Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up);
            _rigidbody.velocity = _rigidbody.velocity.normalized * maxSpeed + new Vector3(0f, h_speed, 0f);
        }

        if (_rigidbody.transform.position.y >= 250)
        {
            _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, -2, _rigidbody.velocity.z);
            _rigidbody.AddForce(new Vector3(0, -20, 0), ForceMode.VelocityChange);
        }


        float movementDirection = Vector3.Dot(Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up).normalized, gameobject.transform.forward);
        if (movementDirection < 0) // Moving backward
        {
            r_value = -r_value;
        }

        // Calculate rotation amount
        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, r_value, 0) * Time.deltaTime);

        // Apply rotation to the Rigidbody
        _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
    }


    /// <summary>
    /// 定义发射子弹代码，无需过多关注
    /// ShellPos:子弹发射位置 flags:是否发射子弹 FireSpeed:发射子弹的速度
    /// 发射成功返回true，否则返回false
    /// </summary>
    public bool Openfire(GameObject fireObject, Transform ShellPos, int flags, float firespeed)
    {
        //Step1:获取子弹的位置
        if (shell != null && flags == 1)
        {
            //Step2:实例化子弹
            //Step3:给子弹添加力
            GameObject shellObj = Instantiate(shell.gameObject, ShellPos.position, ShellPos.transform.rotation);
            if (shellObj.TryGetComponent<Rigidbody>(out var shellRididbody))
            {
                shellRididbody.velocity = ShellPos.forward * firespeed;
                //  shellObj.GetComponent<ShellControl>().Set_father(gameObject);
                if (shellObj.TryGetComponent<ShellControl>(out var shellControl))
                {
                    shellControl.Set_father(fireObject);
                }
                else
                {
                    Debug.LogError("shellControl is null");
                }
                //设置坦克发射
                // Debug.Log("发射子弹");
                return true;
             
            }            
        }
        return false;
    }

    /// <summary>
    /// 定义炮弹伤害代码
    /// RLTank:受到伤害的坦克 damage:伤害值
    public void GetDamage(Rl RLTank, int damage)
    {
        if (RLTank.tankAttributes.PH > 0)
        {
            RLTank.tankAttributes.PH -= damage;
            if (RLTank.tankAttributes.PH <= 0)
            {
                //Step4:死亡则false坦克
                //RLTank.tankExplosion.transform.parent = null;
                RLTank.GetComponent<Rl>().AddReward(-5f);
                RLTank.tankExplosion.Play();
                RLTank.gameManage.TankDamage(RLTank.gameObject);

                return;
            }
            RLTank.phSlider.value = RLTank.tankAttributes.PH;
        }

    }
    public void GetDamage(NR NRTank, int damage)
    {       
       
        if (NRTank.tankAttributes.PH > 0)
        {
            NRTank.tankAttributes.PH -= damage;
            if (NRTank.tankAttributes.PH <= 0)
            {
                //Step4:死亡则false坦克
                //NRTank.tankExplosion.transform.parent = null;
                NRTank.tankExplosion.Play();
                NRTank.gameManage.TankDamage(NRTank.gameObject);
                 
                return;
            }
            NRTank.phSlider.value = NRTank.tankAttributes.PH;
        }

    }

    /// <summary>
    /// 修改物体材质
    /// parentTransform:父物体 material:材质
    public void ChangeMaterialsRecursively(Transform parentTransform, Material material)
    {
        foreach (Transform child in parentTransform)
        {
            // 获取子物体的Renderer组件（假设材质在Renderer组件中）
            Renderer childRenderer = child.GetComponent<Renderer>();

            // 如果子物体有Renderer组件，则更改其材质
            if (childRenderer != null)
            {
                childRenderer.material = material;
            }

            // 递归调用以处理子物体的子物体
            ChangeMaterialsRecursively(child, material);
        }
    }

   
}

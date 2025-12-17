using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Commo;
using UnityEngine.AI;
using Unity.MLAgents.Sensors;
using System.Linq;
using Unity.MLAgents;
using System;
using Google.Protobuf.WellKnownTypes;


//从RL中引用TankAttributes


public class NR : MonoBehaviour
{
    //初始化参数
    public TeamType tankTeam;
    public Commo BaseFunc;
    public GameManage gameManage;

    public Slider phSlider;
    public ParticleSystem tankExplosion;

    public TankAttributes tankAttributes;
    public Individual CurrentTarget;
    public List<EnemyInfo> Enemies { get; set; } = new();

    //Fire
    public Transform ShellPos;
    public bool Istest = false;
    private TextMeshPro NUM_text;

    //NVmesh
    //private NavMeshAgent agent;
    public Rigidbody agentrb;
    public float UpdateFrequency;

    //public bool isDead;


    private void FixedUpdate()
    {
        tankAttributes.firetime++;

 
        UpdateTargetPosition(CurrentTarget);
        if (Vector3.Distance(transform.position, CurrentTarget.transform.position) < 260)
        {

            ShellPos.transform.forward = (CurrentTarget.transform.position - ShellPos.transform.position).normalized;
            if (tankAttributes.firetime > tankAttributes.cooldowntime && BaseFunc.Openfire(gameObject, ShellPos, 1, 1200))
            {
                //Debug.Log("NR Fire,:" + tankAttributes.firetime);
                tankAttributes.firetime = 0;
                //Debug.Log(this.gameObject.name + " shooting --" + CurrentTarget.gameObject.name +
                //" position --" + Vector3.Distance(transform.position, CurrentTarget.transform.position));
            }
        }


    }

    public void TankInitialize()
    {
        UpdateFrequency = 1.5f * Time.deltaTime;
        BaseFunc = GetComponent<Commo>();
        //agent = GetComponent<NavMeshAgent>();
        agentrb = GetComponent<Rigidbody>();

        //初始化属性
        tankAttributes.PH = tankAttributes.PHFULL;
        tankAttributes.firetime = 0;

        //初始化血条UI
        phSlider.maxValue = tankAttributes.PHFULL;
        phSlider.value = tankAttributes.PHFULL;

        //初始化字体
        NUM_text = transform.Find("Text").GetComponent<TextMeshPro>();
        NUM_text.text = tankAttributes.TankNum.ToString();

        //初始化Agent信息
        //agent.speed = 18f; // Adjust the speed
        //agent.acceleration = 50f; // Adjust the acceleration
        //agent.stoppingDistance = 30f; // Distance at which the agent will stop from the target
        //agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        //agent.avoidancePriority = 50 - 2*tankAttributes.TankNum; // Set between 0 to 99 (lower means higher priority)
        //agent.enabled = true;
    }



    /// <summary>
    /// 测试环境函数
    /// </summary>  
    private void TestAgentBehavior()
    {
        // Generate a random number between -1 and 1 for the movement input
        float randomMoveInput = UnityEngine.Random.Range(-20f, 80f);
        float hrandomMoveInput = UnityEngine.Random.Range(0f, 50f);
        // Set rotation input to -1 for testing
        float testRotateInput = UnityEngine.Random.Range(-100f, 100f);

        // Set discrete action for firing (0 for no fire, 1 for fire)
        int testFireAction = UnityEngine.Random.Range(0, 2); // Randomly choose between not firing and firing

        // Apply the test inputs to the movement and firing functions
        BaseFunc.Move(gameObject, agentrb, randomMoveInput * 20, testRotateInput * tankAttributes.rotateSpeed, tankAttributes.MaxSpeed, CurrentTarget.transform.position);
        if (BaseFunc.Openfire(gameObject, ShellPos, 1, 1200))
        {
            tankAttributes.firetime = 0;
        }
    }



    /// <summary>
    /// 控制规则方移动的代码，不需要过多关注
    /// </summary>  
    private void UpdateTargetPosition(Individual targetAgent)
    {
        //if (!agent.isActiveAndEnabled)
        if (!this.isActiveAndEnabled)
        {
            Debug.Log("Agent is not active and enabled.");
            //agent.enable = true;
            this.enabled = true;
            return;
        }
        if (targetAgent == null)
        {
            Debug.LogError("Target agent is null.");
            return;
        }

        //用速度控制个体朝向target运动
        Vector3 targetPosition = targetAgent.transform.position;
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        float dis = Vector3.Distance(this.transform.position, targetPosition);
        if (!(dis <= 80 && IsFireingTarget(transform, targetAgent.transform.position)))
        {
            agentrb.transform.forward = Vector3.ProjectOnPlane(directionToTarget, Vector3.up).normalized;
            agentrb.velocity = agentrb.transform.forward * 100f;
            //Debug.Log(dis);


            float dis_y = this.transform.position.y - targetPosition.y;
            float ratio = Mathf.Abs(dis_y) / Mathf.Sqrt(Mathf.Pow(dis, 2) - Mathf.Pow(dis_y, 2));
            if (dis_y > 0)
            {
                agentrb.velocity += ratio * 150f * Vector3.down;
            }
            else
                agentrb.velocity += ratio * 150f * Vector3.up;

            //朝向变化
            int r_value = 20;
            float movementDirection = Vector3.Dot(Vector3.ProjectOnPlane(agentrb.velocity, Vector3.up).normalized, this.transform.forward);
            if (movementDirection < 0) // Moving backward
            {
                r_value = -r_value;
            }

            // Calculate rotation amount
            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, r_value, 0) * Time.deltaTime);

            // Apply rotation to the Rigidbody
            agentrb.MoveRotation(agentrb.rotation * deltaRotation);
        }
        else
        {
            agentrb.velocity = new Vector3(0, 0, 0);
        }
        
    }

    /// <summary>
    /// 控制规则方移动判定的代码，不需要过多关注
    /// </summary>
    private bool IsFireingTarget(Transform agentTransform, Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - agentTransform.position).normalized;
        Vector3 forward = agentTransform.forward.normalized;

        float angle = Vector3.Angle(forward, directionToTarget);

        // Determine if the angle is to the left or right
        Vector3 cross = Vector3.Cross(forward, directionToTarget);
        if (cross.y < 0) angle = -angle; // Reverse angle direction if target is to the left

        return Mathf.Abs(angle) <= 30;//加强判定，3D中30°内为判定标准
    }


}

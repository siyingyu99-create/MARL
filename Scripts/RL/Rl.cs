using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
//using UnityEngine.AI;

using UnityEngine.UI;
using static Commo;

using Debug = UnityEngine.Debug;


//TankRed 编号为0 
//对于tank进行编号


public class Rl : Agent
{
    //初始化参数

    public TeamType tankTeam;
    public Commo BaseFunc;
    public GameManage gameManage;

    //统计奖励信息
    public float reward = 0;
    public float punish = 0;

    private BehaviorParameters behaviorParameters;
    public Rigidbody _rigidbody;
    public Slider phSlider;
    public ParticleSystem tankExplosion;


    public TankAttributes tankAttributes;

    public List<EnemyInfo> Enemies { get; set; } = new();
    //Fire
    public Transform ShellPos;
    public bool Istest = false;
    private TextMeshPro NUM_text;
    private int dicisionEnemy;
    //public bool isDead;
    public int lastDisicion;
    public LayerMask obstacleMask;

    private bool _teamLogged = false;
    //private bool _printedSpec = false;

    public override void OnEpisodeBegin()
    {
        //if (!_printedSpec)
        //{
        //    _printedSpec = true;

        //    var bp = GetComponent<BehaviorParameters>();
        //    var brain = bp != null ? bp.BrainParameters : null;

        //    int vecSize = brain != null ? brain.VectorObservationSize : -1;
        //    int vecStack = brain != null ? brain.NumStackedVectorObservations : -1;

        //    // ✅ Release18 兼容：从 BrainParameters 拿 ActionSpec
        //    ActionSpec actionSpec = brain != null ? brain.ActionSpec : ActionSpec.MakeContinuous(0);

        //    int continuous = actionSpec.NumContinuousActions;
        //    var discrete = actionSpec.BranchSizes;

        //    var sb = new StringBuilder();
        //    sb.Append("[POLICY SPEC] ");
        //    sb.Append($"obj={gameObject.name} ");
        //    sb.Append($"behavior={bp?.BehaviorName} team={bp?.TeamId} ");
        //    sb.Append($"vecSize={vecSize} vecStack={vecStack} ");
        //    sb.Append($"cont={continuous} discBranches={discrete.Length} disc=[");
        //    for (int i = 0; i < discrete.Length; i++)
        //    {
        //        sb.Append(discrete[i]);
        //        if (i < discrete.Length - 1) sb.Append(",");
        //    }
        //    sb.Append("]");

        //    Debug.Log(sb.ToString());
        //}

        if (!_teamLogged)
        {
            var bp = GetComponent<BehaviorParameters>();
            Debug.Log($"[TEAM CHECK] name={bp.BehaviorName}, team={bp.TeamId}, obj={gameObject.name}");
            _teamLogged = true;
        }

        base.OnEpisodeBegin();
    }

    /// <summary>
    /// 每帧移动，当敌方个体进入发射范围会开火：BaseFunc.Openfire(gameObject, ShellPos, 1, 1200)
    /// </summary>
    private void FixedUpdate()
    {
        tankAttributes.firetime++;

        // 安全保护：Enemies 可能在极少数时序下尚未填充
        if (Enemies == null || Enemies.Count == 0) return;

        // 追击可见目标（原逻辑）
        if (dicisionEnemy >= 0 && dicisionEnemy < Enemies.Count && !Enemies[dicisionEnemy].Enemy.IsUnityNull())
        {
            UpdateTargetPosition(Enemies[dicisionEnemy].Enemy);

            if (Enemies[dicisionEnemy].Distance < 260)
            {
                ShellPos.transform.forward = (Enemies[dicisionEnemy].Enemy.transform.position - ShellPos.transform.position).normalized;
                if (tankAttributes.firetime > tankAttributes.cooldowntime && BaseFunc.Openfire(gameObject, ShellPos, 1, 1200))
                {
                    tankAttributes.firetime = 0;
                }
            }
        }
        else
        {
            // 关键补丁：看不到敌人时不要原地罚站，做一个“温和的搜索巡航”
            // 仍然复用你原来的 Move()，不引入新控制维度
            Vector3 searchTarget = transform.position + transform.forward * 300f + Vector3.up * 20f;
            BaseFunc.Move(gameObject, _rigidbody, 20f, 20f, tankAttributes.MaxSpeed, searchTarget);
        }
    }

    public void TankInitialize()
    {
        //初始化参数
        behaviorParameters = gameObject.GetComponent<BehaviorParameters>();


        _rigidbody = GetComponent<Rigidbody>();
        Debug.Log("999" + _rigidbody.position);
        BaseFunc = GetComponent<Commo>();
        behaviorParameters.TeamId = (int)tankTeam;

        //初始化属性
        tankAttributes.PH = tankAttributes.PHFULL;
        tankAttributes.firetime = 0;

        //初始化血条UI
        phSlider.maxValue = tankAttributes.PHFULL;
        phSlider.value = tankAttributes.PHFULL;

        //初始化字体
        NUM_text = transform.Find("Text").GetComponent<TextMeshPro>();
        NUM_text.text = tankAttributes.TankNum.ToString();

        //初始化对手
        Enemies = new List<EnemyInfo>(gameManage.NumRed);

    }

    /// <summary>
    /// ！！神经网络控制转化为具体的控制，修改奖励函数，重点关注（以下是一个参考模板）
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (Enemies == null || Enemies.Count == 0) return;

        var discreteActions = actionBuffers.DiscreteActions;
        dicisionEnemy = Mathf.Clamp(discreteActions[0], 0, Enemies.Count - 1);

        // 轻微时间惩罚：鼓励更快结束战斗
        AddReward(-0.001f);

        // 统计可见敌人数量 & 最近敌人距离
        int visibleCount = 0;
        float minDis = float.MaxValue;
        for (int i = 0; i < Enemies.Count; i++)
        {
            if (!Enemies[i].Enemy.IsUnityNull())
            {
                visibleCount++;
                if (Enemies[i].Distance < minDis) minDis = Enemies[i].Distance;
            }
        }

        // 如果当前完全不可见：给一点点惩罚（配合 FixedUpdate 的搜索移动）
        if (visibleCount == 0)
        {
            AddReward(-0.02f);
            lastDisicion = dicisionEnemy;
            return;
        }

        // 选到不可见/已死目标：惩罚（比你原来的 -3 更温和，避免早期梯度太炸）
        if (Enemies[dicisionEnemy].Enemy.IsUnityNull())
        {
            AddReward(-0.5f);
            lastDisicion = dicisionEnemy;
            return;
        }

        // ===== 选到可见目标：开始 shaping =====
        float dis = Mathf.Max(1f, Enemies[dicisionEnemy].Distance);

        // 1) 越近越好（连续 shaping）
        AddReward(0.15f * (400f / dis));

        // 2) 选“最近的那批”目标再加一点（减少瞎选远目标）
        if (dis <= minDis * 1.10f) AddReward(0.2f);

        // 3) 别频繁换目标（轻度黏性）
        if (dicisionEnemy == lastDisicion) AddReward(0.05f);
        else AddReward(-0.02f);

        // 4) 轻度“分摊火力”：同一目标被太多人盯会扣一点
        //    仍然不改观测空间，只用当前场上队友 lastDisicion 做个近似
        int teamSize = 0;
        int sameTarget = 0;
        if (gameManage != null && gameManage.activeBlues != null)
        {
            foreach (var ind in gameManage.activeBlues)
            {
                if (ind != null && ind.Type == IndividualType.RL)
                {
                    teamSize++;
                    var rl = ind.TankgameObject.GetComponent<Rl>();
                    if (rl != null && rl.lastDisicion == dicisionEnemy) sameTarget++;
                }
            }
        }

        int ideal = Mathf.Max(1, Mathf.CeilToInt(teamSize / (float)visibleCount));
        if (sameTarget > ideal) AddReward(-0.03f * (sameTarget - ideal));
        else AddReward(0.01f);

        lastDisicion = dicisionEnemy;
    }

    /// <summary>
    /// ！！收集智能体的观察信息,这里添加自定义的观测信息，重点关注（以下是一个参考模板）
    /// </summary>  
    public override void CollectObservations(VectorSensor sensor)
    {

        for (int i = 0; i < Enemies.Count; i++)
        {
            //print(Enemies.Count);
            //获取敌方信息
            if (!Enemies[i].Enemy.IsUnityNull())
                sensor.AddObservation(GetOtherAgentData(Enemies[i].Enemy, Enemies[i]));
            else
                sensor.AddObservation(new float[] { i / 5.0f, 0, 0, 0, 0});
            
        }
        
    }

    /// <summary>
    /// 手动操作函数
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {

    }

    /// <summary>
    /// 获取队友的属性信息(可选)
    /// </summary>
    /// <param name="Tank"></param>
    /// <param name="Dis"></param>
    /// <returns></returns>
    private float[] GetFriendAgentData(Individual friend)
    {
        var otherAgentdata = new float[4];
        var relativePosition = friend.transform.position - this.gameObject.transform.position;
        if (!friend.TankgameObject.activeSelf)
        {
            otherAgentdata[0] = 0.01f;
            otherAgentdata[1] = 0.01f;
            otherAgentdata[2] = 0.01f;
            otherAgentdata[3] = 0.02f;
        }
        else
        {
            otherAgentdata[0] = relativePosition.x / 1800;
            otherAgentdata[1] = relativePosition.z / 1800;
            otherAgentdata[2] = relativePosition.y / 1800;
            otherAgentdata[3] = friend.TankgameObject.GetComponent<Rl>().dicisionEnemy / 5f;
        }

        return otherAgentdata;
    }

    /// <summary>
    /// 获取敌方智能体的属性信息
    /// </summary>
    /// <param name="Tank"></param>
    /// <param name="Dis"></param>
    /// <returns></returns>
    private float[] GetOtherAgentData(Individual Tank, EnemyInfo enemyinfo)
    {
        var otherAgentdata = new float[5];
        var relativePosition = Tank.transform.position - this.gameObject.transform.position;

        //根据distance判断敌方坦克:存活但不可见/存活且可见，此处可以改进
        otherAgentdata[0] = (Tank.TankgameObject.GetComponent<NR>().tankAttributes.TankNum - 1) / 5f;
        otherAgentdata[1] = enemyinfo.Distance / 1800;
        otherAgentdata[2] = relativePosition.x / 1800;
        otherAgentdata[3] = relativePosition.z / 1800;
        otherAgentdata[4] = relativePosition.y / 1800;

        return otherAgentdata;
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

        // Apply the test inputs to the movement and firing functions
        BaseFunc.Move(gameObject, _rigidbody, randomMoveInput * 20, testRotateInput * tankAttributes.rotateSpeed, tankAttributes.MaxSpeed, Enemies[0].Enemy.transform.position);
        if (tankAttributes.firetime > tankAttributes.cooldowntime && BaseFunc.Openfire(gameObject, ShellPos, 1, 1200))
        {
            tankAttributes.firetime = 0;
        }
    }


    /// <summary>
    /// 控制智能体移动判定的代码，不需要过多关注
    /// </summary>
    private bool IsFireingTargetAngle(Transform agentTransform, Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - agentTransform.position).normalized;
        Vector3 forward = agentTransform.forward.normalized;

        float angle = Vector3.Angle(forward, directionToTarget);

        // Determine if the angle is to the left or right
        Vector3 cross = Vector3.Cross(forward, directionToTarget);
        if (cross.y < 0) angle = -angle; // Reverse angle direction if target is to the left
        return Mathf.Abs(angle) <= 30;//加强判定，3D中30°内为判定标准
    }

    /// <summary>
    /// 控制智能体移动的代码，不需要过多关注
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
        if (!(dis <= 80 && IsFireingTargetAngle(transform, targetAgent.transform.position)))
        {
            _rigidbody.transform.forward = Vector3.ProjectOnPlane(directionToTarget, Vector3.up).normalized;
            _rigidbody.velocity = _rigidbody.transform.forward * 100f;
            //Debug.Log(dis);


            float dis_y = this.transform.position.y - targetPosition.y;
            float ratio = Mathf.Abs(dis_y) / Mathf.Sqrt(Mathf.Pow(dis, 2) - Mathf.Pow(dis_y, 2));
            if (dis_y > 0)
            {
                _rigidbody.velocity += ratio * 150f * Vector3.down;
                //Debug.Log(agentrb.velocity.y);
            }
            else
                _rigidbody.velocity += ratio * 150f * Vector3.up;

            //朝向变化
            int r_value = 20;
            float movementDirection = Vector3.Dot(Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up).normalized, this.transform.forward);
            if (movementDirection < 0) // Moving backward
            {
                r_value = -r_value;
            }

            // Calculate rotation amount
            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, r_value, 0) * Time.deltaTime);

            // Apply rotation to the Rigidbody
            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
        }
        else
        {
            _rigidbody.velocity = new Vector3(0, 0, 0);
        }

    }
}

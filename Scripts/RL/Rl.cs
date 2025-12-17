//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using TMPro;
//using Unity.IO.LowLevel.Unsafe;
//using Unity.MLAgents;
//using Unity.MLAgents.Actuators;
//using Unity.MLAgents.Policies;
//using Unity.MLAgents.Sensors;
//using Unity.VisualScripting;
//using UnityEngine;
//using Debug = UnityEngine.Debug;
////using UnityEngine.AI;

//using UnityEngine.UI;
//using static Commo;


////TankRed 编号为0 
////对于tank进行编号


//public class Rl : Agent
//{
//    //初始化参数

//    public TeamType tankTeam;
//    public Commo BaseFunc;
//    public GameManage gameManage;

//    //统计奖励信息
//    public float reward = 0;
//    public float punish = 0;

//    private BehaviorParameters behaviorParameters;
//    public Rigidbody _rigidbody;
//    public Slider phSlider;
//    public ParticleSystem tankExplosion;


//    public TankAttributes tankAttributes;

//    public List<EnemyInfo> Enemies { get; set; } = new();
//    //Fire
//    public Transform ShellPos;
//    public bool Istest = false;
//    private TextMeshPro NUM_text;
//    private int dicisionEnemy;
//    //public bool isDead;
//    public int lastDisicion;
//    public LayerMask obstacleMask;

//    // ====== Reward 超参数（可以后期调）======
//    [Header("Reward Shaping")]
//    public float stepPenalty = -0.001f;          // 每步时间惩罚
//    public float invalidTargetPenalty = -0.2f;   // 选到无效目标（死亡/遮挡）
//    public float keepTargetBonus = 0.02f;        // 连续选择同一目标
//    public float switchTargetPenalty = -0.02f;   // 切换目标惩罚
//    public float nearTargetBonus = 0.05f;        // 越近越奖励
//    public float lowHpTargetBonus = 0.05f;       // 选残血目标奖励
//    public float losBonus = 0.03f;               // 有视线（无遮挡）奖励

//    private bool HasLineOfSight(Transform enemyTf)
//    {
//        if (enemyTf == null) return false;

//        Vector3 origin = ShellPos != null ? ShellPos.position : transform.position + Vector3.up * 1.0f;
//        Vector3 target = enemyTf.position + Vector3.up * 1.0f;
//        Vector3 dir = (target - origin);
//        float dist = dir.magnitude;
//        dir /= Mathf.Max(dist, 1e-6f);

//        // obstacleMask 里只放“障碍物”层
//        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstacleMask))
//        {
//            // 被障碍物打到 => 无视线
//            return false;
//        }
//        return true;
//    }

//    private float GetNormalizedHp(Individual enemy)
//    {
//        // 这里按你代码推断：红方用 NR，且有 tankAttributes.PH / PHFULL
//        var nr = enemy.TankgameObject.GetComponent<NR>();
//        if (nr == null) return 1f;
//        return Mathf.Clamp01(nr.tankAttributes.PH / Mathf.Max(1f, nr.tankAttributes.PHFULL));
//    }

//    /// <summary>
//    /// 每帧移动，当敌方个体进入发射范围会开火：BaseFunc.Openfire(gameObject, ShellPos, 1, 1200)
//    /// </summary>
//    private void FixedUpdate()
//    {
//        tankAttributes.firetime++;

//        if (!Enemies[dicisionEnemy].Enemy.IsUnityNull())
//        {
//            UpdateTargetPosition(Enemies[dicisionEnemy].Enemy);

//            if (Enemies[dicisionEnemy].Distance < 260)
//            {
//                ShellPos.transform.forward = (Enemies[dicisionEnemy].Enemy.transform.position - ShellPos.transform.position).normalized;
//                if (tankAttributes.firetime > tankAttributes.cooldowntime && BaseFunc.Openfire(gameObject, ShellPos, 1, 1200))
//                {
//                    tankAttributes.firetime = 0;
//                    //Debug.Log(this.gameObject.name + " shooting --" + CurrentTarget.gameObject.name +
//                    //" position --" + Vector3.Distance(transform.position, CurrentTarget.transform.position));
//                }
//            }
//        }
//    }

//    public void TankInitialize()
//    {
//        //初始化参数
//        behaviorParameters = gameObject.GetComponent<BehaviorParameters>();


//        _rigidbody = GetComponent<Rigidbody>();
//        Debug.Log("999" + _rigidbody.position);
//        BaseFunc = GetComponent<Commo>();
//        behaviorParameters.TeamId = (int)tankTeam;

//        //初始化属性
//        tankAttributes.PH = tankAttributes.PHFULL;
//        tankAttributes.firetime = 0;

//        //初始化血条UI
//        phSlider.maxValue = tankAttributes.PHFULL;
//        phSlider.value = tankAttributes.PHFULL;

//        //初始化字体
//        NUM_text = transform.Find("Text").GetComponent<TextMeshPro>();
//        NUM_text.text = tankAttributes.TankNum.ToString();

//        //初始化对手
//        Enemies = new List<EnemyInfo>(gameManage.NumRed);

//        Debug.Log($"{gameObject.name} tag={gameObject.tag} tankTeam={tankTeam} TeamId={(int)tankTeam}");

//        Debug.Log($"[{name}] tag={gameObject.tag} team={tankTeam} TeamId={behaviorParameters.TeamId} " +
//          $"Behavior={behaviorParameters.BehaviorName} Model={(behaviorParameters.Model ? behaviorParameters.Model.name : "NULL")} " +
//          $"BehaviorType={behaviorParameters.BehaviorType}");

//    }

//    /// <summary>
//    /// ！！神经网络控制转化为具体的控制，修改奖励函数，重点关注（以下是一个参考模板）
//    /// </summary>
//    public override void OnActionReceived(ActionBuffers actionBuffers)
//    {
//        //if (name == "Blue_RL_1") Debug.Log($"act0={actionBuffers.DiscreteActions[0]}");

//        AddReward(stepPenalty);

//        var discreteActions = actionBuffers.DiscreteActions;
//        dicisionEnemy = discreteActions[0];

//        // 保护：Enemies 为空时直接返回（避免越界）
//        if (Enemies == null || Enemies.Count == 0) return;
//        dicisionEnemy = Mathf.Clamp(dicisionEnemy, 0, Enemies.Count - 1);

//        // 如果敌人全部不可用（死亡/不可见/被你们的逻辑置空），这一步不给奖惩
//        bool allHideOrDead = true;
//        for (int i = 0; i < Enemies.Count; i++)
//        {
//            if (!Enemies[i].Enemy.IsUnityNull())
//            {
//                allHideOrDead = false;
//                break;
//            }
//        }
//        if (allHideOrDead) return;

//        // 目标无效：直接惩罚
//        if (Enemies[dicisionEnemy].Enemy.IsUnityNull())
//        {
//            AddReward(invalidTargetPenalty);
//            lastDisicion = dicisionEnemy;
//            return;
//        }

//        var target = Enemies[dicisionEnemy].Enemy;
//        float dis = Mathf.Max(Enemies[dicisionEnemy].Distance, 1f);

//        // 是否无遮挡（视线清晰）
//        bool los = HasLineOfSight(target.transform);
//        if (los) AddReward(losBonus);
//        else AddReward(invalidTargetPenalty * 0.5f); // 被挡也要轻惩罚，逼它别选这种

//        // 连续选择同一目标：鼓励“集火”
//        if (dicisionEnemy == lastDisicion) AddReward(keepTargetBonus);
//        else AddReward(switchTargetPenalty);

//        // 越近越好（0~1）：例如 dis=0~400 线性给奖励，超过400就基本不给
//        float nearScore = Mathf.Clamp01(1f - dis / 400f);
//        AddReward(nearTargetBonus * nearScore);

//        // 残血更值得打：hp 越低奖励越高
//        float hp01 = GetNormalizedHp(target);         // 1=满血，0=没血
//        float lowHpScore = 1f - hp01;                 // 越残越大
//        AddReward(lowHpTargetBonus * lowHpScore);

//        lastDisicion = dicisionEnemy;
//    }

//    /// <summary>
//    /// ！！收集智能体的观察信息,这里添加自定义的观测信息，重点关注（以下是一个参考模板）
//    /// </summary>  
//    public override void CollectObservations(VectorSensor sensor)
//    {

//        for (int i = 0; i < Enemies.Count; i++)
//        {
//            //print(Enemies.Count);
//            //获取敌方信息
//            if (!Enemies[i].Enemy.IsUnityNull())
//                sensor.AddObservation(GetOtherAgentData(Enemies[i].Enemy, Enemies[i]));
//            else
//                sensor.AddObservation(new float[] { i / 5.0f, 0, 0, 0, 0});

//        }

//    }

//    /// <summary>
//    /// 手动操作函数
//    /// </summary>
//    /// <param name="actionsOut"></param>
//    public override void Heuristic(in ActionBuffers actionsOut)
//    {

//    }

//    /// <summary>
//    /// 获取队友的属性信息(可选)
//    /// </summary>
//    /// <param name="Tank"></param>
//    /// <param name="Dis"></param>
//    /// <returns></returns>
//    private float[] GetFriendAgentData(Individual friend)
//    {
//        var otherAgentdata = new float[4];
//        var relativePosition = friend.transform.position - this.gameObject.transform.position;
//        if (!friend.TankgameObject.activeSelf)
//        {
//            otherAgentdata[0] = 0.01f;
//            otherAgentdata[1] = 0.01f;
//            otherAgentdata[2] = 0.01f;
//            otherAgentdata[3] = 0.02f;
//        }
//        else
//        {
//            otherAgentdata[0] = relativePosition.x / 1800;
//            otherAgentdata[1] = relativePosition.z / 1800;
//            otherAgentdata[2] = relativePosition.y / 1800;
//            otherAgentdata[3] = friend.TankgameObject.GetComponent<Rl>().dicisionEnemy / 5f;
//        }

//        return otherAgentdata;
//    }

//    /// <summary>
//    /// 获取敌方智能体的属性信息
//    /// </summary>
//    /// <param name="Tank"></param>
//    /// <param name="Dis"></param>
//    /// <returns></returns>
//    private float[] GetOtherAgentData(Individual Tank, EnemyInfo enemyinfo)
//    {
//        var otherAgentdata = new float[5];
//        var relativePosition = Tank.transform.position - this.gameObject.transform.position;

//        //根据distance判断敌方坦克:存活但不可见/存活且可见，此处可以改进
//        otherAgentdata[0] = (Tank.TankgameObject.GetComponent<NR>().tankAttributes.TankNum - 1) / 5f;
//        otherAgentdata[1] = enemyinfo.Distance / 1800;
//        otherAgentdata[2] = relativePosition.x / 1800;
//        otherAgentdata[3] = relativePosition.z / 1800;
//        otherAgentdata[4] = relativePosition.y / 1800;

//        return otherAgentdata;
//    }

//    /// <summary>
//    /// 测试环境函数
//    /// </summary>  
//    private void TestAgentBehavior()
//    {
//        // Generate a random number between -1 and 1 for the movement input
//        float randomMoveInput = UnityEngine.Random.Range(-20f, 80f);
//        float hrandomMoveInput = UnityEngine.Random.Range(0f, 50f);
//        // Set rotation input to -1 for testing
//        float testRotateInput = UnityEngine.Random.Range(-100f, 100f);

//        // Apply the test inputs to the movement and firing functions
//        BaseFunc.Move(gameObject, _rigidbody, randomMoveInput * 20, testRotateInput * tankAttributes.rotateSpeed, tankAttributes.MaxSpeed, Enemies[0].Enemy.transform.position);
//        if (tankAttributes.firetime > tankAttributes.cooldowntime && BaseFunc.Openfire(gameObject, ShellPos, 1, 1200))
//        {
//            tankAttributes.firetime = 0;
//        }
//    }


//    /// <summary>
//    /// 控制智能体移动判定的代码，不需要过多关注
//    /// </summary>
//    private bool IsFireingTargetAngle(Transform agentTransform, Vector3 targetPosition)
//    {
//        Vector3 directionToTarget = (targetPosition - agentTransform.position).normalized;
//        Vector3 forward = agentTransform.forward.normalized;

//        float angle = Vector3.Angle(forward, directionToTarget);

//        // Determine if the angle is to the left or right
//        Vector3 cross = Vector3.Cross(forward, directionToTarget);
//        if (cross.y < 0) angle = -angle; // Reverse angle direction if target is to the left
//        return Mathf.Abs(angle) <= 30;//加强判定，3D中30°内为判定标准
//    }

//    /// <summary>
//    /// 控制智能体移动的代码，不需要过多关注
//    /// </summary>
//    private void UpdateTargetPosition(Individual targetAgent)
//    {
//        //if (!agent.isActiveAndEnabled)
//        if (!this.isActiveAndEnabled)
//        {
//            Debug.Log("Agent is not active and enabled.");
//            //agent.enable = true;
//            this.enabled = true;
//            return;
//        }
//        if (targetAgent == null)
//        {
//            Debug.LogError("Target agent is null.");
//            return;
//        }

//        //用速度控制个体朝向target运动
//        Vector3 targetPosition = targetAgent.transform.position;
//        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
//        float dis = Vector3.Distance(this.transform.position, targetPosition);
//        if (!(dis <= 80 && IsFireingTargetAngle(transform, targetAgent.transform.position)))
//        {
//            _rigidbody.transform.forward = Vector3.ProjectOnPlane(directionToTarget, Vector3.up).normalized;
//            _rigidbody.velocity = _rigidbody.transform.forward * 100f;
//            //Debug.Log(dis);


//            float dis_y = this.transform.position.y - targetPosition.y;
//            float ratio = Mathf.Abs(dis_y) / Mathf.Sqrt(Mathf.Pow(dis, 2) - Mathf.Pow(dis_y, 2));
//            if (dis_y > 0)
//            {
//                _rigidbody.velocity += ratio * 150f * Vector3.down;
//                //Debug.Log(agentrb.velocity.y);
//            }
//            else
//                _rigidbody.velocity += ratio * 150f * Vector3.up;

//            //朝向变化
//            int r_value = 20;
//            float movementDirection = Vector3.Dot(Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up).normalized, this.transform.forward);
//            if (movementDirection < 0) // Moving backward
//            {
//                r_value = -r_value;
//            }

//            // Calculate rotation amount
//            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, r_value, 0) * Time.deltaTime);

//            // Apply rotation to the Rigidbody
//            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
//        }
//        else
//        {
//            _rigidbody.velocity = new Vector3(0, 0, 0);
//        }

//    }
//}

using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;
using static Commo;

using Debug = UnityEngine.Debug;

public class Rl : Agent
{
    // ===== 基础引用 =====
    public TeamType tankTeam;
    public Commo BaseFunc;
    public GameManage gameManage;

    private BehaviorParameters behaviorParameters;
    public Rigidbody _rigidbody;
    public Slider phSlider;
    public ParticleSystem tankExplosion;

    // ⚠️ 不能删：EnvFunc/Commo.GetDamage 等都在用
    public TankAttributes tankAttributes;

    // 对手列表：元素类型必须是 Commo.EnemyInfo（struct）
    public List<EnemyInfo> Enemies { get; set; } = new();

    // Fire
    public Transform ShellPos;

    // Debug/Control
    private TextMeshPro NUM_text;
    public LayerMask obstacleMask;

    // 动作：选择敌人编号
    private int dicisionEnemy = 0;
    public int lastDisicion = -1;

    // ===== Reward Shaping 参数（按需调）======
    [Header("Reward Shaping")]
    public float stepPenalty = -0.001f;
    public float invalidTargetPenalty = -0.2f;
    public float losBonus = 0.03f;
    public float keepTargetBonus = 0.02f;
    public float switchTargetPenalty = -0.02f;
    public float nearTargetBonus = 0.05f;      // 距离越近越奖励
    public float lowHpTargetBonus = 0.05f;     // 残血目标更奖励
    public float nearDistanceMax = 400f;       // 近距离奖励的尺度（你也可以改 260/500/800）

    // ========== 生命周期 ==========
    public override void Initialize()
    {
        behaviorParameters = GetComponent<BehaviorParameters>();
        _rigidbody = GetComponent<Rigidbody>();
        BaseFunc = GetComponent<Commo>();

        // TeamId
        behaviorParameters.TeamId = (int)tankTeam;
    }

    public void TankInitialize()
    {
        behaviorParameters = GetComponent<BehaviorParameters>();
        _rigidbody = GetComponent<Rigidbody>();
        BaseFunc = GetComponent<Commo>();

        behaviorParameters.TeamId = (int)tankTeam;

        // 初始化属性
        tankAttributes.PH = tankAttributes.PHFULL;
        tankAttributes.firetime = 0;

        // 初始化血条
        if (phSlider != null)
        {
            phSlider.maxValue = tankAttributes.PHFULL;
            phSlider.value = tankAttributes.PH;
        }

        // 初始化编号显示
        var textTf = transform.Find("Text");
        if (textTf != null)
        {
            NUM_text = textTf.GetComponent<TextMeshPro>();
            if (NUM_text != null) NUM_text.text = tankAttributes.TankNum.ToString();
        }

        // 初始化对手列表：一定要“预填充”，否则 EnvFunc.RlUpdateEnemies 用 enemiesList[i]= 会越界
        int enemyCount = Mathf.Max(1, tankAttributes.EnemyNum);
        Enemies = new List<EnemyInfo>(enemyCount);
        for (int i = 0; i < enemyCount; i++)
            Enemies.Add(new EnemyInfo(0f, null));

        dicisionEnemy = 0;
        lastDisicion = -1;
    }

    public override void OnEpisodeBegin()
    {
        // 一般 EnvFunc.Reset 会做更完整的复位，这里只兜底
        if (tankAttributes.PHFULL > 0)
        {
            tankAttributes.PH = tankAttributes.PHFULL;
            if (phSlider != null) phSlider.value = tankAttributes.PH;
        }

        tankAttributes.firetime = 0;
        dicisionEnemy = Mathf.Clamp(dicisionEnemy, 0, Mathf.Max(0, tankAttributes.EnemyNum - 1));
        lastDisicion = -1;
    }

    void OnEnemyHit(Agent shooter, float damage)
    {
        shooter.AddReward(0.05f * damage);  // 每造成1点伤害奖励0.05
    }

    void OnEnemyKilled(Agent killer)
    {
        killer.AddReward(0.5f);
        foreach (var agent in blueTeamAgents)
        {
            if (agent != killer && !agent.IsDone) agent.AddReward(0.1f);
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // 假设动作分支0是移动方向，分支1是攻击目标（包括一个“不攻击”选项）
        // 屏蔽攻击无效目标
        for (int targetIndex = 0; targetIndex < totalEnemies; targetIndex++)
        {
            if (!enemy[targetIndex].IsAlive || !EnemyInRange(targetIndex))
            {
                actionMask.SetActionEnabled(branchIndex: 1, actionIndex: targetIndex, false);
            }
        }
        // 如智能体当前无法攻击（如装弹冷却），也屏蔽整个攻击分支或特定动作
        if (!CanAttack)
        {
            for (int act = 0; act < AttackActionsCount; act++)
                actionMask.SetActionEnabled(branchIndex: 1, actionIndex: act, false);
        }
    }

    // ========== 动作接收（目标选择）==========
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (StepCount % 200 == 0)
            Debug.Log($"[{name}] a0={actionBuffers.DiscreteActions[0]} last={lastDisicion} enemyCount={Enemies?.Count}");

        AddReward(stepPenalty);

        if (Enemies == null || Enemies.Count == 0) return;

        var discreteActions = actionBuffers.DiscreteActions;
        int a0 = discreteActions[0];

        int maxIdx = Mathf.Min(Enemies.Count - 1, Mathf.Max(0, tankAttributes.EnemyNum - 1));
        dicisionEnemy = Mathf.Clamp(a0, 0, maxIdx);

        // 取出当前目标信息
        EnemyInfo info = Enemies[dicisionEnemy];
        Individual enemyInd = info.Enemy;

        // 目标无效：惩罚
        if (!IsEnemyValid(enemyInd))
        {
            AddReward(invalidTargetPenalty);
            lastDisicion = dicisionEnemy;
            return;
        }

        // 有无遮挡
        bool los = HasLineOfSight(enemyInd.TankgameObject.transform);
        if (los) AddReward(losBonus);
        else AddReward(invalidTargetPenalty * 0.5f);

        // 集火/切换
        if (dicisionEnemy == lastDisicion) AddReward(keepTargetBonus);
        else if (lastDisicion != -1) AddReward(switchTargetPenalty);

        // 距离奖励（用 EnvFunc 里算的距离更省）
        float dis = Mathf.Max(info.Distance, 1f);
        float nearScore = Mathf.Clamp01(1f - dis / Mathf.Max(nearDistanceMax, 1f));
        AddReward(nearTargetBonus * nearScore);

        // 残血奖励（优先打残血）
        float hp01 = GetNormalizedHp(enemyInd);
        float lowHpScore = 1f - hp01;
        AddReward(lowHpTargetBonus * lowHpScore);

        lastDisicion = dicisionEnemy;
    }

    // ========== 观测 ==========
    public override void CollectObservations(VectorSensor sensor)
    {
        int enemyCount = Mathf.Max(1, tankAttributes.EnemyNum);

        // 保证长度够
        if (Enemies == null) Enemies = new List<EnemyInfo>(enemyCount);
        while (Enemies.Count < enemyCount) Enemies.Add(new EnemyInfo(0f, null));

        for (int i = 0; i < enemyCount; i++)
        {
            var info = Enemies[i];
            if (IsEnemyValid(info.Enemy))
                sensor.AddObservation(GetOtherAgentData(info.Enemy, info));
            else
                sensor.AddObservation(new float[] { i / 5.0f, 0, 0, 0, 0 });
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var da = actionsOut.DiscreteActions;
        int best = 0;
        float bestD = float.MaxValue;

        for (int i = 0; Enemies != null && i < Enemies.Count; i++)
        {
            var e = Enemies[i].Enemy;
            if (!IsEnemyValid(e)) continue;

            float d = Vector3.Distance(transform.position, e.TankgameObject.transform.position);
            if (d < bestD) { bestD = d; best = i; }
        }
        da[0] = best;
    }

    // ========== 每帧开火（沿用你原逻辑）==========
    private void FixedUpdate()
    {
        tankAttributes.firetime++;

        if (Enemies == null || Enemies.Count == 0) return;

        dicisionEnemy = Mathf.Clamp(dicisionEnemy, 0, Enemies.Count - 1);

        var info = Enemies[dicisionEnemy];
        var enemyInd = info.Enemy;

        // 目标无效：简单巡逻/前进一点，避免站桩（可选但强烈建议）
        if (!IsEnemyValid(enemyInd))
        {
            // 你也可以换成地图中心、红方出生点等
            _rigidbody.velocity = transform.forward * 30f;
            return;
        }

        // ✅ 关键：追目标（否则永远不会靠近进入射程）
        UpdateTargetPosition(enemyInd);

        // 进入射程才开火
        if (info.Distance > 0 && info.Distance < 260f && ShellPos != null)
        {
            Vector3 aimDir = (enemyInd.TankgameObject.transform.position - ShellPos.position).normalized;
            ShellPos.forward = aimDir;

            if (tankAttributes.firetime > tankAttributes.cooldowntime)
            {
                bool fired = BaseFunc.Openfire(gameObject, ShellPos, 1, 1200);
                if (fired) tankAttributes.firetime = 0;
            }
        }
    }

    // ========== 工具函数 ==========
    private bool IsEnemyValid(Individual enemyInd)
    {
        if (enemyInd == null) return false;
        if (enemyInd.TankgameObject == null) return false;
        if (!enemyInd.TankgameObject.activeSelf) return false;
        return true;
    }

    private bool HasLineOfSight(Transform enemyTf)
    {
        if (enemyTf == null) return false;

        Vector3 origin = ShellPos != null ? ShellPos.position : transform.position + Vector3.up * 1.0f;
        Vector3 target = enemyTf.position + Vector3.up * 1.0f;

        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist < 1e-3f) return true;
        dir /= dist;

        // obstacleMask 里只放障碍物层
        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstacleMask))
        {
            return false;
        }
        return true;
    }

    private float GetNormalizedHp(Individual enemyInd)
    {
        if (enemyInd == null || enemyInd.TankgameObject == null) return 1f;

        // 兼容敌人是 NR / Rl
        if (enemyInd.TankgameObject.TryGetComponent<NR>(out var nr))
            return Mathf.Clamp01(nr.tankAttributes.PH / Mathf.Max(1f, nr.tankAttributes.PHFULL));

        if (enemyInd.TankgameObject.TryGetComponent<Rl>(out var rl))
            return Mathf.Clamp01(rl.tankAttributes.PH / Mathf.Max(1f, rl.tankAttributes.PHFULL));

        return 1f;
    }

    private float[] GetOtherAgentData(Individual tank, EnemyInfo enemyinfo)
    {
        var otherAgentdata = new float[5];
        var rel = tank.TankgameObject.transform.position - this.gameObject.transform.position;

        // TankNum 归一化（你原来是 /5f）
        int tankNum = 0;
        if (tank.TankgameObject.TryGetComponent<NR>(out var nr))
            tankNum = nr.tankAttributes.TankNum;
        else if (tank.TankgameObject.TryGetComponent<Rl>(out var rl))
            tankNum = rl.tankAttributes.TankNum;

        otherAgentdata[0] = (tankNum - 1) / 5f;
        otherAgentdata[1] = enemyinfo.Distance / 1800f;
        otherAgentdata[2] = rel.x / 1800f;
        otherAgentdata[3] = rel.z / 1800f;
        otherAgentdata[4] = rel.y / 1800f;

        return otherAgentdata;
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

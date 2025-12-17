using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using Unity.MLAgents;
using Unity.VisualScripting;
using UnityEngine;

using Debug = UnityEngine.Debug;

public class GameManage : MonoBehaviour
{
    // Start is called before the first frame update
    [Tooltip("Max Environment Steps")]
    public int MaxEnvironmentSteps = 30000;

    [Tooltip("存储不同地图的初始化信息")]
    public MapInfo mapData = new();

    private Config config;
    private EnvFunc envFunc;
    private FollowTargetManager followTargetManager;
   
  
    public int m_ResetTimer;
    public int NumBlue;
    public int NumRed;
    public int NumBlueAlive;
    public int NumRedAlive;
    public int Blue_win;
    public int Red_win;

    //对局总数
    public int Rounds;

    public GameObject[] shell_all;

    /// <summary>
    /// 存储个体的列表
    /// </summary>
    /// 
    public List<Individual> IndividualsBlue ;
    public List<Individual> IndividualsRed ;

    public List<Individual> activeBlues = new ();
    public List<Individual> activeReds = new ();

    //TODO:建立AgentGroup函数，这里要根据对手双方来建立
    public SimpleMultiAgentGroup Group_Agent_Blue ;
    public SimpleMultiAgentGroup Group_Agent_Red ;
    void Start()
    { 
        EnvInit();       
        //创建红蓝队伍Agent
        envFunc.InitMapData();
        envFunc.CreateAgentTeams(this, config.TeamBlue, config.TeamRed, Group_Agent_Blue, out IndividualsBlue);
        envFunc.CreateAgentTeams(this, config.TeamRed, config.TeamBlue, Group_Agent_Red, out IndividualsRed);
        //初始化存活列表
        activeBlues = IndividualsBlue;
        activeReds = IndividualsRed;
        envFunc.CalcEnemyDisStart(activeBlues, activeReds, config.TeamBlue, config.TeamRed);
        followTargetManager.CamerInit(config, IndividualsBlue, IndividualsRed);
       
    }

    // Update is called once per frame

    private void FixedUpdate()
    {
        Whteher_win();
        envFunc.CalcEnemyDis(activeBlues, activeReds, config.TeamBlue, config.TeamRed);//为red更新敌方列表
        envFunc.TargetFresh(activeBlues, activeReds, config);
        m_ResetTimer++;
    }
    

    /// <summary>
    /// 初始化环境信息
    /// </summary>
    public void EnvInit()
    {
        config = GetComponent<Config>();
        envFunc = GetComponent<EnvFunc>();
        followTargetManager = GetComponent<FollowTargetManager>();

        NumBlue = config.TeamBlue.num;
        NumRed = config.TeamRed.num;
        NumBlueAlive = NumBlue;
        NumRedAlive = NumRed;
        Blue_win = 0;
        Red_win = 0;
        Rounds = 0;
        m_ResetTimer = 0;
        if (config.TeamBlue.type == IndividualType.RL)
        {
            Group_Agent_Blue = new();
        }
        if (config.TeamRed.type == IndividualType.RL)
        {
            Group_Agent_Red = new();
        }
    }
    

    /// <summary>
    /// 判断游戏是否结束
    /// </summary>  
    public void Whteher_win()
    {
        if (NumBlueAlive == 0 && NumRedAlive == 0)
        {
            //游戏结束
            Group_Agent_Red?.EndGroupEpisode();
            Group_Agent_Blue?.EndGroupEpisode();
            ResetScene();
            envFunc.CalcEnemyDisStart(activeBlues, activeReds, config.TeamBlue, config.TeamRed);
            return;
        }
        //红队胜利
        else if (NumBlueAlive == 0) 
        {
            Debug.Log("【结算】红胜（蓝全灭）");

            Red_win++;
            envFunc.TeamsWin(activeReds, Group_Agent_Red, Group_Agent_Blue);
            ResetScene();
            envFunc.CalcEnemyDisStart(activeBlues, activeReds, config.TeamBlue, config.TeamRed);
            return;
        }
        //蓝队胜利
        else if (NumRedAlive == 0)
        {
            Debug.Log("【结算】蓝胜（红全灭）");

            Blue_win++;
            envFunc.TeamsWin(activeBlues, Group_Agent_Blue, Group_Agent_Red);
            ResetScene();
            envFunc.CalcEnemyDisStart(activeBlues, activeReds, config.TeamBlue, config.TeamRed);
            return;
        }

        //超过最大步数,结束游戏
        if (m_ResetTimer >= MaxEnvironmentSteps)
        {
            //Group_Agent_Red?.GroupEpisodeInterrupted();
            //Group_Agent_Blue?.GroupEpisodeInterrupted();

            ////存活个体设置为false
            //ResetScene();
            //envFunc.CalcEnemyDisStart(activeBlues, activeReds, config.TeamBlue, config.TeamRed);
            Debug.Log("【结算】超时");

            const float TIMEOUT_PENALTY = -1.0f;

            // 只对存在的 RL group 加惩罚
            Group_Agent_Blue?.AddGroupReward(TIMEOUT_PENALTY);
            Group_Agent_Red?.AddGroupReward(TIMEOUT_PENALTY);

            Group_Agent_Red?.GroupEpisodeInterrupted();
            Group_Agent_Blue?.GroupEpisodeInterrupted();

            ResetScene();
            envFunc.CalcEnemyDisStart(activeBlues, activeReds, config.TeamBlue, config.TeamRed);

        }
    }

    /// <summary>
    /// 重置游戏
    /// </summary>
    public void ResetScene()
    {
        // 遍历找到的游戏对象
        Rounds++;
        Debug.Log("对局场次: " + (Rounds).ToString() );
        Debug.Log("蓝方胜率: " + (Blue_win / (float)Rounds).ToString("f3"));

        envFunc.AgentFalse(activeBlues, activeReds);
        m_ResetTimer = 0;
        shell_all = GameObject.FindGameObjectsWithTag("Shell");
        foreach (GameObject shell in shell_all) { Destroy(shell);}
        //重置红蓝队伍个体
        envFunc.ResetAgentTeams(IndividualsBlue, TeamType.Blue, config.TeamBlue.num, config.TeamBlue.posType, config.TeamBlue.type, Group_Agent_Blue);
        envFunc.ResetAgentTeams(IndividualsRed, TeamType.Red, config.TeamRed.num, config.TeamRed.posType, config.TeamRed.type, Group_Agent_Red);

        activeBlues = IndividualsBlue;
        activeReds = IndividualsRed;
        NumBlueAlive = NumBlue;
        NumRedAlive = NumRed;

        Debug.Log($"TeamRed.type={config.TeamRed.type}  TeamBlue.type={config.TeamBlue.type}");
        Debug.Log($"Group_Agent_Red null? {Group_Agent_Red == null}, Group_Agent_Blue null? {Group_Agent_Blue == null}");
    }


    public void TankDamage(GameObject go)
    {
        // 1) 如果是 RL，先从 group 移除
        if (go.TryGetComponent<Rl>(out var rl))
        {
            if (rl.tankTeam == TeamType.Blue) Group_Agent_Blue?.UnregisterAgent(rl);
            else Group_Agent_Red?.UnregisterAgent(rl);
        }

        // 2) 物理清零（很重要）
        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        go.SetActive(false);

        // 3) 活体列表更新
        activeBlues.RemoveAll(ind => ind.TankgameObject == go);
        activeReds.RemoveAll(ind => ind.TankgameObject == go);
        NumBlueAlive = activeBlues.Count;
        NumRedAlive = activeReds.Count;

        // 4) 让 NR / 其他逻辑方立刻换目标（避免追着失活目标）
        ForceReTargetForAllNR();
    }

    private void ForceReTargetForAllNR()
    {
        foreach (var r in activeReds)
        {
            var nr = r.TankgameObject.GetComponent<NR>();
            if (nr != null && (nr.CurrentTarget == null || !nr.CurrentTarget.TankgameObject.activeInHierarchy))
                nr.CurrentTarget = PickRandomAlive(activeBlues);
        }
        foreach (var b in activeBlues)
        {
            var nr = b.TankgameObject.GetComponent<NR>();
            if (nr != null && (nr.CurrentTarget == null || !nr.CurrentTarget.TankgameObject.activeInHierarchy))
                nr.CurrentTarget = PickRandomAlive(activeReds);
        }
    }

    private Individual PickRandomAlive(List<Individual> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }


}

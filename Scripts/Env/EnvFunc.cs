using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static Commo;
using static Config;
using static GameManage;

using Debug = UnityEngine.Debug;

/// <summary>
/// 维护双方、地图初始化以及所有重置的代码，维护个体和群体列表更新
/// </summary>
public class MapInfo
{
    public Vector3 topLeft;
    public Vector3 bottomRight;
    public Vector3 fixedStartPointsBlue;
    public Vector3 fixedStartPointsRed;
    public Vector3 fixedEndPointsBlue;
    public Vector3 fixedEndPointsRed;
    public float fixedPointsBlueRot;
    public float fixedPointsRedRot;
    public List<Transform> spawnPoints = new();
}
public class EnvFunc : MonoBehaviour
{
  
    [Header("个体预制体")]
    public Rl RlPrefab;
    public NR NrPrefab;

    [Header("地图属性信息")]
    public GameObject map;
    public MapInfo mapinfo;
    public LayerMask obstacleMask1;
    public LayerMask obstacleMask2;//设定障碍物的LayerMask
    private int layerMask;
    private int[] blueEnemyVisible;
    private int[] redEnemyVisible;
    private int[] RlIsVisible;
    List<int> randomPosBlue;
    List<int> randomPosRed;



    private GameManage gameManage;

    private void Start()
    {
        gameManage = GetComponent<GameManage>();
        if (gameManage is null)
            Debug.Log("gameManage is null");
        else
        {
            Debug.Log("gameManage is intilize");
        }
        layerMask = LayerMask.GetMask("Obstacle");
    }

    /// <summary>
    /// 初始化地图数据，初始化部分无需过多关注
    /// </summary>
    public void InitMapData()
    {
        mapinfo = new();
        Transform mapPos = map.transform.Find("Pos");
        if (mapPos != null)
        {
            Transform temp;
            temp = mapPos.Find("topLeft");
            mapinfo.topLeft = temp ? temp.position : Vector3.zero;
            temp = mapPos.Find("bottomRight");
            mapinfo.bottomRight = temp ? temp.position : Vector3.zero;
            temp = mapPos.Find("fixedStartPointsBlue");
            mapinfo.fixedStartPointsBlue = temp ? temp.position : Vector3.zero;
            mapinfo.fixedPointsBlueRot = temp ? temp.position.y : 0;
            temp = mapPos.Find("fixedStartPointsRed");
            mapinfo.fixedStartPointsRed = temp ? temp.position : Vector3.zero;
            mapinfo.fixedPointsRedRot = temp ? temp.position.y : 0;
            temp = mapPos.Find("fixedEndPointsBlue");
            mapinfo.fixedEndPointsBlue = temp ? temp.position : Vector3.zero;
            temp = mapPos.Find("fixedEndPointsRed");
            mapinfo.fixedEndPointsRed = temp ? temp.position : Vector3.zero;

            Debug.Log("Map data initialized.");
        }
    }

    /// <summary>
    /// 给定队伍信息，以及出生个体类型，返回具体的位置
    /// </summary>  
    public (Vector3, Quaternion) SetPosition(PosType posType, MapInfo currentMap, TeamType TankTeam, int TankNum, int TeamCount)
    {

        switch (posType)
        {
            case PosType.Fixed:
                //Debug.Log("Fixed");
                if(currentMap is null)
                {
                    Debug.Log("Map data not initialized.");
                    return (Vector3.zero, Quaternion.Euler(0, 0, 0));
                }
                Vector3 startPoint = (TankTeam == TeamType.Blue) ? currentMap.fixedStartPointsBlue : currentMap.fixedStartPointsRed;
                Vector3 endPoint = (TankTeam == TeamType.Blue) ? currentMap.fixedEndPointsBlue : currentMap.fixedEndPointsRed;
                float rot = (TankTeam == TeamType.Blue) ? currentMap.fixedPointsBlueRot : currentMap.fixedPointsRedRot;
                Vector3 direction = (endPoint - startPoint).normalized;
                float segmentLength = Vector3.Distance(startPoint, endPoint) / (TeamCount - 1);
                return (startPoint + UnityEngine.Random.Range(0.94f, 1.06f) * segmentLength * (TankNum - 1) * direction + 
                    UnityEngine.Random.Range(0.85f, 1.15f) * new Vector3(0, 30, 0) * TankNum, Quaternion.Euler(0, rot, 0));

            case PosType.Random:
                if (randomPosBlue == null || randomPosBlue.Count == 0)
                {
                    randomPosBlue = new List<int> { 0, 1, 2, 3, 4 };
                    for (int i = randomPosBlue.Count - 1; i > 0; i--)
                    {
                        int randomIndex = Random.Range(0, i + 1);
                        // 交换位置
                        int temp = randomPosBlue[i];
                        randomPosBlue[i] = randomPosBlue[randomIndex];
                        randomPosBlue[randomIndex] = temp;
                    }
                }
                if (randomPosRed == null || randomPosRed.Count == 0)
                {
                    randomPosRed = new List<int> { 0, 1, 2, 3, 4 };
                    for (int i = randomPosRed.Count - 1; i > 0; i--)
                    {
                        int randomIndex = Random.Range(0, i + 1);
                        // 交换位置
                        int temp = randomPosRed[i];
                        randomPosRed[i] = randomPosRed[randomIndex];
                        randomPosRed[randomIndex] = temp;
                    }
                }

                if (currentMap is null)
                {
                    Debug.Log("Map data not initialized.");
                    return (Vector3.zero, Quaternion.Euler(0, 0, 0));
                }
                Vector3 startPoint_ = (TankTeam == TeamType.Blue) ? currentMap.fixedStartPointsBlue : currentMap.fixedStartPointsRed;
                Vector3 endPoint_ = (TankTeam == TeamType.Blue) ? currentMap.fixedEndPointsBlue : currentMap.fixedEndPointsRed;
                float rot_ = (TankTeam == TeamType.Blue) ? currentMap.fixedPointsBlueRot : currentMap.fixedPointsRedRot;
                Vector3 direction_ = (endPoint_ - startPoint_).normalized;
                float segmentLength_ = Vector3.Distance(startPoint_, endPoint_) / TeamCount;
                Vector3 position = Vector3.zero;
               
                if(TankTeam == TeamType.Blue)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        position = startPoint_ + (randomPosBlue[0] + UnityEngine.Random.Range(0.03f, 0.97f)) * segmentLength_ * direction_ +
                        UnityEngine.Random.Range(1.2f, 10f) * new Vector3(0, 20, 0) +
                        startPoint_.x * new Vector3(-1, 0, 0) * UnityEngine.Random.Range(0.06f, 0.73f);
                        if (!Physics.CheckBox(position, new Vector3(8, 8, 8), Quaternion.identity, obstacleMask1))
                        {
                            randomPosBlue.RemoveAt(0);
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 50; i++)
                    {
                        position = startPoint_ + (randomPosRed[0] + UnityEngine.Random.Range(0.03f, 0.97f)) * segmentLength_ * direction_ +
                        UnityEngine.Random.Range(1.2f, 10f) * new Vector3(0, 20, 0) +
                        startPoint_.x * new Vector3(-1, 0, 0) * UnityEngine.Random.Range(0.06f, 0.73f);
                        if (!Physics.CheckBox(position, new Vector3(8, 8, 8), Quaternion.identity, obstacleMask1))
                        {
                            randomPosRed.RemoveAt(0);
                            break;
                        }
                    }
                }

                return (position, Quaternion.Euler(0, rot_, 0));
        }

        return (Vector3.zero, Quaternion.Euler(0, 0, 0));
    }

    /// <summary>
    /// 生成RL队伍
    /// </summary>
    public void CreateAgentTeams(GameManage gameManage, TeamInfo teaminfo, TeamInfo ememyinfo, SimpleMultiAgentGroup simpleMultiAgentGroup, out List<Individual> individuals)
    {
        individuals = new();
        GameObject prefab = teaminfo.type == IndividualType.RL ? RlPrefab.gameObject : NrPrefab.gameObject;
        int EnymyCount = ememyinfo.num;
        for (int i = 0; i < teaminfo.num; i++)
        {
            Individual newIndividual = CreateIndividual(gameManage, teaminfo.type, teaminfo, i+1, prefab, EnymyCount, simpleMultiAgentGroup);
            individuals.Add(newIndividual);
        }
        if(teaminfo.team == TeamType.Blue)
            blueEnemyVisible = new int[EnymyCount];
        else
            redEnemyVisible = new int[EnymyCount];
        // PrintOpponentList(individuals, "Test001");
    }


    /// <summary>
    /// 生成个体
    /// </summary>
    public Individual CreateIndividual(GameManage gameManage, IndividualType individualType, TeamInfo teaminfo, int index, GameObject prefab,  int enemycount, SimpleMultiAgentGroup simpleMultiAgentGroup)
    {
        //TODO：teamtype和 teaminfo里边的信息多余了，可以删掉
        
        (Vector3 pos, Quaternion rot) = SetPosition(teaminfo.posType, mapinfo, teaminfo.team, index, teaminfo.num);
        GameObject tank = Instantiate(prefab, pos, rot);
        if (individualType == IndividualType.NR)
        {
            NR NrTank = tank.GetComponent<NR>();
            NrTank.tankAttributes.PHFULL = teaminfo.PHFULL;
            NrTank.tankAttributes.MaxSpeed = teaminfo.MaxSpeed;
            //NrTank.tankAttributes.hMaxSpeed = teaminfo.hMaxSpeed;
            NrTank.tankAttributes.rotateSpeed = teaminfo.rotateSpeed;
            NrTank.tankAttributes.cooldowntime = teaminfo.cooldowntime;
            NrTank.tankAttributes.EnemyNum = enemycount;
            NrTank.tankTeam = teaminfo.team;
            NrTank.tag = "Tank_" + teaminfo.team.ToString();
            NrTank.tankAttributes.TankNum = index;
            NrTank.gameManage = gameManage;
            NrTank.name = teaminfo.team.ToString() + "_" + teaminfo.type.ToString() + "_" + (index);
            //NrTank.isDead = false;
            NrTank.TankInitialize();
        }
        else
        {
            Rl RlTank = tank.GetComponent<Rl>();
            RlTank.tankAttributes.PHFULL = teaminfo.PHFULL;
            RlTank.tankAttributes.MaxSpeed = teaminfo.MaxSpeed;
            //RlTank.tankAttributes.hMaxSpeed = teaminfo.hMaxSpeed;           
            RlTank.tankAttributes.rotateSpeed = teaminfo.rotateSpeed;
            RlTank.tankAttributes.cooldowntime = teaminfo.cooldowntime;
            RlTank.tankAttributes.EnemyNum = enemycount;
            RlTank.tankTeam = teaminfo.team;
            RlTank.tag = "Tank_" + teaminfo.team.ToString();
            RlTank.tankAttributes.TankNum = index;
            RlTank.gameManage = gameManage;
            RlTank.name = teaminfo.team.ToString() + "_" + teaminfo.type.ToString() + "_" + (index);
            RlTank.TankInitialize();
            //RlTank.isDead = false;
            simpleMultiAgentGroup.RegisterAgent(RlTank);
            //ChangeTankSensor(RlTank, teaminfo.team);
        }

        ChangeTankColor(tank, teaminfo.material, teaminfo.team == TeamType.Blue ? Color.blue : Color.red);
        Individual individualComponent = tank.AddComponent<Individual>();
        individualComponent.Type = teaminfo.type;
        individualComponent.Team = teaminfo.team;
        individualComponent.TankgameObject = tank;
        return individualComponent;
    }


    /// <summary>
    /// 重置队伍个体
    /// </summary>
    public void ResetAgentTeams(List<Individual> individuals, TeamType teamType, int teamCount, PosType postype, IndividualType individualType, SimpleMultiAgentGroup simpleMultiAgentGroup)
    {       
        foreach (var individual in individuals)
        {
            if (individualType == IndividualType.NR)
                ResetNRIndividual(individual, teamType, teamCount, postype);
            else
                ResetRlIndividual(individual, teamType, teamCount, postype, simpleMultiAgentGroup);
        }
        if(teamType == TeamType.Red) blueEnemyVisible = new int[individuals.Count];
        if(teamType == TeamType.Blue) redEnemyVisible = new int[individuals.Count];
    }

    /// <summary>
    /// 重置 RL 个体（师兄使用的是需要置false再重置）
    /// </summary>
    public void ResetRlIndividual(Individual individual, TeamType teamType, int teamCount, PosType postype, SimpleMultiAgentGroup simpleMultiAgentGroup)
    {
        individual.TankgameObject.SetActive(true);
        Rl RlTank = individual.TankgameObject.GetComponent<Rl>();
        simpleMultiAgentGroup.RegisterAgent(RlTank);
        (Vector3 pos, Quaternion rot) = SetPosition(postype, mapinfo, teamType, RlTank.tankAttributes.TankNum, teamCount);
        RlTank.transform.SetPositionAndRotation(pos, rot);
        RlTank.tankAttributes.PH = RlTank.tankAttributes.PHFULL;
        RlTank.phSlider.value = RlTank.tankAttributes.PH;
        RlTank.tankAttributes.firetime = 0;
        //simpleMultiAgentGroup.RegisterAgent(RlTank);

    }
    /// <summary>
    /// 重置 NR 个体
    /// </summary>
    public void ResetNRIndividual(Individual individual, TeamType teamType, int teamCount, PosType postype)
    {
        individual.TankgameObject.SetActive(true);
        NR NrTank = individual.TankgameObject.GetComponent<NR>();
        (Vector3 pos, Quaternion rot) = SetPosition(postype, mapinfo, teamType, NrTank.tankAttributes.TankNum, teamCount);
        NrTank.transform.SetPositionAndRotation(pos, rot);
        NrTank.tankAttributes.PH = NrTank.tankAttributes.PHFULL;
        NrTank.phSlider.value = NrTank.tankAttributes.PH;
        NrTank.tankAttributes.firetime = 0;
    }

    /// <summary>
    /// 更改坦克颜色
    /// </summary>
    public void ChangeTankColor(GameObject tank, Material material, Color color)
    {
        // 使用Transform.Find找到TankRenderers/TankFree_Body
        Transform TankRenderTransform = tank.transform.Find("TankRenderers");
        Transform text = tank.transform.Find("Text");
        text.GetComponent<TextMeshPro>().color = color;
        // 从TankFree_Body获取MeshRenderer
        foreach (var renderer in TankRenderTransform.GetComponentsInChildren<MeshRenderer>())
        {
            // 设置新的材质
            renderer.material = material;
        }
    }

    /// <summary>
    /// 每帧更新一次对手的位置信息
    /// </summary>    
    public void CalcEnemyDis(List<Individual> individualsActiveBlue, List<Individual> individualsActiveRed, TeamInfo blueInfo, TeamInfo redInfo)
    {

        VisibleEnemies(individualsActiveBlue, individualsActiveRed, blueInfo, redInfo);

        foreach (var blue in individualsActiveBlue)
        {
            if (blue.Type == IndividualType.RL)
                RlUpdateEnemies(blue, individualsActiveRed, redInfo);
            else
                NRUpdateEnemies(blue, individualsActiveRed);
        }

        foreach (var red in individualsActiveRed)
        {
            if (red.Type == IndividualType.RL)
                RlUpdateEnemies(red, individualsActiveBlue, blueInfo);
            else
                NRUpdateEnemies(red, individualsActiveRed);

        }
        
    }
    
    
    public void CalcEnemyDisStart(List<Individual> individualsActiveBlue, List<Individual> individualsActiveRed, TeamInfo blueInfo, TeamInfo redInfo)
    {
        if(blueInfo.type == IndividualType.RL)
        {
            foreach (var blue in individualsActiveBlue)
            {
                UpdateEnemiesStart(blue, individualsActiveRed);
            }
        }
        
        if(redInfo.type == IndividualType.RL)
        {
            foreach (var red in individualsActiveRed)
            {
                UpdateEnemiesStart(red, individualsActiveBlue);
            }
        }

    }
    

    /// <summary>
    /// (NR)更新个体的对手列表
    /// </summary>
    private void NRUpdateEnemies(Individual individual, List<Individual> opponents)
    {
        var enemiesList = individual.TankgameObject.GetComponent<NR>().Enemies;
        enemiesList.Clear();
        foreach (var opponent in opponents)
        {
            float dis = Vector3.Distance(individual.gameObject.transform.position, opponent.gameObject.transform.position);
            enemiesList.Add(new EnemyInfo(dis, opponent));
        }

        enemiesList.Sort((a, b) => a.Distance.CompareTo(b.Distance));
    }

    /// <summary>
    /// 蓝方(RL)判断敌方是否可见，存入数组
    /// </summary>
    private void VisibleEnemies(List<Individual> individualsActiveBlue, List<Individual> individualsActiveRed, TeamInfo blueInfo, TeamInfo redInfo)
    {
        if(blueInfo.type == IndividualType.RL)
        {
            blueEnemyVisible = new int[blueEnemyVisible.Length];
            foreach (var blue in individualsActiveBlue)
            {
                foreach (var red in individualsActiveRed)
                {
                    if (!Physics.Linecast(blue.gameObject.transform.position, red.gameObject.transform.position, layerMask))
                    {
                        //看得见敌方目标置1
                        if (redInfo.type == IndividualType.NR)
                            blueEnemyVisible[red.TankgameObject.GetComponent<NR>().tankAttributes.TankNum - 1] = 1;
                        else
                            blueEnemyVisible[red.TankgameObject.GetComponent<Rl>().tankAttributes.TankNum - 1] = 1;
                    }
                }
            }
        }



        if (redInfo.type == IndividualType.RL)
        {
            redEnemyVisible = new int[redEnemyVisible.Length];

            foreach (var red in individualsActiveRed)
            {

                foreach (var blue in individualsActiveBlue)
                {
                    if (!Physics.Linecast(red.gameObject.transform.position, blue.gameObject.transform.position, layerMask))
                    {
                        //看得见敌方目标置1
                        if(blueInfo.type == IndividualType.NR)
                            redEnemyVisible[blue.TankgameObject.GetComponent<NR>().tankAttributes.TankNum - 1] = 1;
                        else
                            redEnemyVisible[blue.TankgameObject.GetComponent<Rl>().tankAttributes.TankNum - 1] = 1;
                    }
                }
            }
        }
    }

    /// <summary>
    /// (RL)更新个体的对手列表
    /// </summary>
    private void RlUpdateEnemies(Individual individual, List<Individual> opponents, TeamInfo oppInfo)
    {
        //var individual = individualWithPos.Ind;
        var enemiesList = individual.TankgameObject.GetComponent<Rl>().Enemies;
        for (int i = 0; i < blueEnemyVisible.Length; i++)
        {
            if (blueEnemyVisible[i] == 0)
                enemiesList[i] = new EnemyInfo(0, null);
        }

        if(oppInfo.type == IndividualType.NR)
        {
            foreach (var opponent in opponents)
            {
                if (blueEnemyVisible[opponent.TankgameObject.GetComponent<NR>().tankAttributes.TankNum - 1] == 1)
                {
                    float dis = Vector3.Distance(individual.gameObject.transform.position, opponent.gameObject.transform.position);
                    enemiesList[opponent.TankgameObject.GetComponent<NR>().tankAttributes.TankNum - 1] = new EnemyInfo(dis, opponent);
                }
            }
        }
        else
        {
            foreach (var opponent in opponents)
            {
                if (blueEnemyVisible[opponent.TankgameObject.GetComponent<Rl>().tankAttributes.TankNum - 1] == 1)
                {
                    float dis = Vector3.Distance(individual.gameObject.transform.position, opponent.gameObject.transform.position);
                    enemiesList[opponent.TankgameObject.GetComponent<Rl>().tankAttributes.TankNum - 1] = new EnemyInfo(dis, opponent);
                }
            }

        }
    }

    /// <summary>
    /// 初始(RL)更新个体的对手列表
    /// </summary>
    private void UpdateEnemiesStart(Individual individual, List<Individual> opponents)
    {
        //var individual = individualWithPos.Ind;
        var enemiesList = individual.TankgameObject.GetComponent<Rl>().Enemies;
        
        enemiesList.Clear();
        foreach (var opponent in opponents)
        {
            float dis = Vector3.Distance(individual.gameObject.transform.position, opponent.gameObject.transform.position);
            enemiesList.Add(new EnemyInfo(dis, opponent));
        }
        enemiesList.Sort((a, b) => a.Enemy.gameObject.name.CompareTo(b.Enemy.gameObject.name));
    }


    /// <summary>
    /// Debug 对手列表, 用于检查对手列表是否正确
    /// </summary>
    private void PrintOpponentList(List<Individual> individuals, string teamName)
    {
        foreach (var individual in individuals)
        {
            if (individual.isActiveAndEnabled)
            {
                string message = teamName + " - " + individual.TankgameObject.name + ": ";
                List<EnemyInfo> enemies = null;

                if (individual.Type == IndividualType.RL)
                {
                    enemies = individual.TankgameObject.GetComponent<Rl>().Enemies;
                }
                else if (individual.Type == IndividualType.NR)
                {
                    enemies = individual.TankgameObject.GetComponent<NR>().Enemies;
                }

                if (enemies != null)
                {
                    foreach (var enemyInfo in enemies)
                    {
                        message += enemyInfo.Enemy.TankgameObject.name + " (Distance: " + enemyInfo.Distance + "), ";
                    }
                }

                Debug.Log(message);
            }
        }
    }

    /// <summary>
    /// 队伍获胜时添加奖励
    public void TeamsWin(List<Individual> winIndividualActive, SimpleMultiAgentGroup winAgentGroup, SimpleMultiAgentGroup loseAgentGroup)
    {
        // 选一个胜方代表用于 Debug（避免 ind 未定义）
        Individual ind = null;
        if (winIndividualActive != null && winIndividualActive.Count > 0)
            ind = winIndividualActive[0];

        BehaviorParameters bp = null;
        if (ind != null && ind.TankgameObject != null)
            bp = ind.TankgameObject.GetComponent<BehaviorParameters>();

        if (winAgentGroup != null)
        {
            winAgentGroup.AddGroupReward(1f);

            foreach (var individual in winIndividualActive)
            {
                // 结束个体添加奖励 + 拉走位置
                if (individual.TankgameObject.tag == "Tank_Blue")
                    individual.TankgameObject.GetComponent<Rl>().transform.SetPositionAndRotation(
                        new Vector3(1000, 2000, -600), Quaternion.Euler(0, 0, 0));
                else
                    individual.TankgameObject.GetComponent<Rl>().transform.SetPositionAndRotation(
                        new Vector3(-600, 2000, 1000), Quaternion.Euler(0, 0, 0));

                individual.TankgameObject.GetComponent<Rl>().AddReward(25f);
            }

            winAgentGroup.EndGroupEpisode();
            loseAgentGroup?.EndGroupEpisode();

            //// Debug：胜方代表个体的信息（TeamId/BehaviorName）
            //if (ind != null && bp != null)
            //    Debug.Log($"[WIN CHECK] winner obj={ind.TankgameObject.name}, team={bp.TeamId}, behavior={bp.BehaviorName}");
            //else
            //    Debug.Log("[WIN CHECK] winner info unavailable (winIndividualActive empty or missing BehaviorParameters)");
        }
        else
        {
            //// NR 赢：打印一个蓝方 RL 的 team，用来确认输方确实是 team=0
            //if (gameManage != null && gameManage.activeBlues != null && gameManage.activeBlues.Count > 0)
            //{
            //    var loserObj = gameManage.activeBlues[0].TankgameObject;
            //    bp = loserObj.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            //    if (bp != null)
            //        Debug.Log($"[WIN CHECK] NR win. Example BLUE loser obj={loserObj.name}, team={bp.TeamId}, behavior={bp.BehaviorName}");
            //}

            // 胜方是 NR，输方如果是 RL，给一个小的负反馈
            if (loseAgentGroup != null) loseAgentGroup.AddGroupReward(-1f);
            loseAgentGroup?.EndGroupEpisode();
        }
    }

    /// <summary>
    /// 存活个体设置为false，方便重置
    /// 
    public void AgentFalse(List<Individual> activeBlues, List<Individual> activeReds)
    {
        foreach (Individual ind in activeBlues)
        {
            //ind.gameObject.SetActive(false);
            if(ind.Type == IndividualType.RL)
                ind.gameObject.SetActive(false);
            else
                ind.gameObject.SetActive(false);
        }
        foreach (Individual ind in activeReds)
        {
            //ind.gameObject.SetActive(false);
            //ind.gameObject.SetActive(false);
            if (ind.Type == IndividualType.RL)
                ind.gameObject.SetActive(false);
            else
                ind.gameObject.SetActive(false);
        }
    }

    ///  <summary>
    ///  两方更新个体目标Target
    ///  
    public void TargetFresh(List<Individual> IndividualActiveBlue, List<Individual> IndividualActiveRed, Config config)
    {
        //更新Blue队伍的目标
        SetTarget(config.TeamBlue.type, config.TeamRed.type, IndividualActiveBlue, IndividualActiveRed);
        //更新Red队伍的目标
        SetTarget(config.TeamRed.type, config.TeamBlue.type, IndividualActiveRed, IndividualActiveBlue);
    }


    /// <summary>
    /// 规则方更新目标
    /// </summary>
    private void SetTarget(IndividualType individualfriend, IndividualType individualenemy, List<Individual> friendlies, List<Individual> enemies)
    {
        if (individualfriend != IndividualType.NR) { return; }

        // Calculate the center of the friendly swarm
        Vector3 swarmCenter = CalculateSwarmCenter(friendlies);

        // Sort enemies based on their distance to the swarm center
        var sortedEnemies = enemies.OrderBy(enemy => Vector3.Distance(swarmCenter, enemy.transform.position)).ToList();

        //为规则方添加遮挡视野 RlIsVisible
        RlIsVisible = new int[sortedEnemies.Count];
        foreach (var red in friendlies)
        {

            for(int j = 0; j <  sortedEnemies.Count; j++)
            {
                if (!Physics.Linecast(red.gameObject.transform.position, sortedEnemies[j].gameObject.transform.position, layerMask))
                {
                    //看得见敌方目标置1
                    RlIsVisible[j] = 1;
                }
            }
        }
        Dictionary<Individual, NR> friendlyNRs = friendlies.ToDictionary(friendly => friendly, friendly => friendly.TankgameObject.GetComponent<NR>());

        // Clear previous target assignments
        foreach (var nr in friendlyNRs.Values)
        {
            nr.CurrentTarget = null;
        }

        //蜂群（规则方）
        int minDisEnemy = 0;
        for(int i = 0; i < RlIsVisible.Length; i++)
        {
            if (RlIsVisible[i] == 1)
            {
                minDisEnemy = i;
                break;
            }
        }
        for (int i = 0; i < RlIsVisible.Length; i++)
        {
            if (RlIsVisible[i] == 0) continue;
            List<Individual> minDisFriend = new List<Individual>();
            foreach (var friend in friendlies)
            {
                minDisFriend.Add(friend);
            }
            minDisFriend = minDisFriend.OrderBy(friend => Vector3.Distance(sortedEnemies[i].transform.position, friend.transform.position)).ToList();
            foreach (var friend in minDisFriend)
            {
                if (friendlyNRs[friend].CurrentTarget == null)
                {
                    friendlyNRs[friend].CurrentTarget = sortedEnemies[i];
                    break;
                }
            }
        }

        // Debug logs
        foreach (var friendly in friendlies)
        {
            if (friendly.TankgameObject.TryGetComponent<NR>(out var nrFriendly))
            {
                if (nrFriendly.CurrentTarget == null)
                {
                    // Assign remaining friendlies to the nearest enemy
                    nrFriendly.CurrentTarget = sortedEnemies[minDisEnemy];
                }
            }
        }

    }

    /// <summary>
    /// 规则方计算蜂群中心
    /// </summary>
    private Vector3 CalculateSwarmCenter(List<Individual> friendlies)
    {
        Vector3 center = Vector3.zero;
        foreach (var friendly in friendlies)
        {
            center += friendly.transform.position;
        }
        return center / friendlies.Count;
    }



}

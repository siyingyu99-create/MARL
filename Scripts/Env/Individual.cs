using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum IndividualType
{
    NR,
    RL
}


public enum TeamType
{
    Blue,
    Red
}

public enum PosType 
{   
    Fixed, 
    Random 
}

public class IndividualWithPosition
{
    public Individual Ind { get; set; }
    public Vector3 Position { get; set; }

    public IndividualWithPosition(Individual ind, Vector3 position)
    {
        Ind = ind;
        Position = position;
    }
}


public class Individual : MonoBehaviour
{
    // 个体类型 RL/NR
    public IndividualType Type { get; set; }
    // 判断是红队/蓝队
    public TeamType Team { get; set; }  
    
    public GameObject TankgameObject { get; set; }

    // 设置个体的目标
    public Individual CurrentTarget { get; set; }
    
}

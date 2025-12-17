using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Config : MonoBehaviour
{   
    [System.Serializable]
    public class TeamInfo
    {
        public IndividualType type;
        public TeamType team;
        public PosType posType;
        public int num;

        //个体属性信息
        public int PHFULL;
        public Material material;
        public float MaxSpeed;
        //public float hMaxSpeed;
        public float MaxHeight;
        public float rotateSpeed;
        public int cooldowntime;
    }


 
    public TeamInfo TeamBlue;
    public TeamInfo TeamRed;


   
}


using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using Unity.VisualScripting;
using UnityEngine;

public class ShellControl : MonoBehaviour
{
    //炮弹属性信息
    public ParticleSystem shellExplosion;
    
    public Commo BaseFunc;
  
    private float explosionRadius = 20;
    public LayerMask tankMask;
    public GameObject father;

    //记录炮弹的发射者，用于判断是否击中队友。
    public void Set_father(GameObject fire)
    {
        father = fire;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //遍历获取tank 碰撞到的图层
        Collider[] tankColliders = Physics.OverlapSphere(transform.position, explosionRadius, tankMask);
        //string fatherTag = father.tag;

        for (int i = 0; i < tankColliders.Length; i++)
        {
            if (tankColliders[i].gameObject.TryGetComponent<Rigidbody>(out var tankRigidbody))
            {
                HandleDamage(tankColliders[i].gameObject);
            }
        }

        //炮弹爆炸
        shellExplosion.transform.parent = null;
        if (shellExplosion != null)
        {
            shellExplosion.Play();
            Destroy(shellExplosion.gameObject, shellExplosion.main.duration);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// 处理个体的伤害函数
    /// </summary>
    void HandleDamage(GameObject tankGameObject)
    {
        bool wasActive = tankGameObject.activeSelf;

        // 造成伤害（原逻辑不变）
        if (tankGameObject.TryGetComponent<Rl>(out var rlTank))
        {
            if (rlTank.TryGetComponent<Commo>(out var baseFunc))
            {
                baseFunc.GetDamage(rlTank, 15);
            }
        }
        else if (tankGameObject.TryGetComponent<NR>(out var nrTank))
        {
            if (nrTank.TryGetComponent<Commo>(out var baseFunc))
            {
                baseFunc.GetDamage(nrTank, 15);
            }
        }

        bool isDeadNow = wasActive && !tankGameObject.activeSelf;

        // 给开火者奖励（原来只有 +8 / -3，这里加“击杀加成”和“友军击杀加罚”）
        if (father.TryGetComponent<Rl>(out var fatherRl))
        {
            bool sameTeam = (tankGameObject.tag == father.tag);

            if (sameTeam)
            {
                fatherRl.AddReward(-3f);
                if (isDeadNow) fatherRl.AddReward(-10f);   // 友军误杀额外惩罚
            }
            else
            {
                fatherRl.AddReward(8f);
                if (isDeadNow) fatherRl.AddReward(12f);    // 击杀额外奖励
            }
        }
    }

}

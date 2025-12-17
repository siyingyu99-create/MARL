//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class ShellControl : MonoBehaviour
//{
//    // 炮弹属性信息
//    public ParticleSystem shellExplosion;

//    public Commo BaseFunc;

//    private float explosionRadius = 20;
//    public LayerMask tankMask;

//    // 发射者
//    public GameObject father;
//    private Rl fatherRl;

//    // 奖励参数（建议从温和开始，训练更稳）
//    [Header("Reward Shaping")]
//    public float hitEnemyReward = 0.2f;        // 命中敌人
//    public float killEnemyReward = 2.0f;       // 击杀敌人（强信号）
//    public float hitFriendPenalty = -0.5f;     // 友伤
//    public float killFriendPenalty = -3.0f;    // 友军击杀（很严重）
//    public float missPenalty = -0.05f;         // 炸空（没伤到任何坦克）

//    // 记录炮弹的发射者，用于判断是否击中队友
//    public void Set_father(GameObject fire)
//    {
//        father = fire;
//        fatherRl = (father != null) ? father.GetComponent<Rl>() : null;
//    }

//    private void OnCollisionEnter(Collision collision)
//    {
//        Collider[] tankColliders = Physics.OverlapSphere(transform.position, explosionRadius, tankMask);

//        int totalDamaged = 0;

//        for (int i = 0; i < tankColliders.Length; i++)
//        {
//            if (tankColliders[i].gameObject.TryGetComponent<Rigidbody>(out var tankRigidbody))
//            {
//                bool damaged = HandleDamageAndReward(tankColliders[i].gameObject);
//                if (damaged) totalDamaged++;
//            }
//        }

//        // 炸到空气：小惩罚，防止“乱开火也无所谓”
//        if (fatherRl != null && totalDamaged == 0)
//        {
//            fatherRl.AddReward(missPenalty);
//        }

//        // 炮弹爆炸特效
//        if (shellExplosion != null)
//        {
//            shellExplosion.transform.parent = null;
//            shellExplosion.Play();
//            Destroy(shellExplosion.gameObject, shellExplosion.main.duration);
//        }

//        Destroy(gameObject);
//    }

//    /// <summary>
//    /// 对目标造成伤害，并将奖励给发射者（fatherRl）
//    /// 返回：是否确实造成了伤害（用于判断炸空）
//    /// </summary>
//    bool HandleDamageAndReward(GameObject tankGameObject)
//    {
//        // 没有发射者就不给任何奖励（但仍然执行伤害）
//        bool canReward = (father != null && fatherRl != null);

//        // 用 tag 判断是否同队（沿用你原逻辑）
//        bool isFriendly = (father != null && tankGameObject.tag == father.tag);

//        // 记录伤害前血量
//        float hpBefore = GetHp(tankGameObject);

//        // 造成伤害（沿用你们 Commo.GetDamage 的逻辑）
//        if (tankGameObject.TryGetComponent<Rl>(out var rlTank))
//        {
//            if (rlTank.TryGetComponent<Commo>(out var baseFunc))
//            {
//                baseFunc.GetDamage(rlTank, 15);
//            }
//            else
//            {
//                return false;
//            }
//        }
//        else if (tankGameObject.TryGetComponent<NR>(out var nrTank))
//        {
//            if (nrTank.TryGetComponent<Commo>(out var baseFunc))
//            {
//                baseFunc.GetDamage(nrTank, 15);
//            }
//            else
//            {
//                return false;
//            }
//        }
//        else
//        {
//            // 不是坦克目标
//            return false;
//        }

//        // 伤害后血量
//        float hpAfter = GetHp(tankGameObject);

//        // 无法读取血量：不奖励
//        if (hpBefore < 0 || hpAfter < 0) return false;

//        // 没掉血：不奖励（可能无敌/护盾/判定未生效）
//        if (hpAfter >= hpBefore) return false;

//        bool killed = (hpAfter <= 0.0001f);

//        // 给发射者奖励/惩罚
//        if (canReward)
//        {
//            if (isFriendly)
//            {
//                fatherRl.AddReward(hitFriendPenalty);
//                if (killed) fatherRl.AddReward(killFriendPenalty);
//            }
//            else
//            {
//                fatherRl.AddReward(hitEnemyReward);
//                if (killed) fatherRl.AddReward(killEnemyReward);
//            }
//        }

//        return true;
//    }

//    /// <summary>
//    /// 读取目标血量（兼容 Rl / NR）
//    /// 读不到返回 -1
//    /// </summary>
//    float GetHp(GameObject tankGameObject)
//    {
//        if (tankGameObject.TryGetComponent<Rl>(out var rlTank))
//        {
//            return rlTank.tankAttributes.PH;
//        }
//        if (tankGameObject.TryGetComponent<NR>(out var nrTank))
//        {
//            return nrTank.tankAttributes.PH;
//        }
//        return -1f;
//    }
//}

using UnityEngine;

public class ShellControl : MonoBehaviour
{
    public ParticleSystem shellExplosion;
    public Commo BaseFunc;

    private float explosionRadius = 20f;
    public LayerMask tankMask;

    // 发射者
    public GameObject father;
    private Rl fatherRl;

    [Header("Reward Shaping")]
    public float hitEnemyReward = 0.2f;
    public float killEnemyReward = 2.0f;
    public float hitFriendPenalty = -0.5f;
    public float killFriendPenalty = -3.0f;
    public float missPenalty = -0.05f;

    public void Set_father(GameObject fire)
    {
        father = fire;
        fatherRl = (father != null) ? father.GetComponent<Rl>() : null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider[] tankColliders = Physics.OverlapSphere(transform.position, explosionRadius, tankMask);

        int totalDamaged = 0;
        for (int i = 0; i < tankColliders.Length; i++)
        {
            bool damaged = HandleDamageAndReward(tankColliders[i].gameObject);
            if (damaged) totalDamaged++;
        }

        // 炸空：轻微惩罚
        if (fatherRl != null && totalDamaged == 0)
            fatherRl.AddReward(missPenalty);

        // 爆炸特效
        if (shellExplosion != null)
        {
            shellExplosion.transform.parent = null;
            shellExplosion.Play();
            Destroy(shellExplosion.gameObject, shellExplosion.main.duration);
        }

        Destroy(gameObject);
    }

    bool HandleDamageAndReward(GameObject tankGameObject)
    {
        bool canReward = (father != null && fatherRl != null);
        bool isFriendly = (father != null && tankGameObject.CompareTag(father.tag));

        float hpBefore = GetHp(tankGameObject);
        if (hpBefore < 0) return false;

        // 造成伤害
        if (tankGameObject.TryGetComponent<Rl>(out var rlTank))
        {
            if (!rlTank.TryGetComponent<Commo>(out var baseFunc)) return false;
            baseFunc.GetDamage(rlTank, 15);
        }
        else if (tankGameObject.TryGetComponent<NR>(out var nrTank))
        {
            if (!nrTank.TryGetComponent<Commo>(out var baseFunc)) return false;
            baseFunc.GetDamage(nrTank, 15);
        }
        else
        {
            return false;
        }

        float hpAfter = GetHp(tankGameObject);
        if (hpAfter < 0) return false;

        // 没掉血就不算命中
        if (hpAfter >= hpBefore) return false;

        bool killed = (hpAfter <= 0.0001f);

        if (canReward)
        {
            if (isFriendly)
            {
                fatherRl.AddReward(hitFriendPenalty);
                if (killed) fatherRl.AddReward(killFriendPenalty);
            }
            else
            {
                fatherRl.AddReward(hitEnemyReward);
                if (killed) fatherRl.AddReward(killEnemyReward);
            }
        }

        return true;
    }

    float GetHp(GameObject tankGameObject)
    {
        if (tankGameObject.TryGetComponent<Rl>(out var rlTank))
            return rlTank.tankAttributes.PH;
        if (tankGameObject.TryGetComponent<NR>(out var nrTank))
            return nrTank.tankAttributes.PH;
        return -1f;
    }
}

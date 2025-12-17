using System.Diagnostics;
using Unity.MLAgents.Policies;
using UnityEngine;

using Debug = UnityEngine.Debug;

public class BehaviorDump : MonoBehaviour
{
    void Start()
    {
        var bps = FindObjectsOfType<BehaviorParameters>(true); // true: 包含未激活对象
        Debug.Log($"[BP DUMP] count={bps.Length}");
        foreach (var bp in bps)
        {
            Debug.Log($"[BP DUMP] obj={bp.gameObject.name}, behavior={bp.BehaviorName}, team={bp.TeamId}, type={bp.BehaviorType}");
        }
    }
}

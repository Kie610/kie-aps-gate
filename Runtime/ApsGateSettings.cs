using UnityEngine;
using VRC.SDKBase;

namespace Kie.ApsGate
{
    /// <summary>
    /// このアバターで kieApsGate を有効にする。
    /// 付いていないアバターは `Tools > kieApsGate > プロジェクト全体で有効化` の
    /// 状態に従う（既定はオフ＝何もしない）。
    /// </summary>
    [AddComponentMenu("Kie/kieApsGate")]
    [DisallowMultipleComponent]
    public class ApsGateSettings : MonoBehaviour, IEditorOnly
    {
        [Tooltip("このアバターで constraint のゲートを行う。" +
                 "オフにすると、プロジェクト全体が有効でもこのアバターだけ除外される。")]
        public bool gateEnabled = true;

        [Tooltip("PhysBone を含むサブツリーもゲートする。ゲート対象内の PhysBone は " +
                 "Reset When Disabled が強制的にオフになり、復帰時に姿勢を捨てなくなる。" +
                 "オンにするとゲートできる constraint が大幅に増える（実測 10 → 53 個）。")]
        public bool gatePhysBoneSubtrees = false;

        [Tooltip("アバター全体の PhysBone の Reset When Disabled をオフにする。" +
                 "APS が固定で切り替えるぶんは、ゲート有効時は設定に関わらず常にその場の姿勢で" +
                 "固まる。これはそれをアバター全体 (他ギミックの PhysBone 含む) へ広げる" +
                 "オプション。PhysBone がリセットされる前提の他ギミックがある場合はオフのままにすること。")]
        public bool freezePbAtCurrentPose = false;
    }
}

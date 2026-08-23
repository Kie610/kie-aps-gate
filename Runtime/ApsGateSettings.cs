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

        [Header("実験 (分身の揺れものが本体の移動・回転に反応する問題の A/B)")]
        [Tooltip("【実験】体固定中 (かつ PB 固定解除中) だけ、固定体の PhysBone (APS_PB 複製) を " +
                 "Immobile World / 1.0 の複製へ切り替える。固定体の髪は世界基準で評価されるようになり、" +
                 "本体の移動・回転・急停止を慣性として拾わなくなる想定。未固定時 (歩いているとき) の" +
                 "髪の挙動は変わらない。効果は実機でのみ確認できる。")]
        public bool immobilizeClonePhysBones = false;

        [Tooltip("【実験】自分が移動・回転している間だけ、固定体の PhysBone (APS_PB 複製) を凍結する。" +
                 "凍結中は固定体の揺れものが完全に静止する (Reset When Disabled は強制オフ済みなので、" +
                 "止まった瞬間の形からそのまま再開する)。機構に関わらず症状を確実に止める代わりに、" +
                 "移動中は固定体の髪が揺れなくなる。")]
        public bool freezeClonePbWhileMoving = false;
    }
}

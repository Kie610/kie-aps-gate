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
    }
}

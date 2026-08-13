// AvatarPoseSystem が張るトラッキング用 constraint を、未固定中は
// GameObject ごと非アクティブにして CPU を浮かせる NDMF プラグイン。
//
// APS 本体には一切手を入れない。NDMF の AfterPlugin で後段に挟まるだけなので
// APS が更新されても追従する。
//
// == 対象の選び方(ここを間違えるとアバターが壊れる) ==
// APS は「未固定=1 / 固定=0」で constraint の m_Enabled をアニメーションする。
// つまり「固定時に APS 自身が切る constraint」＝「クローン骨格を体へ追従させる
// ためだけのもの」。これは静止時に止めても、固定直前の Prepare 2フレームで
// 復帰すれば間に合う。
//
// 逆に APS が m_Enabled を触っていない constraint (`Head_Const` など) は
// **実ボーンを保持している**。これを静止時に切ると解除後も実ボーンが
// 古いクローンに引かれたままになり、メッシュが戻らず姿勢が引き伸ばされる。
// 絶対に対象へ入れないこと。
using System.Collections.Generic;
using System.Linq;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

[assembly: ExportsPlugin(typeof(Kie.ApsGate.ApsConstraintGatePlugin))]

namespace Kie.ApsGate
{
    public class ApsConstraintGatePlugin : Plugin<ApsConstraintGatePlugin>
    {
        public override string QualifiedName => "com.kie.kie-aps-gate";
        public override string DisplayName => "kieApsGate";

        private const string ApsPlugin = "ZeroFactory.AvatarPoseSystem.NDMF";
        private const string FixParam = "APS_FixBody";
        private const string PrefKey = "ApsConstraintGate.Enabled";

        private const string MenuPath = "Tools/kieApsGate/プロジェクト全体で有効化";

        /// プロジェクト全体の既定。**既定はオフ**。
        /// アバターに kieApsGate コンポーネントが付いていればそちらが優先される。
        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, false);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath)]
        private static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static readonly HashSet<string> ConstraintTypes = new HashSet<string>
        {
            "VRCParentConstraint", "VRCRotationConstraint",
            "VRCPositionConstraint", "VRCScaleConstraint",
            "ParentConstraint", "RotationConstraint", "PositionConstraint", "ScaleConstraint",
        };

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating)
                .AfterPlugin(ApsPlugin)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Gate APS tracking constraints", Apply);
        }

        private static void Apply(BuildContext ctx)
        {
            var root = ctx.AvatarRootObject;

            // コンポーネントが付いていればそれが優先。無ければプロジェクト全体の既定
            // (既定はオフ)。つまり「入れただけでは何も起きない」。
            var settings = root.GetComponentInChildren<ApsGateSettings>(true);
            if (!(settings != null ? settings.gateEnabled : Enabled)) return;

            bool allowPb = settings != null && settings.gatePhysBoneSubtrees;

            // APS のコンポーネントはこの時点で既に消費済みなので存在チェックはしない。
            var paths = CollectGatedPaths(root);

            // APS 自身が m_IsActive を握っているパスには触らない。
            // 触ると2レイヤーが同じプロパティを取り合い、後勝ちで APS の意図が潰れる。
            var apsOwned = CollectApsIsActivePaths(root);
            int clash = paths.Count(apsOwned.Contains);
            if (clash > 0) paths = paths.Where(p => !apsOwned.Contains(p)).ToList();

            Debug.Log($"[APS Gate] 候補 {paths.Count} 件" +
                      (clash > 0 ? $" (APS が m_IsActive を持つ {clash} 件を除外)" : ""));
            if (paths.Count == 0) return;

            var gated = new List<string>();
            var gatedRoots = new List<Transform>();
            foreach (var path in paths)
            {
                var t = root.transform.Find(path);
                if (t == null || !IsSafeToGate(t, allowPb)) continue;
                gatedRoots.Add(t);
                gated.Add(path);
            }

            // クローン骨格そのもの。constraint を持たない中間オブジェクトも
            // まとめて落としたいのでサブツリーごと。
            // APS 自身が `ap.transform.Find("WorldFix/FixRoot")` と決め打ちしている名前。
            var worldFix = FindWorldFix(root.transform);
            if (worldFix == null)
            {
                Debug.LogWarning("[APS Gate] WorldFix/FixRoot が見つかりません。" +
                                 "クローン骨格側はゲートせず続行します。");
            }
            else if (!allowPb && CountPhysBones(worldFix) is var pb && pb > 0)
            {
                // 非アクティブにした PhysBone は、戻した瞬間に現在の姿勢を捨てて
                // レスト位置から初期化される。クローン骨格に PhysBone が入っていると
                // 固定した瞬間に揺れものがレスト位置で固まる(実測: ツインテールを
                // たわませた状態で固定 → レスト位置へスナップ)。
                // ここは個別 constraint と違って一括で落としていたため素通しだった。
                Debug.LogWarning(
                    $"[APS Gate] クローン骨格 '{PathOf(worldFix, root.transform)}' に PhysBone が "
                    + $"{pb} 個あるためゲートしません。"
                    + "非アクティブにすると固定時に揺れものがレスト位置で固まります。"
                    + "(kieApsGate の「PhysBone を含むサブツリーもゲートする」で有効化できます)");
            }
            else
            {
                gatedRoots.Add(worldFix);
                gated.Add(PathOf(worldFix, root.transform));
                Debug.Log($"[APS Gate] クローン骨格 '{PathOf(worldFix, root.transform)}' もゲート");
            }

            // ゲート対象に PhysBone が入るなら、復帰時に姿勢を捨てないよう
            // resetWhenDisabled を倒しておく。SetActive の前に行うこと。
            if (allowPb)
            {
                int forced = gatedRoots.Sum(ForceNoResetWhenDisabled);
                if (forced > 0)
                    Debug.Log($"[APS Gate] ゲート対象内の PhysBone {forced} 個を resetWhenDisabled=false に強制");
            }

            foreach (var t in gatedRoots) t.gameObject.SetActive(false);

            // APS の揺れ物固定(APS_FixPB)を「その場の姿勢」で固めるための強制。
            // ゲートとは独立した機能だが、同じ Fix フローの品質改善なのでここで行う。
            if (settings != null && settings.freezePbAtCurrentPose)
            {
                int n = ForceNoResetWhenDisabled(root.transform);
                Debug.Log($"[APS Gate] 揺れ物固定: アバター全体の PhysBone {n} 個を resetWhenDisabled=false に強制");
            }

            if (gated.Count == 0)
            {
                Debug.LogWarning("[APS Gate] ゲートできる対象がありませんでした。");
                return;
            }

            BuildGateLayer(ctx, root, gated);
            Debug.Log($"[APS Gate] {gated.Count} 個のトラッキング constraint を {FixParam} でゲートしました");
        }

        /// APS の生成クリップから「m_Enabled を 1 にも 0 にもされている constraint」のパスを引く。
        /// これが「クローン追従専用＝静止時に止めてよい」ものの定義。
        private static List<string> CollectGatedPaths(GameObject root)
        {
            var on = new HashSet<string>();
            var off = new HashSet<string>();
            foreach (var clip in AllClips(root))
            {
                foreach (var b in AnimationUtility.GetCurveBindings(clip))
                {
                    if (b.propertyName != "m_Enabled") continue;
                    if (b.type == null || !ConstraintTypes.Contains(b.type.Name)) continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, b);
                    if (curve == null || curve.length == 0) continue;
                    (curve.keys[0].value > 0.5f ? on : off).Add(b.path);
                }
            }
            on.IntersectWith(off);
            return on.ToList();
        }

        /// APS(や他ギミック)が m_IsActive をアニメーションしているパス
        private static HashSet<string> CollectApsIsActivePaths(GameObject root)
        {
            var set = new HashSet<string>();
            foreach (var clip in AllClips(root))
                foreach (var b in AnimationUtility.GetCurveBindings(clip))
                    if (b.propertyName == "m_IsActive" && b.type == typeof(GameObject))
                        set.Add(b.path);
            return set;
        }

        private static IEnumerable<AnimationClip> AllClips(GameObject root)
        {
            var seen = new HashSet<RuntimeAnimatorController>();
            var controllers = new List<RuntimeAnimatorController>();

            foreach (var ma in root.GetComponentsInChildren<ModularAvatarMergeAnimator>(true))
                if (ma != null && ma.animator != null) controllers.Add(ma.animator);

            var desc = root.GetComponent<VRCAvatarDescriptor>();
            if (desc != null && desc.baseAnimationLayers != null)
                foreach (var l in desc.baseAnimationLayers)
                    if (!l.isDefault && l.animatorController != null) controllers.Add(l.animatorController);

            foreach (var c in controllers)
            {
                if (c == null || !seen.Add(c)) continue;
                foreach (var clip in c.animationClips)
                    if (clip != null) yield return clip;
            }
        }

        /// クローン骨格の根を探す。
        /// 自身の名前では引かない — APS のバージョンで `WorldFix` / `APS_WorldFix` と揺れる。
        /// 子の `FixRoot` で引く(APS が `Find("WorldFix/FixRoot")` と決め打ちしている側)。
        /// MA が (MA WorldFixedRoot) 配下へ移動させるため元と移動先が同時に存在しうるので、
        /// 中身が多い方を選ぶ。
        private static Transform FindWorldFix(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Where(t => t != null && t.Find("FixRoot") != null)
                .OrderByDescending(CountLiveConstraints)
                .FirstOrDefault(t => CountLiveConstraints(t) > 0);
        }

        private static int CountLiveConstraints(Transform t)
        {
            return t.GetComponentsInChildren<Behaviour>(true)
                .Count(b => b != null && b.enabled && ConstraintTypes.Contains(b.GetType().Name));
        }

        /// サブツリー内の PhysBone / Collider の数。
        /// **enabled は見ない。** 編集時に無効でも、APS がアニメーションで有効化する
        /// ことがあるため、1 個でもあれば落とさない判断にする。
        private static int CountPhysBones(Transform t)
        {
            return t.GetComponentsInChildren<Component>(true)
                .Count(c => c != null
                            && (c.GetType().Name == "VRCPhysBone"
                                || c.GetType().Name == "VRCPhysBoneCollider"));
        }

        /// 落として安全か。見た目や物理を持つものは触らない。
        private static bool IsSafeToGate(Transform t, bool allowPb)
        {
            foreach (var c in t.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                var n = c.GetType().Name;
                if (n == "SkinnedMeshRenderer" || n == "MeshRenderer" ||
                    n == "ParticleSystem" || n == "Light" || n == "Camera" ||
                    n == "Animator" || n == "AudioSource")
                    return false;
                // PhysBone があるなら止めてはいけない。**enabled は見ない。**
                // 0.2.0-alpha までは enabled なものだけ弾いていたが、APS のクローン骨格の
                // PhysBone は編集時に無効で、固定時にアニメーションで有効化される。
                // 非アクティブのまま戻すと現在の姿勢を捨ててレスト位置から初期化されるので、
                // 固定した瞬間に揺れものが固まる。
                // gatePhysBoneSubtrees のときは resetWhenDisabled を倒したうえで
                // ゲートするので許可する。
                if (!allowPb && (n == "VRCPhysBone" || n == "VRCPhysBoneCollider"))
                    return false;
            }
            return true;
        }

        /// サブツリー内の全 VRCPhysBone の resetWhenDisabled を false にする。戻り値は変更数。
        private static int ForceNoResetWhenDisabled(Transform t)
        {
            int changed = 0;
            foreach (var b in t.GetComponentsInChildren<Behaviour>(true))
            {
                if (b == null || b.GetType().Name != "VRCPhysBone") continue;
                var f = b.GetType().GetField("resetWhenDisabled");
                if (f == null) continue;
                if ((bool)f.GetValue(b)) { f.SetValue(b, false); changed++; }
            }
            return changed;
        }

        private static string PathOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            while (t != null && t != root) { parts.Insert(0, t.name); t = t.parent; }
            return string.Join("/", parts);
        }

        /// m_IsActive を APS_FixBody で 0/1 する 1 レイヤーを FX へ合流させる
        private static void BuildGateLayer(BuildContext ctx, GameObject root, List<string> paths)
        {
            var off = new AnimationClip { name = "APSGate_Off" };
            var on = new AnimationClip { name = "APSGate_On" };
            foreach (var p in paths)
            {
                off.SetCurve(p, typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0f, 0f, 0f));
                on.SetCurve(p, typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0f, 0f, 1f));
            }

            var ac = new AnimatorController { name = "APSGate" };
            ac.AddParameter(FixParam, AnimatorControllerParameterType.Bool);
            ac.AddLayer("APSGate");
            var sm = ac.layers[0].stateMachine;

            var sOff = sm.AddState("Off", new Vector2(300, 0));
            sOff.motion = off;
            sOff.writeDefaultValues = false;
            var sOn = sm.AddState("On", new Vector2(300, 80));
            sOn.motion = on;
            sOn.writeDefaultValues = false;
            sm.defaultState = sOff;

            AddTransition(sOff, sOn, true);
            AddTransition(sOn, sOff, false);

            var layers = ac.layers;
            layers[0].defaultWeight = 1f;
            ac.layers = layers;

            ctx.AssetSaver.SaveAsset(off);
            ctx.AssetSaver.SaveAsset(on);
            ctx.AssetSaver.SaveAsset(ac);
            foreach (var l in ac.layers) ctx.AssetSaver.SaveAsset(l.stateMachine);

            var holder = new GameObject("APSGate");
            holder.transform.SetParent(root.transform, false);
            var merge = holder.AddComponent<ModularAvatarMergeAnimator>();
            merge.animator = ac;
            merge.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            merge.deleteAttachedAnimator = true;
            merge.pathMode = MergeAnimatorPathMode.Absolute;
            merge.matchAvatarWriteDefaults = false;
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, bool wanted)
        {
            var tr = from.AddTransition(to);
            tr.hasExitTime = false;
            tr.duration = 0f;
            tr.AddCondition(wanted ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, FixParam);
        }
    }
}

// AvatarPoseSystem の後段に挟まる NDMF プラグイン。役割は 2 つ。
//
// 1. ゲート (CPU 削減): APS が張るトラッキング用 constraint を、未固定中は
//    GameObject ごと非アクティブにして CPU を浮かせる。
// 2. PB 固定品質: APS は全 PhysBone を `APS_PB` 子オブジェクトへ複製して元を破棄し、
//    固定時にその複製を m_IsActive=0 で切る。resetWhenDisabled が ON の複製は
//    **切られた瞬間にレスト位置へ戻ってから固まる** (ミルフィ実測: 39 個中 19 個が ON、
//    変位の約 7 割を喪失)。APS が固定で切り替える複製に限って resetWhenDisabled を
//    倒し、「固定した瞬間の形」で固まる・解除時はその姿勢から物理が再開する、へ直す。
//    APS 作者自身がこの強制を一度実装しており (現在はコメントアウトで利用者判断に
//    委ねられている)、後段から書くのは設計意図の範囲内。
//
// APS 本体には一切手を入れない。NDMF の AfterPlugin で後段に挟まるだけなので
// APS が更新されても追従する。壊れる条件: APS が複製名 `APS_PB` を変える /
// 切り替えを m_IsActive 以外にする / FixBody のパラメータ名を変える —
// いずれも検出できず素通しになるだけで、アバターは壊さない (警告を出す)。
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

            // == PB 固定品質 (常時・ゲートとは独立) ==
            // APS が固定時に m_IsActive で切り替える PhysBone 複製 (APS_PB) の
            // resetWhenDisabled を倒す。ON のままだと「固定した瞬間にレスト位置へ
            // 戻ってから固まる」。倒すと固定した瞬間の形で固まり、解除時はその姿勢から
            // 物理が再開する (レストへは戻さない)。
            // 対象は APS の生成クリップから引いた複製に限る — アバターの他の PhysBone
            // (他ギミックがリセット前提で切り替えるもの) には触らない。
            // アバター全体へ広げたい場合だけ freezePbAtCurrentPose を使う。
            var pbPaths = CollectApsFixedPbPaths(root);
            if (pbPaths.Count == 0)
            {
                Debug.LogWarning("[APS Gate] APS が固定時に切り替える PhysBone 複製 (APS_PB) が" +
                                 "見つかりません。APS の版が想定と違う可能性があります" +
                                 " (PB 固定品質の強制はスキップ。ゲートは続行)");
            }
            else
            {
                int frozen = 0;
                foreach (var p in pbPaths)
                {
                    var t = root.transform.Find(p);
                    if (t != null) frozen += ForceNoResetWhenDisabled(t);
                }
                Debug.Log($"[APS Gate] PB 固定品質: APS が切り替える複製 {pbPaths.Count} 箇所のうち " +
                          $"{frozen} 個を resetWhenDisabled=false に強制" +
                          " (固定した瞬間の形で固まり、解除時はその姿勢から再開)");
            }

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

            // 「固定体の揺れものが本体の移動・回転を慣性として拾う」問題への対策は
            // 2026-08-23 の実機 A/B (round 1/2) の結果、両案とも不採用で撤去した。
            // 機序と結論は CHANGELOG の「調査記録」を参照 (Immobile World は世界固定
            // ボーンへの慣性注入を打ち消さない / 移動中凍結は動作するが UX 上不採用)。

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
            // パスを出すのは切り分けのため — 件数だけだと「何が止まったのか」を
            // あとから確かめる手段が無い (2026-08-23 の調査で困った)
            Debug.Log($"[APS Gate] {gated.Count} 個のトラッキング constraint を {FixParam} でゲートしました:\n  "
                      + string.Join("\n  ", gated));
        }

        /// APS が固定時に m_IsActive で切り替える PhysBone 複製 (`APS_PB`) のパス。
        /// constraint と同じく「1 にも 0 にもされている」ことを条件にする —
        /// 片側しか無いものは切り替えではない。名前 `APS_PB` は APS が
        /// `pb.transform.Find("APS_PB")` と決め打ちしている側の定数。
        private static List<string> CollectApsFixedPbPaths(GameObject root)
        {
            var on = new HashSet<string>();
            var off = new HashSet<string>();
            foreach (var clip in AllClips(root))
            {
                foreach (var b in AnimationUtility.GetCurveBindings(clip))
                {
                    if (b.propertyName != "m_IsActive" || b.type != typeof(GameObject)) continue;
                    if (b.path != "APS_PB" && !b.path.EndsWith("/APS_PB")) continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, b);
                    if (curve == null || curve.length == 0) continue;
                    (curve.keys[0].value > 0.5f ? on : off).Add(b.path);
                }
            }
            on.IntersectWith(off);
            return on.ToList();
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

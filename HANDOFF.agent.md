# Agent handoff v1

updated: 2026-08-23
repo: D:/GitHub_WorkSpace/VRC/Packages/com.kie.kie-aps-gate (origin = github.com/Kie610/kie-aps-gate)
work_branch: main
upstream: origin/main (同期済み・0.5.0-alpha は push 済み / Release 未作成)
base: main@2e1522b
goal: APS の追従 constraint を未固定中だけ止め、固定時は揺れものを「その場の形」で固める

## State

complete:
- C: [Unreleased] 実験フラグ 2 本 (既定オフ・**分身の揺れものが本体の移動・回転を
  慣性として拾う問題**の実機 A/B 用)。`immobilizeClonePhysBones` = 分身側 PB を
  Immobile World / 1.0 へ強制 / `freezeClonePbWhileMoving` = 移動・回転中だけ分身 PB を
  凍結 (Velocity/AngularY 判定・しきい値 0.1 m/s / 15 deg/s)。**A/B の結果が出るまで
  リリースしない**
- C: 0.5.0-alpha (ローカル)。「ポーズを固定した瞬間に揺れものがレスト状態になる」を
  設定なしで直した回。APS が固定時に切り替える PhysBone 複製 (`APS_PB`) に限って
  `resetWhenDisabled` を自動で倒す (ゲート有効時の常時動作)。コンポーネント未設置
  (プロジェクト全体で有効化) でも効く — 0.4.0-alpha はこの経路で PB 対策が黙って無効だった
- C: 0.4.0-alpha 公開済み (Reset When Disabled で PhysBone サブツリーも安全に落とせる回)
- C: 既定オフ + コンポーネント / 一括メニューでの有効化 (0.2.0-alpha)

verified:
- C: 2026-08-23 — evidence: status=PASS; kind=runtime; command=ApsGateBuildTest.Run
  (DevProject・unity-gate 経由); scope=実験フラグ 2 本の NDMF 実ビルド構造検証
  (シナリオ C: 分身 73 個の immobileType=World & immobile=1.0 / 凍結レイヤーの FX 合流 /
  Velocity・AngularY パラメータ / 凍結クリップのカーブ 73 = 分身 PB 全数) + 既存 A/B 回帰;
  counts=**18 / 18 PASS**。**実機での効果判定は未実施** (下記 not-run)
- C: 2026-08-23 — evidence: status=PASS; kind=runtime(実機); command=VRChat へ
  アップロードして目視 (ユーザー実施); scope=固定した瞬間の揺れものの形の保持と
  解除後の再開、および 2 窓起動のリモート側での見え方; counts=目視 OK
  (ローカル / リモートとも問題なし)
- C: 2026-08-23 — evidence: status=PASS; kind=runtime; command=Unity.exe -batchmode
  -executeMethod ApsGateBuildTest.Run (DevProject); environment=Windows 11 / Unity 2022.3.22f1
  batchmode / NDMF フルビルド (AAO Trace&Optimize はテスト複製から除外);
  scope=Milfy Variant + APS プレハブ素置き (症状の出た構成の再現) のシナリオ 2 本
  (A: コンポーネント有り / B: 無し + プロジェクト全体で有効化);
  counts=passed=11, failed=0。要点: APS_PB 複製 40 個の resetWhenDisabled がすべて false
  (ON だった 20 個を強制)・A でクローン骨格ゲート + APSGate 層合流 (51 constraint)・
  B でクローン骨格は非ゲート (73 PB 保護) + 8 constraint
- C: 2026-08-19 — evidence: status=PASS; kind=runtime; scope=本番アバターでの動作確認; counts=止められたゲート数 53
- C: 0.4.0-alpha 時の Play 実測: resetWhenDisabled=false なら揺れ物固定で姿勢 100% 保持
  (固定中ドリフト 0.0°)。0.5.0-alpha の常時強制は同じ機構の適用範囲を変えただけ

not-run:
- U: **実験フラグの実機 A/B** (ユーザーのみ実施可能)。手順:
  1. `kieApsGate` コンポーネントで `immobilizeClonePhysBones` だけ ON → アップロード →
     体固定 + PB 固定解除 → その場回転・接近・周回で分身の髪の流れを見る
  2. 直れば案1採用 (凍結フラグは削除)。直らなければ `freezeClonePbWhileMoving` も ON →
     移動中に凍結されること・停止後に自然へ戻ることを確認
  3. どちらも不発なら kieApsGate では塞げない結論 → VRChat SDK へフィードバック
     (最小再現: 素のアバター + MA World Fixed Object の箱 + 髪チェーン PB 1 本、
     その場回転で流れる)。APS 作者へも「分身 PB へ Immobile World を検討」と報告可能
- U: 凍結しきい値 (0.1 m/s / 15 deg/s) の実機調整。歩き出しの取りこぼし・
  微動での発火があれば定数を直す

## Decisions

- C: APS 本体へ手を入れず、NDMF の `AfterPlugin` で後段に挟まる (不変)
- C: 2026-08-23 — **PB 固定品質はゲート有効時の常時動作** (作り直しはしない判断)。
  機序: APS は全 PhysBone を `APS_PB` 複製へ移して元を破棄し、固定時に複製を
  m_IsActive=0 で切る。resetWhenDisabled が ON の複製は切られた瞬間にレスト位置へ
  戻ってから固まる (ミルフィ 40 個中 20 個が ON = 症状の主因。ゲートした constraint は
  無関係)。APS 作者自身が reset 強制を一度実装しコメントアウトで利用者判断に
  委ねている (AvatarPoseSystemPlugin.cs:1607) ため、後段から書くのは設計意図の範囲内
- C: 対象は APS の生成クリップから引いた複製に限る (他ギミックの PhysBone に触らない)。
  `freezePbAtCurrentPose` は「アバター全体へ広げる」オプションとして存続。
  **3 フィールドとも名前・型・既定値は不変** (公開契約変更なし)
- C: 壊れる条件 = APS が複製名 `APS_PB` / m_IsActive 切り替え / FixBody パラメータ名を
  変えたとき。いずれも警告を出して素通し (アバターは壊さない)
- C: 既定はオフ (0.2.0-alpha の決定・不変)
- C: 2026-08-23 — 分身慣性問題の設計判断。**「AllMotion ⊇ World だから World は無効」を
  棄却**: 公式文の両者は包含でなく基準系が別 (AllMotion = root の親 / World = シーン
  ルート)。分身の親は constraint 補正済みで AllMotion は測る動きが無い = 効かないのが
  仕様どおりで、World が効く余地は残る (ワールド固定小物の定石とも一致)。撤回済みの
  Immobile World 案を実験フラグとして復活し、機構非依存の凍結案と実機 A/B する。
  PB の慣性基準は DLL 非公開 + Emulator 再現不能のため、**A/B 自体を機構検証を兼ねる
  実験として設計** (推測で単一案に張らない)

## Next

- Release: AGENTS.md の手順 1 (version と CHANGELOG を合わせてコミット) と push は
  2026-08-23 に完了 (main@bd5030f)。**残りは Actions の `Build Release`
  (workflow_dispatch) を手で実行するところから**
- 検証ハーネス: `DevProject/Assets/kieApsGateDebug/` (専用シーン
  kieApsGate_Test.unity + ApsGateBuildTest。AAO T&O はバッチで PhysBone を全削除する
  ためテスト複製から外している)

## Paths

- C: `Editor/ApsConstraintGate.cs` — 判定・停止・PB 固定品質の実装
- C: `Runtime/ApsGateSettings.cs` — 利用者が置くコンポーネント
- C: `../../DevProject/Assets/kieApsGateDebug/` — 検証ハーネス (別リポジトリ)

# Agent handoff v1

updated: 2026-08-23
repo: D:/GitHub_WorkSpace/VRC/Packages/com.kie.kie-aps-gate (origin = github.com/Kie610/kie-aps-gate)
work_branch: main
upstream: origin/main (0.5.0-alpha はローカルのみ・未 push)
base: main@2e1522b
goal: APS の追従 constraint を未固定中だけ止め、固定時は揺れものを「その場の形」で固める

## State

complete:
- C: 0.5.0-alpha (ローカル)。「ポーズを固定した瞬間に揺れものがレスト状態になる」を
  設定なしで直した回。APS が固定時に切り替える PhysBone 複製 (`APS_PB`) に限って
  `resetWhenDisabled` を自動で倒す (ゲート有効時の常時動作)。コンポーネント未設置
  (プロジェクト全体で有効化) でも効く — 0.4.0-alpha はこの経路で PB 対策が黙って無効だった
- C: 0.4.0-alpha 公開済み (Reset When Disabled で PhysBone サブツリーも安全に落とせる回)
- C: 既定オフ + コンポーネント / 一括メニューでの有効化 (0.2.0-alpha)

verified:
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
- U: 実機 (VRChat) での目視 — 固定した瞬間の揺れものの形の保持と解除後の再開。
  ユーザーのみ実施可能。手順: Milfy Variant + APS のアバターで、髪を揺らした状態で
  ポーズ固定 → 形が保たれること / 解除 → その姿勢から揺れが再開すること

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

## Next

- 実機目視 (上記 not-run) → 問題なければ Release 手順 (AGENTS.md) へ
- 検証ハーネス: `DevProject/Assets/kieApsGateDebug/` (専用シーン
  kieApsGate_Test.unity + ApsGateBuildTest。AAO T&O はバッチで PhysBone を全削除する
  ためテスト複製から外している)

## Paths

- C: `Editor/ApsConstraintGate.cs` — 判定・停止・PB 固定品質の実装
- C: `Runtime/ApsGateSettings.cs` — 利用者が置くコンポーネント
- C: `../../DevProject/Assets/kieApsGateDebug/` — 検証ハーネス (別リポジトリ)

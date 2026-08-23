# Agent handoff v1

updated: 2026-08-23
repo: D:/GitHub_WorkSpace/VRC/Packages/com.kie.kie-aps-gate (origin = github.com/Kie610/kie-aps-gate)
work_branch: main
upstream: origin/main (同期済み・0.5.0-alpha は push 済み / Release 未作成)
base: main@2e1522b
goal: APS の追従 constraint を未固定中だけ止め、固定時は揺れものを「その場の形」で固める

## State

complete:
- C: [Unreleased] 凍結レイヤーの**永久凍結バグ修正** (実機 round 2 で発現)。Idle が
  空クリップで m_Enabled を書き戻す者がいなかった → Idle 側で明示的に 1 を書き戻す。
  併せて他ギミックが m_Enabled をアニメーションしている PB・ビルド時無効の PB を
  対象から除外
- C: **実機 A/B round 2 の結果** (ユーザー実施): 案1 (Immobile World 切替) は
  対象・配管が正しい状態で**不発が確定** → 削除予定 (契約変更のため承認待ち)。
  案2 (移動中凍結) は上記バグで判定不能 → 修正後の再テスト待ち
- C: [Unreleased] 実験フラグ 2 本の **round 2 再実装** (既定オフ・実機 A/B 用)。
  round 1 は実機で両方不発 — 原因は対象の取り違え (APS_WorldFix 配下の 73 個は
  すべてポーズ操作ハンドルで、固定体の髪は**本体側 APS_PB 複製**が揺らしている)。
  `immobilizeClonePhysBones` = APS_PB ごとに Immobile World / 1.0 の複製
  (APSGate_PB_World) を作り、体固定中 (かつ PB 固定解除中) だけ FX レイヤー
  (APSGatePbWorld) で交差切替。未固定時の通常挙動は不変 /
  `freezeClonePbWhileMoving` = 凍結レイヤーを APS_PB + World 複製へ再照準
  (しきい値 0.1 m/s / 15 deg/s は据え置き)。**A/B の結果が出るまでリリースしない**
- C: 0.5.0-alpha (ローカル)。「ポーズを固定した瞬間に揺れものがレスト状態になる」を
  設定なしで直した回。APS が固定時に切り替える PhysBone 複製 (`APS_PB`) に限って
  `resetWhenDisabled` を自動で倒す (ゲート有効時の常時動作)。コンポーネント未設置
  (プロジェクト全体で有効化) でも効く — 0.4.0-alpha はこの経路で PB 対策が黙って無効だった
- C: 0.4.0-alpha 公開済み (Reset When Disabled で PhysBone サブツリーも安全に落とせる回)
- C: 既定オフ + コンポーネント / 一括メニューでの有効化 (0.2.0-alpha)

verified:
- C: 2026-08-23 — evidence: status=PASS; kind=runtime; command=ApsGateBuildTest.Run
  (DevProject・unity-gate 経由); scope=round 2 の NDMF 実ビルド構造検証。
  A: 実ボーン切替 (_Const → fix 骨格) の constraint 49 個 = 機構読解の固定化 + 既存回帰 /
  C: APS_PB 40 : World 複製 40 の 1:1、World/1.0・reset 無効・既定非アクティブ 40/40、
  切替クリップ 80 カーブ (=2N)、凍結クリップ 80 カーブ (=APS_PB+World)、
  ハンドルへの World 強制ゼロ /
  D: DLC 併用 (ExtraBone + PropPlacer 実物) でも 1:1 維持・DLC ハンドル生成無傷;
  counts=**32 / 32 PASS**。**実機での効果判定は未実施** (下記 not-run)
- C: 2026-08-23 — 実機 A/B round 1 (ユーザー実施): 両フラグとも**不発**。
  対象取り違えが原因 (Decisions 参照)。機構仮説 (Immobile World) の反証にはならない
- C: 2026-08-23 — DLC 3 種 (ExtraBone 1.0.2 / PropPlacer 2.0.0 / AlterBody 2.1.0) を
  KonoAsset (D:\DataOkiba\...\_tmp) から DevProject へ導入済み。検索は
  `_tools/konoasset-search/search.py`
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
- U: **案2 (移動中凍結) の実機再テスト** (永久凍結バグ修正後)。手順:
  `freezeClonePbWhileMoving` だけ ON (`immobilizeClonePhysBones` は OFF — 不発確定済み)
  → アップロード → 体固定 + PB 固定解除 → 移動・回転中に固定体の髪が止まること /
  **静止したら数秒以内に揺れが再開すること** (前回はここが永久に固まった) を確認
  - 効けば案2採用 → 案1の削除と案2の扱い (実験卒業) をユーザーと確定
  - 効かなければ kieApsGate では塞げない結論 → VRChat SDK へフィードバック
    (最小再現: 素のアバター + MA World Fixed Object の箱 + 髪チェーン PB 1 本、
    その場回転で流れる)。代替は ExtraBone ワークフロー (揺れ対象を手動でハンドル化)
- U: PB 固定 (APS_FixPB) の ON/OFF を挟んだときの切替往復 (World⇄通常) で形が
  破綻しないかの実機目視
- U: 凍結しきい値 (0.1 m/s / 15 deg/s) の実機調整。歩き出しの取りこぼし・
  微動での発火があれば定数を直す
- U: AlterBody 併用構成の検証 (Known limitations 参照。別アバターの用意が要る)

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
- C: 2026-08-23 — **round 2 の対象再選定** (プラグイン読解 + PB 監査全件集計で確定)。
  APS の体固定は実ボーンの駆動元切替 (BoneProxy + `<骨名>_Const` の constraint、
  AvatarPoseSystemPlugin.cs 2101-2153)。固定体 = 実メッシュ + 実 PB (APS_PB 複製)、
  歩く方 = ゴーストのプロキシ体。APS_WorldFix 配下の 73 個は全てポーズ操作ハンドル
  (Main 51 / Sub 19 / 専用 3。クロス参照 0)。よって round 1 の不発は Immobile World の
  反証ではなく対象違い。round 2 は APS_PB を対象に、静的変更でなく**複製の交差切替**
  にする (静的に World へ変えると未固定時の通常の髪挙動まで変わるため — APS 作者が
  直さない理由もおそらくこれ)。フィールド名は同名のまま再実装 (未リリースのため
  契約変更に当たらない。「分身の PB」という意味は固定体=実 PB と分かった今むしろ正確)
- C: 2026-08-23 — ユーザー仮説の採否: 「ExtraBone があれば PB 固定は不要」=不採用
  (APS_PB 複製は凍結/解凍切替の実装手段で必要) / 「PB 固定解除の目的=矛盾解消」=
  一部採用 (「全 PB 一律複製は過剰」は不採用) / 「調整したい PB にだけ ExtraBone」=
  採用 (推奨ワークフロー。ExtraBone 化したチェーンはハンドル化されライブシミュレーション
  から外れる = 症状も出ない。AvatarPoseSystemPlugin.cs:1502 で元 PB の root はクローンへ
  付け替え)

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

# Agent handoff v1

updated: 2026-08-19
repo: D:/GitHub_WorkSpace/VRC/Packages/com.kie.kie-aps-gate (origin = github.com/Kie610/kie-aps-gate)
work_branch: main
upstream: origin/main (同期済み)
base: main@2e1522b
goal: AvatarPoseSystem の追従 constraint を、ポーズを固定していない間だけ止める

## State

complete:
- C: 0.4.0-alpha 公開済み。Reset When Disabled で PhysBone サブツリーも安全に落とせるようにした回
- C: 既定オフ + コンポーネント / 一括メニューでの有効化 (0.2.0-alpha)

verified:
- C: 2026-08-19 — evidence: status=PASS; kind=runtime; scope=本番アバターでの動作確認; counts=止められたゲート数 53 (0.3.0-alpha で PhysBone を避けていたぶんが回復)

not-run:
- C: none

## Decisions

- C: APS 本体へ手を入れず、NDMF の `AfterPlugin` で後段に挟まる。APS の更新へ追従できる状態を保つため
- C: 既定はオフ。アバターによっては固定中の挙動に影響が出るため、利用者が明示的に有効化する

## Next

- 追加の作業予定なし。APS 側の更新があったときに追従を確認する

## Paths

- C: `Editor/ApsConstraintGate.cs` — 判定と停止の実装
- C: `Runtime/ApsGateSettings.cs` — 利用者が置くコンポーネント
- C: `../../DevProject` — Unity 検証プロジェクト

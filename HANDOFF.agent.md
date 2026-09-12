# Agent handoff v1

updated: 2026-09-13
repo: D:/GitHub_WorkSpace/VRC/kie-packages/com.kie.kie-aps-gate (origin = github.com/Kie610/kie-aps-gate)
work_branch: main
upstream: origin/main@a44fcd3
base: main@d969dcc
goal: APS の追従 constraint を未固定中だけ止め、固定時は揺れものを「その場の形」で固める

## State

complete:
- A: 2026-08-23 の旧 handoff には 1.0.0 リリース済み、開発終了、0.5.0-alpha の修正済みと記録されている。現在の外部公開状態は今回再確認していない。原文は `docs/handoff-history.md` に保存した。

verified:
<!-- 過去の自由文記録は現行 evidence fields へ再検証していないため空欄 -->

not-run:
- U: U1 過去の runtime 検証記録は、原文に自由文の折返しや旧 counts 表記が含まれるため、status・kind・command・environment・scope・counts をすべて機械的に確定できない。原文は履歴に保存し、今回の再検証は未実施。
- U: U2 AlterBody 併用検証は別アバターが必要なため保留。
- U: U3 VRChat SDK へのフィードバックは未実施。
- U: U4 今回は管理文書のみの更新であり、Unity・compile・runtime・hardware 検証は未実行。

- U: U5 バックアップ保存先未指定のため今回未実施。親AGENTSの対象表に従い、保存先や外部権限を推測で追加しない。

## Decisions

- C: APS 本体へ手を入れず、NDMF の `AfterPlugin` で後段に挟まる。
- C: 既定はオフ。`ApsGateSettings` または一括メニューで指定したアバターだけをビルド時に対象にする。
- C: `APS_PB` の複製に限って `resetWhenDisabled` を扱い、他ギミックの PhysBone には触れない。
- C: 0.2.0-alpha 以降の公開契約、パッケージ ID、利用者向けフィールド名は `AGENTS.md` を正本とする。
- C: 追加機能は終了し、以後は APS 側の更新追従と不具合修正を行う。
- C: 手順パッケージの採否、適合、不採用理由は `docs/handoff-history.md` の `## Migration record` を正本とする。

## Next

- APS 側の更新追従または不具合修正が必要になったら、`AGENTS.md` の契約と `docs/agent-appendix.md` の該当手順を読み、avatar-dev の複製アバターで検証する — blocked-by: none
- 過去の runtime 証拠を現行 schema で再利用する必要が生じたら、原文と実行ログを照合して不足 fields を埋める — blocked-by: U1
- AlterBody 併用検証を行う場合は、別アバターを用意してから実施する — blocked-by: U2

## Paths

- C: `AGENTS.md` — 常時守る規則、公開契約、検証境界、handoff の正本と更新責任。
- C: `HANDOFF.md` — handoff 文書へのリンクだけを置く index。
- C: `docs/agent-appendix.md` — 作業別の参照条件、検証、統合と更新手順。
- C: `docs/handoff-history.md` — 旧 handoff 本文、経緯、手順適用記録。
- C: `CHANGELOG.md` — リリース履歴と慣性問題の調査記録。
- C: `Editor/ApsConstraintGate.cs` — 判定・停止・PB 固定品質の実装。
- C: `Runtime/ApsGateSettings.cs` — 利用者が置くコンポーネント。
- C: `../../avatar-dev/Assets/kieApsGateDebug/` — 検証ハーネス。

## Resume protocol

1. `AGENTS.md` を読み、実装・仕様・検証の変更対象に応じて `docs/agent-appendix.md` の該当節だけを読む。
2. 現在の仕様・操作・契約は `AGENTS.md` と `README.md`、リリース経緯は `CHANGELOG.md`、現在の進捗・判断・未解決事項はこの handoff、過去の状態は `docs/handoff-history.md` から直接確認する。
3. `git status -sb` と live の HEAD/upstream を確認し、既存 dirty を保全する。アバター実装を変更した場合だけ `../../avatar-dev` の複製で compile・runtime・hardware の証拠を分けて記録する。
4. 子担当は割当範囲だけを変更し、共有状態・検証結果・未解決事項を親へ返す。親が差分と証拠を検品して統合し、handoff の State/Decisions/Next と schema を保つ。
5. push・公開・Release・外部書込みは別途明示許可がある場合だけ行う。

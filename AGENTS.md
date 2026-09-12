# Agent instructions

kieApsGate（`com.kie.kie-aps-gate`）のリポジトリ。**このフォルダのルートがそのまま
VPM パッケージ**である。

## Scope and precedence

ユーザーの明示指示、この文書、既存コード、の順に優先する。
ワークスペース全体の振り分け規則は `../../AGENTS.md` にある。

このリポジトリは実装だけを持つ。**Unity 上での検証は `../../avatar-dev` で行う**
（`file:` 参照で読まれている）。

## Project contract

利用者はアバター制作者。公開契約は次の 3 つ。

- コンポーネント `ApsGateSettings`（`Runtime/ApsGateSettings.cs`）とそのフィールド名
- ビルド後のふるまい（未固定中は APS の追従 constraint が止まり、固定すると元に戻る）
- パッケージ ID `com.kie.kie-aps-gate`

`Editor/ApsConstraintGate.cs` の内部構成は自由に変えてよい。

## Dependencies and prior art

- `com.vrchat.avatars` ^3.7.0 / `nadena.dev.ndmf` ^1.14.0 / `nadena.dev.modular-avatar` ^1.10.0
- **対象は AvatarPoseSystem（ZeroFactory）の生成物**。APS は購入アセットで、
  このリポジトリには含まれない
- NDMF の `AfterPlugin` で APS の後段に挟まる

## Invariants

- **APS 本体へ手を入れない。** 後段で結果を書き換えるだけにする。APS が更新されても
  追従できる状態を保つことが、この設計の目的そのものである
- 既定はオフ。`ApsGateSettings` を置いたアバター、または一括メニューで指定したアバターに限り、
  ビルド時に APS の追従 constraint を停止する（0.2.0-alpha で決定）
- PhysBone のサブツリーを落とすときは `Reset When Disabled` の扱いを壊さない
  （0.3.0-alpha で「固定時にレスト位置で固まる」不具合を踏んでいる）

## Change scope

- 1 つの変更で触るのは 1 つの関心事に限る
- `CHANGELOG.md` は版ごとに書く。ふるまいが変わる修正は、どう変わるかを利用者の言葉で書く

## Safety and truth

- 実際に実行した検査だけを報告する。skip した検査と未実行の検査は PASS ではない。
  合否は件数付きで書く
- 実アバターを対象にする検証は必ず複製へ行う
- 明示的な権限なしに push、Release 作成、公開、remote 変更を行わない

## Commands

Unity の検証は `../../avatar-dev` を開いて行う。APS の実物が要るため、購入アセットの
入ったプロジェクトでしか確かめられない検証がある。その場合は対象と手順を報告に書く。

## Release

1. `package.json` の `version` と `CHANGELOG.md` の見出しを合わせてコミットする
2. push 後、GitHub Actions の `Build Release`（`workflow_dispatch`）を手で実行する
3. `../../vpm-listing` の `Build Repo Listing` が Release を拾う

## Handoff maintenance

現在の状態は `HANDOFF.agent.md` が正本。作業メモをワークスペース直下へ置かない。
過去の状態・経緯と手順適用記録は `docs/handoff-history.md` に保存する。
`HANDOFF.agent.md` の更新時は、`C`/`A`/`U`、実行済み検証の件数、未実行項目、`Next` の
`blocked-by` を明記し、実際の Git 状態を優先する。文書の変更後は
`C:/Users/Kie/.codex/skills/agent-handoff/scripts/validate_handoff.py --root .` と
`git diff --check` を実行する。

文書整備・委任・並行実装・リリース前は `docs/agent-appendix.md` の該当節だけを読む。担当者は変更した現行仕様・操作を既存の正本へ反映し、状態の変化をHANDOFFへ記録する。共有状態は親が検品して統合する。境界ごと・検品後・終了前に更新要否を確認し、実質的な変化がない日時更新はしない。

## Design priorities

動作の維持、公開契約、現在要件を満たす最小実装、長期的整合の順で判断する。将来の要件を先取りしない。

## Design

端から端まで動く最小構成を保ち、機能を一つずつ足す。未完成の作り直しで既存を置き換えない。不要な抽象化や依存を追加せず、既存機能・導入済み依存を先に確認する。内部実装の互換層を不要に残さず、公開契約の変更には移行経路を用意する。内部か公開か不明な場合は削除せず確認する。

- READMEへAPS標準仕様の解説（PB固定・除外設定）を書かない（既存のユーザー制約）。

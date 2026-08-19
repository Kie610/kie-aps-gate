# Agent instructions

kieApsGate（`com.kie.kie-aps-gate`）のリポジトリ。**このフォルダのルートがそのまま
VPM パッケージ**である。

## Scope and precedence

ユーザーの明示指示、この文書、既存コード、の順に優先する。
ワークスペース全体の振り分け規則は `../../AGENTS.md` にある。

このリポジトリは実装だけを持つ。**Unity 上での検証は `../../DevProject` で行う**
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

Unity の検証は `../../DevProject` を開いて行う。APS の実物が要るため、購入アセットの
入ったプロジェクトでしか確かめられない検証がある。その場合は対象と手順を報告に書く。

## Release

1. `package.json` の `version` と `CHANGELOG.md` の見出しを合わせてコミットする
2. push 後、GitHub Actions の `Build Release`（`workflow_dispatch`）を手で実行する
3. `../../vpm-listing` の `Build Repo Listing` が Release を拾う

## Handoff maintenance

現在の状態は `HANDOFF.agent.md` が正本。作業メモをワークスペース直下へ置かない。

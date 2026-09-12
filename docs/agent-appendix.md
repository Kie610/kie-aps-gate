# Agent appendix

この補遺は `AGENTS.md` から参照する作業別手順の正本です。常時守る規則と公開契約は `AGENTS.md`、現在状態は `HANDOFF.agent.md`、リリース経緯は `CHANGELOG.md` が正本です。過去の本文と手順適用記録は `docs/handoff-history.md` に保存します。

## 読む条件

- 実装変更: `AGENTS.md` の契約・不変条件を確認し、対象コードと既存テストへ直接進む。
- アバター動作変更: `../../docs/agent-operations.md` の「アバターの動作検証」を読み、`../../avatar-dev` の複製で検証する。
- handoff 更新・引き継ぎ: `HANDOFF.agent.md` と agent-handoff schema を読む。参照先が既知なら直接進み、履歴は現行資料で根拠不足・矛盾がある場合だけ読む。
- 共有状態の統合: 親が差分、コマンド、件数、未実行理由、未解決事項を検品してから handoff を更新する。

## 文書の役割と更新責任

- `AGENTS.md`: 常時守る規則、公開契約、検証境界。担当者が規則または参照条件を変更したとき更新する。
- `HANDOFF.agent.md`: 現在の State/Decisions/Next。作業担当者が実際の進捗、判断、証拠、未解決事項の変化後に更新する。
- `HANDOFF.md`: handoff のリンク index。文書の追加・移動時に更新する。
- `docs/handoff-history.md`: 過去本文と移行記録。root handoff を短縮・schema 変換するとき、原文と procedure を保存する。
- `docs/agent-appendix.md`: 作業別の読む条件と統合手順。運用ルールを変更するとき更新する。

## 検証と真実性

`validate_handoff.py --root <repository-root>`、`git diff --check`、限定 diff、Git status を実行する。compile・runtime・hardware は分け、skipped と not-run を PASS にしない。過去の自由文から command/environment/scope/counts を完全に復元できない検証は `verified` に移さず、原文を履歴へ保存し、`not-run` の U 項目として再検証未実施を記録する。

## 統合境界

子担当は割当ファイルだけを変更し、push・公開・Release・外部書込み・破壊操作を行わない。親は変更パスと既存 dirty を確認し、証拠の実在と件数を検品して統合する。Unity を実行しない文書作業では、その理由を handoff と報告へ明記する。

## 並行実装・リリース・バックアップ

並行実装は専用worktreeとbranchへ分離し、共有checkoutへ同時書込みしない。親が差分・検証・既存変更の保持を確認して統合する。リリースではAGENTSの既存手順と公開契約に従い、版の変更と公開操作は権限を確認して実行する。親のバックアップ対象表に従い、保存先未指定のバックアップは未実施として記録する。

# Changelog

このファイルの書式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に従う。

## [0.1.0-alpha] - 2026-08-12

初回リリース。

### Added

- AvatarPoseSystem のトラッキング用 constraint を、ポーズ未固定中は
  GameObject ごと非アクティブにする NDMF プラグイン。
  `APS_FixBody` が立つと復帰する。
- 対象の特定は APS の生成クリップから引く（`m_Enabled` を 1 にも 0 にも
  されている constraint = クローン追従専用）。名前に依存しないので
  APS の内部名が変わっても追従する。
- クローン骨格サブツリー（`FixRoot` を子に持つオブジェクト）も併せてゲート。
- `Tools > APS Gate > Enabled` で一時的に無効化できる。

### 実測

ミルフィ素体 + ギミック7種で **-1.83 ms/frame**（12.80 → 10.97）。
APS を丸ごと削除した場合の -2.83 ms に対して約 65% を回収。

### Known limitations

- アバター個別の無効化が無い。プロジェクト単位で一律にかかる。
- APS 自身が `m_Enabled` を管理している constraint（実測で 56 個中 8 個相当）は
  対象外。これらも止めるには Prepare の 2〜3 フレームだけ再有効化する制御が要り、
  窓を外すとポーズがズレる／固定後に流れるため、意図的に見送っている。
  取りこぼしは VRCConstraintJob 換算で約 0.83 ms。

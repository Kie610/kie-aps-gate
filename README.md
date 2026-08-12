# kieApsGate

AvatarPoseSystem（ZeroFactory）が張るトラッキング用 constraint を、**ポーズを固定していない間は止めておく** NDMF プラグイン。

APS 本体には一切手を入れない。NDMF の `AfterPlugin` で後段に挟まるだけなので、APS が更新されても追従する。

## 何のために

APS はポーズ固定機能のために、ヒューマノイド全ボーンぶんのクローン骨格を作り、それを実際の体へ追従させる constraint を張る。**この constraint は「固定していない間」ずっと毎フレーム動いている。**

固定していない時間のほうが圧倒的に長いので、そこを止める。

## 効果（実測）

ミルフィ素体 + ギミック7種のアバターを Unity の Play モードで 240 フレーム平均。

| 構成 | フレーム時間 | VRCConstraintJob |
|---|---|---|
| そのまま | 12.80 ms | 2.41 ms |
| **kieApsGate あり** | **10.97 ms** | 1.49 ms |
| APS を丸ごと削除 | 9.97 ms | 0.63 ms |

**-1.83 ms。** APS を削除して得られる 2.83 ms のうち約 65% を、機能を残したまま回収する。

数字はアバター構成で変わる。APS のハンドルが多いほど効く。

## 使い方

入れるだけ。コンポーネントも設定も無い。**プロジェクト内の APS 入りアバター全部**に自動でかかる。

ビルド時に Console へこう出れば効いている:

```
[APS Gate] 候補 56 件
[APS Gate] クローン骨格 'AvatarPoseSystem/APS_WorldFix' もゲート
[APS Gate] 48 個のトラッキング constraint を APS_FixBody でゲートしました
```

「候補 0 件」なら効いていない。APS の実装が変わったサイン。

`Tools > APS Gate > Enabled` で一時的に切れる（切り分け用）。

## 仕組み

APS は「未固定=1 / 固定=0」で constraint の `m_Enabled` をアニメーションする。つまり **固定時に APS 自身が切る constraint = クローンを体へ追従させるためだけのもの**。これを生成済みクリップのバインディングから引いて、対象を名前に依存せず特定する。

対象を `SetActive(false)` にしたうえで、`m_IsActive` を `APS_FixBody` で 0/1 する 1 レイヤーを FX へ合流させる。APS は `m_Enabled`、こちらは `m_IsActive` を触るのでプロパティが衝突しない。

固定の瞬間にクローンが追いついている必要があるが、APS の遷移は `Unfix → Prepare → Prepare2 → Fix` と 2 フレームの猶予があり、ParentConstraint はステートレスなので 1 回評価すればソースへスナップする。

### 触らないもの

**APS が `m_Enabled` を触っていない constraint には手を出さない。** `Head_Const` のように**実ボーンを保持している**ものが含まれ、これを止めると解除後も実ボーンが古いクローンに引かれたままになる（メッシュが戻らない／姿勢を変えるとボーンが引き伸ばされる）。

## 制限

- アバター個別の無効化はまだ無い。プロジェクト単位で一律にかかる。
- `Tools > APS Gate > Enabled` の状態は EditorPrefs に保存される（プロジェクトではなくユーザー単位）。

## 必要なもの

- Unity 2022.3
- VRChat Avatars SDK 3.7 以降 / NDMF 1.14 以降 / Modular Avatar 1.10 以降
- AvatarPoseSystem（ZeroFactory）— 無い場合は何もしない

## ライセンス

MIT

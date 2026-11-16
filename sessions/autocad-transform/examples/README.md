# 図形変換の実演：目的と構成

Visual Studioで完成済みのコードを読み、デバッガーで引数・途中計算・結果を確かめ、AutoCAD図面上でも比較するための案内。
同じ固定入力に対する異なるAPIや実装方法を、数学的な意味と対応させる。

## コードの管理先と読む順序

実演コードは [autocad-net側の図形変換ディレクトリ](../../../autocad-net/samples/autocad-transform/README.md) で管理する。
既存の [AutoCadGeometrySamples.sln](../../../autocad-net/AutoCadGeometrySamples.sln) と [AutoCadGeometrySamples.csproj](../../../autocad-net/AutoCadGeometrySamples.csproj) を利用し、ここにコードや別プロジェクトを複製しない。

題材は座標系 → 任意軸アルゴリズム → OCS/WCS相互変換 → 基本変換・逆変換 → 合成の順に読む構成とする。
各題材では、目的と固定入力、複数のコード、共通する数学的な意味、期待結果、適用条件と使い分けを照合する。

## 実演で確認すること

- 入力・途中計算・結果が、どの座標系の点や方向を表しているか。
- コードの違いが、基底・行列・線形結合・内積のどの違いに対応するか。
- 見た目だけでなく、座標・基底・行列が固定入力に対する期待結果と一致するか。
- 図面に変更を加える場合の対象と副作用、適用条件・退化・誤差。

対象は既存ソリューションに合わせたAutoCAD 2026 Update 1.2以降／.NET 10／Visual Studio。
職場の更新版・実行環境との適合と、ビルド・NETLOAD・実行は未検証である。
環境と手動確認の共通事項は [AutoCAD参考コードのセットアップ](../../../autocad-net/docs/setup.md) を参照する。
AutoCADを必要とする実演コードを、独立したC#教材やそのCIへ混ぜない。

[詳細解説の導入と構成](../explanation.md) ／ [この教材のトップへ戻る](../README.md)

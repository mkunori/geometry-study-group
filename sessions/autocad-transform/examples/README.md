# 図形変換の実演：WCS・OCSと任意軸

Visual Studioで完成済みのコードを読み、デバッガーで引数・途中計算・結果を確かめ、AutoCAD図面上でも比較するための案内。
同じ固定入力に対する異なるAPIや実装方法を、数学的な意味と対応させる。

## コードの管理先と読む順序

実演コードは [autocad-net側の図形変換ディレクトリ](../../../autocad-net/samples/autocad-transform/README.md) で管理する。
既存の [AutoCadGeometrySamples.sln](../../../autocad-net/AutoCadGeometrySamples.sln) と [AutoCadGeometrySamples.csproj](../../../autocad-net/AutoCadGeometrySamples.csproj) を利用し、ここにコードや別プロジェクトを複製しない。

題材は座標系 → 任意軸アルゴリズム → OCS/WCS相互変換 → 基本変換・逆変換 → 合成の順に読む構成とする。
各題材では、目的と固定入力、複数のコード、共通する数学的な意味、期待結果、適用条件と使い分けを照合する。

第1〜3章のコードは、次の順に読む。

| ファイル | 数学的な役割 |
| --- | --- |
| 1. [CoordinateSystemCommands.cs](../../../autocad-net/samples/autocad-transform/CoordinateSystemCommands.cs) | 同じPolyline頂点のOCS・WCS座標と、取得API・行列・線形結合を比較 |
| 2. [ArbitraryAxisMath.cs](../../../autocad-net/samples/autocad-transform/ArbitraryAxisMath.cs) | 単位化、1/64の分岐、成分外積からOCS基底を作る |
| 3. [ArbitraryAxisCommands.cs](../../../autocad-net/samples/autocad-transform/ArbitraryAxisCommands.cs) | 10法線の自作・PlaneToWorld基底、直交性・右手系を照合 |
| 4. [TransformDemoSupport.cs](../../../autocad-net/samples/autocad-transform/TransformDemoSupport.cs) | 数値出力と新規図形登録。表示長と基底長を区別 |

## コマンド・固定入力・期待結果

| コマンド | 固定入力 | 数学上の期待結果 | 図面への変更 |
| --- | --- | --- | --- |
| `GEOAT_COORDINATES` | OCS頂点(0,0),(4,0),(3,2),(0,1)。AはN=+Z,h=0、BはN=(0,3/5,4/5),h=5 | 頂点2のOCS値は(3,2)。WCSはA=(3,2,0)、B=(−3,1.4,5.2)。BのC=(0,3,4) | 新規Polyline2個＋Line7本 |
| `GEOAT_ARBITRARY_AXIS` | 正負Z、+X、B、Z近傍、X境界の直前・等号・直後、Y境界の等号、斜め法線の10例 | 各基底長1、各組の内積0、(X×Y)・N=1。BはX=(−1,0,0)、Y=(0,−4/5,3/5)。自作/APIの軸の成分差が小さいことを照合 | 新規Line9本 |

これらは実測値ではなく、[詳細解説](../explanation.md) の数式から得た期待値である。
閉輪郭のbulge・幅は0、長さは作図単位で統一し、mmなどを仮定しない。
境界の固定入力はb=1/64、δ=2^−20、x=b(1−δ),b,b(1+δ)、N=(x,0,√(1−x²))とする。
Z近傍はN=(0,1/128,√16383/128)、Y境界はN=(0,b,√(1−b²))、斜め例は(1,2,2)方向である。
両絶対成分が厳密に1/64未満のときだけY分岐とし、等号はZ分岐に入る。

## デバッガーで追う変数

- `CoordinateSystemCommands.Inspect`：`normal`・`elevation`・`frame`、頂点番号2の `ocs2d`・`ocs3d`・`wcsByGetter`・`wcsByMatrix`・`wcsByBasis`。
- `ArbitraryAxisMath.Build`：単位化後の `n`、`usesWorldY`、`helper`、外積を単位化した `x`・`y`。
- `ArbitraryAxisCommands.Inspect`：`input`・`frame`・`planeToWorld`、同じ標準基底から独立に求めた `apiX`・`apiY`・`apiZ`。

EditorにはG17で各座標・基底・分岐に使った成分・長さ・内積・右手系・残差を出力する。
同じ初期状態から各実装を計算し、APIの結果を自作計算の入力に流用しない。
表示用に丸めた値から判定をやり直さず、ローカル変数と数式を照合する。

## 実演で確認すること

- 入力・途中計算・結果が、どの座標系の点や方向を表しているか。
- コードの違いが、基底・行列・線形結合・内積のどの違いに対応するか。
- 見た目だけでなく、座標・基底・行列が固定入力に対する期待結果と一致するか。
- 図面に変更を加える場合の対象と副作用、適用条件・退化・誤差。

## 図面表示・条件・誤差

専用の空図面でコマンドを個別に実行し、必要なら利用者がズーム全体や等角表示で確認する。
コマンドは引数・選択なしで、新規図形だけをモデル空間へ追加する。
保存・既存図形・UCS・ビュー・ズーム・レイヤー設定は変更せず、再実行は同位置へ重複追加する。

- 座標の実演：Aは黄色、Bはシアン。灰色はWCS原点からの3軸、赤・緑・青はCからのX・Y・N、マゼンタはO→5N。
- 任意軸の実演：すべて共通原点Oから描く。灰色はWCS、自作X・Y・Nは赤・緑・青、APIのX・Y・Zはシアン・マゼンタ・黄色。

各軸の表示長3は数学上の基底長1とは別で、Cから描く軸もOCS原点がCという意味ではない。
Lineの原点から終点へ正方向に延ばし、矢印・文字Entityは作らない。
軸が重なって見えるだけで一致とせず、数値・残差を比べる。
現在図面の通常の非Sessionコマンドを対象とし、別図面・モデルレスのDocumentLock管理は扱わない。
Commit前の例外はTransaction破棄でロールバックするが、Commit後の失敗まで変更なしとは断定しない。

ゼロ・NaN・無限大の法線は数学部分で拒否し、APIへ渡さない。
単位化では最大絶対成分で尺度調整するが、入力精度・極端な成分比・分岐付近の丸めや軸の不連続性までは解決しない。
1/64という分岐規則と、結果を比較する許容誤差を混同しない。

対象は既存ソリューションに合わせたAutoCAD 2026 Update 1.2以降／.NET 10／Visual Studio。
職場の更新版・実行環境との適合と、ビルド・NETLOAD・実行は未検証である。
環境と手動確認の共通事項は [AutoCAD参考コードのセットアップ](../../../autocad-net/docs/setup.md) を参照する。
AutoCADを必要とする実演コードを、独立したC#教材やそのCIへ混ぜない。

署名と規約の照合先は [実演コード側の公式資料一覧](../../../autocad-net/samples/autocad-transform/README.md#公式資料との静的照合) を参照する。

[詳細解説](../explanation.md) ／ [この教材のトップへ戻る](../README.md)

# 図形変換の実演：WCS・OCSと任意軸

Visual Studioで完成済みのコードを読み、デバッガーで引数・途中計算・結果を確かめ、AutoCAD図面上でも比較するための案内。  
同じ固定入力に対する異なるAPIや実装方法を、数学的な意味と対応させる。

## コードの管理先と読む順序

実演コードは [autocad-net側の図形変換ディレクトリ](../../../autocad-net/samples/autocad-transform/README.md) で管理する。  
既存の [AutoCadGeometrySamples.sln](../../../autocad-net/AutoCadGeometrySamples.sln) と [AutoCadGeometrySamples.csproj](../../../autocad-net/AutoCadGeometrySamples.csproj) を利用し、ここにコードや別プロジェクトを複製しない。

題材は座標系 → 任意軸アルゴリズム → OCS/WCS相互変換 → 基本変換・逆変換 → 合成の順に読む構成とする。  
詳細解説では、API紹介と図解を先に読み、固定入力・複数のコード・数学的な理由・期待結果・適用条件と使い分けを照合する。

第1〜5章は一覧の1〜7、第6〜8章は8〜16を参照する。

| ファイル | 数学的な役割 |
| --- | --- |
| 1. [CoordinateSystemCommands.cs](../../../autocad-net/samples/autocad-transform/CoordinateSystemCommands.cs) | 同じPolyline頂点のOCS・WCS座標と、取得API・行列・線形結合を比較 |
| 2. [ArbitraryAxisMath.cs](../../../autocad-net/samples/autocad-transform/ArbitraryAxisMath.cs) | 単位化、1/64の分岐、成分外積からOCS基底を作る |
| 3. [ArbitraryAxisCommands.cs](../../../autocad-net/samples/autocad-transform/ArbitraryAxisCommands.cs) | 10法線の自作・PlaneToWorld基底、直交性・右手系を照合 |
| 4. [OcsToWcsCommands.cs](../../../autocad-net/samples/autocad-transform/OcsToWcsCommands.cs) | 取得API・専用行列・自作基底行列・線形結合の4方式を同じOCS頂点で比較 |
| 5. [WcsToOcsCommands.cs](../../../autocad-net/samples/autocad-transform/WcsToOcsCommands.cs) | 専用逆変換・Inverse・内積・転置と、往復・平面外の点を比較 |
| 6. [OcsWcsDemoDrawing.cs](../../../autocad-net/samples/autocad-transform/OcsWcsDemoDrawing.cs) | 固定輪郭と描画だけを共有。学習対象の変換計算はコマンド内 |
| 7. [TransformDemoSupport.cs](../../../autocad-net/samples/autocad-transform/TransformDemoSupport.cs) | 数値・行列出力と新規図形登録。表示長と基底長を区別 |
| 8. [TranslationCommands.cs](../../../autocad-net/samples/autocad-transform/TranslationCommands.cs) | 成分加算・Add・Displacement、点と方向・Entityの違い |
| 9. [RotationCommands.cs](../../../autocad-net/samples/autocad-transform/RotationCommands.cs) | sin/cos、Point2d／Point3d、原点／任意中心の回転 |
| 10. [CompositionCommands.cs](../../../autocad-net/samples/autocad-transform/CompositionCommands.cs) | 逐次変換・積・Pre/Post・逆変換、対称輪郭の診断 |
| 11. [LocalFrameCommands.cs](../../../autocad-net/samples/autocad-transform/LocalFrameCommands.cs) | Bの平面でWCS／ローカル基準の移動・回転を比較 |
| 12. [AlignmentCommands.cs](../../../autocad-net/samples/autocad-transform/AlignmentCommands.cs) | 回転＋移動・AlignCoordinateSystem・成分行列・線形結合 |
| 13. [ScalingCommands.cs](../../../autocad-net/samples/autocad-transform/ScalingCommands.cs) | 中心からの計算・ScaleBy・一様Scaling |
| 14. [MirroringCommands.cs](../../../autocad-net/samples/autocad-transform/MirroringCommands.cs) | YZ鏡映3方式とY軸の半回転を区別 |
| 15. [ProjectionCommands.cs](../../../autocad-net/samples/autocad-transform/ProjectionCommands.cs) | 内積・OrthoProject・法線／斜方向のProject |
| 16. [BasicTransformDrawing.cs](../../../autocad-net/samples/autocad-transform/BasicTransformDrawing.cs) | 固定入力・G17出力・輪郭と比較点の描画のみを共有 |

## 実演の順序

先にWCS・OCS・任意軸・相互変換を扱い、その後で関連する基本変換へ進む。  
各コマンドは前の実行結果を入力に使わず、固定入力を毎回作る。  
必須題材を説明した後は、当日の疑問に合わせて補足を選べる。

| 順序 | 実演コマンド | 本文・練習問題との対応 |
| --- | --- | --- |
| 1 | [`GEOAT_COORDINATES`](../../../autocad-net/samples/autocad-transform/CoordinateSystemCommands.cs) | 第1〜2章・問1。同じ頂点をOCSとWCSで読む。OとC、NormalとElevationを分ける |
| 2 | [`GEOAT_ARBITRARY_AXIS`](../../../autocad-net/samples/autocad-transform/ArbitraryAxisCommands.cs) | 第3章・問2。補助軸、外積の順序、正規化、厳密な1/64の分岐を追う |
| 3 | [`GEOAT_OCS_TO_WCS`](../../../autocad-net/samples/autocad-transform/OcsToWcsCommands.cs) | 第4章・問3。4方式の同値性と、高さ・変換方向の誤りを比べる |
| 4 | [`GEOAT_WCS_TO_OCS`](../../../autocad-net/samples/autocad-transform/WcsToOcsCommands.cs) | 第5章・問3。内積・逆行列・転置、往復と射影の違いを確かめる |

続けて第6章の移動→原点回転→合成→ローカル基準の順に見ると、前半の基底と操作の順序がつながる。  
逆変換は `GEOAT_COMPOSITION`、任意中心の回転は第7.1節の `GEOAT_CENTER_ROTATION` で比較できる。  
配置・拡大縮小・鏡映・射影は選択的な補足とし、すべてを順に実行する必要はない。  
[練習問題](../homework/problems.md) の問4は、合成順序とローカル移動を復習する発展問題である。

## Visual StudioとAutoCADで開く準備

以下は講師が職場の対象環境で行う手順であり、実施済みの記録ではない。  
環境導入・DLL参照・信頼済みのロード先は [共通セットアップ](../../../autocad-net/docs/setup.md) に従う。

1. 同じコミットのソリューションとソースを開く。  
   局所変数を追う実演では、ソリューションにある `Debug|x64` を選び、DLLとPDBを同じビルドから用意する。  
   通常の配布確認に使うRelease構成と、デバッグ用の成果物を取り違えない。  
2. 対象のAutoCADで専用の空図面・モデルタブを開き、自作のDLLをNETLOADする。  
   起動済みの対象AutoCADプロセスへ、Visual Studioの「デバッグ → プロセスにアタッチ」で接続する。  
   複数のAutoCADがある場合は対象を確認し、.NETのコードを追えるデバッガーを選ぶ。  
3. 「モジュール」で `AutoCadGeometrySamples.dll` のロード元とシンボルを確認する。  
   実演用DLL・対応PDB・開いているソースが一致し、ブレークポイントが有効になってからコマンドを実行する。  
4. 下の候補で止め、ローカル・ウォッチ・呼び出し履歴から入力→途中計算→結果を追う。  
   ライブラリ内部へ入り続けず、比較したい代入はステップオーバーで確認する。  
   結果の代入行では、その行を実行した後に値を見る。

[Microsoftのアタッチ手順](https://learn.microsoft.com/en-us/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger?view=visualstudio) と [シンボル・ソースの対応](https://learn.microsoft.com/en-us/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=visualstudio) も参照できる。  
これらの一般手順の確認は、AutoCADでのアタッチやブレークポイント停止の実機成功を意味しない。

### 最初の4コマンドで止める場所

行番号は変更でずれるため、メソッド名とコード中の文を目印にする。  
変数は、その宣言が実行済みで有効範囲内にあるところで読む。

| コードの場所・停止候補 | 絞り込みと見る値 |
| --- | --- |
| `CoordinateSystemCommands.Inspect` の頂点出力 | `index == 2`、Bなら `elevation == 5`。`ocs2d`・`ocs3d`・`wcsByGetter`・`wcsByMatrix`・`wcsByBasis` を後の表の期待値と比較 |
| `ArbitraryAxisMath.Build` の `return new Frame` | 計算済みの `n`・`usesWorldY`・`helper`・`x`・`y`。境界例では正規化後の成分で分岐を読む |
| `ArbitraryAxisCommands.Inspect` の入力法線出力 | `input`・`frame`・`planeToWorld`・`apiX`・`apiY`・`apiZ`。正負Z・B・境界・斜めのラベルを確認 |
| `OcsToWcsCommands.Compare` の頂点出力と診断例の出力 | `index == 2`。4結果と `apiValues`・`constructedValues`、往復後の `backToOcs`。Bだけでなく斜め法線でも `wrongDirection` を比較 |
| `WcsToOcsCommands.Compare` の固定WCS点出力 | `world`・4つのOCS結果。後の往復出力で各 `backBy...`、最後の出力で `forcedHeight`・`projected` を確認 |
| `WcsToOcsCommands.WcsToOcs` の平面外頂点の受入れ出力 | `outsideOcs`・`outsideAccepted`・`vertexPlaneTolerance`、追加前の `reconstructed`。平面外のQを受け入れないことを確認 |

## コマンド・固定入力・期待結果

| コマンド | 固定入力 | 数学上の期待結果 | 図面への変更 |
| --- | --- | --- | --- |
| [`GEOAT_COORDINATES`](../../../autocad-net/samples/autocad-transform/CoordinateSystemCommands.cs) | OCS頂点(0,0),(4,0),(3,2),(0,1)。AはN=+Z,h=0、BはN=(0,3/5,4/5),h=5 | 頂点2のOCS値は(3,2)。WCSはA=(3,2,0)、B=(−3,1.4,5.2)。BのC=(0,3,4) | 新規Polyline2個＋Line7本 |
| [`GEOAT_ARBITRARY_AXIS`](../../../autocad-net/samples/autocad-transform/ArbitraryAxisCommands.cs) | 正負Z、+X、B、Z近傍、X境界の直前・等号・直後、Y境界の等号、斜め法線の10例 | 各基底長1、各組の内積0、(X×Y)・N=1。BはX=(−1,0,0)、Y=(0,−4/5,3/5)。自作/APIの軸の成分差が小さいことを照合 | 新規Line9本 |
| [`GEOAT_OCS_TO_WCS`](../../../autocad-net/samples/autocad-transform/OcsToWcsCommands.cs) | A・BとN=(1/3,2/3,2/3)の斜め例。各4頂点、hは順に0・5・5 | 4方式で同じWCS位置。Bの頂点2は(−3,7/5,26/5)。斜めの頂点2は約(−1.612899700333,3.482404531833,4.824045318333)。全頂点のOCS往復も照合 | Bの新規Polyline1個＋Line23本 |
| [`GEOAT_WCS_TO_OCS`](../../../autocad-net/samples/autocad-transform/WcsToOcsCommands.cs) | A・B・斜め例の手計算で固定した平面上／平面外のWCS点。Bの4つのWCS頂点 | BのPはOCS(3,2,5)、Q=P+2Nは(3,2,7)。高さを保つ往復は各元の点。Qをh=5の頂点として追加しない | 逆変換したBの新規Polyline1個＋Line14本 |

これらは実測値ではなく、[詳細解説](../explanation.md) の数式から得た期待値である。  
閉輪郭のbulge・幅は0、長さは作図単位で統一し、mmなどを仮定しない。  
境界の固定入力はb=1/64、δ=2^−20、x=b(1−δ),b,b(1+δ)、N=(x,0,√(1−x²))とする。  
Z近傍はN=(0,1/128,√16383/128)、Y境界はN=(0,b,√(1−b²))、斜め例は(1,2,2)方向である。  
両絶対成分が厳密に1/64未満のときだけY分岐とし、等号はZ分岐に入る。

## デバッガーで追う変数

- `CoordinateSystemCommands.Inspect`：`normal`・`elevation`・`frame`、頂点番号2の `ocs2d`・`ocs3d`・`wcsByGetter`・`wcsByMatrix`・`wcsByBasis`。
- `ArbitraryAxisMath.Build`：単位化後の `n`、`usesWorldY`、`helper`、外積を単位化した `x`・`y`。
- `ArbitraryAxisCommands.Inspect`：`input`・`frame`・`planeToWorld`、同じ標準基底から独立に求めた `apiX`・`apiY`・`apiZ`。
- `OcsToWcsCommands.Compare`：`ocs2d`・`ocs3d`、`byGetter`・`byPlaneToWorld`・`byBasisMatrix`・`byCombination`、`basisValues`・`apiValues`・`constructedValues`・`backToOcs`。
- 同メソッドの頂点2：`withoutElevation`・`wrongDirection`・`twiceHeight`・`differentBasis` は誤りを見分ける診断例。正しい結果や描画には混ぜない。
- `WcsToOcsCommands.Compare`：`world`、`worldToPlane`・`inverse`・`transpose`、`byWorldToPlane`・`byInverse`・`byDot`・`byTranspose`、各 `backBy...`、`forcedHeight`・`projected`。
- `WcsToOcsCommands.WcsToOcs`：`ocs`・`outsideOcs`、`vertexPlaneTolerance`・`outsideAccepted`、追加する `reconstructed` のNormal・Elevation・頂点。

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
- OCS→WCS：BのPolylineはシアン、4方式の閉輪郭は順に赤・緑・青・黄色のLine列。同じ位置に重ね、WCS軸・CからのOCS軸・O→5Nの7本も描く。
- WCS→OCS：逆変換したBのPolylineはシアン、平面上Pは緑、平面外Qは赤の小さな十字3本。P→Q=2Nの線はマゼンタ。さらに同じ7本の軸・高さ線を描く。

各軸の表示長3は数学上の基底長1とは別で、Cから描く軸もOCS原点がCという意味ではない。  
Lineの原点から終点へ正方向に延ばし、矢印・文字Entityは作らない。  
軸が重なって見えるだけで一致とせず、数値・残差を比べる。  
現在図面の通常の非Sessionコマンドを対象とし、別図面・モデルレスのDocumentLock管理は扱わない。  
Commit前の例外はTransaction破棄でロールバックするが、Commit後の失敗まで変更なしとは断定しない。

ゼロ・NaN・無限大の法線は数学部分で拒否し、APIへ渡さない。  
単位化では最大絶対成分で尺度調整するが、入力精度・極端な成分比・分岐付近の丸めや軸の不連続性までは解決しない。  
1/64という分岐規則と、結果を比較する許容誤差を混同しない。

相互変換の比較では、固定例Bの行列が対称で逆行列も同じになる点に注意する。  
向き・転置を間違えたコードの対照には、斜め法線も使う。  
平面外のQ=(−3,13/5,34/5)をOCS(3,2,7)と表し直すだけでは射影にならない。  
Zを5へ置き換えてWCSへ戻す操作は、平面上のPへ位置を変える操作として別に出力する。  
頂点の受入れ判定は有限性とZ・Elevationの差を確認し、この小さな固定例だけで1e−10作図単位を使う。  
許容範囲内でもZを捨てれば微小な位置調整になるため、厳密な平面条件や任意の尺度の精度保証と混同しない。

対象は既存ソリューションに合わせたAutoCAD 2026 Update 1.2以降／.NET 10／Visual Studio。  
職場の更新版・実行環境との適合と、ビルド・NETLOAD・実行は未検証である。  
環境と手動確認の共通事項は [AutoCAD参考コードのセットアップ](../../../autocad-net/docs/setup.md) を参照する。  
AutoCADを必要とする実演コードを、独立したC#教材やそのCIへ混ぜない。

署名と規約の照合先は [実演コード側の公式資料一覧](../../../autocad-net/samples/autocad-transform/README.md#公式資料との静的照合) を参照する。

## 第6〜8章の実演

第6章は移動・回転・合成・ローカル基準・逆変換を中心に読む。  
AlignCoordinateSystemによる配置は第6章末の補足、拡大縮小・鏡映・射影は第7章の選択的な補足である。  
任意中心の回転は、中心をコードから読み取るための比較として第7章の最初に置く。

### コマンド・固定入力・期待値

| コマンド | 固定入力 | 数学上の期待結果 | 新規Line |
| --- | --- | --- | --- |
| [`GEOAT_TRANSLATION`](../../../autocad-net/samples/autocad-transform/TranslationCommands.cs) | WCS頂点2(3,2,0)、d=(10,0,3) | 3方式で(13,2,3)。方向(3,2,1)は不変 | 28本 |
| [`GEOAT_ROTATION`](../../../autocad-net/samples/autocad-transform/RotationCommands.cs) | 頂点2、原点・+Z・+π/2 | 4方式で(−2,3,0) | 35本 |
| [`GEOAT_COMPOSITION`](../../../autocad-net/samples/autocad-transform/CompositionCommands.cs) | 同じR、T=(10,0,3) | 方式1〜4のTRは(8,3,3)、方式5のRTは(−2,13,3)。逆変換で元の点へ | 42本 |
| [`GEOAT_LOCAL_FRAME`](../../../autocad-net/samples/autocad-transform/LocalFrameCommands.cs) | C基準(3,2,0)、Bの軸、C=(0,3,4) | 移動：WCS Xへ2は(−1,7/5,26/5)、ローカルXへ2は(−5,7/5,26/5)。回転：WCS原点は(−7/5,−3,26/5)、C・Nは(2,3/5,29/5) | 49本 |
| [`GEOAT_ALIGNMENT`](../../../autocad-net/samples/autocad-transform/AlignmentCommands.cs) | 標準フレームからC=(10,0,3)、X=+Y、Y=−X、Z=+Z | 4方式で頂点2は(8,3,3)。原点と3軸上の点も照合 | 35本 |
| [`GEOAT_CENTER_ROTATION`](../../../autocad-net/samples/autocad-transform/RotationCommands.cs) | 頂点2、中心C=(1,1,0)、+Z・+π/2 | 5方式で(0,3,0)。Cは固定 | 42本 |
| [`GEOAT_SCALING`](../../../autocad-net/samples/autocad-transform/ScalingCommands.cs) | 頂点2、中心C=(1,1,0)、正の一様2倍 | 3方式で(5,3,0)。逆行列で(3,2,0) | 28本 |
| [`GEOAT_MIRRORING`](../../../autocad-net/samples/autocad-transform/MirroringCommands.cs) | 頂点2、YZ平面。比較用にY軸の+π回転 | 鏡映3方式は(−3,2,0)。半回転と違いが出るZ点は鏡映で+Z、半回転で−Z | 35本 |
| [`GEOAT_PROJECTION`](../../../autocad-net/samples/autocad-transform/ProjectionCommands.cs) | Bを2N進めた輪郭。頂点2はQ=(−3,13/5,34/5) | 正射影3方式はP=(−3,7/5,26/5)。斜方向D=(1,0,−1)の2方式は(−1/2,13/5,43/10) | 42本 |

これらは数式による期待値であり、AutoCAD実機の出力記録ではない。

### 基本変換の結果を共通の場所で比べる

第6〜7章の9コマンドは、各コマンドの行列を用意した後に `BasicTransformDrawing.Show` へ入る。  
`Point3d result = methods[method].Apply(probes[index]);` の次の出力行を停止候補にする。  
`index == 4` は頂点2、`index == 3` はZ軸上の比較点である。  
`method` は0始まりだが、教材の「方式1」は `methods[0]` に当たる。

`probes[index]`・`methods[method].Label`・`first`・`result` を比べる。  
ローカル配置ではprobesの入力はローカル成分、射影では頂点2が平面外のQ、それ以外の基本変換ではWCSの固定点である。  
同じ場所で停止しても、入力の座標系まで同じだとは考えない。

呼び出し履歴から元のコマンドのフレームへ戻ると、次の表の行列や固定値を追える。  
ラムダ内だけの `distance`・`parameter` を読む場合は、ProjectionCommandsのその計算行で止める。  
すべての局所変数が同時に同じスコープで見えるわけではない。

### 第6〜7章のデバッガーで追う箇所

| 読む場所 | デバッガーで照合する変数 |
| --- | --- |
| TranslationCommands.Translation | displacement、translation、translatedDirection、一時LineのStartPoint／EndPoint |
| RotationCommands.Rotation／CenterRotation | angle、cosine／sine、center、rotation、toOrigin、fromOrigin、sandwich |
| CompositionCommands.Composition | rotation／translation、product、byPre／byPost、reverseOrder、inverse、undoMove／undoRotate、byInverse／byUndoSteps |
| LocalFrameCommands.LocalFrame | localToWorld、localDisplacement／worldDisplacement、localMove／worldMove／equivalentMove、worldCenter／worldAxis、equivalentRotation |
| AlignmentCommands.Alignment | centerと3軸、byProduct、byAlign、values、byElements |
| ScalingCommands.Scaling | center、scale、scaling、scaledと逆変換 |
| MirroringCommands.Mirroring | plane、mirror、halfTurn。Z軸上の点の符号 |
| ProjectionCommands.Projection | normal、center、plane、oblique、denominator、parallel、outside。各方式内のdistance／parameter |
| BasicTransformDrawing.Show | probes、input、first、result、outlines、reference／referenceProbe。同じ入力が各方式へ渡ること |

### 描画・適用条件・未検証事項

すべて引数・選択なしの通常コマンドで、専用の空図面を使う。  
灰色は比較元、各方式は順に赤・緑・青・黄・マゼンタ・シアンとする。  
各方式で閉輪郭の4辺と、入力のZ軸上の点を変換した小さな十字3本を描く。  
同じ操作の線は重なるため、座標・行列16要素・距離のずれもG17で確認する。  
原点・X/Y/Z軸上の点・頂点2は数値でも照合する。  
図形の表示位置は比較しやすいようにずらさず、再実行は同じ位置へ重複追加する。

保存・既存図形・UCS・ビュー・ズーム・レイヤー設定は変更しない。  
現在図面の通常の非Sessionコマンドが対象で、別図面やモデルレスのDocumentLock管理は扱わない。  
Commit前の例外はTransaction破棄でロールバックするが、Commit後の失敗まで変更なしとは断定しない。  
Translationの一時LineはEntity.TransformByを読むための例で、DBへは追加しない。

LocalFrameの入力はCから測る(u,v,0)で、OCSの(u,v,5)と混ぜない。  
灰色の輪郭は配置済みB、灰色の比較点はM(0,0,1)=(0,18/5,24/5)である。  
方式2・3が同じ移動、方式5・6が同じ回転になる。  
一様2倍以外の尺度や、鏡映を含む配置へ回転軸の対応を広げない。

射影は最初の3方式が正射影、後ろ2方式が斜方向の射影である。  
Qを座標変換で表し直すだけの操作とは異なる。  
平行なD=(1,0,0)はN・D=0と表示するだけで、Projectへ渡さない。  
分母1e−12のガードは固定した入力用で、方向の長さによらない一般の角度判定ではない。  
座標照合は絶対1e−10作図単位＋相対1e−12、行列は無次元で絶対・相対1e−12を一例とし、任意の尺度の精度を保証しない。

Managed公式資料のMirrorとProjectには、直線や2D回転の説明が混在する。  
Planeを渡す署名と2026版のネイティブ幾何資料で照合した範囲は、詳細解説の「第6〜7章の公式出典と検証範囲」に従う。  
職場の参照DLLによるビルド・NETLOAD・実機実行・表示は未検証である。  
AutoCADを必要とする実演を、数学基礎編のプロジェクト・自動テスト・既存CIへ追加しない。

## 再実行と確認記録

各コマンドは固定入力から独立に計算するが、描いた図形は図面へ残る。  
再実行すると同位置へ増えるため、別の専用空図面で次の比較を始めると区別しやすい。  
コマンドに図形削除や自動保存は含めない。

DLLを更新した場合は、古いロード済みDLLで結果を取らない。  
同じビルドのDLL・PDBへ入れ替え、必要に応じてAutoCADを終了・再起動してから、NETLOADとアタッチを行う。  
停止できないときは、対象プロセス・モジュールのロード元・PDBとソースの一致・Debug構成を確認する。

| 確認の種類 | この教材で確認できている範囲 |
| --- | --- |
| 静的照合 | 本文・図・演習・コードの固定値、APIの公式仕様、コマンド名と変数・描画処理の対応 |
| 数学上の期待値 | 成分計算・基底・行列から求めた値。AutoCADの出力記録ではない |
| 職場の実機確認 | 参照DLLでのビルド・NETLOAD・アタッチ・ブレークポイント停止・13コマンドの実行と図面表示は未検証 |

実機確認では、製品・更新版・.NET・Visual Studio・対象コミット・構成と、実行したコマンドを記録する。  
期待値に対するG17の実測値・残差・図面上の対応・例外の有無を区別し、再実行による重複も確かめる。  
結果や不一致はIssue／PRへ記録し、未実施の項目を成功扱いにしない。  
DLL・ライセンス・職場固有パス・業務図面・機密ログは含めない。

[詳細解説](../explanation.md) ／ [実演コード側の仕様照合と管理方針](../../../autocad-net/samples/autocad-transform/README.md) ／ [この教材のトップへ戻る](../README.md)

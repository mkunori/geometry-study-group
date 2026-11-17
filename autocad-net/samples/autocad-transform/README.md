# AutoCAD実践編：図形変換の実演コード

[同じ図形変換のいろいろな実現方法を見てみよう](../../../sessions/autocad-transform/README.md) の実演コードを管理する場所。
WCS・OCS・任意軸アルゴリズムを最優先に、異なるAPIや実装方法を数学で読み解く。
全8章に対応する参考コードを収録するが、AutoCAD参照DLLによるビルド・NETLOAD・実機実行・表示は未検証である。

## 共通ソリューションとの関係

既存の [AutoCadGeometrySamples.sln](../../AutoCadGeometrySamples.sln) と [AutoCadGeometrySamples.csproj](../../AutoCadGeometrySamples.csproj) を使う。
プロジェクトは `samples/**/*.cs` を収録する構成で、テーマごとのコードをこのディレクトリに追加する。
セッション側には [実演の説明](../../../sessions/autocad-transform/examples/README.md) を置き、ソースや別ソリューションを二重管理しない。

## ファイルと読む順序

| ファイル | 読む内容 |
| --- | --- |
| 1. [CoordinateSystemCommands.cs](CoordinateSystemCommands.cs) | 同じ4頂点から2つのPolylineを作り、OCS/WCS・取得API・行列・線形結合を比較 |
| 2. [ArbitraryAxisMath.cs](ArbitraryAxisMath.cs) | AutoCAD型に依存しない単位化・外積・厳密な分岐・位置の成分計算 |
| 3. [ArbitraryAxisCommands.cs](ArbitraryAxisCommands.cs) | 自作・API基底を10法線で照合し、直交性・右手系と無効入力を確認 |
| 4. [OcsToWcsCommands.cs](OcsToWcsCommands.cs) | 取得API・PlaneToWorld・自作基底行列・線形結合を局所変数に並べて比較 |
| 5. [WcsToOcsCommands.cs](WcsToOcsCommands.cs) | WorldToPlane・Inverse・内積・転置、往復、平面外の点の扱い |
| 6. [OcsWcsDemoDrawing.cs](OcsWcsDemoDrawing.cs) | 固定OCS輪郭と描画のみ。変換計算を隠さない |
| 7. [TransformDemoSupport.cs](TransformDemoSupport.cs) | 座標・行列・残差の出力とTransaction・表示用Line |

## 起動コマンド・固定入力・期待値

利用者が職場の対象環境で確認する場合、既存ソリューションをビルドし、専用空図面でDLLを `NETLOAD` して次のコマンドを起動する。
これは確認手順であり、実行成功の記録ではない。
前提は [共通セットアップ](../../docs/setup.md) に従う。

| コマンド | 固定入力 | 数学上の期待値 | 副作用 |
| --- | --- | --- | --- |
| `GEOAT_COORDINATES` | OCS輪郭(0,0),(4,0),(3,2),(0,1)。A：N=+Z,h=0。B：N=(0,3/5,4/5),h=5 | 頂点2のOCS値(3,2)に対し、WCSはA=(3,2,0)、B=(−3,7/5,26/5)。BのC=(0,3,4)、X=(−1,0,0)、Y=(0,−4/5,3/5) | 新規Polyline2個・Line7本 |
| `GEOAT_ARBITRARY_AXIS` | 正負Z、+X、B、Z近傍、X境界の直前・等号・直後、Y境界の等号、(1,2,2)方向の10例 | 基底長1、各内積0、(X×Y)・N=1。両絶対成分が1/64未満だけY分岐、等号はZ分岐。自作/API成分差が小さいことを照合 | 新規Line9本 |
| `GEOAT_OCS_TO_WCS` | A・B・斜め法線N=(1/3,2/3,2/3)、同じ4頂点、h=0・5・5 | Bの頂点2は(−3,7/5,26/5)。斜め例は約(−1.612899700333,3.482404531833,4.824045318333)。4方式のWCS・全16要素・OCS往復を照合 | 新規Polyline1個・Line23本 |
| `GEOAT_WCS_TO_OCS` | A・B・斜め例で手計算から固定した平面上／平面外の点。Bの4つのWCS頂点 | PはOCS(3,2,5)、P+2Nは(3,2,7)。往復は元のWCS点。Zをhへ置換したときだけPへ射影 | 新規Polyline1個・Line14本 |

頂点はOCSのPoint2dとして追加し、Normal・Elevationを先に設定する。
閉輪郭のbulge・幅は0、長さは作図単位で統一する。
Bは(0,3,4)を単位化し、h=5をOCSのZ成分にする。
境界はb=1/64、δ=2^−20、x=b(1−δ),b,b(1+δ)、N=(x,0,√(1−x²))。
Z近傍はN=(0,1/128,√16383/128)、Y境界はN=(0,b,√(1−b²))とする。
全期待値と導出は [本文第3章](../../../sessions/autocad-transform/explanation.md#3-任意軸アルゴリズムarbitrary-axis-algorithm) に対応する。

## デバッガーと表示・副作用

`Coordinates` → `Inspect` の `normal`・`elevation`・`frame`、頂点番号2の `ocs2d`・`ocs3d`・`wcsByGetter`・`wcsByMatrix`・`wcsByBasis` を確認する。
次に `ArbitraryAxisMath.Build` の `n`・`usesWorldY`・`helper`・`x`・`y`、`ArbitraryAxisCommands.Inspect` の `apiX`・`apiY`・`apiZ` を追う。
比較は同じ初期状態から独立に行い、EditorにもG17で入力・基底・分岐成分・長さ・内積・右手系・残差を出力する。

座標コマンドのAは黄色、Bはシアン、WCS軸は灰色。
BのC=(0,3,4)からX・Y・Nを赤・緑・青で描き、O→5Nをマゼンタで示す。
任意軸コマンドは全て共通原点Oから描き、灰色=WCS、赤・緑・青=自作X・Y・N、シアン・マゼンタ・黄色=API X・Y・Zとする。
表示長3は単位基底の長さ1とは別で、Cに描く軸もOCS原点を移したという意味ではない。
Lineの原点から終点へ正方向に延ばすが、矢印・文字Entityは追加しない。
見た目の重なりだけで等価とせず、成分と残差も照合する。

引数・既存図形の選択はなく、モデル空間へ新規図形だけを登録する。
保存・既存図形・UCS・ビュー・ズーム・レイヤー設定は変更せず、再実行は重複追加する。
必要な場合は利用者がズーム全体や等角表示で確認する。
通常の非SessionコマンドでAppendEntity → AddNewlyCreatedDBObject → Commitと進み、新規図形とTransactionはusingで破棄する。
Commit前の例外はロールバックされるが、Commit後の失敗まで変更なしとは断定しない。
別図面・モデルレス・SessionのDocumentLock管理は扱わない。

ゼロ・NaN・無限大の法線はAPIへ渡す前に数学部分で拒否する。
単位化では最大絶対成分で尺度調整するが、入力精度・極端な成分比・境界付近の丸め・軸の不連続性は残る。
1/64は分岐規則であり、数値照合の許容誤差ではない。
実機での自作/API基底の一致は検証期待であり、実測結果ではない。

## 第4〜5章の比較とデバッガー

`OcsToWcsCommands.Compare` では `ocs3d` から `byGetter`・`byPlaneToWorld`・`byBasisMatrix`・`byCombination` をそれぞれ求める。
`basisValues` は数学上の基底を列に並べ、配列には行ごとに詰める。
`apiValues`・`constructedValues` と4つのWCS結果・残差、`backToOcs` を比較する。
固定例Bの行列は対称で逆行列も同じなので、方向や転置の誤りを見分ける対照に斜め法線を使う。
頂点2の `withoutElevation`・`wrongDirection`・`twiceHeight`・`differentBasis` は誤りの診断例として数値だけを出力し、正しい4方式や描画の比較元にはしない。

`WcsToOcsCommands.Compare` では、同じ手計算の固定WCS点から `byWorldToPlane`・`byInverse`・`byDot`・`byTranspose` を独立に求める。
`worldToPlane`・`inverse`・`transpose` の行列、各 `backBy...` の往復結果を追う。
`forcedHeight` はOCSのZをElevationに置き換えた別の入力で、`projected` への射影を往復と区別して読む。
内積・転置を使えるのは原点共通の正規直交基底だからであり、一般の行列や移動を含む4×4に適用しない。

Bの平面上のP=(−3,7/5,26/5)、平面外のQ=(−3,13/5,34/5)は、OCSでそれぞれ(3,2,5)・(3,2,7)。
WCS→OCS→WCSで元の位置を保つが、QのZを5に置き換えれば距離2だけ動いてPへ射影される。
斜め例の平面上の点は第4章の式、平面外はそこへ2Nを足した固定値で、いずれも実行時に順変換した出力を比較元に使わない。
XY平面の対照は(3,2,0)と(3,2,2)、h=0とする。

`WcsToOcsCommands.WcsToOcs` の `worldVertices` はBの既存固定値4点。
`ocs` が有限で、ZとElevationの差が `vertexPlaneTolerance=1e-10` 作図単位以内の点だけを新しいPolylineへ追加する。
平面外の `outsideOcs` は `outsideAccepted=False` が期待され、追加しない。
この許容差は小さな固定例の入力整合確認用で、一般用途の保証や厳密な平面条件ではない。
許容範囲内でZを捨てても位置調整が生じる点は [第5章](../../../sessions/autocad-transform/explanation.md#5-wcsからocsへの変換方法の比較) に従う。

描画はBだけを対象にする。
OCS→WCSではシアンのPolylineに4方式のLine輪郭を赤・緑・青・黄色で重ねる。
WCS→OCSでは再構成Polylineはシアン、Pは緑・Qは赤の十字3本ずつ、P→QはマゼンタのLine。
両方とも灰色WCS3軸、Cからの赤・緑・青のOCS3軸、O→5Nのマゼンタ線を追加する。
各軸の表示長3と十字幅0.24は表示の値で、基底長・判定の許容差とは別である。
保存や既存図形・UCS・ビュー・ズーム・レイヤー設定の変更はなく、再実行は重複追加となる。

## 全体の題材構成

| 題材 | 読み解く内容 |
| --- | --- |
| 座標系 | Polylineの頂点取得、Normal・Elevation、WCS・UCS・OCS |
| 任意軸アルゴリズム | 法線からOCS基底を生成する外積・正規化・分岐規則 |
| OCS/WCS相互変換 | 専用API、変換行列、基底の線形結合、正規直交基底との内積 |
| 基本変換・逆変換 | 座標系を考慮した移動・回転と、元へ戻す変換 |
| 合成と比較 | 順次適用、行列積、前後乗算、座標系の対応付け |

各題材を別のC#ファイルで管理し、コマンド名にはテーマ専用の `GEOAT_...` を使う。
既存の `GEO00_...`〜`GEO05_...` や、他のコマンドと衝突しないことを確認する。
各コードには、固定入力・座標系・単位・期待結果・適用条件・図面への副作用と、数学的な意図を記す。

## 対象環境と検証範囲

対象はAutoCAD 2026 Managed .NET APIとVisual Studio。
既存ソリューションのAutoCAD 2026 Update 1.2以降／.NET 10という対象を暫定的に維持する。
職場の更新版・実行環境を確認したうえで必要な対応を判断し、2024〜2025対応を前提にしない。

個々のAPIはAutoCAD 2026公式資料で仕様と適用条件を照合する。
共通の [公式リファレンス案内](../../docs/references.md) と [セットアップ](../../docs/setup.md) を参照する。
既存参考コードを含め、ビルド・NETLOAD・コマンド実行・実機表示は未検証である。

実演では完成済みのコードをVisual Studioに表示し、デバッガーで引数・途中計算・結果を追い、AutoCAD図面上の結果とも比較する。
実機確認は職場で行い、未実施の確認を成功済みと記載しない。
AutoCADを必要とするコードを、独立したC#教材・自動テスト・既存CIへ組み込まない。

## 公式資料との静的照合

確認日：2026-11-16。以下はAutoCAD 2026版の署名・座標系・規約を照合した一覧で、ビルド・実機確認とは区別する。
図面DB・所有権・共通APIは [既存のリファレンス](../../docs/references.md) に従う。

| 2026版公式資料 | 使用箇所と確認内容 |
| --- | --- |
| [Polyline()](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Polyline.html) ／ [AddVertexAt](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_AddVertexAt_int_Point2d_double_double_double.html) | `Polyline()`、`void AddVertexAt(int,Point2d,double,double,double)`。ECS入力、0始まり、末尾への追加 |
| [GetPoint2dAt](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_GetPoint2dAt_int.html) ／ [GetPoint3dAt](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_GetPoint3dAt_int.html) | Point2dはOCS、Point3dはWCS。Coordinatesの同じ頂点番号を照合 |
| [Normal](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Normal.html) ／ [Elevation](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Elevation.html) | 読み書き可能なVector3d・double。法線はWCS、平面の高さはOCS |
| [Closed](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Closed.html) ／ [NumberOfVertices](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_NumberOfVertices.html) | 読み書き可能なboolと読み取り専用int。末尾から先頭へ閉じ、取得時は0以上頂点数未満の番号を使う |
| [DXFのOCS](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-DXF/files/GUID-D99F1509-E4E4-47A3-8691-92EA07DC88F5.htm) ／ [LWPOLYLINE](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-DXF/files/GUID-748FC305-F3F2-4F74-825A-61F04D757A50.htm) | OCS原点・平面基準点・各保存値の区別 |
| [任意軸アルゴリズム](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-DXF/files/GUID-E19E5B42-0CC7-4EBA-B29F-5E1D595149EE.htm) | 単位N、AND・厳密な1/64比較、H×NとN×X、単位化 |
| [PlaneToWorld(Vector3d)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_PlaneToWorld_Vector3d.html) | `static Matrix3d PlaneToWorld(Vector3d)`。平面→世界。分岐の根拠はDXF資料 |
| [Point3d.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_TransformBy_Matrix3d.html) ／ [Vector3d.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_TransformBy_Matrix3d.html) | 戻り値Point3d・Vector3d、左からの行列。値型のコピーから結果を保持 |
| [ColorIndex](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_ColorIndex.html) | 読み書き可能なint。新規図形の表示色の区別 |
| [Matrix3dの配列コンストラクター](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Matrix3d_double__.html) ／ [ToArray](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_ToArray.html) | `Matrix3d(double[])`、`double[] ToArray()`。行順の16要素と列に並べる基底を区別し、出力も照合 |
| [WorldToPlane](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_WorldToPlane_Vector3d.html) ／ [Inverse](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Inverse.html) ／ [Transpose](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Transpose.html) | `static Matrix3d WorldToPlane(Vector3d)`、`Matrix3d Inverse()`、`Matrix3d Transpose()`。変換方向と転置を使える条件を区別 |
| [Vector3d.DotProduct](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_DotProduct_Vector3d.html) | `double DotProduct(Vector3d)`。同じWCS成分の単位基底との内積でOCS各成分を得る |

## 第6〜8章の実演

第6章は移動・回転・合成・ローカル基準・逆変換を中心に読む。  
AlignCoordinateSystemによる配置は第6章末の補足、拡大縮小・鏡映・射影は第7章の選択的な補足である。  
任意中心の回転は、中心をコードから読み取るための比較として第7章の最初に置く。

### 追加ファイルと読む順序

| ファイル | 数学的な役割 |
| --- | --- |
| [TranslationCommands.cs](TranslationCommands.cs) | 成分加算・Add・Displacement、点と方向・Entityの違い |
| [RotationCommands.cs](RotationCommands.cs) | sin/cos、Point2d／Point3d、原点／任意中心の回転 |
| [CompositionCommands.cs](CompositionCommands.cs) | 逐次変換・積・Pre/Post・逆変換、対称輪郭の診断 |
| [LocalFrameCommands.cs](LocalFrameCommands.cs) | Bの平面でWCS／ローカル基準の移動・回転を比較 |
| [AlignmentCommands.cs](AlignmentCommands.cs) | 回転＋移動・AlignCoordinateSystem・成分行列・線形結合 |
| [ScalingCommands.cs](ScalingCommands.cs) | 中心からの計算・ScaleBy・一様Scaling |
| [MirroringCommands.cs](MirroringCommands.cs) | YZ鏡映3方式とY軸の半回転を区別 |
| [ProjectionCommands.cs](ProjectionCommands.cs) | 内積・OrthoProject・法線／斜方向のProject |
| [BasicTransformDrawing.cs](BasicTransformDrawing.cs) | 固定入力・G17出力・輪郭と比較点の描画のみを共有 |

### コマンド・固定入力・期待値

| コマンド | 固定入力 | 数学上の期待結果 | 新規Line |
| --- | --- | --- | --- |
| `GEOAT_TRANSLATION` | WCS頂点2(3,2,0)、d=(10,0,3) | 3方式で(13,2,3)。方向(3,2,1)は不変 | 28本 |
| `GEOAT_ROTATION` | 頂点2、原点・+Z・+π/2 | 4方式で(−2,3,0) | 35本 |
| `GEOAT_COMPOSITION` | 同じR、T=(10,0,3) | 方式1〜4のTRは(8,3,3)、方式5のRTは(−2,13,3)。逆変換で元の点へ | 42本 |
| `GEOAT_LOCAL_FRAME` | C基準(3,2,0)、Bの軸、C=(0,3,4) | 移動：WCS Xへ2は(−1,7/5,26/5)、ローカルXへ2は(−5,7/5,26/5)。回転：WCS原点は(−7/5,−3,26/5)、C・Nは(2,3/5,29/5) | 49本 |
| `GEOAT_ALIGNMENT` | 標準フレームからC=(10,0,3)、X=+Y、Y=−X、Z=+Z | 4方式で頂点2は(8,3,3)。原点と3軸上の点も照合 | 35本 |
| `GEOAT_CENTER_ROTATION` | 頂点2、中心C=(1,1,0)、+Z・+π/2 | 5方式で(0,3,0)。Cは固定 | 42本 |
| `GEOAT_SCALING` | 頂点2、中心C=(1,1,0)、正の一様2倍 | 3方式で(5,3,0)。逆行列で(3,2,0) | 28本 |
| `GEOAT_MIRRORING` | 頂点2、YZ平面。比較用にY軸の+π回転 | 鏡映3方式は(−3,2,0)。半回転と違いが出るZ点は鏡映で+Z、半回転で−Z | 35本 |
| `GEOAT_PROJECTION` | Bを2N進めた輪郭。頂点2はQ=(−3,13/5,34/5) | 正射影3方式はP=(−3,7/5,26/5)。斜方向D=(1,0,−1)の2方式は(−1/2,13/5,43/10) | 42本 |

これらは数式による期待値であり、AutoCAD実機の出力記録ではない。

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

[本文の第6〜8章](../../../sessions/autocad-transform/explanation.md) ／ [実演の説明](../../../sessions/autocad-transform/examples/README.md)


[実践教材のトップへ戻る](../../../sessions/autocad-transform/README.md) ／ [AutoCAD参考コードのトップへ戻る](../../README.md)

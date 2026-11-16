# AutoCAD実践編：図形変換の実演コード

[同じ図形変換のいろいろな実現方法を見てみよう](../../../sessions/autocad-transform/README.md) の実演コードを管理する場所。
WCS・OCS・任意軸アルゴリズムを最優先に、異なるAPIや実装方法を数学で読み解く。
第1〜3章の参考コードを収録するが、AutoCAD参照DLLによるビルド・NETLOAD・実機実行・表示は未検証である。

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
| 4. [TransformDemoSupport.cs](TransformDemoSupport.cs) | 数値出力・Transaction・表示用Line。数学と描画を分離 |

## 起動コマンド・固定入力・期待値

利用者が職場の対象環境で確認する場合、既存ソリューションをビルドし、専用空図面でDLLを `NETLOAD` して次のコマンドを起動する。
これは確認手順であり、実行成功の記録ではない。
前提は [共通セットアップ](../../docs/setup.md) に従う。

| コマンド | 固定入力 | 数学上の期待値 | 副作用 |
| --- | --- | --- | --- |
| `GEOAT_COORDINATES` | OCS輪郭(0,0),(4,0),(3,2),(0,1)。A：N=+Z,h=0。B：N=(0,3/5,4/5),h=5 | 頂点2のOCS値(3,2)に対し、WCSはA=(3,2,0)、B=(−3,7/5,26/5)。BのC=(0,3,4)、X=(−1,0,0)、Y=(0,−4/5,3/5) | 新規Polyline2個・Line7本 |
| `GEOAT_ARBITRARY_AXIS` | 正負Z、+X、B、Z近傍、X境界の直前・等号・直後、Y境界の等号、(1,2,2)方向の10例 | 基底長1、各内積0、(X×Y)・N=1。両絶対成分が1/64未満だけY分岐、等号はZ分岐。自作/API成分差が小さいことを照合 | 新規Line9本 |

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

[実践教材のトップへ戻る](../../../sessions/autocad-transform/README.md) ／ [AutoCAD参考コードのトップへ戻る](../../README.md)

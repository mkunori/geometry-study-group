# AutoCAD実践編：図形変換の実演コード

[同じ図形変換のいろいろな実現方法を見てみよう](../../../sessions/autocad-transform/README.md) の実演コードを管理する場所。  
WCS・OCS・任意軸アルゴリズムを最優先に、異なるAPIや実装方法を数学で読み解く。  
全8章に対応する参考コードを収録するが、AutoCAD参照DLLによるビルド・NETLOAD・実機実行・表示は未検証である。

## 共通ソリューションとの関係

既存の [AutoCadGeometrySamples.sln](../../AutoCadGeometrySamples.sln) と [AutoCadGeometrySamples.csproj](../../AutoCadGeometrySamples.csproj) を使う。  
プロジェクトは `samples/**/*.cs` を収録し、セッション側にコードや別ソリューションを複製しない。  
対象環境とローカルDLL参照は [共通セットアップ](../../docs/setup.md) に従う。

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
| 8. [TranslationCommands.cs](TranslationCommands.cs) | 成分加算・Add・Displacement、点と方向・Entityの違い |
| 9. [RotationCommands.cs](RotationCommands.cs) | sin/cos、Point2d／Point3d、原点／任意中心の回転 |
| 10. [CompositionCommands.cs](CompositionCommands.cs) | 逐次変換・積・Pre/Post・逆変換、対称輪郭の診断 |
| 11. [LocalFrameCommands.cs](LocalFrameCommands.cs) | Bの平面でWCS／ローカル基準の移動・回転を比較 |
| 12. [AlignmentCommands.cs](AlignmentCommands.cs) | 回転＋移動・AlignCoordinateSystem・成分行列・線形結合 |
| 13. [ScalingCommands.cs](ScalingCommands.cs) | 中心からの計算・ScaleBy・一様Scaling |
| 14. [MirroringCommands.cs](MirroringCommands.cs) | YZ鏡映3方式とY軸の半回転を区別 |
| 15. [ProjectionCommands.cs](ProjectionCommands.cs) | 内積・OrthoProject・法線／斜方向のProject |
| 16. [BasicTransformDrawing.cs](BasicTransformDrawing.cs) | 固定入力・G17出力・輪郭と比較点の描画のみを共有 |

## 実演で使う案内

入力・期待値・停止箇所・副作用は、次の案内へまとめる。  
実測結果と数学上の期待値を分け、コードを変更するときもこの案内と本文を照合する。

| 案内 | 確認する内容 |
| --- | --- |
| [実演の順序](../../../sessions/autocad-transform/examples/README.md#実演の順序) | 必須の4コマンドから始め、基本変換と補足を選ぶ |
| [Visual StudioとAutoCADで開く準備](../../../sessions/autocad-transform/examples/README.md#visual-studioとautocadで開く準備) | Debug構成・DLL／PDB・アタッチと、最初の4コマンドの停止候補 |
| [コマンド・固定入力・期待結果](../../../sessions/autocad-transform/examples/README.md#コマンド固定入力期待結果) | 第1〜5章の4コマンド、保存値・座標・基底・高さ |
| [第6〜8章の実演](../../../sessions/autocad-transform/examples/README.md#第68章の実演) | 9コマンドの固定値・結果、共通の結果表示で止める場所、各コマンドの変数 |
| [再実行と確認記録](../../../sessions/autocad-transform/examples/README.md#再実行と確認記録) | 重複追加、ロード済みDLL、静的確認と実機未検証の区別 |

13コマンドはすべて引数・選択なしで、現在図面のモデル空間へ新規図形を追加する。  
保存・既存図形・UCS・ビュー・ズーム・レイヤー設定は変更せず、再実行は同位置へ重複追加する。  
Commit前の例外はTransaction破棄でロールバックするが、Commit後の失敗まで変更なしとは断定しない。  
通常の非Sessionコマンドが対象で、別図面・モデルレスのDocumentLock管理は扱わない。  
Translationの一時LineはDBへ登録せず破棄する。  
AutoCADを必要とする実演を、数学基礎編のプロジェクト・自動テスト・既存CIへ混ぜない。

## 公式資料との静的照合

確認日：2026-11-16。以下はAutoCAD 2026版の署名・座標系・規約を照合した一覧で、ビルド・実機確認とは区別する。
図面DB・所有権・共通APIは [既存のリファレンス](../../docs/references.md) に従う。

| 2026版公式資料 | 使用箇所と確認内容 |
| --- | --- |
| [Polyline()](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Polyline.html) ／ [AddVertexAt](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_AddVertexAt_int_Point2d_double_double_double.html) | `Polyline()`、`void AddVertexAt(int,Point2d,double,double,double)`。ECS入力、0始まり、末尾への追加 |
| [GetPoint2dAt](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_GetPoint2dAt_int.html) ／ [GetPoint3dAt](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_GetPoint3dAt_int.html) | Point2dはOCS、Point3dはWCS。Coordinatesの同じ頂点番号を照合 |
| [Normal](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Normal.html) ／ [Elevation](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Elevation.html) | 読み書き可能なVector3d・double。法線はWCS、平面の高さはOCS |
| [Closed](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Closed.html) ／ [NumberOfVertices](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_NumberOfVertices.html) | 読み書き可能なboolと読み取り専用int。末尾から先頭へ閉じ、取得時は0以上頂点数未満の番号を使う |
| [DXFのOCS](https://help.autodesk.com/cloudhelp/2026/JPN/AutoCAD-DXF/files/GUID-D99F1509-E4E4-47A3-8691-92EA07DC88F5.htm) ／ [LWPOLYLINE](https://help.autodesk.com/cloudhelp/2026/JPN/AutoCAD-DXF/files/GUID-748FC305-F3F2-4F74-825A-61F04D757A50.htm) | OCS原点・平面基準点・各保存値の区別 |
| [任意軸アルゴリズム](https://help.autodesk.com/cloudhelp/2026/JPN/AutoCAD-DXF/files/GUID-E19E5B42-0CC7-4EBA-B29F-5E1D595149EE.htm) | 単位N、AND・厳密な1/64比較、H×NとN×X、単位化 |
| [PlaneToWorld(Vector3d)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_PlaneToWorld_Vector3d.html) | `static Matrix3d PlaneToWorld(Vector3d)`。平面→世界。分岐の根拠はDXF資料 |
| [Point3d.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_TransformBy_Matrix3d.html) ／ [Vector3d.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_TransformBy_Matrix3d.html) | 戻り値Point3d・Vector3d、左からの行列。値型のコピーから結果を保持 |
| [ColorIndex](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_ColorIndex.html) | 読み書き可能なint。新規図形の表示色の区別 |
| [Matrix3dの配列コンストラクター](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Matrix3d_double__.html) ／ [ToArray](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_ToArray.html) | `Matrix3d(double[])`、`double[] ToArray()`。行順の16要素と列に並べる基底を区別し、出力も照合 |
| [WorldToPlane](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_WorldToPlane_Vector3d.html) ／ [Inverse](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Inverse.html) ／ [Transpose](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Transpose.html) | `static Matrix3d WorldToPlane(Vector3d)`、`Matrix3d Inverse()`、`Matrix3d Transpose()`。変換方向と転置を使える条件を区別 |
| [Vector3d.DotProduct](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_DotProduct_Vector3d.html) | `double DotProduct(Vector3d)`。同じWCS成分の単位基底との内積でOCS各成分を得る |

第6〜7章で使う回転・合成・配置・スケール・鏡映・射影の署名と条件は、[本文の公式出典と検証範囲](../../../sessions/autocad-transform/explanation.md#第67章の公式出典と検証範囲) に従う。  
Managed資料のMirrorとProjectに混在する説明は、Planeを渡す署名と2026版ネイティブ幾何資料を照合して読む。  
その静的照合を、実機での結果や例外の確認済みという意味には扱わない。

[実践教材のトップへ戻る](../../../sessions/autocad-transform/README.md) ／ [AutoCAD参考コードのトップへ戻る](../../README.md)

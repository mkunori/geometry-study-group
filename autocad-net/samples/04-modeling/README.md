# 第4回：2Dから3Dモデル

**公式資料とAPI仕様を静的に照合した参考コード／AutoCAD実機未検証・ビルド未検証。** Windows版AutoCAD 2026 Update 1.2（2026.1.2）以降／.NET 10／Visual Studio 2026を対象にする。NETLOAD・コマンド実行・図形生成は未確認。

[ModelingCommands.cs](ModelingCommands.cs) は共通の [Class Library](../../AutoCadGeometrySamples.csproj) 内の参考コード。[数学教材の詳細解説](../../../sessions/04-modeling/explanation.md)、[ModelingMath.cs](../../../sessions/04-modeling/examples/ModelingMath.cs)、[MeshBuilder.cs](../../../sessions/04-modeling/examples/MeshBuilder.cs)、[Program.cs](../../../sessions/04-modeling/examples/Program.cs) の固定例Bの直柱と、固定例Cの経路・フレームへ対応させる。数学教材の本文・実装・期待値・公開状態は変更しない。

## 読む順序と手動実行の案内

[00：共通導入](../00-common/README.md) と [セットアップ](../../docs/setup.md) を読み、`CreateLocalProfile` → `BuildLocalSolid` → `LocalToWorld` → `AppendToModelSpace` の順に追う。断面・生成物・図面DBへ登録する対象の違いを、AutoCADなしでもソースで確認できる。

利用者が対象製品・更新版・参照DLLを用意して手動確認する場合は、セットアップに従って [共通ソリューション](../../AutoCadGeometrySamples.sln) をVisual Studioでビルドし、専用の空図面で生成DLLを `NETLOAD` する。その後、次のコマンド名を入力する。座標入力や既存図形の選択は不要。これは確認手順の案内であり、実行済みの記録ではない。

| コマンド | 図面へ増えるもの | 位置・形の数学上の期待値 |
| --- | --- | --- |
| `GEO04_PROFILE` | 閉じたPolylineを1つ | X=0のYZ平面。Y=-1〜1、Z=-0.5〜0.5の長方形輪郭 |
| `GEO04_EXTRUDE` | Solid3dを1つ | X=0〜5、Y=-1〜1、Z=-0.5〜0.5の直柱。断面積2、体積10 |
| `GEO04_SWEEP` | 直線区間だけを使うSolid3dを1つ | 最終経路(0,0,0)→(5,0,0)。同じ位置・断面の直柱を意図する |

長さは作図単位で、mmなどは仮定しない。既存図形は消去・編集せず、図面の保存・UCS・表示方向・ズームも変更しない。図面がなければ何もしない。再実行は同位置への重複追加となり、押し出しとスイープも同位置に重なるため、利用者が見比べる場合は別々の空図面で確認する。

## ローカルの閉じた長方形断面

`CreateLocalProfile` はPolylineの法線をWCS +Z、Elevationを0に設定してから、次の順序で頂点を追加する。

| 番号 | 局所(u,v) | 次の辺 |
| --- | --- | --- |
| 0 | (-1,-0.5) | +u方向 |
| 1 | (1,-0.5) | +v方向 |
| 2 | (1,0.5) | -u方向 |
| 3 | (-1,0.5) | Closedで0へ戻る |

X右・Y上で反時計回り。`AddVertexAt` のPoint2dは**PolylineのECS**の座標であり、一般にWCSのXY座標ではない。本例は法線+Z・標高0なので、生成段階のECSの(u,v)とWCSのXYが一致する。現在のUCSを使う入力処理ではない。

bulge・始端幅・終端幅はすべて0で、直線辺だけにする。始点を5番目に重複保存せず、`Closed = true` が最後の頂点と最初の頂点を結ぶ。幅2・高さ1の単純で閉じた平面輪郭に限定する。

$$
A = 2\cdot1 = 2
$$

穴・自己交差・開いた輪郭・重複点・零長辺・ゼロ面積・非平面輪郭を受け付ける汎用処理は作らない。座標は固定値であり、利用者入力の検証や輪郭の修復も対象外。

## PolylineからRegion、Solid3dへの対応

`GEO04_PROFILE` は輪郭を配置してDB登録するだけ。押し出し・スイープでは、別に作った一時輪郭を次の順に扱う。

1. `profile.Explode(curves)` で、DB未登録のLineへ分解する。Polylineそのものを消去する操作ではない。
2. この長方形に対応する4本のLineであることを検査する。
3. `Region.CreateFromCurves(curves)` で閉ループからRegionを作る。
4. 生成結果が単一のRegionであることを検査し、想定外の件数・型なら中止する。
5. 空のSolid3dへ押し出し、または直線スイープの形を生成する。
6. Solid3dを最終フレームへ配置し、そのSolid3dだけをモデル空間へ登録する。一時Polyline・Line・Region・経路は登録しない。

Region.CreateFromCurvesの2026 ReferenceにはLine、Arc、Ellipse、Circle、Spline、Polyline3d、Polyline2dが入力型として列挙される。一方、Developer's Guideは閉じたポリラインからの変換も説明している。本例はReferenceに明記されたLineへ分解して渡し、軽量Polylineを直接渡すことに依存しない。

同Referenceは既存の入力曲線をForWriteで開かないよう注意している。今回の入力はメモリ上の一時Lineであり、既存図形をGetObjectで開いて渡さない。Developer's Guideのメモリ上の一時曲線を使う例とも照合した。複数のRegionが生成される場合もあるため、戻り値を無条件に先頭1個だけ使って残りを放置しない。

押し出しには次のオーバーロードを採用する。

```csharp
void CreateExtrudedSolid(Entity sweepEntity, Vector3d directionVector, SweepOptions sweepOptions);
```

`sweepEntity` は一時Region。ローカルの方向ベクトル (0,0,5) は、向きが+Z、長さが5なので、断面に垂直な距離5の押し出しになる。方向と距離の意味、およびRegionが対象であることは2026 ObjectARXの対応するメソッドでも確認した。

`CreateOptions(false)` はAlignをNoAlignment、DraftAngleを0、ScaleFactorを1、TwistAngleを0にして、テーパー・拡縮・ねじれを加えない。SweepOptionsはBuilderから生成してusingで破棄する。成功を保証する入力修復やモデルカーネルのエラー回避処理ではない。

## 教材と同じN・B・Tフレームへ配置する

今回使うのは固定例Bの始点C=(0,0,0)、N=+Y、B=+Z、T=+X。固定例Aの中心 (10,20,30) は使わない。ローカルのu/vが断面内、wが押し出し方向の座標に対応する。

$$
P(u,v,w)=C+u\mathbf{N}+v\mathbf{B}+w\mathbf{T}
$$

`LocalToWorld` は `AlignCoordinateSystem` のfromに標準フレーム、toにCとN/B/Tをこの順で渡す。第1回と同じ列方式では次の行列になる。

$$
M =
\begin{bmatrix}
0 & 0 & 1 & 0 \\
1 & 0 & 0 & 0 \\
0 & 1 & 0 & 0 \\
0 & 0 & 0 & 1
\end{bmatrix}
$$

$$
\begin{aligned}
P(u,v,w) &=
\begin{bmatrix}
w \\
u \\
v
\end{bmatrix}
\end{aligned}
$$

ローカルXY面の+Z法線はT=+Xへ、ローカルZ=0〜5はWCSのX=0〜5へ移る。N×B=Tで、反射やスケールを含まない右手系の剛体変換。Polylineの座標を後からAddVertexAtへWCSとして再投入せず、生成後のEntityに `TransformBy(M)` を適用する。

| 断面頂点 | 始端のWCS座標 | 終端のWCS座標 |
| --- | --- | --- |
| 0 | (0,-1,-0.5) | (5,-1,-0.5) |
| 1 | (0,1,-0.5) | (5,1,-0.5) |
| 2 | (0,1,0.5) | (5,1,0.5) |
| 3 | (0,-1,0.5) | (5,-1,0.5) |

この8座標は既存Program.csのExpectedVerticesと一致する数学上の期待値。CADソリッドの頂点列を取得して照合した値ではない。

## 外向き法線・体積とメッシュとの違い

輪郭の法線+Xと、閉じた直柱の始端キャップの外向き法線-Xは役割が違う。数学教材の外向きの表面構成は次の通り。

| 面 | 外向き法線の期待値 |
| --- | --- |
| 始端 X=0 | -X |
| 終端 X=5 | +X |
| u=-1／u=1の側面 | -Y／+Y |
| v=-0.5／v=0.5の側面 | -Z／+Z |

ローカルの始端-Z・終端+Zと側面の外向きを、同じ剛体変換で移したもの。数学教材は各側面とキャップを三角形へ分け、**8共有頂点・12三角形**の表面メッシュを構成する。一方、Solid3dは面・辺などの境界表現を持つCADソリッドであり、このコードは三角形配列を渡さない。AutoCADが12三角形を保存する、B-repの面・辺番号がメッシュの番号に一致する、とは主張しない。

$$
V=A\cdot L=2\cdot5=10
$$

10は閉じた直柱の幾何学的な体積で、単位は作図単位の3乗。メッシュで囲む立体と、このソリッド生成で意図する立体が同じだから同じ値を期待する。Solid3dの体積取得APIは使用せず、実際の体積・B-repの面法線・カーネルが作った境界構造は未確認である。

## 発展：スイープはまず直線区間だけに限定する

教材の折れ線 (0,0,0)→(5,0,0)→(5,4,0) は、位置だけでは断面の姿勢とコーナーの接続が決まらない例。Tを接線、N/Bを断面内の基底とする。

| 場所 | T | N | B |
| --- | --- | --- | --- |
| 始点から最初の区間 | +X | +Y | +Z |
| 曲がり角の後から終点 | +Y | -X | +Z |

両フレームともN×B=T。ただし角 (5,0,0) の局所点 (1,0) は、角の手前のフレームで (5,1,0)、後のフレームで (4,0,0) となる。断面を単純につなげるだけでは、留め継ぎ・丸め・ねじれや重なりの処理を決められない。

`GEO04_SWEEP` はこの経路の**最初の直線区間だけ**を対象とする。ローカルではXY断面と (0,0,0)→(0,0,5) の一時Lineを用い、生成後の配置でWCSの (0,0,0)→(5,0,0) に対応させる。折れ線全体や終点 (5,4,0) までのソリッドは作らず、経路のLineもDBへ登録しない。

```csharp
void CreateSweptSolid(Entity sweepEntity, Entity pathEntity, SweepOptions sweepOptions);
```

スイープ対象はRegion、経路はLine。2026 ObjectARX Referenceの要求に従い、`CreateOptions(true)` は `AlignSweepEntityToPath` を明示する。BasePointは断面中心・経路始点と同じ原点、テーパー0・拡縮1・ねじれ0で、最初の直線の一定フレームを意図する。押し出しのNoAlignmentをスイープへ流用しない。

直線の固定例でも実際の成功・姿勢・例外は未確認。折れ線の接線不連続、急曲がり、変曲点、自己交差、断面と経路の不整合などへ一般化せず、必ず生成できるとは説明しない。曲がる経路のフレーム輸送や深いB-rep解析、汎用の穴あり輪郭は範囲外。

## 所有権・図面登録・失敗時の扱い

| オブジェクト | 所有者と破棄・登録 |
| --- | --- |
| 表示用Polyline | `AddProfile` がusingで管理。配置後にモデル空間へ登録し、確定後にラッパーを閉じる |
| 生成用Polyline | `BuildLocalSolid` のusing。DBへ登録しない。作成途中の設定例外もCreateLocalProfile内で破棄する |
| Explodeでできた全Line | 入力curvesの全要素をfinallyで個別にDispose。DBへ登録しない |
| CreateFromCurvesでできた全Region | 戻り値regionsの全要素をfinallyで個別にDispose。件数異常・型違い・ソリッド生成失敗でも同じ経路を通る |
| 2つのDBObjectCollection | それぞれのusing。取り出した要素のラッパーは別に管理し、コレクションのDisposeだけに依存しない |
| SweepOptions／経路Line | 生成を囲むusing。SweepOptionsBuilderは通常のマネージクラス |
| Solid3d | `CreateAndAddSolid` のusing。ローカル生成・一時オブジェクトの後始末・配置が済んだ後、そのSolid3dだけをDBへ登録 |
| Transaction | `AppendToModelSpace` のusing。既存BlockTableとModelSpaceはTransactionが管理 |

DBObjectCollectionの公式仕様では、要素を取り出してラッパーが作られた場合と未取得の場合で、コレクション破棄時の扱いが異なる。本例は全要素を明示的に処理する。未取得のRegionを取り残さず、モデル空間へ移したEntityを一時生成物用のDisposeContentsへ渡さない。

図面DBへの登録は [共通導入](../00-common/README.md) と同じ順序で、ModelSpaceを書き込みで開き、AppendEntity→AddNewlyCreatedDBObject→Commitとする。一時輪郭やRegionを作る段階では既存図形を開かない。現在図面の通常の非Sessionコマンドであり、別図面・モデルレス操作用のDocumentLockの例ではない。

AutoCADの例外はEditorに表示し、生成条件と合わない件数・型も中止として報告する。Commit前の例外ならTransactionの破棄で登録を戻すが、Commit後の後処理まで「変更なし」とは断定しない。生成途中・成功時・例外時の実際の破棄やロールバックは実機未検証。

## 2026版公式APIとの静的照合

確認日：2026-11-01。実際の2026版URLと利用箇所を以下へ記載する。図面DB・Transactionの共通APIは [共通リファレンス](../../docs/references.md) も参照。

| 公式資料 | 署名・規約・利用箇所 |
| --- | --- |
| [Polyline()](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Polyline.html) | 空のPolylineを生成。`CreateLocalProfile` |
| [AddVertexAt](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_AddVertexAt_int_Point2d_double_double_double.html) | `void AddVertexAt(int, Point2d, double, double, double)`。Point2dはECS、bulge・線幅0 |
| [Closed](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Closed.html) | bool、最後の頂点から最初へ閉じる |
| [Normal](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Normal.html) ／ [Elevation](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Elevation.html) | WCSの法線と平面の高さ。ローカル生成時は+Z・0 |
| [Entity.Explode](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_Explode_DBObjectCollection.html) | `void Explode(DBObjectCollection)`。DB未登録の生成物を追記し、呼出し側が所有する |
| [Region.CreateFromCurves](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Region_CreateFromCurves_DBObjectCollection.html) | `static DBObjectCollection CreateFromCurves(DBObjectCollection)`。許可型、閉ループ、入力の開き方、生成Regionの破棄責任 |
| [Create Regions (.NET)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-9CD22AE5-8F66-4925-A155-95852BAFD565.htm) | メモリ上の一時曲線、複数Regionになりうる条件。入力と生成物を区別 |
| [DBObjectCollection](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_DBObjectCollection.html) | DisposableWrapper・IList、要素のラッパー生成と破棄の責任 |
| [Solid3d()](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Solid3d_Solid3d.html) | 空のSolid3dへ形を生成。`CreateAndAddSolid` |
| [CreateExtrudedSolid](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Solid3d_CreateExtrudedSolid_Entity_Vector3d_SweepOptions.html) ／ [対応するObjectARX](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-RefGuide/files/OARX-RefGuide-AcDb3dSolid__createExtrudedSolid_AcDbEntity__AcGeVector3d__AcDbSweepOptions_.html) | void、Entity・Vector3d・SweepOptions。Regionが対象、方向ベクトルの長さが距離 |
| [CreateSweptSolid](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Solid3d_CreateSweptSolid_Entity_Entity_SweepOptions.html) ／ [対応するObjectARX](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-RefGuide/files/OARX-RefGuide-AcDb3dSolid__createSweptSolid_AcDbEntity__AcDbEntity__AcDbSweepOptions_.html) | void、対象Entity・経路Entity・SweepOptions。Regionと経路曲線、AlignSweepEntityToPathの要求 |
| [SweepOptions](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_SweepOptions.html) ／ [SweepOptionsBuilder](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_SweepOptionsBuilder.html) ／ [ToSweepOptions](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_SweepOptionsBuilder_ToSweepOptions.html) | Builderで設定し、返るDisposableWrapperのSweepOptionsをusing管理 |
| [Align](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_SweepOptionsBuilder_Align.html) ／ [列挙型](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_SweepOptionsAlignOption.html) ／ [BasePoint](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_SweepOptionsBuilder_BasePoint.html) | 押し出しとスイープでAlignを分け、整列基点は原点 |
| [DraftAngle](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_SweepOptionsBuilder_DraftAngle.html) ／ [ScaleFactor](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_SweepOptionsBuilder_ScaleFactor.html) ／ [TwistAngle](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_SweepOptionsBuilder_TwistAngle.html) | doubleのオプションを0・1・0に設定 |
| [AlignCoordinateSystem](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_AlignCoordinateSystem_Point3d_Vector3d_Vector3d_Vector3d_Point3d_Vector3d_Vector3d_Vector3d.html) ／ [Entity.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_TransformBy_Matrix3d.html) | 標準フレーム→N/B/Tの配置先、生成したEntity自身を変換 |

## 未検証の範囲

数学上の形・8座標・面の向き・断面積・体積と、公式APIの署名・使用条件・所有権を静的に照合した参考コードである。実DLLの解決、ビルド、NETLOAD、コマンド登録、Polylineの表示、Explode／Regionの件数・向き、ソリッド生成、B-repと体積、例外・破棄・図面登録は未確認。

TODO：利用者が対象環境で手動確認する場合は、各コマンドの追加図形・配置・閉じ方・押し出し距離を数学上の期待値と照合する。スイープは直線区間だけとし、折れ線全体が成功するとは扱わない。SDK・Autodesk DLLや自動検証は追加せず、第4回の準備中／レビュー待ち・PDF自動生成対象外の状態を維持する。

[第4回トップへ戻る](../../../sessions/04-modeling/README.md) ／
[参考コードトップへ戻る](../../README.md) ／ [セットアップ](../../docs/setup.md) ／ [公式リファレンス](../../docs/references.md)

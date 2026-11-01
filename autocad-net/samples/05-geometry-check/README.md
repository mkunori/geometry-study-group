# 第5回：図形の内外・干渉

**公式資料とAPI仕様を静的に照合した参考コード／AutoCAD実機未検証・ビルド未検証。** Windows版AutoCAD 2026 Update 1.2（2026.1.2）以降／.NET 10／Visual Studio 2026を対象にする。NETLOAD・コマンド実行・API結果は未確認。

[InterferenceCommands.cs](InterferenceCommands.cs) は共通の [Class Library](../../AutoCadGeometrySamples.csproj) 内の参考コード。[数学教材の詳細解説](../../../sessions/05-geometry-check/explanation.md)、[AabbChecks.cs](../../../sessions/05-geometry-check/examples/AabbChecks.cs)、[Program.cs](../../../sessions/05-geometry-check/examples/Program.cs) の固定例C・D・Eに対応する。数学教材の本文・実装・期待値・公開状態は変更しない。

## 読む順序と手動実行の案内

[00：共通導入](../00-common/README.md) と [セットアップ](../../docs/setup.md) を読んだ後、下の3コマンドを別の問いとして追う。AutoCADがなくても数式・固定値とソースを読み比べられる。

利用者が対象製品・更新版・参照DLLを用意して手動確認する場合は、セットアップに従って [共通ソリューション](../../AutoCadGeometrySamples.sln) をVisual Studioでビルドし、空図面で生成DLLを `NETLOAD` してコマンド名を入力する。追加の座標入力・図形選択は不要。これは確認手順の案内であり、実行済みの記録ではない。

| コマンド | 問いと表示 | 図面への変更 |
| --- | --- | --- |
| `GEO05_BOUNDS` | GeometricExtentsからXYのAABB候補を計算。長方形A/B/C/Dと離隔した2三角形を比較 | なし |
| `GEO05_INTERSECT` | 有限LineのIntersectWithの点コレクション。AABB候補とは別に件数と全点を表示 | なし |
| `GEO05_SOLIDS` | K/K2・K/K3のCheckInterferenceフラグと、別操作のコピー上のBoolIntersect | なし |

すべてWCSの同じ直交座標系・作図単位で、mmなどは仮定しない。点・幾何型はdoubleで扱い、System.Numericsのfloatを使う教材と区別する。全図形・箱・コピーはDB未登録の一時オブジェクトで、出力はEditorだけ。図面の登録・編集・消去・保存・UCS変更は行わず、Transactionも作らない。図面がなければ戻る。

## 判定の段階を分ける

| 段階／API | 得られるもの | そのままでは得られないもの |
| --- | --- | --- |
| GeometricExtents＋区間比較 | WCS軸に平行な包囲箱と、次の判定へ残す候補 | 実体の内外、接触、正の面積・体積の重なり |
| Entity.IntersectWith | 指定した延長条件でのWCSの返却点コレクション | 重複区間、接触面積、ソリッドの共通体積、点包含の三値分類 |
| Solid3d.CheckInterference | 採用したC#署名ではboolの干渉フラグ | Inside／Boundary／Outside、Separated／Touching／Overlappingの三値分類や共有ソリッド |
| Solid3d.BooleanOperation | 受け手を変更して共通形状などを作る操作 | 原本をそのまま残す保証や、接触・離隔を一律に表す読取専用フラグ |

数学上の「交点がある」「境界だけで接触する」「正の体積が重なる」「包囲箱だけ重なる」を分ける。APIのboolや点の件数へ、教材の分類名を無条件に対応させない。

## AABB：図形の範囲で候補を絞る

`Entity.GeometricExtents` の戻り値は `Autodesk.AutoCAD.DatabaseServices.Extents3d`。MinPoint・MaxPointはWCSの点で、包囲箱の辺はWCSのX/Y/Z軸に平行である。回転した形の局所的な箱や、厳密な外形そのものではない。型・生成状態などによって範囲取得が失敗する場合も、離隔とは扱わない。

2D例はZ=0なので、`ShowBounds2D` はXYだけを比較する。Z幅0を3Dの接触・体積干渉の分類へ混ぜない。各軸の共通区間の候補幅を計算する。

$$
\Delta_x = \min(a_{\max x},b_{\max x})-\max(a_{\min x},b_{\min x})
$$

$$
\Delta_y = \min(a_{\max y},b_{\max y})-\max(a_{\min y},b_{\min y})
$$

いずれかが負なら包囲箱は候補外、両方が非負なら候補。負の値は実在する負の共有長ではなく、区間が離れていることを表す。0を含む候補と、両軸に正幅がある候補も表示で区別するが、実体の接触・重なりの確定には使わない。

| 数学の閉長方形 | X区間 | Y区間 |
| --- | --- | --- |
| A | [0,2] | [0,2] |
| B | [2,4] | [1,3] |
| C | [2.1,4] | [1,3] |
| D | [1,3] | [1,3] |

| 対 | 手計算の候補幅(Δx,Δy) | 長方形領域の数学的な関係 | 共有面積 |
| --- | --- | --- | --- |
| A/B | (0,1) | 境界で接触 | 0 |
| A/C | (-0.1,1) | 離隔 | 0 |
| A/D | (1,1) | 正の面積で重なる | 1 |

ソースは閉じたPolylineでこれらの**輪郭**を作り、その範囲を取得する。Polyline自体が塗りつぶされたRegionになるわけではない。表の面積と分類は、境界を含む長方形領域についての手計算である。SDKが返す範囲・数値は未検証で、-0.1などはdoubleの微小な残差を含みうる。

AABBだけで実体を判定できない例として、第5回のT1とT2も使う。

| 対象 | 頂点 | 実体を区別する条件 |
| --- | --- | --- |
| T1 | (0,0)、(3,0)、(0,3) | x+yは3以下 |
| T2 | (2,2)、(5,2)、(2,5) | x+yは4以上 |

包囲箱 [0,3]² と [2,5]² は正幅で重なるが、三角形は離隔する。候補抽出後に形状に対応する判定が必要な例で、AutoCADのAPIでこの三角形の三値分類を実行したとは扱わない。図形の姿勢・種類・表示状態なども含む一般的な範囲情報から、厳密なソリッド干渉を断定しない。

## 曲線の交点：点集合を体積と読まない

`GEO05_INTERSECT` はZ=0の非零長Lineを使い、`OnBothOperands` でどちらも延長しない。毎回新しいPoint3dCollectionを作り、IntPtr版のGSマーカーはともに0。結果はvoidの戻り値ではなく、引数のコレクションへ追記されるWCSの点である。

| 固定例 | 第1線分 | 第2線分 | 手計算上の関係 |
| --- | --- | --- | --- |
| 交差 | (0,0)→(2,2) | (0,2)→(2,0) | 交点(1,1,0)が1点 |
| 平行 | (0,0)→(2,2) | (0,0.5)→(2,2.5) | AABBは重なるが線分は離隔 |

Countと全返却点を表示する。交点ありは曲線の共通点についての情報で、正の共有面積・体積の重なりではない。空の点コレクションや複数の返却点から、一般のソリッドの非干渉・重複区間・接触面積を推定しない。例外は結果未取得として報告し、点がないという結果へ変換しない。

t/u・端点・延長・同一直線・重複区間の詳しい扱いは [第3回の参考コード](../03-intersection/README.md) を参照。この章では判定の種類の比較に限定し、同じ詳細ソルバーを作らない。

## Solid3d：共通体積1と面接触を分ける

`CreateBoxAt` は正の3辺で一時Solid3dを作り、初期GeometricExtentsのMinPointから希望するWCS下端への変位で配置する。CreateBoxのReferenceは原点を重心として説明しており、原点を下端だと仮定しない。生成後の範囲から位置を合わせ、実際のMinPoint・MaxPointも表示する。

| 箱 | 希望する下端 | 希望する上端 | CreateBoxに渡すX/Y/Z長 |
| --- | --- | --- | --- |
| K | (0,0,0) | (2,2,2) | (2,2,2) |
| K2 | (1,1,1) | (3,3,3) | (2,2,2) |
| K3 | (2,1,1) | (3,2,2) | (1,1,1) |

K/K2の共有集合は [1,2]³。

$$
V_{K\cap K2}=(2-1)(2-1)(2-1)=1
$$

K/K3はx=2、yとzが[1,2]の面を共有する。共有集合は空ではないが、3Dの共通体積は0で、接触面積は1。

$$
V_{K\cap K3}=0\cdot1\cdot1=0
$$

$$
A_{\mathrm{contact}}=1\cdot1=1
$$

これらは既存AabbChecksの固定例Eと同じ手計算。軸平行な箱そのものだから区間積が共有体積に対応するのであり、一般の図形の包囲箱の共通体積を実体の体積と読み替えない。

採用するManaged版の署名は次の通り。

```csharp
bool CheckInterference(Solid3d otherSolid);
```

`ShowInterference` はK/K2とK/K3を別々に呼び、**実際にAPIから得たboolを表示するためのコード**である。ソースと表の数学上の期待値を、API実行済みの結果とは呼ばない。公式Managed Referenceは干渉を調べると説明するが、この署名だけでは面接触をtrue/falseのどちらとして扱うかを確定できない。本例は両方のフラグを未確認とし、教材のTouching／Overlappingへ変換しない。

同Referenceには共有オブジェクトのメモリ責任への注記もあるが、C#署名はboolのみで、共有Solid3dの戻り値やout引数を公開していない。これを独自のC#オーバーロードとして実装しない。対応するC++版は、createNewSolid・solidsInterfere・commonVolumeSolidを持ち、生成した共有ソリッドは呼出し側がDBへ登録するか不要時に破棄する責任を負う。**C++側の出力契約と、このManaged版の利用範囲を区別する。** このコードはC++版を直接呼ばず、フラグから共有ソリッドを取得することもしない。

## 発展：Booleanは両方をコピーしてから行う

共通体積の形を作る参考例は、CheckInterferenceと別の操作として `ShowBooleanIntersection` に分ける。

```csharp
void BooleanOperation(BooleanOperationType operation, Solid3d solid);
```

KとK2の両方をCloneして、受け手のresultへ `BoolIntersect` を適用する。共通形状は新しい戻り値ではなく、**変更された受け手result**。元のK/K2にはBooleanを適用しない。数学上の期待形状は [1,2]³で体積1だが、コードは結果の包囲箱だけを表示し、体積測定は行わない。

公式資料では成功時に引数側のShapeManagerが削除され、対応するObjectARX Referenceでは失敗時に双方のShapeManagerが削除される場合も説明される。そのため引数のoperandもコピーにし、操作後の形を再利用しない。失敗後にコピーへ再試行したり、原本へ切り替えて続行したりしない。

Cloneの戻り値はobjectで、失敗時にnullになりうる。`CloneSolid` はSolid3dであることを調べてから所有権を渡し、想定外の破棄可能な結果も片付ける。受け手・引数の両コピーはusingで破棄し、Boolean後に中身を消費されたラッパーも閉じる。結果はDBへ追加しない。

Boolean例は既知の正体積の組K/K2だけに限定し、干渉フラグを条件にせず別途実行する。面接触のK/K3へ一律にBoolIntersectを適用して、空形状や例外から接触・離隔を決める処理は作らない。例外や結果範囲の取得失敗も、共通体積0とは扱わない。

## 数学教材の内外・SATはReferenceとして残す

| 数学教材の方法 | 対応する参照先・適用範囲 |
| --- | --- |
| Inside／Boundary／Outside | [L字多角形のレイ法](../../../sessions/05-geometry-check/examples/PolygonChecks.cs)。境界の先行判定・半開区間を含む数学のアルゴリズム |
| Separated／Touching／Overlapping | [2D凸SAT](../../../sessions/05-geometry-check/examples/ConvexSat.cs)。凸性などの前提を持つ射影区間の判定 |
| 3D凸箱の点包含 | [半空間テスト](../../../sessions/05-geometry-check/examples/ConvexBoxChecks.cs)。外向き面法線の条件を組み合わせる |

GeometricExtents・IntersectWith・CheckInterferenceが、これらの三値分類や一般メッシュの点包含をそのまま実装するとは説明しない。特にAABB内の点が、凹L字の内部とは限らない。API側で明確に確認できない分類は、数学の参照方法として分離する。

## 許容誤差・失敗・所有権

候補幅の負・0・正は、取得したdouble値に対してそのまま比較する。微小な正の隙間を固定しきい値で0へ丸めて接触と扱わない。一方、これはCADカーネルの許容差と一致する保証ではない。ソリッド生成・干渉・Booleanの許容差、図形の尺度や姿勢による影響は実機未確認で、単一のBooleanや固定しきい値で全ての状態を決めない。

非有限・逆転した範囲や非有限の候補幅は判定保留。範囲取得・交点・干渉・Booleanの例外は結果未取得として示し、通常の候補外・false・共通体積0へ変換しない。数値は有効数字17桁で表示する。

| 対象 | 所有者・後始末 |
| --- | --- |
| 一時Polyline・Line | 各コマンドのusing。生成途中のPolyline設定例外も作成ヘルパー内でDisposeして再送出 |
| Point3dCollection | 各IntersectWith呼出しを囲むusing。点は値型で、DB登録しない |
| K・K2・K3 | 呼出し側のusing。CreateBoxAtの生成・移動が失敗した場合も内部でDispose |
| Cloneした両Solid3d | Booleanのusing。成功・マネージ例外のどちらも破棄経路を通し、結果も保存しない |
| DB・Transaction | 一時図形をDBへ追加せず、既存図形も開かないため作成しない。図面変更を伴う登録方法は [第4回](../04-modeling/README.md) を参照 |

この章にはModelSpaceへの登録・AppendEntity・Commit・既存図形の消去がない。図面に可視化する場合の所有権移転は別途設計する。例外時の実際の破棄、原本が保たれること、Booleanで消費されたラッパーの後始末を実機確認したとは扱わない。

## 2026版公式APIとの静的照合

確認日：2026-11-01。実際の2026版URLと利用箇所を以下へ記載する。PolylineのECS・Closedと一時図形の基本は第4回、IntersectWithの延長規約は第3回の参照一覧も参照。

| 公式資料 | 署名・規約・利用箇所 |
| --- | --- |
| [Entity.GeometricExtents](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_GeometricExtents.html) | 読取専用Extents3d。WCS軸平行の包囲範囲。候補と一時箱の配置に使用 |
| [Extents3d.MinPoint](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Extents3d_MinPoint.html) ／ [MaxPoint](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Extents3d_MaxPoint.html) | 読取専用Point3d。名前空間はDatabaseServices |
| [Entity.IntersectWith](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_IntersectWith_Entity_Intersect_Point3dCollection_IntPtr_IntPtr.html) | `void IntersectWith(Entity, Intersect, Point3dCollection, IntPtr, IntPtr)`。追記・WCS・マーカー0 |
| [Intersect](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Intersect.html) ／ [Point3dCollection](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3dCollection.html) | 延長しないOnBothOperands。コレクションのIList・DisposableWrapperと全点の列挙 |
| [Solid3d.CreateBox](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Solid3d_CreateBox_double_double_double.html) | `void CreateBox(double, double, double)`。WCS軸に沿う3辺と原点重心の説明 |
| [Matrix3d.Displacement](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Displacement_Vector3d.html) ／ [Entity.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_TransformBy_Matrix3d.html) | 初期MinPointから指定下端への変位で一時箱を移動 |
| [Solid3d.CheckInterference](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Solid3d_CheckInterference_Solid3d.html) | `bool CheckInterference(Solid3d)`。このC#署名は共有ソリッドを公開せず、面接触のフラグは未確定 |
| [C++側のcheckInterference](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-RefGuide/files/OARX-RefGuide-AcDb3dSolid__checkInterference_AcDb3dSolid__Adesk__Boolean_Adesk__Boolean__AcDb3dSolid___const.html) | 共有ソリッドの生成指定・出力・所有権があるC++契約とManaged版を区別。直接呼ばない |
| [Solid3d.BooleanOperation](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Solid3d_BooleanOperation_BooleanOperationType_Solid3d.html) ／ [C++側のbooleanOper](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-RefGuide/files/OARX-RefGuide-AcDb3dSolid__booleanOper_AcDb__BoolOperType_AcDb3dSolid_.html) | void、受け手の変更と引数側のShapeManager消費、失敗時の双方の扱い |
| [BooleanOperationType](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_BooleanOperationType.html) ／ [RXObject.Clone](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Runtime_RXObject_Clone.html) | BoolIntersect、`object Clone()`と失敗時のnull。両方をコピーして型を検査 |

## 未検証の範囲

数式・固定入力・数学上の期待値と、公式APIの署名・規約・所有権を静的に照合した参考コードである。実DLLの解決、ビルド、NETLOAD、コマンド登録、範囲・点・干渉フラグ、Cloneの独立性、Booleanの形・体積、例外と破棄は未確認。


[第5回トップへ戻る](../../../sessions/05-geometry-check/README.md) ／
[参考コードトップへ戻る](../../README.md) ／ [セットアップ](../../docs/setup.md) ／ [公式リファレンス](../../docs/references.md)

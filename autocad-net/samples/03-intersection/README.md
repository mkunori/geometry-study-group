# 第3回：直線・平面の交点

**公式資料とAPI仕様を静的に照合した参考コード／AutoCAD実機未検証・ビルド未検証。** Windows版AutoCAD 2026 Update 1.2（2026.1.2）以降／.NET 10／Visual Studio 2026を対象にする。NETLOAD・コマンド実行・APIの返却点は未確認。

[IntersectionCommands.cs](IntersectionCommands.cs) を共通の [Class Library](../../AutoCadGeometrySamples.csproj) で扱う。[数学教材の詳細解説](../../../sessions/03-intersection/explanation.md)、[IntersectionMath.cs](../../../sessions/03-intersection/examples/IntersectionMath.cs)、[Program.cs](../../../sessions/03-intersection/examples/Program.cs)、[既存の結果分類](../../../sessions/03-intersection/examples/IntersectionResults.cs) と同じ固定例を使用する。数学教材の本文・実装・期待値・公開状態は変更しない。

## 読む順序と手動実行の案内

[00：共通導入](../00-common/README.md) と [セットアップ](../../docs/setup.md) を読んだ後、数式による分類とAPI返却点の表示を分けてソースを追う。AutoCADのない環境でも読める参考例である。

利用者が対象製品・更新版・参照DLLを用意して手動確認する場合は、セットアップに従って [共通ソリューション](../../AutoCadGeometrySamples.sln) をVisual Studioでビルドし、空図面で生成DLLを `NETLOAD` する。その後、次のコマンド名を入力する。追加の座標入力や図形選択は不要。これは確認手順の案内であり、実行済みの記録ではない。

| コマンド／メソッド | 扱う例 | 計算・出力 |
| --- | --- | --- |
| `GEO03_LINES` / `LinesAndSegments` | 2線分の内部交差・範囲外・端点 | 数式によるt/uと分類、4種類の延長指定によるAPI返却点 |
| `GEO03_OVERLAP` / `ParallelAndOverlap` | 平行・同一無限直線・重複区間・離れた区間・端点・零長・ほぼ平行 | 数学式の補助分類のみ。これらの例ではLineも点コレクションも作らない |
| `GEO03_PLANES` / `LinesAndPlanes` | 直線と無限平面、z=2とx=1の交線 | Point3d・Vector3dで数式を計算。Plane／Line3dの交差APIは呼ばない |

全コマンドは現在図面のEditorに表示するだけ。既存図形の選択・編集、新規図形のDB登録、UCS変更、保存は行わない。図面がなければ戻る。自動ビルド・自動テスト・GitHub Actionsには組み込まない。

## 図面エンティティと数学的な図形を分ける

| 対象 | この参考コードでの扱い |
| --- | --- |
| `Autodesk.AutoCAD.DatabaseServices.Line` | 始点・終点を持つ図面エンティティ。今回だけはDB未登録の一時オブジェクトとして作り、IntersectWithへ渡す |
| `Autodesk.AutoCAD.Geometry.Line3d` | 数学的な無限直線。Lineの別名ではない。本例では作成しない |
| 数学の無限直線 | P+tr。tは任意の実数。2DはWCSのZ=0に限定 |
| 数学の閉線分 | P〜P+r。終点までの変位rを正規化せず、tを両端込みの[0,1]に制限 |
| 数学の無限平面 | 点Aと非ゼロ法線nで表す。三角形ABCから法線を作っても三角形の内部には制限しない |

すべてWCSの同じ直交座標系を用い、3Dは右手系。現在のUCSや画面上の投影へ変換する例ではない。長さは作図単位で、mmなどを仮定しない。幾何型の成分はdoubleで扱い、System.Numericsのfloatによる既存教材とは混同しない。

## 2Dの交点式と閉線分の範囲

第3回の固定例Aを、Z=0へ埋め込む。

| 要素 | 値 |
| --- | --- |
| P、r | (0,0,0)、(4,4,0) |
| Q、s | (0,4,0)、(4,-4,0) |
| 第1Lineの両端 | (0,0,0)→(4,4,0) |
| 第2Lineの両端 | (0,4,0)→(4,0,0) |

`ShowLinePair` は始点から終点への `GetVectorTo` でrとsを作る。交点では次が成立する。

$$
P+t\mathbf{r}=Q+u\mathbf{s}
$$

w=Q-Pとして、s・rとの2Dスカラー外積を取るとtとuを取り出せる。ソースでは `CrossProduct` のZ成分が2Dスカラー外積に対応する。

$$
\begin{aligned}
D &= \mathrm{cross}_2(\mathbf{r},\mathbf{s}) \\
&= 4(-4)-4\cdot4 \\
&= -32
\end{aligned}
$$

$$
\begin{aligned}
t &= \frac{\mathrm{cross}_2(\mathbf{w},\mathbf{s})}{D} \\
&= \frac{-16}{-32} = 0.5
\end{aligned}
$$

$$
\begin{aligned}
u &= \frac{\mathrm{cross}_2(\mathbf{w},\mathbf{r})}{D} \\
&= \frac{-16}{-32} = 0.5
\end{aligned}
$$

両方の式に代入すると (2,2,0) になる。tとuがともに[0,1]の内部なので、有限線分でも内部の1点で交わる。無限直線の解を得る処理と、閉区間を検査する処理を分けている。

| 固定例 | 第1線分 | 第2線分 | t、u | 無限直線の期待値 | 閉線分の期待分類 |
| --- | --- | --- | --- | --- | --- |
| A：内部 | (0,0)→(4,4) | (0,4)→(4,0) | 0.5、0.5 | (2,2,0) | 内部の1点 |
| C：範囲外 | (0,0)→(2,0) | (3,-1)→(3,1) | 1.5、0.5 | (3,0,0) | 共有点なし |
| C：端点 | (0,0)→(2,0) | (2,-1)→(2,1) | 1、0.5 | (2,0,0) | 端点で接触する1点 |

表の2D座標はすべてZ=0。これらは**数式からの期待値**であり、APIで同じ値が返ったという記録ではない。

## Entity.IntersectWithの署名・延長・点集合

使うのはPlaneを受け取らない、次のIntPtr版。long版は2026の公式オーバーロード一覧でObsoleteとなっている。

```csharp
void IntersectWith(
    Entity entityPointer,
    Intersect intersectType,
    Point3dCollection points,
    IntPtr thisGraphicSystemMarker,
    IntPtr otherGraphicSystemMarker);
```

`first.IntersectWith(second, mode, points, IntPtr.Zero, IntPtr.Zero)` と呼ぶ。`this` は第1Line、`entityPointer` は第2Line。GSマーカー0はサブエンティティを指定しないという意味で、延長指定とは別である。戻り値はboolやPoint3dではなくvoid。交点は引数のコレクションへ**追記**され、座標はWCSになる。

延長の意味は、ManagedのIntersect列挙型と、そのラップ元の2026 ObjectARX Reference／Developer's Guideを対応させて確認した。

| Intersect | 延長して計算する対象 | このLine例での数学的な範囲 |
| --- | --- | --- |
| `OnBothOperands` | どちらも延長しない | tとuの両方が[0,1] |
| `ExtendThis` | 第1Lineだけ | tは実数、uは[0,1] |
| `ExtendArgument` | 第2Lineだけ | tは[0,1]、uは実数 |
| `ExtendBoth` | 両方 | tとuは実数 |

これは交差計算で延長を考慮する指定で、Lineの始点・終点を書き換える操作ではない。今回の非平行な固定Line例については、次の対応を意図する。実際の端点許容誤差や延長対応・点の件数は未確認。

| 固定例 | OnBothOperands | ExtendThis | ExtendArgument | ExtendBoth |
| --- | --- | --- | --- | --- |
| A：内部 | (2,2,0) | (2,2,0) | (2,2,0) | (2,2,0) |
| C：範囲外 | なし | (3,0,0) | なし | (3,0,0) |
| C：端点 | (2,0,0) | (2,0,0) | (2,0,0) | (2,0,0) |

`QueryTemporaryLines` は指定ごとに新しい `Point3dCollection` を作るため、以前の点が混ざらない。Countと全返却点を表示し、各点からt/uを射影で求め直して、両方の直線への残差も表示する。空のコレクションを「すべての図形で交差なし」と一般化せず、複数要素を「重複区間」とも扱わない。

公式のManaged Reference冒頭にはbounding boxの辺への言及もあるが、同ページの派生クラス実装の説明とDeveloper's Guideはエンティティ間の交点を扱う。本例は通常のLineどうしのIntersectWithの読み方を示し、`BoundingBoxIntersectWith` は呼ばない。この記述を根拠に各派生クラスの結果を保証しない。公式資料上、延長した見かけの交点への対応やエラー条件は実装に依存する。コードはAutoCADの例外を報告し、例外を「交点なし」へ読み替えない。例外コードごとの発生条件は未検証である。

Planeを追加引数に取る別版は、見かけの交点を求めるための投影平面を指定するもの。直線とそのPlaneの交点を求めるAPIとしては使わず、本例でも採用しない。

## 平行・一致・重複区間は数学式で補助分類する

分母Dが厳密に0なら、wとrの外積から位置の条件を調べる。別直線の平行と同一直線を分け、ほぼ平行は判定保留にする。コレクションの点数で無数の共有点を表そうとしない。

| 固定例 | 無限直線の数学的分類 | 有限線分の数学的分類 |
| --- | --- | --- |
| (0,0)→(4,0)、(0,2)→(2,2) | 平行な別直線、共有点なし | 共有点なし |
| (0,0)→(4,0)、(1,0)→(3,0) | 同一無限直線 | 重複区間 (1,0)〜(3,0) |
| (0,0)→(4,0)、(2,0)→(6,0) | 同一無限直線 | 重複区間 (2,0)〜(4,0) |
| (0,0)→(4,0)、(5,0)→(7,0) | 同一無限直線 | 共有点なし |
| (0,0)→(4,0)、(4,0)→(6,0) | 同一無限直線 | 端点(4,0)で接触 |

同一直線と分かった後、`ShowCollinearInterval` で第2線分の両端を第1線分のt座標へ射影する。

$$
\begin{aligned}
t_0 &= \frac{(Q-P)\cdot\mathbf{r}}{\mathbf{r}\cdot\mathbf{r}} \\
t_1 &= \frac{(Q+\mathbf{s}-P)\cdot\mathbf{r}}{\mathbf{r}\cdot\mathbf{r}}
\end{aligned}
$$

$$
\begin{aligned}
\ell &= \max(0,\min(t_0,t_1)) \\
h &= \min(1,\max(t_0,t_1))
\end{aligned}
$$

重複の固定例では第2線分のt座標が0.5〜1.5なので、[0,1]との共通部分は0.5〜1。これをP+trへ戻すと (2,0,0)〜(4,0,0) となる。下端が上端を超えればなし、等しければ1点、下端が小さければ重複区間。ただしわずかに離れた境界付近は保留し、接触へ丸めない。

平行・一致・重複区間に対するIntersectWithの返却仕様は本コードの対象外であり、これらではAPIを呼ばない。自動で重複区間の両端が返るとは説明しない。

## 3Dの直線と無限平面

A=(0,0,2)、B=(2,0,2)、C=(0,3,2) から、非正規化の法線を作る。

$$
\mathbf{n}=(B-A)\times(C-A)=
\begin{bmatrix}
0 \\
0 \\
6
\end{bmatrix}
$$

平面条件は $\mathbf{n}\cdot(X-A)=0$ 、すなわちz=2。`GetVectorTo`・`CrossProduct`・`DotProduct` で式を表し、Planeのオブジェクトは作らない。直線P+tdを代入してtを求める。

$$
t=\frac{\mathbf{n}\cdot(A-P)}{\mathbf{n}\cdot\mathbf{d}}
$$

P=(1,2,0)、d=(2,0,4) の場合は次の通り。

$$
\begin{aligned}
\mathbf{n}\cdot\mathbf{d} &= 24 \\
\mathbf{n}\cdot(A-P) &= 12 \\
t &= 12/24 = 0.5
\end{aligned}
$$

$$
P+0.5\mathbf{d}=
\begin{bmatrix}
2 \\
2 \\
2
\end{bmatrix}
$$

`ShowLinePlane` はtと交点、法線の長さで割った平面への距離残差を表示する。P〜P+dの線分終点は (3,2,4) で、t=0.5は[0,1]内。ただし (2,2,2) は三角形ABCの外側であり、ここで求めたのは三角形を含む**無限平面**との交点である。

| 追加の固定例（平面はz=2） | 数式による分類 |
| --- | --- |
| P=(1,2,0)、d=(1,0,0) | 分母0・分子12。平行で共有点なし |
| P=(1,2,2)、d=(1,0,0) | 分母0・分子0。直線全体が平面内 |
| ゼロ方向 | 直線を定められない未定義入力 |
| ゼロ法線 | 平面を定められない未定義入力 |
| P=(1,2,0)、d=(1,0,1e-13) | ほぼ平行として判定保留。小さな分母で割らない |

## 発展：2平面の共有部分は無限直線

z=2とx=1の単位法線をそれぞれ (0,0,1)、(1,0,0) とすると、外積から交線の方向が得られる。

$$
\mathbf{v}=\mathbf{n}_1\times\mathbf{n}_2=
\begin{bmatrix}
0 \\
1 \\
0
\end{bmatrix}
$$

方向だけでは位置は決まらない。x=1・z=2を同時に満たす点としてy=0を選び、通過点を (1,0,2) とする。共有部分は次の無限直線。

$$
X(t)=
\begin{bmatrix}
1 \\
t \\
2
\end{bmatrix}
\qquad (t\in\mathbb{R})
$$

`ShowTwoPlaneExample` はこの固定した2平面の手計算だけに対応する。方向・通過点・別の点 (1,1,2)、通過点の両平面への残差と方向の直交条件を表示する。汎用の2平面ソルバーや、有限な面同士の交線計算へは拡張しない。

## 結果の分類と再代入での確認

| 分類 | この例で読む条件・注意 |
| --- | --- |
| 交点1点 | 数学ではt/uまたはtを持ち、両方の式を満たす。APIの1要素だけでこれを保証しない |
| なし | 数学で有効な図形の共有部分が空。APIの例外や未定義入力とは区別 |
| 端点で接触 | 数学では閉区間内の唯一の点で、t/uのいずれかが0または1 |
| 複数のAPI返却点 | 全要素を表示。この非平行Line例で複数なら分類保留。重複の有無や順序も未確認 |
| 重複区間 | 同一直線上の閉区間の共通部分。両端を持つが、その内部にも無数の共有点がある |
| 同一無限直線 | 有限線分の範囲を外した直線全体の一致。線分の共有部分は別途調べる |
| 直線全体が平面内 | 平面との式で分母・分子がともに0。唯一の点を返す状態ではない |
| 未定義入力 | 零長・ゼロ法線・非有限値など。原点や空集合を答えとして代用しない |
| 判定保留 | ほぼ平行、距離・媒介変数の境界付近、非有限の中間値、残差超過など |

利用者が手動確認する場合は、各返却点を次の手順で調べる。手順の実機実施は今回の完了条件には含めない。

1. 点とt/uが有限であることを確認する。API返却点Xでは、各方向への射影からt/uを求め直す。
2. P+trとQ+usの両方を計算し、それぞれXとの差の長さを確認する。射影の係数だけが範囲内でも、直線上にある証明にはならない。
3. 延長指定の表に従って、必要なt/uが両端込みの[0,1]にあるかを確認する。端点への丸めや許容誤差による範囲の拡張は行わない。
4. 平面の交点ではX=P+tdと平面条件を確認する。平面の残差は法線との内積を法線長で割り、距離の尺度で比べる。
5. 重複区間は両端を両線分の式・範囲へ代入する。2平面の交線は通過点が両平面上にあり、方向が両法線と直交することを確認する。

## 数値の尺度と所有権

小さな固定入力用として、既存IntersectionMath.csと同じ数値の4つのしきい値を別々に持つ。

| 定数 | 数値 | 尺度 |
| --- | --- | --- |
| `MinimumDirectionLength` | 1e-12 | 作図単位。方向の下限。零長を通常のLineとして扱わない |
| `AngularTolerance` | 1e-12 | 無次元。2D外積／方向長の積、または法線と方向の内積／長さの積 |
| `DistanceTolerance` | 1e-9 | 作図単位。直線・平面の残差、平行な別図形の距離 |
| `ParameterTolerance` | 1e-12 | 無次元。[0,1]のすぐ外側などを保留するために使い、範囲を広げない |

ゼロ法線は直接拒否し、非ゼロ法線へ方向の長さの下限を流用しない。非有限入力・中間値・分母が小さい場合を分岐してから除算する。しきい値はSDK内部の許容誤差を設定するものではなく、任意スケールや厳密な位相判定を保証しない。数値は有効数字17桁で表示し、残差をゼロに置換しない。

一時Lineは `using` で2本作り、コレクションも各呼出しの `using` で破棄する。DBへ追加せずTransactionへも登録しないため、ModelSpaceのオープン・AppendEntity・Commitは不要。通常の現在図面のコマンド内で使い、別図面やモデルレス操作は扱わない。DB未登録Lineに対する実際の呼出し可否と、成功・例外時の後始末は実機未検証である。

## 2026版公式APIとの静的照合

確認日：2026-11-01。署名・引数・規約と以下の実際の2026版URLを確認した。図面取得・Editorへの表示・破棄の共通方針は [共通リファレンス](../../docs/references.md) も参照。

| 公式資料 | 照合した内容・利用箇所 |
| --- | --- |
| [Line(Point3d, Point3d)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Line_Line_Point3d_Point3d.html) | WCSの始点・終点で作る一時Line。`QueryTemporaryLines` |
| [Entity.IntersectWithのIntPtr版](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_IntersectWith_Entity_Intersect_Point3dCollection_IntPtr_IntPtr.html) | Entity、Intersect、点コレクション、GSマーカー2つの5引数、void、追記・WCS・派生クラス依存 |
| [IntersectWithのオーバーロード一覧](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-__OVERLOADED_IntersectWith_Autodesk_AutoCAD_DatabaseServices_Entity.html) | Planeの有無、long版のObsoleteとIntPtr版の選択 |
| [Intersect列挙型](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Intersect.html) | OnBothOperands・ExtendThis・ExtendArgument・ExtendBothの実在 |
| [AcDbEntity::intersectWith](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-RefGuide/files/OARX-RefGuide-AcDbEntity__intersectWith_AcDbEntity__AcDb__Intersect_AcGePoint3dArray__Adesk__GsMarker_Adesk__GsMarker_const.html) | ラップ元の4つの延長指定の定義、対応しない場合やエラー条件は実装依存であること |
| [Intersecting for Points](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide/files/GUID-C5F5E80A-4E6B-4741-8487-6171AB74B6FE.htm) | エンティティ間の交点、this／引数の延長、Plane引数が投影平面であること。C++例の古いint署名はC#へ流用しない |
| [Point3dCollection](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3dCollection.html) | DisposableWrapper・IList。返却点の列挙とusingによる破棄 |
| [Point3dCollection()](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3dCollection_Point3dCollection.html) | 引数なしの新しいコレクションを各API呼出しで作成 |
| [Point3dCollection.Count](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3dCollection_Count.html) | 読み取り専用intの件数。0・1・複数を区別 |
| [Line3d](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Line3d.html) | 無限の3D幾何直線であり、図面のLineと区別。本例では生成しない |
| [Point3d.GetVectorTo](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_GetVectorTo_Point3d.html) | `Vector3d GetVectorTo(Point3d)`。方向・辺・残差を作成 |
| [Vector3d.CrossProduct](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_CrossProduct_Vector3d.html) | `Vector3d CrossProduct(Vector3d)`。2DのZ成分、三角形法線、交線方向 |
| [Vector3d.DotProduct](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_DotProduct_Vector3d.html) | `double DotProduct(Vector3d)`。区間への射影・平面条件 |
| [Vector3d.Length](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_Length.html) | 読み取り専用double。方向長・残差の尺度 |

## 未検証の範囲

数式・固定入力・期待値と公式API仕様を静的に照合した参考コードである。参照DLLの解決、ビルド、NETLOAD、登録・実行・Editor表示、一時Line間のIntersectWith、各延長指定の返却点・端点許容誤差・例外・破棄は未確認。SDK・Autodesk DLLや自動検証は追加しない。

TODO：利用者が対象環境で手動確認する場合は、数学上の期待値とAPI返却点・件数を別々に記録し、上の再代入手順で照合する。重複区間・同一直線のAPI出力は対象外とし、本文の補助分類を使う。第3回の準備中／レビュー待ちの状態と、PDF自動生成対象は維持する。

[第3回トップへ戻る](../../../sessions/03-intersection/README.md) ／
[参考コードトップへ戻る](../../README.md) ／ [セットアップ](../../docs/setup.md) ／ [公式リファレンス](../../docs/references.md)

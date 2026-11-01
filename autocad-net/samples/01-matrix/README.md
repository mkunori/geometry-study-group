# 第1回：行列・座標変換

**公式資料とAPI仕様を静的に照合した参考コード／AutoCAD実機未検証・ビルド未検証。** Windows版AutoCAD 2026 Update 1.2（2026.1.2）以降／.NET 10／Visual Studio 2026を対象にする。NETLOAD・コマンド実行・表示確認は行っていない。

[MatrixCommands.cs](MatrixCommands.cs) を、共通の [Class Library](../../AutoCadGeometrySamples.csproj) で扱う。[数学教材の詳細解説](../../../sessions/01-matrix/explanation.md)、[ColumnMath.cs](../../../sessions/01-matrix/examples/ColumnMath.cs)、[Program.cs](../../../sessions/01-matrix/examples/Program.cs) の `LocalToWorld3D`・`CompositionOrder` に対応する。数学教材の本文・実装・期待値は変更しない。

## 読む順序と利用者が確認する場合の操作

まず [00：共通導入](../00-common/README.md) と [セットアップ](../../docs/setup.md) を読む。AutoCADなしでも以下の式・入力表とソースを対応させて読める。

利用者が対象製品・更新版・参照DLLを用意して手動確認する場合は、セットアップに従って共通ソリューションをVisual Studioでビルドし、専用の空図面で生成DLLを `NETLOAD` する。その後、次のコマンド名を入力する。追加の座標入力・図形選択は不要。これは確認手順の案内であり、動作確認済みの記録ではない。

| コマンド／ソースのメソッド | 固定入力 | 数学上の期待値 | 図面への変更 |
| --- | --- | --- | --- |
| `GEO01_TRANSFORM` / `Transform` | 点・方向 (2,1,0)、原点中心のZ+90°回転、変位 (10,0,3) | 点は (-1,2,0)→(9,2,3)、方向は (-1,2,0)→(-1,2,0) | 変換前後の新規Lineを2本追加 |
| `GEO01_LOCAL` / `LocalToWorld` | ローカル点 (2,1,0)、下記の原点Cと3基底 | WCS点 (9,2,3)、WCS方向 (-1,2,0) | なし。座標の表示のみ |
| `GEO01_ORDER` / `CompositionOrder` | 点 (1,0,0)、原点中心のZ+90°回転、変位 (3,0,0) | 回転→移動 (3,1,0)、移動→回転 (0,4,0) | なし。中間点・合成結果の表示のみ |

すべての計算値は `Editor.WriteMessage(string)` で報告する。長さは作図単位で、mmなどを仮定しない。図面がなければ何もしない。自動ビルド・自動テスト・GitHub Actionsには追加しない。

## 列方式の4×4行列と点・方向

右手系で、原点を通る+Z軸周りに+90°回転する。+Z側から原点を見て反時計回り。`RotationAboutWorldZ` は `Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin)` を使い、角度はラジアン。回転中心・軸はWCSで指定し、現在のUCSから取得しない。

教材の列方式とAutoCADの `Matrix3d` は、ともに左から行列を掛ける。今回の回転 $R$ と移動 $T$ は次の通り。

$$
R =
\begin{bmatrix}
0 & -1 & 0 & 0 \\
1 & 0 & 0 & 0 \\
0 & 0 & 1 & 0 \\
0 & 0 & 0 & 1
\end{bmatrix}
$$

$$
T =
\begin{bmatrix}
1 & 0 & 0 & 10 \\
0 & 1 & 0 & 0 \\
0 & 0 & 1 & 3 \\
0 & 0 & 0 & 1
\end{bmatrix}
$$

`Matrix3d.Displacement(new Vector3d(10, 0, 3))` が $T$ に対応する。点の同次成分は1なので移動列が加わる。

$$
\begin{aligned}
Rp &=
\begin{bmatrix}
-1 \\
2 \\
0 \\
1
\end{bmatrix} \\
T(Rp) &=
\begin{bmatrix}
9 \\
2 \\
3 \\
1
\end{bmatrix}
\end{aligned}
$$

方向の同次成分は0なので、回転後の方向に移動列は加わらない。

$$
\begin{aligned}
Rv &=
\begin{bmatrix}
-1 \\
2 \\
0 \\
0
\end{bmatrix} \\
T(Rv) &= Rv
\end{aligned}
$$

ソースでは点を `Point3d`、方向を `Vector3d` としてそれぞれ `TransformBy(Matrix3d)` に渡す。位置と方向を同じ型で扱わない。法線の一般的な変換や透視投影は扱わない。

これらの幾何型は値型である。比較元を別変数へコピーし、`TransformBy` と行列の前後乗算の返り値を保持する書き方にしている。回転前・回転直後・移動後を同じ変数で上書きせず、途中の座標を追える。

## 合成の左右と変換する順序

公式仕様で `PreMultiplyBy(leftSide)` は左からの積、`PostMultiplyBy(rightSide)` は右からの積。`pre` / `post` は掛ける位置であり、「図形に先に／後に作用する」という意味ではない。

| 数学の積 | AutoCADでRのコピーから作る方法 | 点への作用 | System.Numericsの行方式で同じ変換 |
| --- | --- | --- | --- |
| $TR$ | `tr = rotation; tr = tr.PreMultiplyBy(translation);` | `p.TransformBy(R)` の後に `TransformBy(T)` | `Rrow * Trow` |
| $RT$ | `rt = rotation; rt = rt.PostMultiplyBy(translation);` | `p.TransformBy(T)` の後に `TransformBy(R)` | `Trow * Rrow` |

AutoCADの `Point3d.TransformBy(M)` は列に左から $M$ を掛ける。教材の `ColumnMath.Multiply3(T, R)` と同じ意味になる。一方、教材の `ToRow4x4`・`ToRow3x2` は転置してSystem.Numericsの行方式へ対応させるための処理で、AutoCAD用にその掛け順や移動成分の配置を流用しない。

`GEO01_ORDER` は既存 `CompositionOrder` の2D同次座標をZ=0の3Dへ埋め込む。この節だけ移動量を (3,0,0)、点を (1,0,0) とする。上の (10,0,3) の例と混ぜない。

| 操作順 | 入力点 | 最初の操作直後 | 最後の点 |
| --- | --- | --- | --- |
| 回転→移動 | (1,0,0) | (0,1,0) | (3,1,0) |
| 移動→回転 | (1,0,0) | (4,0,0) | (0,4,0) |

列方式の途中式は次の通り。 $T_o$ はこの順序比較用の移動行列を表す。

$$
\begin{aligned}
T_o(Rp_o) &= T_o
\begin{bmatrix}
0 \\
1 \\
0 \\
1
\end{bmatrix} \\
&=
\begin{bmatrix}
3 \\
1 \\
0 \\
1
\end{bmatrix}
\end{aligned}
$$

$$
\begin{aligned}
R(T_op_o) &= R
\begin{bmatrix}
4 \\
0 \\
0 \\
1
\end{bmatrix} \\
&=
\begin{bmatrix}
0 \\
4 \\
0 \\
1
\end{bmatrix}
\end{aligned}
$$

ソースは逐次適用と合成行列による適用の両方を表示する。順序を入れ替えると、移動後の点を回すため結果が変わる。AutoCADでの一致は未確認であり、表は数式から得た期待値である。

## ローカル座標からWCSへの配置

`GEO01_LOCAL` は同じ3D例を、回転と移動の手順ではなく「ローカル原点Cと3本の基底」で表す。ローカル断面の点を $(x,y,0)$ とし、配置先フレームをWCSの成分で次のように与える。

| 要素 | WCS成分 | 意味 |
| --- | --- | --- |
| C | (10,0,3) | ローカル原点の配置先 |
| X基底 | (0,1,0) | ローカルX方向の配置先 |
| Y基底 | (-1,0,0) | ローカルY方向の配置先 |
| Z基底 | (0,0,1) | ローカルZ方向の配置先 |

3基底は単位長さ・互いに直交する右手系。配置は次の式で表せる。

$$
P_W = C + x\mathbf{e}_X + y\mathbf{e}_Y + z\mathbf{e}_Z
$$

これを同次行列にすると、3基底が左上の各列、Cが移動列になる。

$$
M = TR =
\begin{bmatrix}
0 & -1 & 0 & 10 \\
1 & 0 & 0 & 0 \\
0 & 0 & 1 & 3 \\
0 & 0 & 0 & 1
\end{bmatrix}
$$

ローカル点 (2,1,0) は、CにX基底の2倍とY基底を加えて (9,2,3) となる。方向ではCを加えず、X基底の2倍とY基底を加えて (-1,2,0)。既存 `LocalToWorld3D` の4×4行列と全16要素・点・方向の期待値をそのまま対応させている。

`AlignCoordinateSystem` の引数は次の順で渡す。

| 引数 | この例で渡す値 |
| --- | --- |
| `fromOrigin`, `fromXAxis`, `fromYAxis`, `fromZAxis` | `Point3d.Origin`, `Vector3d.XAxis`, `Vector3d.YAxis`, `Vector3d.ZAxis`（標準フレーム） |
| `toOrigin`, `toXAxis`, `toYAxis`, `toZAxis` | C、上表のX基底・Y基底・Z基底（WCSで表した配置先） |

公式の「fromフレームをtoフレームへ写す」という定義に従う。標準フレームの数値をローカル座標の入力として用い、返る行列で配置先のWCS座標を得る。公式Developer's GuideのUCS→WCS例でも、標準フレームをfrom、配置先の原点・軸をtoに渡している。この向きを逆にしてWCS→ローカルの座標を求める例ではない。

ここでの「ローカル」は数学上のフレームであり、AutoCADの現在のUCSとは別。UCSの作成・切替・`Editor.CurrentUserCoordinateSystem` の取得は行わない。モデル空間は図形の保存先で、モデル空間へ追加すること自体は座標変換ではない。変換を済ませたWCSの端点を持つLineをモデル空間へ登録する。

## 図面に追加するLineと後始末

`GEO01_TRANSFORM` の `AddComparisonLines` だけが図面DBを書き換える。既存図形は選択・変更・複製せず、同じ端点のLineを2つ新規作成し、一方にだけ `Entity.TransformBy(TR)` を適用する。

| 新規Line | 始点の期待値（WCS） | 終点の期待値（WCS） |
| --- | --- | --- |
| 変換前 | (0,0,0) | (2,1,0) |
| 変換後 | (10,0,3) | (9,2,3) |

`Entity.TransformBy` はEntity自身を変更するvoidメソッドなので、新しい図形を返すAPIとしては扱わない。始点も移動するため、変換後のLineを「原点から (9,2,3) への線」にしない。今回使う回転＋移動は剛体変換であり、Lineの直交・一様スケールの制約を満たす。任意の非一様スケールやせん断へは拡張しない。

00共通と同じく、現在の図面の通常の非SessionコマンドからTransactionを開始し、BlockTableを読み取り、ModelSpaceのBlockTableRecordを書き込みで開く。新規Lineを `AppendEntity` した後に `AddNewlyCreatedDBObject` で登録し、両方の登録後に `Commit` する。新規LineとTransactionは `using` で破棄し、既存のGetObject対象の後始末はTransactionに任せる。Commit前の例外は破棄でロールバックされるが、Commit後の例外まで「変更なし」とは断定しない。別図面・Session・モデルレスの場合に必要となるDocumentLockの例ではない。

保存、表示方向、ズーム、UCS、レイヤー設定は変更しない。専用の空図面で確認する際は、必要に応じて利用者がズーム全体などで2本を探す。再実行すると同じ位置へ重複追加される。

## 2026版公式APIとの静的照合

確認日：2026-11-01。以下の公式ページで型・引数・戻り値・規約を照合した。図面DB・Transaction・所有権の共通APIは [共通リファレンス](../../docs/references.md) も参照。

| 公式資料 | 照合した箇所と利用箇所 |
| --- | --- |
| [Matrix3d Structure](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d.html) | 点の同次成分1、方向0、左からの行列積、右端の移動列。全コマンドの前提 |
| [Rotation](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Rotation_double_Vector3d_Point3d.html) | `Matrix3d Rotation(double, Vector3d, Point3d)`。右手系、角度・軸・中心。`RotationAboutWorldZ` |
| [Rotate Objects (.NET)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-DAF76951-DD0C-413F-86A8-471E2B94C1C0.htm) | Rotationの角度単位がラジアンであること。`Math.PI / 2` |
| [Displacement](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Displacement_Vector3d.html) | `Matrix3d Displacement(Vector3d)`。戻り値は行列。`Transform`・`CompositionOrder` |
| [PreMultiplyBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_PreMultiplyBy_Matrix3d.html) | `Matrix3d PreMultiplyBy(Matrix3d leftSide)`。左から掛けたTRを保持 |
| [PostMultiplyBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_PostMultiplyBy_Matrix3d.html) | `Matrix3d PostMultiplyBy(Matrix3d rightSide)`。右から掛けたRTを保持 |
| [AlignCoordinateSystem](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_AlignCoordinateSystem_Point3d_Vector3d_Vector3d_Vector3d_Point3d_Vector3d_Vector3d_Vector3d.html) | 戻り値Matrix3d、from原点・3軸→to原点・3軸の8引数の順序。`LocalToWorld` |
| [Define a User Coordinate System (.NET)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-096085E3-5AD5-4454-BF10-C9177FDB5979.htm) | UCS→WCSのC#例でfromが標準フレーム、toが配置先であること。UCSを設定する処理自体は本例には導入しない |
| [Point3d.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_TransformBy_Matrix3d.html) | `Point3d TransformBy(Matrix3d leftSide)`。点を左から変換 |
| [Vector3d.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_TransformBy_Matrix3d.html) | `Vector3d TransformBy(Matrix3d leftSide)`。方向を左から変換 |
| [Entity.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_TransformBy_Matrix3d.html) | `void TransformBy(Matrix3d transform)`、自身の状態を変更、図形種別の変換制約。`AddComparisonLines` の新規Line |

## 未検証の範囲

数式と固定値の対応、公式API署名・引数・規約、リンクと日本語XMLコメントを静的に確認した参考コードである。実DLLへの参照解決、コンパイル、NETLOAD、コマンド登録、計算値の表示、Lineの追加・後始末・例外時の動作は未検証。

三角関数の近似で、理論上の0に微小な残差が出たり、9や2に近い数値として表示されたりする可能性がある。`Coordinates` は有効数字17桁で表示し、残差をゼロへ丸めない。既存コンソールのdouble用許容誤差は小さな固定入力向けの $10^{-12}$ であり、ここで実機検証済みの保証や汎用的な図面精度を意味しない。

[第1回トップへ戻る](../../../sessions/01-matrix/README.md) ／
[参考コードトップへ戻る](../../README.md) ／ [セットアップ](../../docs/setup.md) ／ [公式リファレンス](../../docs/references.md)

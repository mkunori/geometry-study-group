# 第2回：内積・外積・法線

**公式資料とAPI仕様を静的に照合した参考コード／AutoCAD実機未検証・ビルド未検証。** Windows版AutoCAD 2026 Update 1.2（2026.1.2）以降／.NET 10／Visual Studio 2026を対象にする。NETLOAD・コマンド実行・出力の確認は行っていない。

[VectorCommands.cs](VectorCommands.cs) は共通の [Class Library](../../AutoCadGeometrySamples.csproj) 内の参考コード。対応する [数学教材の詳細解説](../../../sessions/02-vector/explanation.md)、[VectorMath.cs](../../../sessions/02-vector/examples/VectorMath.cs)、[Program.cs](../../../sessions/02-vector/examples/Program.cs) の6題材と同じ数値例を使う。数学教材の本文・実装・期待値と、準備中／レビュー待ちの公開状態は変更しない。

## 読む順序と手動実行の案内

まず [00：共通導入](../00-common/README.md) と [セットアップ](../../docs/setup.md) を読む。AutoCADがなくても、以下の手計算・APIの使い方・期待値をソースで追える。

利用者が対象製品・更新版・参照DLLを用意して手動確認する場合は、セットアップに従って [共通ソリューション](../../AutoCadGeometrySamples.sln) をVisual Studioでビルドし、空図面で生成DLLを `NETLOAD` する。その後、以下のコマンド名を入力する。追加の座標入力・図形選択は不要。これは利用者向けの確認手順であり、動作確認済みの記録ではない。

| コマンド／メソッド | 主な固定入力 | 数学上の期待値 |
| --- | --- | --- |
| `GEO02_DOT` / `DotAndProjection` | a=(3,4,0)、b=(2,0,0) | 内積6、長さ5／2、角度約53.130102°、射影(3,0,0)、残差(0,4,0) |
| `GEO02_CROSS` / `CrossAndArea` | u=(2,0,0)、v=(0,3,0)と一般の3成分の例 | 外積(0,0,6)、逆順(0,0,-6)、平行四辺形面積6、三角形面積3 |
| `GEO02_NORMAL` / `OrientationAndNormal` | A=(1,1,0)、B=(4,1,0)、C=(3,3,0)と退化・小さい面の例 | Z成分+6、CはA→Bの左、ABCの単位法線+Z、ACBは-Z |

全コマンドは現在の図面の `Editor.WriteMessage(string)` へ数値を表示するだけで、図形の作成・編集・保存・UCS変更は行わない。図面DBへアクセスしないため、Transaction・DocumentLock・DBObjectの作成や後始末も不要。図面がなければ何もしない。自動ビルド・自動テスト・GitHub Actionsへの追加は行わない。

## 座標系・型・単位の前提

点は `Autodesk.AutoCAD.Geometry.Point3d`、方向・変位・法線は同じ名前空間の `Vector3d` で表す。すべて同じWCSの直交座標系を用い、2Dの例はZ=0のXY平面に置く。X右・Y上、3Dは右手系とし、現在のUCSや画面の上下を取得して判定する例にはしない。

AutoCADの `Vector3d` は3成分をdoubleで受け取る幾何型。一方、数学教材で比較する `System.Numerics.Vector3` はfloatであり、本ファイルでは使用もインポートもしない。数学教材のタプルによる手実装はdouble。名前が似ていても型と精度を混同しない。

長さは作図単位で、mmなどは仮定しない。変位どうしの内積と外積の大きさは作図単位の2乗、単位法線・cos・ラジアンの角度は無次元として区別する。

## 内積・長さ・角度：手計算からAPIへ

a=(3,4,0)、b=(2,0,0) の内積は対応する成分の積の和。

$$
\begin{aligned}
\mathbf{a}\cdot\mathbf{b} &= 3\cdot2 + 4\cdot0 + 0\cdot0 \\
&= 6
\end{aligned}
$$

長さはそれぞれ $\lVert\mathbf{a}\rVert=5$ 、 $\lVert\mathbf{b}\rVert=2$ 。内積を長さの積で割ると、向きだけを比べるcosが得られる。

$$
\begin{aligned}
\cos\theta &= \frac{\mathbf{a}\cdot\mathbf{b}}{\lVert\mathbf{a}\rVert\lVert\mathbf{b}\rVert} \\
&= \frac{6}{5\cdot2} \\
&= \frac{3}{5}
\end{aligned}
$$

$$
\begin{aligned}
\theta &= \arccos(3/5) \\
&\approx 0.9272952180016122\ \mathrm{rad}
\end{aligned}
$$

$$
\theta_{\mathrm{deg}} = \theta\cdot\frac{180}{\pi}
\approx 53.13010235415598^\circ
$$

ソースの `a.DotProduct(b)` が内積、`a.Length`・`b.Length` が長さに対応する。`TryAngle` で長さと計算結果を検査してから、有限なcosの比を `Math.Clamp(cosine, -1.0, 1.0)` で挟み、`Math.Acos` に渡す。クランプは丸めによるわずかな定義域外への対処であり、ゼロ長や非有限値を有効な角度に置き換えるためには使わない。

同じ入力を検査した後で `a.GetAngleTo(b)` も表示する。公式の1引数版は0〜πの角度を返すため、ラジアンの角度として180/π倍して度へ変換する。これは最小のなす角であり、符号付き角度や0〜2πの回転角ではない。基準法線を渡す別オーバーロードは使わない。

## 正射影：単位ではないbの大きさを補正する

射影先はbが張る直線で、線分への最近点や面への射影ではない。射影を $\mathbf{q}=t\mathbf{b}$ とおき、残差がbと直交する条件から係数を求める。

$$
\begin{aligned}
(\mathbf{a}-t\mathbf{b})\cdot\mathbf{b} &= 0 \\
\mathbf{a}\cdot\mathbf{b} - t(\mathbf{b}\cdot\mathbf{b}) &= 0
\end{aligned}
$$

$$
\begin{aligned}
t &= \frac{\mathbf{a}\cdot\mathbf{b}}{\mathbf{b}\cdot\mathbf{b}} \\
&= \frac{6}{4} \\
&= \frac{3}{2}
\end{aligned}
$$

$$
\begin{aligned}
\mathbf{q} &= \frac{3}{2}
\begin{bmatrix}
2 \\
0 \\
0
\end{bmatrix} \\
&=
\begin{bmatrix}
3 \\
0 \\
0
\end{bmatrix}
\end{aligned}
$$

残差は (0,4,0) で、bとの内積は0。bの長さは2なので、内積6をそのままbに掛けると (12,0,0) となり誤る。分母の $\mathbf{b}\cdot\mathbf{b}$ を省略してよいのはbが単位ベクトルの場合だけである。

`TryProject` は分母を `b.DotProduct(b)` で求めて検査し、係数を `b.MultiplyBy(factor)` に渡す。残差は `a.Subtract(projection)` に対応し、そのbとの内積も出力する。ゼロのbは除算前に拒否するが、非ゼロのbへゼロのaを射影するとゼロになる。

## 3D外積：向きと面積を区別する

右手系の成分式は次の通り。結果はスカラーではなく3Dベクトル。

$$
\mathbf{u}\times\mathbf{v} =
\begin{bmatrix}
u_yv_z-u_zv_y \\
u_zv_x-u_xv_z \\
u_xv_y-u_yv_x
\end{bmatrix}
$$

u=(2,0,0)、v=(0,3,0) ではZ成分が $2\cdot3-0\cdot0=6$ となる。`u.CrossProduct(v)` と `v.CrossProduct(u)` は次の期待値に対応する。

$$
\mathbf{u}\times\mathbf{v} =
\begin{bmatrix}
0 \\
0 \\
6
\end{bmatrix}
$$

$$
\mathbf{v}\times\mathbf{u} =
\begin{bmatrix}
0 \\
0 \\
-6
\end{bmatrix}
$$

外積の長さ6は平行四辺形の面積、半分の3は同じ2辺を持つ三角形の面積。順序を逆にすると向きは反転するが、長さと通常の面積は変わらない。ソースでは `cross.Length` と `cross.Length / 2` を表示する。

一般の3成分でも、a=(1,2,3)、b=(4,5,6) とすると次のようになる。

$$
\begin{aligned}
\mathbf{a}\times\mathbf{b} &=
\begin{bmatrix}
2\cdot6-3\cdot5 \\
3\cdot4-1\cdot6 \\
1\cdot5-2\cdot4
\end{bmatrix} \\
&=
\begin{bmatrix}
-3 \\
6 \\
-3
\end{bmatrix}
\end{aligned}
$$

aとの内積は -3+12-9=0、bとの内積は -12+30-18=0。`CrossAndArea` は外積の3成分に加えて、双方との `DotProduct` の値を表示する。

## 2Dの左右と三角形の単位法線

A=(1,1,0)、B=(4,1,0)、C=(3,3,0) の位置をそのまま外積せず、共通の始点Aから辺を作る。`a.GetVectorTo(b)` がB-A、`a.GetVectorTo(c)` がC-Aに対応する。

$$
\mathbf{AB}\times\mathbf{AC} =
\begin{bmatrix}
3 \\
0 \\
0
\end{bmatrix}
\times
\begin{bmatrix}
2 \\
2 \\
0
\end{bmatrix}
=
\begin{bmatrix}
0 \\
0 \\
6
\end{bmatrix}
$$

3Dの外積からZ成分だけを取り出したスカラーが、2Dの $u_xv_y-u_yv_x$ に対応する。X右・Y上で+6なので、CはA→Bの左側。符号付き三角形面積は+3、通常の面積は3。画面でYを下向きに取る座標では符号の解釈を反転する必要があり、画面上の見た目だけで判定しない。

面の向きとしては、外積をその長さで割って単位法線を得る。

$$
\mathbf{n} = \frac{\mathbf{AB}\times\mathbf{AC}}{\lVert\mathbf{AB}\times\mathbf{AC}\rVert}
=
\begin{bmatrix}
0 \\
0 \\
1
\end{bmatrix}
$$

BとCを交換すると外積は (0,0,-6)、単位法線は (0,0,-1)。`WriteTriangleNormal` はABC・ACBそれぞれの単位法線とその長さ1を表示する。頂点から面法線を作る例であり、既存法線の非一様スケールに対する変換ではない。

## ゼロ・退化・許容誤差の方針

`TryAngle`・`TryProject`・`TryTriangleNormal` は定義条件と有限性を検査し、扱えない場合にfalseを返す。falseのときのout変数の既定値は答えとして使わず、特に未定義の単位法線を (0,0,0) で代用しない。

| 条件／追加する固定例 | 期待する扱い |
| --- | --- |
| aまたはbがゼロの角度 | 未定義。acos・GetAngleToへ進まない |
| ゼロのbへの射影 | 未定義。分母で割らない |
| aがゼロ、b=(2,0,0)の射影 | 定義可能。結果は(0,0,0) |
| A=(1,1,0)、B=(4,1,0)、C=(2,1,0) | 一直線。外積0なのでGetNormalを呼ばない |
| 上のABCでC=A、またはB=A | 重複点。外積0なのでGetNormalを呼ばない |
| A=(0,0,0)、B=(1,1,0)、C=(1,1+1e-13,0) | doubleで外積に微小なZ成分が残りうるが、面積の下限以下なので扱わない |
| NaN・Infinityを含む入力や途中の桁あふれ | 成分・長さ・積・係数・結果を検査して拒否。クランプで隠さない |

上のゼロ・退化・小さい面の固定例はコマンドからも表示する。非有限値を意図的にAutoCADへ生成・入力する例は加えず、対応するガードをソースで示す。

`MinimumLength` と `MinimumCrossLength` は、既存 `VectorMath.cs` と同じ数値 $10^{-12}$ だが、別の尺度である。

| しきい値 | 単位・適用箇所 |
| --- | --- |
| `MinimumLength` | 作図単位。角度の両入力の長さを比較。射影では分母が長さの2乗なので下限も2乗する |
| `MinimumCrossLength` | 作図単位の2乗。三角形の外積の長さ（通常の面積の2倍）を比較 |

`TryTriangleNormal` は外積の長さが有限で下限を**超えた後**にだけ `cross.GetNormal(tolerance)` を呼ぶ。公式仕様では、`GetNormal(Tolerance)` は長さがEqualVector未満だと正規化せず入力と同じベクトルを返すため、呼び出せば必ず単位法線になると扱わない。

この例は `new Tolerance(MinimumCrossLength, MinimumLength)` を渡す。第1引数EqualVectorをこの外積の数値の下限に対応させ、第2引数EqualPointはこの呼出しでは使わない。引数なしの `GetNormal()` が使う `Tolerance.Global` を変更せず、暗黙のGlobalへの依存も避ける。API許容誤差と教材側の事前検査を別々に明示している。

ほぼ平行な2辺は、外積の絶対値だけでなく次の無次元量でも考える必要がある。

$$
s = \frac{\lVert\mathbf{AB}\times\mathbf{AC}\rVert}{\lVert\mathbf{AB}\rVert\lVert\mathbf{AC}\rVert}
= |\sin\theta|
$$

この比は両辺が非ゼロで積・比が有限な場合に使う。今回のコードは既存教材と同じ小さな固定入力と絶対的な面積下限の例にとどめ、任意スケールでのほぼ平行判定や厳密な退化・位相判定を保証しない。作図単位や座標の大きさを変える場合は、長さ・面積の下限と無次元の角度許容誤差を別々に設計する。

## 2026版公式APIとの静的照合

確認日：2026-11-01。使用するオーバーロードと規約を以下の公式資料で確認した。現在図面の取得とEditorへの出力は [共通リファレンス](../../docs/references.md) を参照。

| 公式資料 | C#側の形と利用箇所 |
| --- | --- |
| [Vector3d(double, double, double)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_Vector3d_double_double_double.html) | 3つのdoubleから方向を作る。全コマンドの固定ベクトル |
| [Point3d(double, double, double)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_Point3d_double_double_double.html) | 3つのdoubleから位置を作る。三角形の頂点 |
| [DotProduct](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_DotProduct_Vector3d.html) | `double DotProduct(Vector3d v)`。内積、射影係数、直交の確認 |
| [Length](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_Length.html) | 読み取り専用doubleプロパティ。長さ・外積の面積・法線の長さ |
| [GetAngleTo(Vector3d)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_GetAngleTo_Vector3d.html) | `double GetAngleTo(Vector3d vector)`。範囲0〜π。`DotAndProjection` でラジアンと度を表示 |
| [MultiplyBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_MultiplyBy_double.html) | `Vector3d MultiplyBy(double value)`。`TryProject` の係数×b |
| [Subtract](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_Subtract_Vector3d.html) | `Vector3d Subtract(Vector3d v)`。射影の残差 |
| [CrossProduct](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_CrossProduct_Vector3d.html) | `Vector3d CrossProduct(Vector3d vector)`。外積・順序反転・三角形法線 |
| [GetVectorTo](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_GetVectorTo_Point3d.html) | `Vector3d GetVectorTo(Point3d point)`。AからB/Cへの辺 |
| [Move Objects (.NET)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-9E9D7A74-4FE7-4E57-B9CB-BA4712B364ED.htm) | 始点のGetVectorToへ終点を渡して変位を作る公式例。B-A／C-Aの向きの確認 |
| [GetNormal(Tolerance)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_GetNormal_Tolerance.html) | `Vector3d GetNormal(Tolerance tolerance)`。長さがEqualVector未満なら入力を返す仕様と、検査後の正規化 |
| [GetNormalのオーバーロード一覧](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-__OVERLOADED_GetNormal_Autodesk_AutoCAD_Geometry_Vector3d.html) | 引数なし版はGlobalを使うため、今回のコードでは明示したTolerance版を選択 |
| [Tolerance(double, double)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Tolerance_Tolerance_double_double.html) | 第1引数equalVector、第2引数equalPoint。`TryTriangleNormal` の局所的な設定 |

## 未検証の範囲

数式・入力・期待値、API署名・引数・規約、ソースと案内の整合性を静的に照合した参考コードである。実DLLの参照解決、コンパイル、NETLOAD、コマンド登録、Editorの出力、API内部の許容誤差・例外時の挙動は未検証。TODO：利用者が対象環境で手動確認する場合は、各表の数学上の期待値と表示値・未定義の扱いを読み比べる。

数値は有効数字17桁で表示し、微小な残差をゼロに置き換えない。既存コンソールの比較用絶対誤差はdouble向け $10^{-12}$ 、floatを含む比較向け $10^{-5}$ だが、これは今回のAutoCAD実機での一致や汎用の図面精度を保証するものではない。

[第2回トップへ戻る](../../../sessions/02-vector/README.md) ／
[参考コードトップへ戻る](../../README.md) ／ [セットアップ](../../docs/setup.md) ／ [公式リファレンス](../../docs/references.md)

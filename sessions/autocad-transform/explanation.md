# 同じ図形変換のいろいろな実現方法を見てみよう

同じ頂点を調べたつもりなのに、返ってくる座標が違う。
そんなときは、数値だけでなく「どの座標系で表した値か」を確認するところから始めよう。

この教材は、AutoCAD APIや実装方法の違いを数学で結び付け、普段見慣れないコードでも処理の意図を理解することを目指す。
WCS・OCSと任意軸アルゴリズムを必須かつ最優先とし、基底や基本変換の復習を各題材に関連付ける。

対象はWindows版AutoCAD 2026 Update 1.2以降／.NET 10／Visual Studio。
API仕様は末尾の2026版公式資料で静的に照合した。
以下の数値は数式から得た期待値であり、職場の参照DLLでのビルド・NETLOAD・AutoCAD実機実行は未検証である。

## 学習の流れ

| 順序 | 主題 |
| --- | --- |
| 1 | 導入：同じ頂点なのに異なる座標が返る？ |
| 2 | WCS・UCS・OCSの役割と違い |
| 3 | 任意軸アルゴリズム（Arbitrary Axis Algorithm） |
| 4 | OCSからWCSへの変換方法の比較 |
| 5 | WCSからOCSへの変換方法の比較 |
| 6 | 座標系を考慮した移動・回転・逆変換・合成 |
| 7 | その他の変換方法の比較 |
| 8 | まとめ：異なるコードの処理意図を読み解く |

第1〜5章を連続して読み、APIから得る座標と、その背景の基底・相互変換を対応させる。
第6〜8章は題材と問いの骨格を示す。

## 1. 導入：同じ頂点なのに異なる座標が返る？

### 同じ頂点番号で比べてみよう

`Polyline` の頂点を調べる、次のコードを考える。

```csharp
Point2d ocs2d = polyline.GetPoint2dAt(2);
Point3d wcs3d = polyline.GetPoint3dAt(2);
```

どちらも頂点番号2を読んでいるのに、成分が違う。
これは別の頂点を取得したからではない！
[`GetPoint2dAt`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_GetPoint2dAt_int.html) はPolyline自身のOCSでの2D位置、[`GetPoint3dAt`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_GetPoint3dAt_int.html) はWCSでの3D位置を返す。
同じ位置を、違う軸に沿って測っているのである。

本文・図・実演では、次のOCS頂点を順に結んだ閉じたPolylineを使う。
頂点番号は0始まり、直線だけの非対称な輪郭とし、bulge・始点幅・終点幅はすべて0にする。
座標の長さ単位は作図単位で統一し、mmなどを仮定しない。

| 頂点番号 | OCSの2D座標 |
| --- | --- |
| 0 | (0,0) |
| 1 | (4,0) |
| 2 | (3,2) |
| 3 | (0,1) |

最後の頂点から最初へも辺を置くが、始点を末尾に重複保存しない。
非対称にするのは、軸の入替えや反転を、輪郭の見た目だけで見逃さないためである。

### XY平面では違いが隠れる

固定例Aでは、`Normal=(0,0,1)`、`Elevation=0` とする。
OCSのX・Y軸がWCSのX・Y軸に一致するので、頂点2は次のようになる。

| API | 頂点2の期待値 |
| --- | --- |
| `GetPoint2dAt(2)` | (3,2) |
| `GetPoint3dAt(2)` | (3,2,0) |

この例だけだと「2D座標にZ=0を追加すればよい」と思えてしまう。
では、図形の平面が傾いたらどうなるだろう？

### 傾いた平面では、Zを追加するだけでは足りない

固定例Bも、4つのOCS頂点は同じままにする。
法線を単位化した `Normal=(0,3/5,4/5)`、`Elevation=5` とする。
法線のWCS成分と長さは、

$$
N=\begin{bmatrix}
0 \\
3/5 \\
4/5
\end{bmatrix},\qquad
\|N\|=\sqrt{(3/5)^2+(4/5)^2}=1.
$$

このときの期待値は次のとおり。
第2章で位置の表し方を、第3章で軸の決め方を説明する。

| 頂点番号 | `GetPoint2dAt`：OCS | `GetPoint3dAt`：WCSの期待値 |
| --- | --- | --- |
| 0 | (0,0) | (0,3,4) |
| 1 | (4,0) | (−4,3,4) |
| 2 | (3,2) | (−3,7/5,26/5) = (−3,1.4,5.2) |
| 3 | (0,1) | (0,11/5,23/5) = (0,2.2,4.6) |

頂点2に高さだけを付けた `(3,2,5)` は、まだOCSで表した3成分である。
それをそのままWCSの点として読むと、違う位置になってしまう。

![同じ非対称Polylineの頂点2を、XY平面の固定例Aと傾斜平面の固定例Bで比較し、OCSの(3,2)がWCSではそれぞれ(3,2,0)と(-3,1.4,5.2)になる図](images/01-polyline-coordinates.svg)

実演の [`GEOAT_COORDINATES`](../../autocad-net/samples/autocad-transform/CoordinateSystemCommands.cs) は、AとBを同じ頂点一覧から独立に生成する。
一方を動かした結果を、もう一方の比較元には使わない。

## 2. WCS・UCS・OCSの役割と違い

### どの原点・軸から測った値か

座標は点そのものではなく、選んだ原点と軸に対する位置の数値表現である。
AutoCADでは、まず次の3つを区別しよう。

| 座標系 | 目的・原点・軸 | この教材での使い方 |
| --- | --- | --- |
| WCS：世界座標系 | 図面の基準となる固定の原点Oと直交XYZ軸 | 法線・各基底の成分、図形の3D位置を表す |
| UCS：ユーザー座標系 | 利用者が作業用に選ぶ原点と直交軸。WCSに対して位置と向きを持つ | OCSとの違いを理解するために扱い、実演では設定を変更しない |
| OCS：オブジェクト座標系。ECSとも呼ぶ | ここで扱うPolylineでは原点はWCS原点と同じ。Z軸の向きは法線、X・Y軸は任意軸アルゴリズムで決める | 平面上の頂点を2成分と共通の高さで記録する |

[公式DXFのOCS説明](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-DXF/files/GUID-D99F1509-E4E4-47A3-8691-92EA07DC88F5.htm) は、OCSの原点がWCS原点と一致し、X・Y軸を一貫した規則で定めることを示している。
任意の位置に原点を置ける一般のローカル座標系と、AutoCADのOCSを同一視しない。
UCSを変えたことだけで、既存Polylineの保存されたOCSを「今のUCS」と読み替えることもできない。

数学の説明では、WCSの標準基底を $W_x,W_y,W_z$ 、OCSの単位基底を $A_x,A_y,N$ と書く。
すべて右手系で、ベクトルの成分はWCSで表す。
各軸の長さは1で無次元、点の座標とElevationは共通の長さ単位を持つ。

### Normal・Elevation・頂点の役割

| Polylineの要素 | 意味・入力座標系 |
| --- | --- |
| `Normal` | 図形平面の法線方向。WCS成分。本文・実演は非ゼロの入力を単位化して使う |
| `Elevation` | OCSのZ方向に測った共通の高さh。一般のWCSのZ座標ではない |
| 頂点の `Point2d` | OCSのXY成分(u,v)。頂点ごとに異なる |
| `AddVertexAt(index, pt, bulge, startWidth, endWidth)` | ptはPolylineのECS、つまりここでのOCSで指定する。WCS点をそのまま渡さない |

[`Normal`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Normal.html)・[`Elevation`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Elevation.html)・[`AddVertexAt`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_AddVertexAt_int_Point2d_double_double_double.html) の2026版仕様と、[LWPOLYLINEのDXF定義](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-DXF/files/GUID-748FC305-F3F2-4F74-825A-61F04D757A50.htm) を対応させる。
DXFでは頂点のOCS成分がグループ10・20、高さが38、法線方向が210・220・230に対応する。
これは通常の `Polyline` の例であり、`Polyline2d`・3D Polyline・任意のEntityの全座標へ一律に流用しない。

実演ではNormalとElevationを先に設定し、各 `Point2d` を追加する。

```csharp
polyline.Normal = new Vector3d(0, 3.0 / 5, 4.0 / 5);
polyline.Elevation = 5;
polyline.AddVertexAt(2, new Point2d(3, 2), 0, 0, 0);
```

この抜粋の頂点番号2は、番号0・1がすでに追加されている前提である。
実行可能な全体は [管理元のソース](../../autocad-net/samples/autocad-transform/CoordinateSystemCommands.cs) を読む。

### OCS原点と、図形平面の基準点は別

OCS原点はWCS原点Oと同じだが、Polylineが乗る平面はOCSの $z=h$ である。
平面上の `(u,v)=(0,0)` に当たる点をCと書くと、

$$
C=O+hN,\qquad P=O+uA_x+vA_y+hN=C+uA_x+vA_y.
$$

OCS座標の各成分は、その軸へ進む量を表すので、3つの変位を足す。
ここでCは図形平面の基準点であって、OCS原点をCへ移したわけではない。
hはNの向きに測った符号付きの高さであり、平面までの最短距離は $|h|$ になる。

Nは単位長さで、平面内の2軸と直交するから、図形平面上の点Pは、

$$
\begin{aligned}
N\cdot(P-O)
&=u(N\cdot A_x)+v(N\cdot A_y)+h(N\cdot N) \\
&=h.
\end{aligned}
$$

つまりElevationは平面を表す条件にもなる。
固定例Bでは $C=(0,3,4)$ であり、h=5でもCのWCSのZ成分は4である。
平面は $3y+4z=25$ となり、頂点2の $(-3,7/5,26/5)$ もこの条件を満たす。

![WCS原点とOCS原点が一致し、法線N=(0,3/5,4/5)に沿った高さ5の点C=(0,3,4)から平面内の変位を足す図。CはOCS原点ではない](images/02-origin-elevation.svg)

### 法線だけでは平面内の軸が決まらない

Nを固定しても、それに垂直な2軸をNの周りに回す自由度が残る。
例えばNが+Zなら、X・Yを使っても、+Y・−Xを使っても、同じ平面に沿う右手系を作れる。
しかし同じ座標 `(3,2)` が表す位置は、軸の選び方で変わる。

AutoCADはこの自由度を任意軸アルゴリズムという規則で固定する。
「法線を指定したら、数学的に唯一のX・Yが決まる」という意味ではない。

![同じ法線N=+Zでも、平面内の直交基底を90度回せることと、AutoCADのOCSが規則で一組の軸を選ぶことを示す図](images/03-normal-freedom.svg)

### 座標の表現変更と、図形の移動を分ける

OCSの `(3,2,5)` をWCSの `(-3,1.4,5.2)` と表すとき、物理的な頂点は動いていない。
`Point3d.TransformBy` で別表現を計算することと、`Entity.TransformBy` で図形自体を変更することは別の操作である。
同じ行列を使える場合でも、何を変換するコードかを読もう。

点Pには原点が必要だが、2点の差という方向・変位には原点を足さない。
同じ平面上の2点を引くと、共通の高さの項も消える。

$$
P_2-P_1=(u_2-u_1)A_x+(v_2-v_1)A_y.
$$

一般のローカル座標変換の平行移動も、方向には加わらない。
今回のOCS全体とWCSは原点を共有するが、そのことと「図形平面の高さが0」は区別する。
UCSについては [公式の座標系の説明](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-0EFA65CC-C1AB-4B99-8159-C31602C1A5E8.htm) も参照できる。
実演の固定入力はOCSまたはWCSと明記して直接与え、現在のUCSから取得しない。

## 3. 任意軸アルゴリズム（Arbitrary Axis Algorithm）

### 外積で平面内の方向を作る

2つの非平行な方向a・bの外積は、両方に垂直な方向になる。
成分で書くと、

$$
a\times b=\begin{bmatrix}
a_yb_z-a_zb_y \\
a_zb_x-a_xb_z \\
a_xb_y-a_yb_x
\end{bmatrix}.
$$

長さは $\|a\|\|b\||\sin\theta|$ 、向きは右手系の規約で決まる。
したがって、Nと平行でない補助軸との外積を取れば、Nに垂直な、平面内の軸を作れる。
外積をその長さで割れば単位長さになる。
平行な場合は外積が0になり、割り算で向きを作ることはできない。
詳しい導出は [数学基礎編の外積](../02-vector/explanation.md) と対応する。

### 公式の分岐は「両方が1/64未満」

[Autodeskの任意軸アルゴリズム](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-DXF/files/GUID-E19E5B42-0CC7-4EBA-B29F-5E1D595149EE.htm) は、単位法線Nから次の規則で基底を作る。
法線をWCS成分で表し、先に長さを1にする。
ここからの比較は単位化後の成分に対して行う。

1. $|N_x|<1/64$ **かつ** $|N_y|<1/64$ なら、補助軸をWCSのY軸にする。
2. それ以外は、補助軸をWCSのZ軸にする。どちらかがちょうど1/64でもこちら。
3. 補助軸×Nを単位化してX軸を作る。
4. N×X軸を単位化してY軸を作る。

補助軸をH、単位化前のX方向をUと書くと、

$$
H=\begin{cases}
W_y & (|N_x|<1/64\ \text{かつ}\ |N_y|<1/64), \\
W_z & (\text{それ以外}),
\end{cases}
$$

$$
U=H\times N,\qquad A_x=\frac{U}{\|U\|},\qquad
A_y=\frac{N\times A_x}{\|N\times A_x\|}.
$$

順序は **H×N、次にN×X** である。
外積の順を逆にすると符号が変わり、同じOCS基底にはならない。
`<` を `<=` に変えたり、比較にepsilonを足したりするのも、公式と異なる規則になる。

なぜ補助軸を切り替えるのか、考えてみよう。
Nが正負Z軸の近くにあると、 $W_z\times N$ は小さくなり、単位化で誤差を増幅しやすい。
そこで、その近傍ではY軸を使う。
判定領域はX・Y成分の両方を比べた正方形状の領域で、一定の角度だけで区切る円形の領域ではない。

選択した外積が0にならないことも確認できる。
単位Nに対し、Z軸を使う側では少なくとも一方のX・Y成分が1/64以上だから、

$$
\|W_z\times N\|=\sqrt{N_x^2+N_y^2}\geq1/64.
$$

Y軸を使う側では $|N_y|<1/64$ なので、

$$
\|W_y\times N\|=\sqrt{N_x^2+N_z^2}
=\sqrt{1-N_y^2}>\sqrt{1-1/4096}.
$$

厳密な実数では、どちらも非ゼロである。
固定入力を単位化せず比較すると、この根拠もOCSの規則も崩れる。

![単位法線のNx・Ny平面で、両成分の絶対値が1/64未満の開いた正方形だけがY軸分岐になり、境界と外側がZ軸分岐になる図](images/04-arbitrary-axis-branches.svg)

### 直交・単位長・右手系を確かめる

X軸はNに垂直で単位長さ。
N×Xから作ったY軸も両方に垂直で、単位長さになる。
さらに、ベクトル三重積の関係から、

$$
A_x\times(N\times A_x)
=N(A_x\cdot A_x)-A_x(A_x\cdot N)=N.
$$

よって $A_x\times A_y=N$ となり、右手系が得られる。
実演では、3本の長さ、3組の内積、 $(A_x\times A_y)\cdot N$ を出力する。
期待値はそれぞれ1、0、1である。

### 固定例B：頂点2のWCS座標を計算する

N=(0,3/5,4/5)はY成分が1/64以上なので、Z軸を使う。

$$
\begin{aligned}
W_z\times N&=\begin{bmatrix}
-3/5 \\
0 \\
0
\end{bmatrix}, \\
A_x&=\begin{bmatrix}
-1 \\
0 \\
0
\end{bmatrix},\qquad
A_y=N\times A_x=\begin{bmatrix}
0 \\
-4/5 \\
3/5
\end{bmatrix}.
\end{aligned}
$$

第2章の位置の式に、頂点2のu=3、v=2と高さh=5を代入する。

$$
\begin{aligned}
P&=3A_x+2A_y+5N \\
&=\begin{bmatrix}
-3 \\
0 \\
0
\end{bmatrix}+\begin{bmatrix}
0 \\
-8/5 \\
6/5
\end{bmatrix}+\begin{bmatrix}
0 \\
3 \\
4
\end{bmatrix} \\
&=\begin{bmatrix}
-3 \\
7/5 \\
26/5
\end{bmatrix}.
\end{aligned}
$$

これで、第1章の異なる座標が同じ頂点を表す理由が分かった。
WCSのXが負になるのはOCSのX軸が−WCS Xを向くからで、WCSのY・Zにはvとhの両方が寄与している。

基底を列に並べれば、[第1回の行列による表現](../01-matrix/explanation.md) にもつながる。
OCS原点とWCS原点が共通なので、この変換行列自体に平行移動はない。
Elevationは入力側の3成分目に含める。

$$
M=\begin{bmatrix}
-1 & 0 & 0 & 0 \\
0 & -4/5 & 3/5 & 0 \\
0 & 3/5 & 4/5 & 0 \\
0 & 0 & 0 & 1
\end{bmatrix},\qquad
M\begin{bmatrix}
3 \\
2 \\
5 \\
1
\end{bmatrix}=\begin{bmatrix}
-3 \\
7/5 \\
26/5 \\
1
\end{bmatrix}.
$$

平面内の2D配置としてCを原点にした行列を別に作ることもできるが、入力を `(u,v,h)` にする方式と二重に高さを加えない。

### 正負Z・斜め法線と境界を比べる

次の表のNは単位法線である。
実演では固定例Bに `(0,3,4)`、3成分が非ゼロの例に `(1,2,2)` を渡し、まず単位化する。
近傍例の $c=\sqrt{16383}/128$ とする。

| 単位法線N | 補助軸 | X軸の期待値 | Y軸の期待値 |
| --- | --- | --- | --- |
| (0,0,1) | Y | (1,0,0) | (0,1,0) |
| (0,0,−1) | Y | (−1,0,0) | (0,1,0) |
| (1,0,0) | Z | (0,1,0) | (0,0,1) |
| (0,3/5,4/5)：固定例B | Z | (−1,0,0) | (0,−4/5,3/5) |
| (0,1/128,c)：Z近傍 | Y | (1,0,0) | (0,c,−1/128) |
| (1/3,2/3,2/3)：斜め | Z | (−2/√5,1/√5,0) | (−2/(3√5),−4/(3√5),√5/3) |

負ZでもX・Yを両方そのまま+WCS X・+WCS Yにしてはいけない。
それではX×Yが+Zになり、指定したNと逆になってしまう。

境界の固定入力は $b=1/64$ 、 $\delta=2^{-20}$ とし、X成分を $b(1-\delta),b,b(1+\delta)$ にする。
それぞれN=(x,0,√(1−x²))として、正Z側の単位法線を作る。

| 境界例 | 補助軸 | X軸の期待値 | Y軸の期待値 |
| --- | --- | --- | --- |
| x=b(1−δ)：直前 | Y | (√(1−x²),0,−x) | (0,1,0) |
| x=b：ちょうど | Z | (0,1,0) | (−√(1−x²),0,x) |
| x=b(1+δ)：直後 | Z | (0,1,0) | (−√(1−x²),0,x) |
| N=(0,b,√(1−b²))：Y成分が境界 | Z | (−1,0,0) | (0,−√(1−b²),b) |

直前はY軸が $(0,1,0)$ になり、ちょうど境界に達するとX軸が $(0,1,0)$ になる。
法線は少ししか変わらなくても、分岐をまたぐX・Y軸は大きく変わり得る。
軸の連続性を無条件に仮定した補間や、頂点のOCS値だけを固定した形状比較は行わない。

### 成分計算とPlaneToWorldを照合する

数学部分は [`ArbitraryAxisMath.cs`](../../autocad-net/samples/autocad-transform/ArbitraryAxisMath.cs) に分け、次の順で計算する。

```csharp
Vec n = Unit(normal);
bool usesWorldY = Math.Abs(n.X) < AxisBoundary && Math.Abs(n.Y) < AxisBoundary;
Vec helper = usesWorldY ? new Vec(0, 1, 0) : new Vec(0, 0, 1);
Vec x = Unit(Cross(helper, n));
Vec y = Unit(Cross(n, x));
```

この抜粋は同ファイル内の `Build` の計算部分で、Vecは3成分の方向、Unitは単位化を表す。
[`GEOAT_ARBITRARY_AXIS`](../../autocad-net/samples/autocad-transform/ArbitraryAxisCommands.cs) は同じNをAPIへ渡し、WCSの各標準基底を別々に変換する。

```csharp
Matrix3d planeToWorld = Matrix3d.PlaneToWorld(Vector(frame.Normal));
Vector3d apiX = Vector3d.XAxis.TransformBy(planeToWorld);
Vector3d apiY = Vector3d.YAxis.TransformBy(planeToWorld);
Vector3d apiZ = Vector3d.ZAxis.TransformBy(planeToWorld);
```

ここでVectorは数学部分の3成分を同じ成分のVector3dにする補助関数で、別の変換ではない。
[`PlaneToWorld(Vector3d)` の仕様](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_PlaneToWorld_Vector3d.html) は法線を用いた平面座標から世界座標への行列を返すと説明している。
基底の向きはDXFのOCS規約・任意軸アルゴリズムと合わせて照合する。
Managed APIのこのページ単体に1/64の分岐が書かれているわけではない。

各基底の成分差の長さが小さく、長さ1・直交・右手系が保たれることが比較の期待である。
同じ出発点から計算し、APIが返した軸を自作計算の入力として流用しない。
第1章のコマンドでは、同じOCS点に対する取得API・PlaneToWorld・基底の線形結合の結果も並べる。
図面上で軸が重なって見えることだけを、一致の根拠にはしない。

### 無効入力と浮動小数点での注意

ゼロ法線は方向を持たず、NaN・無限大も通常の幾何入力として扱えない。
数学部分はこれらをAPIへ渡す前に拒否する。
任意に小さい非ゼロ方向を数学的なゼロと決め付けることはしないが、実務では入力精度に応じた「方向を信頼できるか」の判断も必要になる。

単位化では最大絶対成分で先に割り、二乗和の桁あふれやアンダーフローを避ける。
それでも極端な成分比や、入力の時点で失われた情報は回復できない。
APIの `GetNormal()` はGlobal許容差を使い、短いベクトルの扱いに条件があるため、自作の単位化と無条件に同一視しない。
実演は数学部分で単位化したNを `PlaneToWorld` に渡す。

1/64の分岐比較は厳密な `<` を使い、数値照合の許容誤差とは分離する。
境界付近では単位化や入力の丸めで分岐が変わる可能性があるので、実際に比較したNの成分も出力する。
G17の表示も、丸めた表示値から分岐をやり直すためのものではない。
厳密な期待値と浮動小数点での計算値を区別し、残差・直交性・右手系を併せて確認する。

## 第1〜3章の公式出典と検証範囲

確認日：2026-11-16。AutoCAD 2026版の公式資料を静的に照合した。
本文の数値・基底・行列は独立した成分計算による期待値であり、AutoCAD実機の出力記録ではない。

| 公式資料 | 照合した対象・適用範囲 |
| --- | --- |
| [DXF：OCS](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-DXF/files/GUID-D99F1509-E4E4-47A3-8691-92EA07DC88F5.htm) | OCS原点、法線方向、平面の高さ。任意ローカル座標との区別 |
| [DXF：任意軸アルゴリズム](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-DXF/files/GUID-E19E5B42-0CC7-4EBA-B29F-5E1D595149EE.htm) | 単位N、両成分の厳密な1/64比較、外積の順序と単位化 |
| [DXF：LWPOLYLINE](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-DXF/files/GUID-748FC305-F3F2-4F74-825A-61F04D757A50.htm) | 頂点・Elevation・押し出し方向の区別 |
| [座標系の説明](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-0EFA65CC-C1AB-4B99-8159-C31602C1A5E8.htm) | WCS・UCS・OCSの役割。個別APIの座標系は各リファレンスを優先 |
| [GetNormal()](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_GetNormal.html) ／ [GetNormal(Tolerance)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_GetNormal_Tolerance.html) | Global許容差と、短いベクトルが単位化されない条件 |

Polylineの各APIは第1・2章、PlaneToWorldは第3章のリンクを参照する。
実演の起動・デバッガーで見る変数・図面への副作用は [実演コードの案内](examples/README.md) にまとめる。

## 4. OCSからWCSへの変換方法の比較

### 同じ入力から4つの書き方を読む

第3章では、OCSの3成分を基底の線形結合でWCSへ表せることを確かめた。
では、普段使っていないAPIでも同じ処理を読めるだろうか？
固定例A・Bの非対称なPolylineを使い、同じ頂点番号を次の4方式で比較する。

| 方式 | 入力と計算 | 共通する意味 |
| --- | --- | --- |
| 1. `GetPoint3dAt` | Polylineから頂点のWCS位置を直接取得 | 保存されたOCS頂点・高さ・法線に対応する位置を読む |
| 2. `PlaneToWorld` | `GetPoint2dAt`のXYとElevationを組み合わせ、平面→世界の行列を適用 | OCSの3成分をWCS成分へ変換 |
| 3. 自作基底行列 | 任意軸アルゴリズムの3基底を列に並べたMatrix3dを適用 | 各基底を、各OCS成分の分だけ足す |
| 4. 線形結合 | uX軸＋vY軸＋h法線を成分で直接計算 | 行列の積を展開したもの |

方式1はPolylineの頂点に対する取得APIであり、任意の点を渡す一般の変換関数ではない。
方式2〜4は、同じOCSを定める基底・同じ3成分を使った場合に、方式1と同じ位置になることを期待する。
APIの実機での一致は未検証である。

実演の [`OcsToWcsCommands.cs`](../../autocad-net/samples/autocad-transform/OcsToWcsCommands.cs) は、各方式の結果を別の変数に保持する。
次は同ファイルの比較部分からの抜粋である。

```csharp
Point2d ocs2d = source.GetPoint2dAt(index);
Point3d ocs3d = new(ocs2d.X, ocs2d.Y, elevation);
Point3d byGetter = source.GetPoint3dAt(index);
Point3d byPlaneToWorld = ocs3d;
byPlaneToWorld = byPlaneToWorld.TransformBy(planeToWorld);
Point3d byBasisMatrix = ocs3d;
byBasisMatrix = byBasisMatrix.TransformBy(basisMatrix);
```

後ろ2方式も、元の `ocs3d` から始める。
一方の変換結果に別の変換を重ねる比較にはしない。

### 基底は列、配列に詰める順は行

OCSのX・Y・Z成分をu・v・wと書く。
Polylineの図形平面上ではw=hだが、任意のOCS点まで含めた変換ではwを自由な3成分目として扱う。
OCS原点とWCS原点が共通なので、

$$
\begin{aligned}
P_W&=uA_x+vA_y+wN, \\
R&=\begin{bmatrix}
(A_x)_x & (A_y)_x & N_x \\
(A_x)_y & (A_y)_y & N_y \\
(A_x)_z & (A_y)_z & N_z
\end{bmatrix},\qquad
P_W=R\begin{bmatrix}
u \\
v \\
w
\end{bmatrix}.
\end{aligned}
$$

Rの第1列は、入力を `(1,0,0)` にしたときの行き先、つまりOCSのX軸である。
第2・3列も同様にY軸・法線を表す。
列ベクトルに左から掛けることと、配列の格納順序は別の話だ。

AutoCADの [`Matrix3d(double[])`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Matrix3d_double__.html) は配列から行列を作り、[`ToArray()`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_ToArray.html) は `(行0,列0)…(行0,列3),(行1,列0)…` の順で16要素を返す。
この行順に対応させてデータを用意し、返された16要素も比較する。

```csharp
double[] basisValues =
[
    frame.XAxis.X, frame.YAxis.X, frame.Normal.X, 0,
    frame.XAxis.Y, frame.YAxis.Y, frame.Normal.Y, 0,
    frame.XAxis.Z, frame.YAxis.Z, frame.Normal.Z, 0,
    0, 0, 0, 1,
];
Matrix3d basisMatrix = new(basisValues);
```

最初の3要素にX軸のX・Y・Zを並べると、基底を行に並べた転置の行列になってしまう。
数学上の各列と、配列の行順をそれぞれ確認しよう。
[`Matrix3d` の公式な点・方向の規約](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d.html) では、点の同次成分は1、方向の同次成分は0である。

![OCS基底を列に並べる数学の行列と、Matrix3dへ行ごとに詰める16要素の配列の対応図。列方式と格納順序を区別する](images/05-basis-matrix-layout.svg)

### 第3章の固定値を4方式で照合する

Bの高さは5で、OCS頂点2の3成分は `(3,2,5)` である。
4方式のWCS期待値は、第3章で導いた $(-3,7/5,26/5)$ と同じになる。
輪郭全体でも、各頂点が次の位置にそろうことを確認する。

| 頂点 | 共通のOCS座標 | A：N=+Z,h=0のWCS | B：N=(0,3/5,4/5),h=5のWCS |
| --- | --- | --- | --- |
| 0 | (0,0,h) | (0,0,0) | (0,3,4) |
| 1 | (4,0,h) | (4,0,0) | (−4,3,4) |
| 2 | (3,2,h) | (3,2,0) | (−3,7/5,26/5) |
| 3 | (0,1,h) | (0,1,0) | (0,11/5,23/5) |

線形結合の成分計算も、別の変数に保持する。

```csharp
Point3d byCombination = new(
    ocs3d.X * frame.XAxis.X + ocs3d.Y * frame.YAxis.X + ocs3d.Z * frame.Normal.X,
    ocs3d.X * frame.XAxis.Y + ocs3d.Y * frame.YAxis.Y + ocs3d.Z * frame.Normal.Y,
    ocs3d.X * frame.XAxis.Z + ocs3d.Y * frame.YAxis.Z + ocs3d.Z * frame.Normal.Z);
```

実演は各頂点の4結果・距離残差と、API行列・自作行列の全16要素を出力する。
図面ではBのPolylineと、4結果から結んだ輪郭を同じ位置に描く。
線が重なって見えることだけでなく、数値も照合する。

### 原点と高さを二重に加えない

OCS全体からWCSへの同次行列は、次の形で移動列が0になる。

$$
M=\begin{bmatrix}
R & 0 \\
0 & 1
\end{bmatrix}.
$$

点の入力は `(u,v,h,1)` で、高さはすでにhNとして加わる。
さらにC=hNを移動列へ入れると、高さを二重に加えてしまう。

平面上のCを原点にした別のローカル座標系なら、次の行列と入力にする。

$$
L=\begin{bmatrix}
R & C \\
0 & 1
\end{bmatrix},\qquad
L\begin{bmatrix}
u \\
v \\
0 \\
1
\end{bmatrix}=M\begin{bmatrix}
u \\
v \\
h \\
1
\end{bmatrix}.
$$

同じ平面上の点を表せても、入力の第3成分と原点の規約が異なる。
OCSの3成分か、Cから測る平面内の座標かをコードから読み取ろう。

方向・変位には原点を足さず、同次成分を0にする。
平面内の変位はOCSで `(Δu,Δv,0)`、WCSでは $\Delta u A_x+\Delta v A_y$ である。
今回のMでは移動列が0なので、同じ3成分の点と方向が数値上同じ結果になっても、両者の意味まで同じにはならない。

### Bだけでは分からない、逆向き・転置の誤り

Bの3×3行列は、たまたま転置すると元と同じになる。
逆行列も同じなので、Bだけで `WorldToPlane` と `PlaneToWorld` の取り違えを検出することはできない。
単純な+Zの例も単位行列で、やはり違いを隠してしまう。

そこで第3章の斜め法線N=(1/3,2/3,2/3)も比較する。
任意軸の基底と行列は、

$$
R_s=\begin{bmatrix}
-2/\sqrt5 & -2/(3\sqrt5) & 1/3 \\
1/\sqrt5 & -4/(3\sqrt5) & 2/3 \\
0 & \sqrt5/3 & 2/3
\end{bmatrix}.
$$

頂点2の `(u,v,h)=(3,2,5)` に対し、順方向は各基底の3倍・2倍・5倍を足すので、

$$
\begin{aligned}
P_s&=3A_x+2A_y+5N \\
&=\begin{bmatrix}
5/3-22/(3\sqrt5) \\
10/3+1/(3\sqrt5) \\
(10+2\sqrt5)/3
\end{bmatrix}.
\end{aligned}
$$

| 斜め法線でOCSの(3,2,5)を渡す方法 | 返る3成分の期待値 |
| --- | --- |
| 正しい順方向R_s | 約(−1.612899700333,3.482404531833,4.824045318333) |
| 逆向きR_sの転置を誤って使う | (−4/√5,11/(3√5),17/3)、約(−1.788854382000,1.639783183500,5.666666666667) |

後者は、外積の符号だけの誤りでも単なる丸め誤差でもない。
同じ入力から独立に計算し、変換の向きと行列要素を確認するための対照例である。

## 5. WCSからOCSへの変換方法の比較

### 内積で、基底に沿う成分を取り出す

順変換が $P_W=uA_x+vA_y+wN$ で、3基底が単位長さ・互いに直交しているとする。
OCS原点はWCS原点Oと同じなので、P_WをOからの位置ベクトルとして内積を取ると、

$$
\begin{aligned}
A_x\cdot P_W
&=u(A_x\cdot A_x)+v(A_x\cdot A_y)+w(A_x\cdot N) \\
&=u.
\end{aligned}
$$

他の2軸も同様で、

$$
\begin{bmatrix}
u \\
v \\
w
\end{bmatrix}=\begin{bmatrix}
A_x\cdot P_W \\
A_y\cdot P_W \\
N\cdot P_W
\end{bmatrix}=R^T P_W.
$$

転置の各行がそれぞれの基底との内積を計算する。
Rの列どうしの内積を並べた $R^T R$ は、対角が1、それ以外が0になるため単位行列である。
したがって、この正規直交基底では $R^{-1}=R^T$ が成立する。

### 4方式を同じWCS点から比べる

[`WcsToOcsCommands.cs`](../../autocad-net/samples/autocad-transform/WcsToOcsCommands.cs) は、手計算で固定した同じWCS点から次の4方式を計算する。

| 方式 | 計算 | 成立条件・意味 |
| --- | --- | --- |
| 1. `WorldToPlane` | 法線からWCS→平面座標の行列を作る | 同じNによるOCSを使う |
| 2. `Inverse` | `PlaneToWorld`の逆行列を適用 | 正しい順方向行列が可逆であること |
| 3. 内積 | Oからの位置ベクトルと各単位基底の内積 | 原点が共通、基底が正規直交であること |
| 4. 転置 | 自作OCS基底の同次行列を転置して適用 | 今回は正規直交かつ移動列0。一般の4×4には拡張しない |

```csharp
Matrix3d worldToPlane = Matrix3d.WorldToPlane(Vector(frame.Normal));
Matrix3d planeToWorld = Matrix3d.PlaneToWorld(Vector(frame.Normal));
Matrix3d inverse = planeToWorld.Inverse();
Matrix3d transpose = basisMatrix.Transpose();
```

`basisMatrix` は第4章と同じ配列から別途作った行列で、Vectorは3成分を同じVector3dへ移すだけの補助関数である。
内積も計算箇所を隠さず、同じ入力点から成分を取り出す。

```csharp
Vector3d displacement = new(world.X, world.Y, world.Z);
Point3d byDot = new(
    displacement.DotProduct(Vector(frame.XAxis)),
    displacement.DotProduct(Vector(frame.YAxis)),
    displacement.DotProduct(Vector(frame.Normal)));
```

ここでPoint3dに入れた値はOCSの座標である。
Point3dという型だけで座標系が自動判定されるわけではないので、変数名・入力規約と一緒に読む。
実演ではAPIの3行列、4方式のOCS座標・残差、その後のWCSへの往復結果を出力する。

### 転置を逆行列として使える範囲

長さを2倍にした基底を含む行列 $S=\operatorname{diag}(2,1,1)$ では、逆行列の第1成分は1/2であり、転置の2とは一致しない。
直交しているだけでは足りず、単位長さも必要である。
せん断を含む一般の基底でも同じように転置を逆行列とは扱えない。

また、原点Cを持つ一般のローカル配置 $P_W=C+Rq$ の逆変換は、

$$
q=R^T(P_W-C)
$$

であり、先にCを引く必要がある。
同次行列全体の転置ではなく、正規直交な3×3部分の転置を使う。

$$
\begin{bmatrix}
R & C \\
0 & 1
\end{bmatrix}^{-1}=\begin{bmatrix}
R^T & -R^T C \\
0 & 1
\end{bmatrix}.
$$

原点の差まで消えるわけではない。
今回OCS全体のMで転置を使えるのは、正規直交であることに加えて移動列が0だからである。

### 平面外の点を変換しても、射影にはならない

Bの頂点2をPとし、法線方向へ2進んだ別のWCS点をQとする。

$$
\begin{aligned}
P&=\begin{bmatrix}
-3 \\
7/5 \\
26/5
\end{bmatrix}, \\
Q=P+2N&=\begin{bmatrix}
-3 \\
13/5 \\
34/5
\end{bmatrix}.
\end{aligned}
$$

Pの平面条件はN・P=5だが、QはN・Q=7になる。
基底と内積を取れば、PのOCS座標は `(3,2,5)`、Qは `(3,2,7)` である。
X・Yが同じでZが違うのは、N方向だけに移動したためだ。

| 固定WCS点 | 4方式のOCS期待値 | 高さを捨てない往復のWCS期待値 |
| --- | --- | --- |
| Aの頂点2：(3,2,0) | (3,2,0) | (3,2,0) |
| Aの平面外：(3,2,2) | (3,2,2) | (3,2,2) |
| BのP：(−3,7/5,26/5) | (3,2,5) | Pと同じ |
| BのQ：(−3,13/5,34/5) | (3,2,7) | Qと同じ |
| 斜め法線のP_s | (3,2,5) | P_sと同じ |
| 斜め法線のP_s+2N | (3,2,7) | P_s+2Nと同じ |

順方向の `OCS→WCS→OCS` も、各頂点の `(u,v,h)` に戻ることを照合する。
逆方向も `WCS→OCS→WCS` とすれば、平面外のQはQのままである。
座標の表現を変えただけで、点は動いていない。

QのOCSのZを7から5へ置き換えて順変換した場合は、別の操作になる。
正規直交基底なので、変化量は法線方向の−2Nで、Pへ戻る。
これは指定した平面への直交射影であり、元のQを復元する往復変換ではない。

![WCSの平面上のPと平面外のQをOCSへ往復しても位置が保たれる一方、QのOCS Zを7からElevation 5へ置き換えるとPへ射影される図](images/06-roundtrip-and-projection.svg)

### Polylineの頂点に戻せる点かを調べる

同じPolylineへ `AddVertexAt` する場合は、OCSのXYだけでなく、求めたZとそのPolylineのElevationが一致するかも調べる。
Qの `(3,2,7)` からXYだけを抜き出してh=5のPolylineへ渡すと、追加される位置はQではなくPになる。
図形平面の条件を満たしていない点から、黙ってZを捨ててはいけない。

実演ではBの4つのWCS頂点を固定入力として逆変換し、次の判定を通った点だけで新しいPolylineを作る。

```csharp
bool finite = double.IsFinite(ocs.X) && double.IsFinite(ocs.Y) && double.IsFinite(ocs.Z);
if (!finite || Math.Abs(ocs.Z - reconstructed.Elevation) > vertexPlaneTolerance)
    throw new System.InvalidOperationException("同じ図形平面上の有限な頂点ではありません。");
reconstructed.AddVertexAt(reconstructed.NumberOfVertices, new Point2d(ocs.X, ocs.Y), 0, 0, 0);
```

Normal・Elevationは頂点追加前に設定し、WCS点を直接AddVertexAtへ渡さない。
Qは高さの差が2なので、この判定を満たさず、頂点へ追加しない。
描画では平面上のPと平面外のQも、別の十字で示す。

### 似ていても違うコードを、数値で見分ける

| 誤りやすい操作 | Bの頂点2に対する結果 | 何を取り違えたか |
| --- | --- | --- |
| 正しいOCSの(3,2,5)を変換 | (−3,7/5,26/5) | 比較の基準 |
| OCSのZを0にしてしまう | (−3,−8/5,6/5) | Elevationを落とした |
| (3,2,5)を変換後、さらにC=5Nを足す | (−3,22/5,46/5) | 高さを二重加算した |
| X'=A_y、Y'=−A_xという別の右手基底を使う | (2,3/5,29/5) | 法線が同じでも、同じOCS基底ではない |

いずれもBの固定値から、基底の線形結合で検算できる。
変換の向きや行と列の入替えは、Bでは見分けられないので第4章の斜め例も使う。
同じ法線・直交性・右手系を確認することは必要だが、AutoCADが選ぶOCSと同じ基底であることの代わりにはならない。

例えば次のコードはOCSの高さを落としてから変換する誤りである。

```csharp
Point3d withoutElevation = new(ocs2d.X, ocs2d.Y, 0);
withoutElevation = withoutElevation.TransformBy(planeToWorld);
```

OCS値を `WorldToPlane` へ渡す逆向きの誤りも、同じようにコードは書けてしまう。
`GEOAT_OCS_TO_WCS` は頂点2について、高さの欠落・逆向き・二重加算・別基底の結果を診断例として出力する。
正しい4方式の結果や描画には混ぜず、Bで見分けられない誤りは斜め例と対比する。

### 許容誤差と、数学的な等価性を分ける

ここまでの期待値は厳密な実数の値である。
浮動小数点では、単位化・内積・逆行列・丸めによって小さな残差が生じるため、完全一致だけで判断しない。
例えば各成分a・bの照合では、絶対誤差と相対誤差を組み合わせた次の基準を使える。

$$
|a-b|\leq\varepsilon_{\mathrm{abs}}
+\varepsilon_{\mathrm{rel}}\max(|a|,|b|).
$$

今回の小さな固定座標なら、絶対許容誤差1e−10作図単位と相対係数1e−12は照合の一例となる。
基底・回転行列の成分は無次元であり、長さの残差と同じ単位ではない。
その場合は、例えば絶対・相対とも1e−12という別の基準で各要素を照合する。
実演コマンドは座標・行列・距離残差をG17で出力し、数値比較の規約を隠さない。

頂点受入れの `vertexPlaneTolerance=1e-10` は、この固定例での平面までの距離の許容値である。
Nが単位長さなので、 $|w-h|$ が距離になる。
許容範囲内でもZを捨てて頂点にすると、その分だけ元の点から位置を調整することになる。
「ほぼ同じ平面」と、厳密に同じ点・同じ平面という数学的条件を区別する。
任意の大きさ・単位・原点の入力へこの値をそのまま流用せず、桁落ちや入力精度を検討する。
任意軸の1/64という分岐定数は別の規則であり、この許容値で分岐を広げない。

## 第4〜5章の公式出典と検証範囲

確認日：2026-11-16。AutoCAD 2026版の公式Managed .NET APIと、第1〜3章のOCS規約を照合した。
固定入力の期待値・成分計算と、AutoCAD参照DLLでのビルド・実機のAPI結果は区別する。

| 公式資料 | 照合した対象 |
| --- | --- |
| [Matrix3d](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d.html) | 左から行列を掛ける列方式、点の同次成分1・方向0、移動列 |
| [配列コンストラクター](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Matrix3d_double__.html) ／ [ToArray](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_ToArray.html) | 配列からの構築、16要素の行順と照合 |
| [PlaneToWorld](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_PlaneToWorld_Vector3d.html) ／ [WorldToPlane](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_WorldToPlane_Vector3d.html) | 同じ法線を用いる、平面→世界と世界→平面の向き |
| [Inverse](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Inverse.html) ／ [Transpose](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Transpose.html) | 戻り値Matrix3d。転置と逆行列が一致する条件は本文の正規直交性から確認 |
| [DotProduct](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_DotProduct_Vector3d.html) | Vector3dの内積、各OCS成分の取り出し |

GetPoint3dAt・GetPoint2dAt・AddVertexAtとTransformByの署名は、第1〜3章と [実演コード側の照合表](../../autocad-net/samples/autocad-transform/README.md#公式資料との静的照合) に従う。
職場の参照DLLでのビルド・NETLOAD・実機実行は未検証であり、実測済みの結果とは記載しない。

## 6. 座標系を考慮した移動・回転・逆変換・合成

順次変換、行列積、`PreMultiplyBy`、`PostMultiplyBy`、`AlignCoordinateSystem` を比較の候補とする。
引数や途中の座標を追い、掛ける位置と図形へ作用する順序をどう読み取るか考える。

## 7. その他の変換方法の比較

任意中心の回転、拡大縮小、鏡映、射影を題材候補とする。
WCS・OCS・任意軸アルゴリズムを先に扱い、各変換の目的・適用条件・使い分けを比較する構成とする。

## 8. まとめ：異なるコードの処理意図を読み解く

API名や書き方が違っても、目的・入力の座標系・数学的な意味・期待結果を対応させて読む。
コードの短さや見た目の一致だけで判断せず、何を変換し、どの条件で使えるかを説明する。

[この教材のトップへ戻る](README.md)

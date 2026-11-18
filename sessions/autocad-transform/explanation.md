# 同じ図形変換のいろいろな実現方法を見てみよう

同じ頂点を調べたつもりなのに、返ってくる座標が違う。  
そんなときは、数値だけでなく「どの座標系で表した値か」を確認するところから始めよう。

この教材は、AutoCAD APIや実装方法の違いを数学で結び付け、普段見慣れないコードでも処理の意図を理解することを目指す。  
WCS・OCSと任意軸アルゴリズムを必須かつ最優先とし、基底や基本変換の復習を各題材に関連付ける。

対象はWindows版AutoCAD 2026 Update 1.2以降／.NET 10／Visual Studio。  
API仕様は章ごとのAutoCAD 2026公式資料で静的に照合した。  
DXFの座標規約と開発者ガイドは日本語版、個別APIの署名と適用条件は英語のリファレンスで確認できる。  
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

第1〜5章では、APIから得る座標と、その背景の基底・相互変換を対応させる。  
続く第6章では、移動・回転・合成・ローカル基準・逆変換を比較する。  
第7章は任意中心の回転と選択的な補足題材、第8章は全体を通じたコードの読み方をまとめる。

[実演の順序と停止候補](examples/README.md#実演の順序) から、読む章に対応するコードを開ける。  
読み終えたら [練習問題](homework/problems.md) と [解答・解説](homework/solutions.md) で、処理の意図を説明してみよう。

## 1. 導入：同じ頂点なのに異なる座標が返る？

### 同じ頂点番号で比べてみよう

`Polyline` の頂点を調べる、次のコードを考える。

| 使用するAPI | 引数 | 戻り値と座標系 |
| --- | --- | --- |
| `Polyline.GetPoint2dAt(int index)` | `int`：0始まりの頂点番号。頂点数未満を指定 | `Point2d`：頂点のOCSでのXY座標 |
| `Polyline.GetPoint3dAt(int value)` | `int`：同じ頂点番号 | `Point3d`：同じ頂点のWCSでのXYZ座標 |

```csharp
Point2d ocs2d = polyline.GetPoint2dAt(2);
Point3d wcs3d = polyline.GetPoint3dAt(2);
```

どちらも頂点番号2を読んでいるのに、成分が違う。  
これは別の頂点を取得したからではない！  
[`GetPoint2dAt`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_GetPoint2dAt_int.html) はPolyline自身のOCS（オブジェクト座標系）での2D位置、[`GetPoint3dAt`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_GetPoint3dAt_int.html) はWCS（世界座標系）での3D位置を返す。  
同じ位置を、違う軸に沿って測っているのである。

まず図の頂点番号2に注目しよう。  
左のOCS座標と右のWCS座標は、同じ頂点を違う軸から測った値である。

![同じ非対称Polylineの頂点2を、XY平面の固定例Aと傾斜平面の固定例Bで比較し、OCSの(3,2)がWCSではそれぞれ(3,2,0)と(-3,1.4,5.2)になる図](images/01-polyline-coordinates.svg)

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
法線の長さを1にした `Normal=(0,3/5,4/5)`、`Elevation=5` とする。  
長さを1にすることを、正規化という。  
この例の法線のWCS成分と長さは、

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

実演の [`GEOAT_COORDINATES`](../../autocad-net/samples/autocad-transform/CoordinateSystemCommands.cs) は、AとBを同じ頂点一覧から独立に生成する。  
一方を動かした結果を、もう一方の比較元には使わない。

## 2. WCS・UCS・OCSの役割と違い

### API紹介：平面の向き・高さ・頂点を指定する

| PolylineのAPI | 型・値と意味・座標系 |
| --- | --- |
| `Normal`（読み書き） | `Vector3d`：平面に垂直な向き。WCS成分。本文・実演では0ではない入力を長さ1にして使う |
| `Elevation`（読み書き） | `double`：OCSのZ方向に測った共通の高さh。WCSのZ座標とは限らない |
| 頂点の `Point2d` | OCSのXY成分(u,v)。頂点ごとに異なる |
| `AddVertexAt` | OCSの2D頂点を追加する。戻り値は `void`。WCS点をそのまま渡さない |

この教材で使う `AddVertexAt` の引数は、次の5つである。

| 引数 | 型 | 意味 |
| --- | --- | --- |
| `index` | `int` | 0始まりの挿入位置。末尾へ追加するときは現在の頂点数 |
| `pt` | `Point2d` | PolylineのECS、ここでのOCSのXY座標 |
| `bulge` | `double` | 円弧の膨らみ。今回は0で直線 |
| `startWidth` | `double` | 頂点から始まる区間の始点幅。今回は0 |
| `endWidth` | `double` | 同じ区間の終点幅。今回は0 |

[`Normal`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Normal.html)・[`Elevation`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_Elevation.html)・[`AddVertexAt`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Polyline_AddVertexAt_int_Point2d_double_double_double.html) の2026版仕様と、[LWPOLYLINEのDXF定義](https://help.autodesk.com/cloudhelp/2026/JPN/AutoCAD-DXF/files/GUID-748FC305-F3F2-4F74-825A-61F04D757A50.htm) を対応させる。  
これは通常の `Polyline` の例であり、`Polyline2d`・3D Polyline・任意のEntityの全座標へ一律に流用しない。

実演ではNormalとElevationを先に設定し、各 `Point2d` を追加する。

```csharp
polyline.Normal = new Vector3d(0, 3.0 / 5, 4.0 / 5);
polyline.Elevation = 5;
polyline.AddVertexAt(2, new Point2d(3, 2), 0, 0, 0);
```

この抜粋の頂点番号2は、番号0・1がすでに追加されている前提である。  
実行可能な全体は [管理元のソース](../../autocad-net/samples/autocad-transform/CoordinateSystemCommands.cs) を読む。

Normalは「向き」、Elevationは「その向きへ測る高さ」である。  
次の4例はすべて同じOCS頂点 `(3,2)` を使い、法線だけ・高さだけを変えた違いを示す。

![共通のOCS頂点(3,2)について、初期状態、Normalだけを変更、高さだけを変更、両方を変更した4例。OCS原点Oと図形平面上のCを区別する](images/08-normal-and-height.svg)

### どの原点・軸から測った値か

座標は点そのものではなく、選んだ原点と軸に対する位置の数値表現である。  
AutoCADでは、まず次の3つを区別しよう。

次の図は、Bの頂点Pを動かさず、測る原点・軸だけを変えた比較である。  
UCSの例だけは、説明用に原点をWCSの `(1,0,0)`、X軸を+WCS Y、Y軸を−WCS X、Z軸を+WCS Zに選んだ。  
実演コードは現在のUCSを変更しない。

![同じ固定例Bの頂点PをWCS、説明用に選んだUCS、OCSで測る図。WCS座標(-3,1.4,5.2)、UCS座標(1.4,4,5.2)、OCS座標(3,2,5)と各原点・軸の対応](images/07-coordinate-system-roles.svg)

| 座標系 | 目的・原点・軸 | この教材での使い方 |
| --- | --- | --- |
| WCS：世界座標系 | 図面の基準となる固定の原点Oと直交XYZ軸 | 法線・各基底の成分、図形の3D位置を表す |
| UCS：ユーザー座標系 | 利用者が作業用に選ぶ原点と直交軸。WCSに対して位置と向きを持つ | OCSとの違いを理解するために扱い、実演では設定を変更しない |
| OCS：オブジェクト座標系。ECSとも呼ぶ | ここで扱うPolylineでは原点はWCS原点と同じ。Z軸の向きは法線、X・Y軸は任意軸アルゴリズムで決める | 平面上の頂点を2成分と共通の高さで記録する |

[公式DXFのOCS説明](https://help.autodesk.com/cloudhelp/2026/JPN/AutoCAD-DXF/files/GUID-D99F1509-E4E4-47A3-8691-92EA07DC88F5.htm) は、OCSの原点がWCS原点と一致し、X・Y軸を一貫した規則で定めることを示している。  
一般のローカル座標系なら、原点を任意の位置に置ける。  
AutoCADのOCSと同じものとして扱わない。  
UCSを変えたことだけで、既存Polylineの保存されたOCSを「今のUCS」と読み替えることもできない。

数学の説明では、WCSの標準基底を $W_x,W_y,W_z$ 、OCSの長さ1の軸（単位基底）を $A_x,A_y,N$ と書く。  
基底とは、座標を測る基準となるベクトルの組である。  
すべて右手系とし、ベクトルの成分はWCSで表す。  
各軸の長さは1で無次元、点の座標とElevationは共通の長さ単位を持つ。

### OCS原点と、図形平面の基準点は別

図では、共通の原点Oと、図形平面上の点Cを分けて見よう。

![WCS原点とOCS原点が一致し、法線N=(0,3/5,4/5)に沿った高さ5の点C=(0,3,4)から平面内の変位を足す図。CはOCS原点ではない](images/02-origin-elevation.svg)

OCS原点はWCS原点Oと同じである。  
一方、Polylineが乗る平面はOCSの $z=h$ にある。  
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

### 法線だけでは平面内の軸が決まらない

次の図では、法線を同じにしたまま、平面内の2軸だけを回している。

![同じ法線N=+Zでも、平面内の直交基底を90度回せることと、AutoCADのOCSが規則で一組の軸を選ぶことを示す図](images/03-normal-freedom.svg)

Nを固定しても、それに垂直な2軸は、Nの周りに回せる。  
例えばNが+Zなら、X・Yを使っても、+Y・−Xを使っても、同じ平面に沿う右手系を作れる。  
しかし同じ座標 `(3,2)` が表す位置は、軸の選び方で変わる。

AutoCADはこの自由度を任意軸アルゴリズムという規則で固定する。  
「法線を指定したら、数学的に唯一のX・Yが決まる」という意味ではない。

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
UCSについては [座標変換の開発者ガイド（日本語）](https://help.autodesk.com/cloudhelp/2026/JPN/OARX-DevGuide-Managed/files/GUID-0EFA65CC-C1AB-4B99-8159-C31602C1A5E8.htm) も参照できる。  
実演の固定入力はOCSまたはWCSと明記して直接与え、現在のUCSから取得しない。

### 補足：DXFに記録される成分

DXFでは頂点のOCS成分がグループ10・20、高さが38、法線方向が210・220・230に対応する。  
APIの値と保存データを対応させるための番号であり、WCSのXYZを頂点の2成分へそのまま保存する規約ではない。

## 3. 任意軸アルゴリズム（Arbitrary Axis Algorithm）

### API紹介：法線から平面→世界の行列を作る

OCSの向きをWCSで知りたいときは、まず `Matrix3d.PlaneToWorld(Vector3d normal)` を見る。  
法線を1本渡すだけで、平面座標をWCSへ変換する `Matrix3d` が返る。  
引数は平面の法線を表すWCSの `Vector3d` で、ここでは先に長さを1にしたNを渡す。  
平面を渡す別のオーバーロードではなく、この法線だけのオーバーロードを使う。

もう一つ使う [`Vector3d.TransformBy(Matrix3d leftSide)`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_TransformBy_Matrix3d.html) は、行列を適用した方向を `Vector3d` で返す。  
次は [管理元の実演](../../autocad-net/samples/autocad-transform/ArbitraryAxisCommands.cs) の抜粋である。  
入力のX・Y・Z方向はOCSの標準軸、結果の `apiX`・`apiY`・`apiZ` はWCS成分での向きとして読む。

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

法線だけでX・Yまで返せるのは、AutoCADが平面内の軸の選び方も決めているからだ。  
図では、補助軸との外積でXを作り、続いてYを作る順序を追ってみよう。

![固定例Bで補助軸Zと法線Nの外積からX方向を作り、長さ1にしてからNとXの外積でYを作る図。逆順の外積は逆向きになる](images/09-arbitrary-axis-steps.svg)

### 外積で平面内の方向を作る

2つの方向a・bが平行でなければ、その外積は両方に垂直な方向になる。  
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
外積をその長さで割ると、向きを保ったまま長さを1にできる。  
平行な場合は外積が0になり、割り算で向きを作ることはできない。  
詳しい導出は [数学基礎編の外積](../02-vector/explanation.md) と対応する。

### 公式の分岐は「両方が1/64未満」

図の正方形の**内側だけ**でY軸を使い、境界と外側ではZ軸を使う。

![単位法線のNx・Ny平面で、両成分の絶対値が1/64未満の開いた正方形だけがY軸分岐になり、境界と外側がZ軸分岐になる図](images/04-arbitrary-axis-branches.svg)

[任意軸アルゴリズムのDXF資料（日本語）](https://help.autodesk.com/cloudhelp/2026/JPN/AutoCAD-DXF/files/GUID-E19E5B42-0CC7-4EBA-B29F-5E1D595149EE.htm) は、単位法線Nから次の規則で基底を作る。  
法線をWCS成分で表し、先に長さを1にする（正規化）。  
ここからの比較は、正規化した後の成分で行う。

1. $|N_x|<1/64$ **かつ** $|N_y|<1/64$ なら、補助軸をWCSのY軸にする。
2. それ以外は、補助軸をWCSのZ軸にする。どちらかがちょうど1/64でもこちら。
3. 補助軸×Nの長さを1にしてX軸を作る。
4. N×X軸の長さを1にしてY軸を作る。

補助軸をH、長さを1にする前のX方向をUと書くと、

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
Nが正負Z軸の近くにあると、 $W_z\times N$ は小さくなり、長さを1にする割り算で、誤差を大きくしやすい。  
そこで、その近傍ではY軸を使う。  
判定領域はX・Y成分の両方を比べた正方形状の領域で、一定の角度だけで区切る円形の領域ではない。

### 直交・単位長・右手系を確かめる

X軸はNに垂直で単位長さ。  
N×Xから作ったY軸も両方に垂直で、単位長さになる。  
この順序ならX×YはNを向き、右手系になる。  
その理由を式で確かめる計算は、章末の補足に残す。  
実演では、3本の長さ、3組の内積、 $(A_x\times A_y)\cdot N$ を出力する。  
期待値はそれぞれ1、0、1である。

0の法線・NaN・無限大は、APIへ渡す前に数学部分で拒否する。  
境界の1/64は軸を選ぶ規則であり、計算結果を照合する許容誤差とは別である。  
分岐をまたぐと軸が大きく変わることもあるので、向きが連続して変わるとは仮定しない。

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
つまり、X軸を3倍、Y軸を2倍、法線を5倍して足す。

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
WCSのXが負になるのは、OCSのX軸が−WCS Xを向くからである。  
WCSのY・Zには、vによる移動分とhによる移動分の両方が加わる。

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

### 成分計算とPlaneToWorldを照合する

数学部分は [`ArbitraryAxisMath.cs`](../../autocad-net/samples/autocad-transform/ArbitraryAxisMath.cs) に分け、次の順で計算する。

```csharp
Vec n = Unit(normal);
bool usesWorldY = Math.Abs(n.X) < AxisBoundary && Math.Abs(n.Y) < AxisBoundary;
Vec helper = usesWorldY ? new Vec(0, 1, 0) : new Vec(0, 0, 1);
Vec x = Unit(Cross(helper, n));
Vec y = Unit(Cross(n, x));
```

この抜粋は同ファイル内の `Build` の計算部分で、Vecは3成分の方向、Unitは長さを1にする処理を表す。  
[`GEOAT_ARBITRARY_AXIS`](../../autocad-net/samples/autocad-transform/ArbitraryAxisCommands.cs) は同じNをAPIへ渡し、WCSの各標準基底を別々に変換する。

自作の軸とAPIの軸のずれが小さく、長さ1・直交・右手系が保たれることを期待する。  
同じ出発点から計算し、APIが返した軸を自作計算の入力として流用しない。  
第1章のコマンドでは、同じOCS点に対する取得API・PlaneToWorld・各軸を何倍かして足した結果も並べる。  
図面上で軸が重なって見えることだけを、一致の根拠にはしない。

### 補足：外積が0にならず、右手系になる理由 (発展)

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

厳密な実数の計算では、どちらも0ではない。  
固定入力を長さ1にせず比較すると、この根拠もOCSの規則も崩れる。

さらに、ベクトル三重積の関係から、

$$
A_x\times(N\times A_x)
=N(A_x\cdot A_x)-A_x(A_x\cdot N)=N.
$$

よって $A_x\times A_y=N$ となり、右手系が得られる。

### 補足：正負Z・斜め法線と境界の固定値 (発展)

次の表のNは単位法線である。  
実演では固定例Bに `(0,3,4)`、3成分がすべて0ではない例に `(1,2,2)` を渡し、まず長さを1にする。  
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

図では境界の直前と、ちょうど境界の軸を比べる。  
Nの変化は小さくても、X・Yは大きく向きを変える。

![b=1/64の直前はYを補助軸に、境界上と直後はZを補助軸に選ぶ図。ほぼ同じ法線でもXとYの向きが切り替わることを同じ斜投影で示す](images/10-boundary-axis-change.svg)

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

### 補足：無効入力と浮動小数点での注意

ゼロ法線は方向を持たず、NaN・無限大も通常の幾何入力として扱えない。  
数学部分はこれらをAPIへ渡す前に拒否する。  
どれほど小さくても、0ではない方向を数学的なゼロと決め付けることはしない。  
ただし実務では、入力精度に応じて「その方向を信頼できるか」も判断する。

長さを1にするときは、まず絶対値が最大の成分で全成分を割る。  
その後で長さを計算し、二乗和が大きすぎて表せなくなる桁あふれや、小さすぎて失われるアンダーフローを避ける。  
それでも極端な成分比や、入力の時点で失われた情報は回復できない。  
APIの `GetNormal()` はGlobal許容差を使い、短いベクトルの扱いには条件がある。  
自作の正規化と、無条件に同じ処理とは考えない。  
実演は数学部分で長さを1にしたNを `PlaneToWorld` に渡す。

1/64の分岐比較は厳密な `<` を使い、数値照合の許容誤差とは分離する。  
境界付近では正規化や入力の丸めで分岐が変わる可能性があるので、実際に比較したNの成分も出力する。  
G17の表示も、丸めた表示値から分岐をやり直すためのものではない。  
厳密な期待値と浮動小数点での計算値を区別し、計算結果のずれ（残差）・直交性・右手系を併せて確認する。

## 第1〜3章の公式出典と検証範囲

確認日：2026-11-16。AutoCAD 2026版の公式資料を静的に照合した。  
本文の数値・基底・行列は独立した成分計算による期待値であり、AutoCAD実機の出力記録ではない。

| 公式資料 | 照合した対象・適用範囲 |
| --- | --- |
| [DXF：OCS](https://help.autodesk.com/cloudhelp/2026/JPN/AutoCAD-DXF/files/GUID-D99F1509-E4E4-47A3-8691-92EA07DC88F5.htm) | OCS原点、法線方向、平面の高さ。任意ローカル座標との区別 |
| [DXF：任意軸アルゴリズム](https://help.autodesk.com/cloudhelp/2026/JPN/AutoCAD-DXF/files/GUID-E19E5B42-0CC7-4EBA-B29F-5E1D595149EE.htm) | 単位N、両成分の厳密な1/64比較、外積の順序と単位化 |
| [DXF：LWPOLYLINE](https://help.autodesk.com/cloudhelp/2026/JPN/AutoCAD-DXF/files/GUID-748FC305-F3F2-4F74-825A-61F04D757A50.htm) | 頂点・Elevation・押し出し方向の区別 |
| [座標系の説明](https://help.autodesk.com/cloudhelp/2026/JPN/OARX-DevGuide-Managed/files/GUID-0EFA65CC-C1AB-4B99-8159-C31602C1A5E8.htm) | WCS・UCS・OCSの役割。個別APIの座標系は各リファレンスを優先 |
| [GetNormal()](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_GetNormal.html) ／ [GetNormal(Tolerance)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_GetNormal_Tolerance.html) | Global許容差と、短いベクトルが単位化されない条件 |

Polylineの各APIは第1・2章、PlaneToWorldは第3章のリンクを参照する。  
実演の起動・デバッガーで見る変数・図面への副作用は [実演コードの案内](examples/README.md) にまとめる。

## 4. OCSからWCSへの変換方法の比較

### API紹介：同じOCS点をWCSで表す

第1・3章の頂点取得と `PlaneToWorld(Vector3d)` を、今度は同じ位置の計算として比べよう。  
新たに使うAPIは、行列を自分で組み立てるためのものだ。

| 使用するAPI | 入力 | 得られる値・座標系 |
| --- | --- | --- |
| `Matrix3d(double[] data)` | 4×4行列の16要素 | 構築した `Matrix3d`。この例ではOCS→WCS |
| [`Point3d.TransformBy(Matrix3d leftSide)`](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_TransformBy_Matrix3d.html) | 変換行列。点は呼出し元の `Point3d` | 変換後の `Point3d`。OCS点に順方向行列を適用するとWCS点 |
| `Matrix3d.ToArray()` | 引数なし | 行ごとに並ぶ16要素の `double[]`。要素の並べ方自体には座標系の変換はない |

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

図の4本の経路は、同じ頂点Pへたどり着くための別々の書き方である。  
頂点取得APIには頂点番号を渡し、残る3方式には同じOCSの3成分を渡す。

![Polylineの頂点番号からWCSを取得する経路と、同じOCS座標(3,2,5)から専用行列・自作行列・各軸の成分計算でWCSへ変換する3経路](images/11-four-forward-methods.svg)

### 4つの書き方に共通する意味

第3章では、各軸をOCSの成分の分だけ進み、それらを足してWCSの位置を求めた。  
この「各軸のベクトルを何倍かして足す」計算を、線形結合という。  
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

### 基底は列、配列に詰める順は行

まず図の左側は列、右側は行を追ってみよう。  
同じ行列でも、数学で基底を並べる向きと配列へ保存する順番は違う。

![OCS基底を列に並べる数学の行列と、Matrix3dへ行ごとに詰める16要素の配列の対応図。列方式と格納順序を区別する](images/05-basis-matrix-layout.svg)

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
列ベクトルに行列を左から掛ける規約と、配列へ保存する順番は、分けて考えよう。

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

各軸を何倍かして足した結果も、別の変数に保持する。

```csharp
Point3d byCombination = new(
    ocs3d.X * frame.XAxis.X + ocs3d.Y * frame.YAxis.X + ocs3d.Z * frame.Normal.X,
    ocs3d.X * frame.XAxis.Y + ocs3d.Y * frame.YAxis.Y + ocs3d.Z * frame.Normal.Y,
    ocs3d.X * frame.XAxis.Z + ocs3d.Y * frame.YAxis.Z + ocs3d.Z * frame.Normal.Z);
```

実演は各頂点の4結果・座標のずれの距離と、API行列・自作行列の全16要素を出力する。  
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

### API紹介：WCS点からOCSの3成分を求める

第4章と逆に、WCSの点が「OCSの各軸へどれだけ進んだ点か」を求める。  
入力・出力の方向を先に確認しよう。

| 使用するAPI | 引数・呼出し元 | 戻り値と使い方 |
| --- | --- | --- |
| `Matrix3d.WorldToPlane(Vector3d normal)` | `Vector3d`：WCS成分の法線N。ここでは長さ1 | `Matrix3d`：WCS→平面座標。第3章と同じNでOCSを定める |
| `Matrix3d.Inverse()` | 引数なし。可逆な順方向行列から呼ぶ | `Matrix3d`：逆行列。OCS→WCSの逆ならWCS→OCS |
| `Vector3d.DotProduct(Vector3d v)` | `Vector3d`：内積の相手。両方を同じWCS成分で表す | `double`：ここでは単位軸方向に測った符号付きの成分 |
| `Matrix3d.Transpose()` | 引数なし。行と列を入れ替える対象の行列から呼ぶ | `Matrix3d`：転置行列。逆変換になるかどうかは別に条件を確かめる |

`WorldToPlane` は、平面を渡す別のオーバーロードではなく、法線 `Vector3d` だけを渡すものを使う。  
次は [管理元の実演](../../autocad-net/samples/autocad-transform/WcsToOcsCommands.cs) の抜粋である。

```csharp
Matrix3d worldToPlane = Matrix3d.WorldToPlane(Vector(frame.Normal));
Matrix3d planeToWorld = Matrix3d.PlaneToWorld(Vector(frame.Normal));
Matrix3d inverse = planeToWorld.Inverse();
Matrix3d transpose = basisMatrix.Transpose();
```

3本の単位軸が互いに直交していれば、各軸との内積で、それぞれの成分だけを取り出せる。  
図のPは第4章と同じ点で、矢印はPを動かす操作ではなく、Oからの位置を分けて測る様子を示す。

![固定例BのWCS点PをOCSの各単位軸へ内積で測り、X成分3・Y成分2・Z成分5を取り出す図。3方向の成分を足す順変換と、成分を測る逆変換を対比する](images/12-dot-product-components.svg)

### 4方式を同じWCS点から比べる

[`WcsToOcsCommands.cs`](../../autocad-net/samples/autocad-transform/WcsToOcsCommands.cs) は、手計算で固定した同じWCS点から次の4方式を計算する。

| 方式 | 計算 | 成立条件・意味 |
| --- | --- | --- |
| 1. `WorldToPlane` | 法線からWCS→平面座標の行列を作る | 同じNによるOCSを使う |
| 2. `Inverse` | `PlaneToWorld`の逆行列を適用 | 正しい順方向行列が可逆であること |
| 3. 内積 | Oからの位置ベクトルと各単位基底の内積 | 原点が共通、基底が正規直交であること |
| 4. 転置 | 自作OCS基底の同次行列を転置して適用 | 今回は正規直交かつ移動列0。一般の4×4には拡張しない |

`basisMatrix` は第4章と同じ配列から別途作った行列で、Vectorは3成分を同じVector3dへ移すだけの補助関数である。  
内積を使う方式でも、同じ入力点から成分を取り出す。  
計算は次のとおりで、補助関数の中へ隠さない。

```csharp
Vector3d displacement = new(world.X, world.Y, world.Z);
Point3d byDot = new(
    displacement.DotProduct(Vector(frame.XAxis)),
    displacement.DotProduct(Vector(frame.YAxis)),
    displacement.DotProduct(Vector(frame.Normal)));
```

ここでPoint3dに入れた値はOCSの座標である。  
Point3dという型だけでは、どの座標系かは決まらない。  
変数名と入力の規約も一緒に読もう。  
実演ではAPIの3行列、4方式のOCS座標・計算結果のずれ、その後のWCSへの往復結果を出力する。

### 内積で、基底に沿う成分を取り出す

順変換は $P_W=uA_x+vA_y+wN$ である。  
3本の基底は長さ1で、互いに直交しているとする。  
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
長さ1で互いに直交する基底を、正規直交基底という。  
したがって、この基底では $R^{-1}=R^T$ が成立する。

### 転置を逆行列として使える条件

長さを2倍にした基底を含む行列 $S=\mathrm{diag}(2,1,1)$ では、逆行列の第1成分は1/2であり、転置の2とは一致しない。  
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

図では、青の往復経路と、Zを置き換える赤の経路を分けて追おう。  
前者は座標の表し方だけを変え、後者は点の位置も変える。

![WCSの平面上のPと平面外のQをOCSへ往復しても位置が保たれる一方、QのOCS Zを7からElevation 5へ置き換えるとPへ射影される図](images/06-roundtrip-and-projection.svg)

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

いずれもBの固定値から、各軸のベクトルを何倍かして足すことで検算できる。  
変換の向きや行と列の入替えは、Bでは見分けられないので第4章の斜め例も使う。  
法線・直交性・右手系の確認は必要である。  
それだけでは、AutoCADが選ぶOCSと同じ基底だとは言えない。

例えば次のコードはOCSの高さを落としてから変換する誤りである。

```csharp
Point3d withoutElevation = new(ocs2d.X, ocs2d.Y, 0);
withoutElevation = withoutElevation.TransformBy(planeToWorld);
```

OCS値を `WorldToPlane` へ渡す逆向きの誤りも、同じようにコードは書けてしまう。  
`GEOAT_OCS_TO_WCS` は頂点2について、高さの欠落・逆向き・二重加算・別基底の結果を診断例として出力する。  
正しい4方式の結果や描画には混ぜず、Bで見分けられない誤りは斜め例と対比する。

### 補足：許容誤差と、数学的な等価性を分ける

ここまでの期待値は厳密な実数の値である。  
浮動小数点では、正規化・内積・逆行列・丸めにより、小さな座標のずれが生じる。  
完全一致だけで判断しない。  
例えば各成分a・bの照合では、絶対誤差と相対誤差を組み合わせた次の基準を使える。

$$
|a-b|\leq\varepsilon_{\mathrm{abs}}
+\varepsilon_{\mathrm{rel}}\max(|a|,|b|).
$$

今回の小さな固定座標なら、絶対許容誤差1e−10作図単位と相対係数1e−12は照合の一例となる。  
基底・回転行列の成分は無次元であり、長さの残差と同じ単位ではない。  
その場合は、例えば絶対・相対とも1e−12という別の基準で各要素を照合する。  
実演コマンドは座標・行列・座標のずれの距離をG17で出力する。  
数値を比較する規約も明示する。

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
| [座標変換の開発者ガイド（日本語）](https://help.autodesk.com/cloudhelp/2026/JPN/OARX-DevGuide-Managed/files/GUID-0EFA65CC-C1AB-4B99-8159-C31602C1A5E8.htm) | WCS・UCS・OCSの役割と、3軸を指定する座標変換の概要 |
| [図形変換の開発者ガイド（日本語）](https://help.autodesk.com/cloudhelp/2026/JPN/OARX-DevGuide-Managed/files/GUID-D4348E23-7ECB-48F6-90B7-FB7EF42DFA8D.htm) | Matrix3dとTransformByによる変換、移動を表す第4列 |
| [Matrix3d（英語）](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d.html) | 左から行列を掛ける列方式、点の同次成分1・方向0、移動列 |
| [配列コンストラクター](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Matrix3d_double__.html) ／ [ToArray](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_ToArray.html) | 配列からの構築、16要素の行順と照合 |
| [PlaneToWorld](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_PlaneToWorld_Vector3d.html) ／ [WorldToPlane](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_WorldToPlane_Vector3d.html) | 同じ法線を用いる、平面→世界と世界→平面の向き |
| [Inverse](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Inverse.html) ／ [Transpose](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Transpose.html) | 戻り値Matrix3d。転置と逆行列が一致する条件は本文の正規直交性から確認 |
| [DotProduct](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Vector3d_DotProduct_Vector3d.html) | Vector3dの内積、各OCS成分の取り出し |

GetPoint3dAt・GetPoint2dAt・AddVertexAtとTransformByの署名は、第1〜3章と [実演コード側の照合表](../../autocad-net/samples/autocad-transform/README.md#公式資料との静的照合) に従う。  
職場の参照DLLでのビルド・NETLOAD・実機実行は未検証であり、実測済みの結果とは記載しない。

## 6. 座標系を考慮した移動・回転・逆変換・合成

ここからは、座標を表し直すだけでなく、点や部材を実際に動かす操作を比べる。  
まずWCSで移動・回転・合成を読み、その後でローカル基準との違いを確かめよう。  
前半のOCSと任意軸の規約は、そのまま使う。

### 固定入力とコードの読み方

特記しない例では、第1章と同じ非対称な4頂点を、WCSのXY平面に置く。  
頂点は順に `(0,0,0),(4,0,0),(3,2,0),(0,1,0)` で、注目する頂点2は `(3,2,0)` である。  
長さは作図単位、角度はラジアンとする。

| 比較対象 | この章で確かめること |
| --- | --- |
| 非対称な輪郭 | 軸の入替えや変換順の違いが、輪郭と各頂点へどう現れるか |
| 原点と各軸上の点 | 移動列と、3本の軸の行き先を分けて読む |
| 方向ベクトル | 平行移動が方向には加わらないこと |
| 行列16要素・座標のずれ | 見た目が重なるだけで、同じ変換と判断しないこと |

各方式には元の点のコピーを渡し、戻り値を保持する。  
`Point3d` や `Matrix3d` の変数を再利用するときも、比較元を上書きしない。  
本文の数値は数学上の期待値であり、AutoCAD実機での計測値ではない。

### 6.1 平行移動：点に変位を足す

#### API紹介

`Point3d.Add(Vector3d value)` は、点に変位を足した `Point3d` を返す。  
`Matrix3d.Displacement(Vector3d vector)` は、その変位を表す `Matrix3d` を作る。  
この例では、点と変位の成分をどちらもWCSで与える。

```csharp
Vector3d displacement = new(10, 0, 3);
Matrix3d translation = Matrix3d.Displacement(displacement);
Point3d byVector = point.Add(displacement);
Point3d byMatrix = point.TransformBy(translation);
```

`point` は元のWCS点である。  
点を動かす向きと距離を、次の図で追ってみよう。

![共通の非対称輪郭をWCSで(10,0,3)だけ移動する図。頂点2は(3,2,0)から(13,2,3)へ移り、方向(3,2,1)には変位を加えない](images/13-translation.svg)

#### 成分加算・ベクトル・行列の対応

元の点に、Xへ10、Yへ0、Zへ3という変位を足す。  
3つの書き方は、どれも次の計算を表している。

$$
\begin{aligned}
P'&=P+d,\\
\begin{bmatrix}
x'\\
y'\\
z'
\end{bmatrix}
&=\begin{bmatrix}
x+10\\
y\\
z+3
\end{bmatrix}.
\end{aligned}
$$

`new Point3d(point.X + 10, point.Y, point.Z + 3)` とも書ける。  
行列では移動量を第4列へ置くので、同次成分1の点には移動が加わる。

$$
T=\begin{bmatrix}
1&0&0&10\\
0&1&0&0\\
0&0&1&3\\
0&0&0&1
\end{bmatrix}.
$$

一方、方向の同次成分は0である。  
`Vector3d(3,2,1)` をTで変換しても `(3,2,1)` のままで、`(13,2,4)` にはならない。  
方向を「原点からの点」として扱うと、不要な移動が混ざってしまう。

実演は [TranslationCommands.cs](../../autocad-net/samples/autocad-transform/TranslationCommands.cs) の `GEOAT_TRANSLATION`。  
成分加算・Add・行列の3方式と、点と方向の違いを見る。

### 6.2 回転：同じ角度でも、平面・軸・中心を確認する

#### API紹介

| API | 引数の型と意味 | 戻り値 |
| --- | --- | --- |
| `Point2d.RotateBy(double angle, Point2d origin)` | 平面内の角度と回転中心。正方向はX軸からY軸へ | 回転後の `Point2d` |
| `Point3d.RotateBy` | `double angle, Vector3d vector, Point3d centerPoint`：角度・回転軸の方向・軸が通る点 | 回転後の `Point3d` |
| `Matrix3d.Rotation` | `double angle, Vector3d axis, Point3d center`：同じ3条件 | 回転を表す `Matrix3d` |

ここでは角度を `Math.PI / 2`（90°）、軸をWCSの+Z、中心をWCS原点にする。  
2Dの例はWCSのXY成分を使い、Z成分は変えない。  
任意の傾いたOCSのXYを、そのままWCSのXYだと読む例ではない。

```csharp
double angle = Math.PI / 2;
Point2d rotated2d = xy.RotateBy(angle, Point2d.Origin);
Point3d rotated3d = point.RotateBy(angle, Vector3d.ZAxis, Point3d.Origin);
Matrix3d rotation = Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
Point3d byMatrix = point.TransformBy(rotation);
```

図は+Z側から原点を見る。  
この見方では、+π/2は反時計回りになる。

![WCSのXY平面で非対称輪郭を原点周りに+π/2回転する図。頂点2の(3,2,0)は(-2,3,0)、+X軸上の点は+Y軸上へ移る](images/14-origin-rotation.svg)

#### sin/cosとAPIの対応

原点周りの2D回転は、元のX方向とY方向の成分を回して足す計算である。  
[数学基礎編の回転](../01-matrix/explanation.md) で扱った式に対応する。

$$
\begin{aligned}
x'&=x\cos\theta-y\sin\theta,\\
y'&=x\sin\theta+y\cos\theta,\\
z'&=z.
\end{aligned}
$$

+π/2では `cosθ=0, sinθ=1` だから、頂点2は `(-2,3,0)` になる。  
原点・X・Y・Z軸上の点の行き先も、順に `(0,0,0),(0,1,0),(-1,0,0),(0,0,1)` となる。

`Point2d` には回転軸を渡す引数がない。  
3Dでは、方向が0ではない軸と、その軸が通る点を指定する必要がある。  
正方向は軸に対する右手の規則で決まるため、軸を逆向きにすると同じ正角でも回転の向きは逆になる。  
APIへ0・NaN・無限大の軸を渡す例には広げない。

実演は [RotationCommands.cs](../../autocad-net/samples/autocad-transform/RotationCommands.cs) の `GEOAT_ROTATION`。  
sin/cos・Point2d・Point3d・行列の4方式を照合する。

### 6.3 合成：行列を掛ける位置と、操作する順序

#### API紹介

行列積 `a * b` は、aとbを掛けた `Matrix3d` を返す。  
`Matrix3d.PreMultiplyBy(Matrix3d leftSide)` は左から、`PostMultiplyBy(Matrix3d rightSide)` は右から掛ける。  
両メソッドの戻り値も `Matrix3d` である。  
名前のPre/Postを、図形へ先に／後に操作する意味だと思い込まないようにしよう。

```csharp
Matrix3d product = translation * rotation; // T R
Matrix3d byPre = rotation;
byPre = byPre.PreMultiplyBy(translation);  // T R
Matrix3d byPost = translation;
byPost = byPost.PostMultiplyBy(rotation); // T R
```

Rは前節の原点周りの+π/2回転、Tは `(10,0,3)` の移動である。  
図の2経路は、同じ入力から出発している。

![同じ点(3,2,0)を回転してから移動すると(8,3,3)、移動してから回転すると(-2,13,3)になる図。行列TRとRTの右端が最初に作用する](images/15-composition-order.svg)

#### 逐次変換・積・Pre/Postを対応させる

列ベクトルへ行列を左から掛けるので、右端のRが先に作用する。

$$
P'=T(RP)=(TR)P.
$$

逐次なら `point.TransformBy(rotation).TransformBy(translation)` と書ける。  
一つの行列なら、前のコードの3方式はすべてTRになる。

$$
TR=\begin{bmatrix}
0&-1&0&10\\
1&0&0&0\\
0&0&1&3\\
0&0&0&1
\end{bmatrix},
\qquad
RT=\begin{bmatrix}
0&-1&0&0\\
1&0&0&10\\
0&0&1&3\\
0&0&0&1
\end{bmatrix}.
$$

| 順序 | 頂点2の途中計算 | WCS期待値 |
| --- | --- | --- |
| 回転→移動：TR | (3,2,0) → (−2,3,0) → (8,3,3) | (8,3,3) |
| 移動→回転：RT | (3,2,0) → (13,2,3) → (−2,13,3) | (−2,13,3) |

`rotation.PostMultiplyBy(translation)` とするとRTになる。  
`translation.PostMultiplyBy(rotation)` との違いは、呼出し元も含めて読まないと分からない。

実演は [CompositionCommands.cs](../../autocad-net/samples/autocad-transform/CompositionCommands.cs) の `GEOAT_COMPOSITION`。  
最初の4方式は同じTR、5番目だけは比較用のRTである。  
0ではない座標のずれは、丸め誤差ではなく、操作の違いを表す場合がある。

### 6.4 ローカル基準かWCS基準か：同じ「Xへ2」でも違う

#### API紹介と短い使用例

ここでは、新しい専用APIではなく、`Displacement` と `TransformBy` の使い分けを見る。  
ローカル→WCSの配置をM、ローカルでの移動をT_Lとすると、`M * T_L` はローカル側で移動してから配置する行列になる。

```csharp
Vector3d localDisplacement = new(2, 0, 0);
Vector3d worldDisplacement = localDisplacement.TransformBy(localToWorld);
Matrix3d equivalentMove = Matrix3d.Displacement(worldDisplacement);
```

入力はローカル成分の**方向・変位**、結果はWCS成分の変位である。  
`Point3d` として変換すると、配置先の原点まで余計に加えてしまう。

![Bの平面の基準点C=(0,3,4)から配置したP=(-3,7/5,26/5)に対し、WCS Xへ2では(-1,7/5,26/5)、ローカルXへ2では(-5,7/5,26/5)へ移動する図](images/16-local-movement.svg)

#### Bの平面にローカル原点Cを置く

第2〜5章のBと同じ軸を使い、今度は原点をCに置く。  
これは、WCS原点を共有するOCS全体とは別の、平面上のローカル座標系である。  
入力は `(u,v,0)`、Cは `(0,3,4)` とする。  
`(u,v,5)` を渡して高さを二重に加えない。

$$
M=\begin{bmatrix}
-1&0&0&0\\
0&-4/5&3/5&3\\
0&3/5&4/5&4\\
0&0&0&1
\end{bmatrix}.
$$

ローカルの頂点2 `(3,2,0)` は、WCSの `P=(-3,7/5,26/5)` に配置される。  
ローカルX軸はWCSの−Xを向くので、同じ2でも移動方向が逆になる。

| 指示 | 行列の位置 | Pの移動先 |
| --- | --- | --- |
| WCS Xへ2 | T_W M | (−1,7/5,26/5) |
| ローカルXへ2 | M T_L | (−5,7/5,26/5) |
| ローカル変位をWCSで表してから移動 | T(Rd_L) M | (−5,7/5,26/5) |

Mの3×3部分をR、移動列をCと書けば、ローカル変位d_LがWCSでRd_Lになるため、

$$
\begin{aligned}
M(q+d_L)&=C+Rq+Rd_L,\\
T(Rd_L)Mq&=C+Rq+Rd_L.
\end{aligned}
$$

この例の `d_L=(2,0,0)` は、WCSでは `(-2,0,0)` になる。  
Mが剛体配置、つまり長さと角度を保つ回転と移動だけなら、変位の長さ2も保たれる。

#### 回転中心と軸も、同じ座標系で指定する

`Matrix3d.Rotation` の引数は、作用させる点と同じ座標系でそろえる。  
ローカルの原点を通る+Z軸の回転をWCS側で行うなら、中心を `M(0,0,0)=C`、軸の方向を `R(0,0,1)=N` にする。  
Nを点として変換してCを足してはいけない。

![Bに配置済みの同じPをWCS原点の+Z軸で回す場合と、Cを通る法線Nの軸で回す場合の図。後者がローカル原点周りの回転に対応する](images/17-local-rotation.svg)

ローカル側の+π/2回転K_Lは、`(3,2,0)` を `(-2,3,0)` にする。  
それをMで配置すれば、次のWCS点になる。

$$
\begin{aligned}
M K_L\begin{bmatrix}
3\\
2\\
0\\
1
\end{bmatrix}
&=\begin{bmatrix}
2\\
3/5\\
29/5\\
1
\end{bmatrix}.
\end{aligned}
$$

| 基準・回転軸・中心 | 作用 | WCS期待値 |
| --- | --- | --- |
| WCS：+Z、原点O | K_W M | (−7/5,−3,26/5) |
| ローカル：+Z、ローカル原点 | M K_L | (2,3/5,29/5) |
| WCSで指定し直す：N、C | $K_{C,N}M$ | (2,3/5,29/5) |

右手系の剛体配置では、中心と軸をこのように変換すれば後ろ2つは同じ回転になる。  
拡大縮小・せん断・鏡映を含む一般のMへ、この回転軸と同じ角度の対応をそのまま広げない。  
一般には $M K_L M^{-1}$ という共役変換を考える必要がある。  
座標系を変えて同じ操作を表すとき、前後にMと逆行列を挟む、という意味である。

実演は [LocalFrameCommands.cs](../../autocad-net/samples/autocad-transform/LocalFrameCommands.cs) の `GEOAT_LOCAL_FRAME`。  
移動は方式2と3、回転は方式5と6がそれぞれ一致する。  
灰色はすでにBへ配置した輪郭であり、比較中にUCS設定を変えているわけではない。

### 6.5 逆変換：最後にした操作から戻す

#### API紹介

`Matrix3d.Inverse()` は、可逆な行列に対して逆行列を `Matrix3d` で返す。  
ここでは回転と移動の積TRが対象である。  
入力は配置後のWCS点、出力は配置前の座標になる。

```csharp
Matrix3d inverse = product.Inverse();
Point3d byInverse = placed.TransformBy(inverse);
Point3d byUndoSteps = placed.TransformBy(undoMove).TransformBy(undoRotate);
```

`undoMove` は `(-10,0,-3)` の移動、`undoRotate` は原点の+Z周りの−π/2回転である。  
[合成順序の図](images/15-composition-order.svg) の上の経路を、終点から逆にたどってみよう。

$$
(TR)^{-1}=R^{-1}T^{-1}.
$$

頂点2は `(8,3,3) → (-2,3,0) → (3,2,0)` と戻る。  
先に回転を戻してから移動を戻すと、通常は元の点には戻らない。  
逆行列を取ることと、行列の全要素の符号を反転することも別である。

方向 `(3,2,1)` をTRで変換すると `(-2,3,1)` になる。  
方向を戻すときは逆回転が働き、逆移動は加わらない。  
実演では `GEOAT_COMPOSITION` で、原点・3軸上の点・頂点2の往復を照合する。

### 6.6 補足：部材の配置を4方式で読む (発展)

#### API紹介

`Matrix3d.AlignCoordinateSystem` は、from側の原点・3軸を、to側の原点・3軸へ写す `Matrix3d` を返す。  
原点はそれぞれ `Point3d`、各軸は `Vector3d` で、引数は合計8つである。

| 引数 | 意味 |
| --- | --- |
| `fromOrigin, fromXAxis, fromYAxis, fromZAxis` | 移す前のフレーム（原点と3軸） |
| `toOrigin, toXAxis, toYAxis, toZAxis` | 移した先のフレーム |

この例はfromを標準フレーム、toをWCSで表した配置先にする。  
UCSの設定そのものを変更するAPIではない。

```csharp
Matrix3d byAlign = Matrix3d.AlignCoordinateSystem(
    Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
    center, xAxis, yAxis, zAxis);
```

![標準フレームの非対称部材をC=(10,0,3)、X軸=(0,1,0)、Y軸=(-1,0,0)、Z軸=(0,0,1)へ配置する図。原点・3軸上の点の行き先と頂点2=(8,3,3)を示す](images/18-alignment.svg)

#### 基底を作り直さず、配置の意味を照合する

ここでは、第6.3節のTRと同じ配置先を使う。  
Cは `(10,0,3)`、X軸は `(0,1,0)`、Y軸は `(-1,0,0)`、Z軸は `(0,0,1)` とする。  
ローカルの各成分を、その軸へ進む量として足せばよい。

$$
P=C+xX+yY+zZ.
$$

| 方法 | この例での意味 |
| --- | --- |
| Rotation＋Displacement | 原点で+π/2回してからCへ移動する |
| AlignCoordinateSystem | 標準の原点・軸を、配置先の原点・軸へ対応させる |
| Matrix3dの成分指定 | X・Y・Zを列、Cを移動列に置き、配列には行ごとに詰める |
| 各軸を何倍かして足す | C+xX+yY+zZを成分ごとに計算する |

頂点2は `C+3X+2Y=(8,3,3)` になる。  
原点はC、3軸上の点は順に `(10,1,3),(9,0,3),(10,0,4)` へ移る。  
原点を試すと移動列、各軸上の点との差を試すと基底列を確認できる。

一般のfromとtoについては、両方のフレームを共通のWCS成分で表した行列F・Gを考える。  
Fの原点と軸をGの原点と軸へ**動かす**行列は $GF^{-1}$ になる。  
一方、from側の座標で表した同じ位置を、to側の座標へ**表し直す**行列は $G^{-1}F$ である。  
入力の表し方と目的が異なるので、この2つを取り違えない。  
ここでは右手系・単位長・直交する3軸に限定し、0の軸・重なる軸・不正なフレームは使わない。  
長さや角度の違う軸に対するAPIの適用範囲を、確認なしに一般化しない。

実演は [AlignmentCommands.cs](../../autocad-net/samples/autocad-transform/AlignmentCommands.cs) の `GEOAT_ALIGNMENT`。  
基底の詳しい復習は [数学基礎編の行列](../01-matrix/explanation.md) と本教材の第3〜4章に戻ろう。

## 7. その他の図形変換

任意中心の回転は、回転中心をコードから読み取るための重要な比較である。  
拡大縮小・鏡映・射影は、必要な題材を選んで実演できる補足として読む。  
それぞれ、同じ操作を行うコードと、似ているが別の操作になるコードを区別しよう。

### 7.1 任意中心の回転：中心を引いて、回して、戻す

#### API紹介

第6.2節の `Point2d.RotateBy`・`Point3d.RotateBy`・`Matrix3d.Rotation` は、中心を引数で指定できる。  
ここでは、中心CをWCSの `(1,1,0)`、軸を+Z、角度を+π/2とする。

```csharp
Point3d center = new(1, 1, 0);
Point3d byPoint = point.RotateBy(Math.PI / 2, Vector3d.ZAxis, center);
Matrix3d byCenter = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, center);
```

入力の点・中心・軸の成分は、すべてWCSでそろえる。  
図の中心Cは動かず、点からCへの相対位置だけが回る。

![中心C=(1,1,0)の周りに(3,2,0)を+π/2回す図。中心を引いた(2,1,0)を(-1,2,0)へ回し、Cを足すと(0,3,0)になる](images/19-center-rotation.svg)

#### 中心指定と、3つの操作の合成

点から中心を引き、原点周りに回してから中心を足す。  
行列で書けば、右端の「中心を引く」操作が最初である。

$$
\begin{aligned}
P'&=C+R(P-C),\\
M_C&=T(C)R T(-C).
\end{aligned}
$$

```csharp
Matrix3d toOrigin = Matrix3d.Displacement(new Vector3d(-center.X, -center.Y, -center.Z));
Matrix3d fromOrigin = Matrix3d.Displacement(new Vector3d(center.X, center.Y, center.Z));
Matrix3d originRotation = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);
Matrix3d sandwich = fromOrigin * originRotation * toOrigin;
```

| 手順 | 頂点2の座標 |
| --- | --- |
| 中心を引く | (3,2,0)−(1,1,0)=(2,1,0) |
| 原点で+π/2回す | (−1,2,0) |
| 中心を足す | (0,3,0) |

C自身なら `P-C=0` なので、変換後もCになる。  
一方、同じ点を原点周りに回すだけなら `(-2,3,0)` であり、中心指定の結果とは異なる。  
Z軸以外でも、回転行列Rと中心をそろえれば同じ合成の考え方を使える。

実演は [RotationCommands.cs](../../autocad-net/samples/autocad-transform/RotationCommands.cs) の `GEOAT_CENTER_ROTATION`。  
中心からのsin/cos、Point2d、Point3d、中心指定の行列、3操作の合成を比べる。

### 7.2 補足：拡大縮小は基準点からの距離を変える (発展)

#### API紹介

`Point3d.ScaleBy(double scaleFactor, Point3d centerPoint)` は、基準点からの一様な拡大縮小を行った `Point3d` を返す。  
`Matrix3d.Scaling(double scaleAll, Point3d center)` は、一様スケールの `Matrix3d` を返す。  
どちらも、点と基準点を同じ座標系で表す。

```csharp
Point3d center = new(1, 1, 0);
Point3d byPoint = point.ScaleBy(2, center);
Matrix3d scaling = Matrix3d.Scaling(2, center);
Point3d byMatrix = point.TransformBy(scaling);
```

中心Cは回転の例と同じ `(1,1,0)`、倍率は2とする。  
原点からではなく、Cからの変位を2倍している点に注目しよう。

![C=(1,1,0)を固定して非対称輪郭を一様2倍にする図。頂点2のCからの変位(2,1,0)が(4,2,0)になり、WCS位置は(5,3,0)](images/20-scaling.svg)

$$
P'=C+s(P-C).
$$

頂点2では `(1,1,0)+2(2,1,0)=(5,3,0)` となる。  
成分ごとの計算、ScaleBy、Scaling＋TransformByは、この式を別の形で書いたものである。

数学的にはsが0でなければ逆倍率1/sで戻せる。  
s=0なら全点がCに重なり、元の点は復元できない。  
負のsでは方向も反転するが、今回のAPI実演は正の2倍に限定する。  
0や負の倍率を、すべてのEntityで利用できると断定しない。

`Matrix3d.Scaling` は一様スケールのAPIである。  
X・Y・Zを別々に拡大する非一様スケールを、一つの倍率だけで指定することはできない。  
非一様スケールの行列を自作できても、CircleなどのEntityがその変換を受け付けるとは限らない。

実演は [ScalingCommands.cs](../../autocad-net/samples/autocad-transform/ScalingCommands.cs) の `GEOAT_SCALING`。  
3方式の結果と、逆行列で `(3,2,0)` へ戻ることを確認する。

### 7.3 補足：鏡映は半回転と同じか？ (発展)

#### API紹介

`Point3d.Mirror(Plane plane)` は、平面に対して鏡映した `Point3d` を返す。  
`Matrix3d.Mirroring(Plane plane)` は、同じ平面の鏡映を表す `Matrix3d` を返す。  
ここでは直線を指定する別のオーバーロードではなく、Planeを指定するものを使う。

`Plane(Point3d origin, Vector3d normal)` で鏡の平面を作る。  
originは平面上の点、normalは0ではない法線である。  
どちらもWCS成分で指定し、Planeは `using` で破棄する。

```csharp
using Plane plane = new(Point3d.Origin, Vector3d.XAxis);
Point3d byPoint = point.Mirror(plane);
Matrix3d mirror = Matrix3d.Mirroring(plane);
Point3d byMatrix = point.TransformBy(mirror);
```

これはWCSのYZ平面、つまり `x=0` に対する鏡映になる。  
図の輪郭に加えて、入力輪郭のXY平面から外れたZ軸上の点も見てみよう。

![YZ平面の鏡映ではXだけが反転し、Y軸周りの半回転ではXとZが反転する図。XY平面の輪郭は同じ位置に見えても、Z軸上の点で違いが分かる](images/21-mirroring.svg)

$$
\begin{aligned}
P_{\mathrm{mirror}}&=(-x,y,z),\\
P_{\mathrm{halfturn}}&=(-x,y,-z).
\end{aligned}
$$

| 入力 | YZ鏡映 | Y軸の+π回転 |
| --- | --- | --- |
| 頂点2：(3,2,0) | (−3,2,0) | (−3,2,0) |
| Z軸上の点：(0,0,1) | (0,0,1) | (0,0,−1) |

Z=0の点だけでは、2つの変換の違いを見分けられない。  
輪郭が非対称でも、入力が一つの平面に限られることで違いが隠れるのである。

鏡映は空間の左右の向きを反転させる。  
この例の行列は対角が−1・1・1で、行列式は−1になる。  
回転は右手系の向きを保ち、行列式は+1である。  
鏡映を2回行えば元へ戻るが、回転と同じ操作になったわけではない。

実演は [MirroringCommands.cs](../../autocad-net/samples/autocad-transform/MirroringCommands.cs) の `GEOAT_MIRRORING`。  
符号反転・Mirror・Mirroringは同じ操作で、4番目の半回転だけは診断用の別操作である。

### 7.4 補足：射影は点を平面へ動かす (発展)

#### API紹介

| API | 引数と意味 | 戻り値 |
| --- | --- | --- |
| `Point3d.OrthoProject(Plane plane)` | 射影先の平面。法線方向に最短で移す | 平面上の `Point3d` |
| `Point3d.Project(Plane plane, Vector3d vector)` | 平面と、射影方向 | 方向に沿った平行射影の `Point3d` |

この例では、第5章のBの平面 `N・X=5` に、第5章の平面外の点Qを射影する。  
平面・入力点・方向は、すべてWCSで表す。  
Projectへ法線Nを渡す場合は正射影と同じ位置になるが、別の方向を渡せば通常は別の位置になる。

```csharp
Vector3d normal = new(0, 3.0 / 5, 4.0 / 5);
using Plane plane = new(new Point3d(0, 3, 4), normal);
Point3d perpendicular = point.OrthoProject(plane);
Point3d alongNormal = point.Project(plane, normal);
Point3d oblique = point.Project(plane, new Vector3d(1, 0, -1));
```

図では、法線方向と斜め方向の到着点を比べる。  
OCSへ表し直すだけならQは動かない、という第5章との違いも確かめよう。

![Bの平面外のQ=(-3,13/5,34/5)から法線方向へ正射影するとP=(-3,7/5,26/5)、D=(1,0,-1)の平行射影ではJ=(-1/2,13/5,43/10)になる図](images/22-projection.svg)

#### 内積から、平面へ動かす量を求める

Cは `(0,3,4)`、Nは長さ1の `(0,3/5,4/5)` とする。  
内積 `N・(Q-C)` は、平面からN方向へ離れた符号付きの距離になる。  
その距離の分だけNを引けば、正射影の点を得られる。

$$
Q_{\perp}=Q-\bigl(N\cdot(Q-C)\bigr)N.
$$

固定点Qでは内積が2なので、`Q−2N=P=(-3,7/5,26/5)` になる。  
成分での内積、OrthoProject、ProjectへのN指定は、この同じ操作を表す。  
Nが長さ1でない一般の法線nなら、引く項を `n・n` で割る必要がある。

$$
Q_{\perp}=Q-\frac{n\cdot(Q-C)}{n\cdot n}n,\qquad n\ne0.
$$

斜めの方向Dでは、直線 `Q+tD` と平面が交わるtを求める。

$$
\begin{aligned}
N\cdot(Q+tD-C)&=0,\\
t&=\frac{N\cdot(C-Q)}{N\cdot D},\\
Q_D&=Q+tD.
\end{aligned}
$$

`D=(1,0,-1)` なら分母は−4/5、分子は−2で、`t=5/2` になる。  
したがって `Q_D=(-1/2,13/5,43/10)` である。  
この点も `3y+4z=25` を満たすが、正射影のPとは違う。  
Projectの方向は平行な直線を定めるものであり、tが正の半直線だけを調べるレイ判定とは区別する。

#### 特殊入力・逆変換・座標変換との違い

- Dが0では射影方向を定められない。
- `N・D=0` でQが平面外なら、直線と平面は交わらない。
- 同じ条件でもQが平面上なら直線全体が平面内にあり、交点が一つには決まらない。
- `N・D` が0に近いと、計算誤差が大きくなりやすい。実演では平行・ほぼ平行な方向をAPIへ渡さない。
- NaN・無限大や0の法線を、通常の幾何入力として扱わない。

射影は、異なる点を同じ位置へ重ねることがある。  
正射影では `P+tN` がすべてPへ移るため、一般の3D点へ戻す逆変換は存在しない。  
同じ平面上の点は正射影しても動かず、2回射影しても結果は同じである。

`WorldToPlane` は、3成分を保持すれば可逆な座標表現の変換である。  
`OrthoProject` は、平面外の点そのものを平面へ動かす操作である。  
型が同じPoint3dでも、処理の目的は同じとは限らない！

実演は [ProjectionCommands.cs](../../autocad-net/samples/autocad-transform/ProjectionCommands.cs) の `GEOAT_PROJECTION`。  
Bの輪郭を2Nだけ進めた4点を固定入力にする。  
最初の3方式は正射影、後ろ2方式は斜方向の射影で、各組の中で結果を照合する。  
平行な `D=(1,0,0)` は内積が0と表示するだけで、Projectへは渡さない。  
APIが特殊入力にどの例外・結果を返すかは実測していない。

### 7.5 補足：点の計算結果と、Entityの変更を分ける

`Point3d.TransformBy(Matrix3d leftSide)` は、変換後の点を返す。  
`Entity.TransformBy(Matrix3d transform)` の戻り値は `void` で、Entity自身を変更する。  
Point3dを計算しただけでは、図面DBの図形は動かない。

```csharp
using Line temporary = new(Point3d.Origin, new Point3d(3, 2, 0));
temporary.TransformBy(translation); // 戻り値を代入するAPIではない
```

`GEOAT_TRANSLATION` では、この一時Lineの始点・終点も読み取る。  
一時LineはDBへ登録せず破棄し、比較結果の描画には別の新規Lineを使う。

[Entity.TransformByの公式仕様](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_TransformBy_Matrix3d.html) によると、受け付ける変換の制約はEntityの実装で決まる。  
既存のCircle・Arcなどには、一様な尺度と直交性を保つ変換の制約がある。  
点には計算できる非一様スケールや、次元を潰す射影行列を、すべてのEntityへ適用できるとは考えない。  
今回の射影の実演は点を先に計算し、その結果からLineを作る。

### 7.6 補足：丸め誤差と比較条件

第5章の絶対誤差・相対誤差の考え方を、この章にも使う。  
小さな固定座標では、座標の照合は絶対1e−10作図単位＋相対1e−12を一例とする。  
無次元の行列要素は、絶対・相対とも1e−12など、座標の距離とは別の基準で見る。  
sin(π)やcos(π/2)も、浮動小数点では厳密な0にならないことがある。

移動量・回転角・中心・軸・変換順序・入力座標系がそろっている組だけを、同じ結果になる比較として扱う。  
意図的に異なる操作を並べた組の差まで、許容誤差で消そうとしない。  
実演のG17表示と行列16要素で、入力・中間値・結果を照合する。

ProjectionCommandsの分母判定1e−12は、固定したN・Dに対する実演のガードである。  
任意の長さのDへ使う角度判定の規約ではない。  
一般化するなら方向の長さで尺度をそろえ、入力精度と求める距離の誤差を考慮する。  
極端に大きい座標、桁落ち、可逆でも数値的に不安定な行列の逆計算を、この固定例で保証しない。

## 第6〜7章の公式出典と検証範囲

確認日：2026-11-17。AutoCAD 2026版のManaged .NET APIの署名・適用条件と、管理元コードを静的に照合した。  
以下はAPIの実機出力を確認した記録ではない。

公式Managed資料のPoint3d.Mirrorには直線、Projectには2D回転の説明が混在する。  
この教材では、Planeを受け取る署名と、同じ2026版のネイティブAPI資料の平面鏡映・平行射影の説明を照合した。  
その記述から、Managed APIの実機での結果や例外の詳細まで確認済みとはしない。

| 2026版公式資料 | 照合対象 |
| --- | --- |
| [移動のガイド（日本語）](https://help.autodesk.com/cloudhelp/2026/JPN/OARX-DevGuide-Managed/files/GUID-9E9D7A74-4FE7-4E57-B9CB-BA4712B364ED.htm) ／ [尺度変更のガイド（日本語）](https://help.autodesk.com/cloudhelp/2026/JPN/OARX-DevGuide-Managed/files/GUID-50D25B16-2FBF-4E81-B934-A26F699F31BC.htm) | 変位ベクトル、基点と一様な尺度係数 |
| [鏡像化のガイド（日本語）](https://help.autodesk.com/cloudhelp/2026/JPN/OARX-DevGuide-Managed/files/GUID-B0565F5A-03CC-426B-9A1D-4280A6187BDA.htm) | MirroringとTransformByの使い方。掲載例はLine3dを使用し、本文の平面鏡映は下のPlane指定の署名で確認 |
| [Matrix3d.Displacement](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Displacement_Vector3d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Matrix3d.Rotation](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Rotation_double_Vector3d_Point3d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Matrix3d.PreMultiplyBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_PreMultiplyBy_Matrix3d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Matrix3d.PostMultiplyBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_PostMultiplyBy_Matrix3d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Matrix3d.AlignCoordinateSystem](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_AlignCoordinateSystem_Point3d_Vector3d_Vector3d_Vector3d_Point3d_Vector3d_Vector3d_Vector3d.html) | 8引数のfrom/toのフレーム |
| [Point2d.RotateBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point2d_RotateBy_double_Point2d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Point3d.RotateBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_RotateBy_double_Vector3d_Point3d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Point3d.Add](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_Add_Vector3d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Point3d.ScaleBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_ScaleBy_double_Point3d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Matrix3d.Scaling](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Scaling_double_Point3d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Point3d.Mirror](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_Mirror_Plane.html) | Planeを渡すオーバーロード |
| [Matrix3d.Mirroring](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d_Mirroring_Plane.html) | 使用する引数・戻り値の型と操作の意味 |
| [Point3d.OrthoProject](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_OrthoProject_Plane.html) | 使用する引数・戻り値の型と操作の意味 |
| [Point3d.Project](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Point3d_Project_Plane_Vector3d.html) | Plane・Vector3d入力とPoint3d出力 |
| [Plane.Plane](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Plane_Plane_Point3d_Vector3d.html) | 使用する引数・戻り値の型と操作の意味 |
| [Entity.TransformBy](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Entity_TransformBy_Matrix3d.html) | void・変更操作・Entity固有の変換制約 |
| [Line.StartPoint](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Line_StartPoint.html) ／ [EndPoint](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Line_EndPoint.html) | 一時LineのWCS位置をPoint3dで読む |
| [行列の積](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Geometry_Matrix3d___Matrix3d_Matrix3d_operator.html) ／ [回転のガイド（日本語）](https://help.autodesk.com/cloudhelp/2026/JPN/OARX-DevGuide-Managed/files/GUID-DAF76951-DD0C-413F-86A8-471E2B94C1C0.htm) | 積の左右、ラジアン・軸・基準点 |
| [ネイティブの鏡映](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-RefGuide/files/OARX-RefGuide-AcGePoint3d__mirror_AcGePlane_.html) ／ [平行射影](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-RefGuide/files/OARX-RefGuide-AcGePoint3d__project_AcGePlane__AcGeVector3d__const.html) ／ [正射影を含むメソッド一覧](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-RefGuide/files/OARX-RefGuide-__MEMBERTYPE_Methods_AcGePoint3d.html) | Managed資料の混在した説明を、2026版の幾何操作と照合 |

Windows版AutoCAD 2026 Update 1.2以降／.NET 10を対象とする。  
職場の参照DLLによるビルド・NETLOAD・AutoCAD実機実行・図面表示は未検証である。  
[実演の案内](examples/README.md) にはコマンド・入力・期待結果・変数・副作用をまとめる。

## 8. まとめ：異なるコードの処理意図を読み解く

APIの名前が違っても、次の5点をそろえて読めば、何をするコードかを追いやすくなる。

| 確認すること | 読むときの問い |
| --- | --- |
| 対象 | 点・方向・図形のどれか。戻り値を使うのか、Entity自身が変わるのか |
| 座標系 | 数値はWCS・OCS・ローカルのどれか。同じ物理位置を表し直すだけか |
| 基準点・軸 | 原点かCか。Normal・Elevation・回転軸をどの座標系で渡しているか |
| 作用する順序 | 逐次処理と行列の積が対応しているか。左／右から掛ける意味は何か |
| 等価になる条件 | 原点共通・正規直交・右手系・同じ中心など、必要な条件がそろっているか |

### 異なるコードが、同じ操作を表す例

| 題材 | 数学的に対応する書き方 |
| --- | --- |
| OCS→WCS | GetPoint3dAt、PlaneToWorld、自作基底行列、各軸を何倍かして足す計算 |
| WCS→OCS | WorldToPlane、Inverse、単位軸との内積、条件を満たす転置 |
| 回転→移動 | 逐次TransformBy、TR、R.PreMultiplyBy(T)、T.PostMultiplyBy(R) |
| ローカルの移動 | M T_L と、変位をWCSへ直した T(Rd_L) M |
| 中心Cの回転 | 中心指定のRotateBy／Rotationと、T(C) R T(−C) |

コードを短く置き換えられるかを考える前に、入力・条件・出力を一組として説明してみよう。  
手計算で期待する点を一つ決めると、行列のどこを調べるかも見えやすくなる。

### 似たコードでも、別の操作になる例

- OCSのXYにElevationを付ける処理と、得たWCS点へもう一度Cを足す処理。
- 同じ行列名でも、TRとRT、Pre/Postの呼出し元が違う場合。
- WCS Xへ動かす処理と、ローカルXへ動かす処理。
- 原点周りの回転と、C周りの回転。
- 高さを保持するWorldToPlaneと、平面へ点を移すOrthoProject。
- YZ平面の鏡映と、Y軸周りの半回転。

### 見た目の一致だけでは足りない

次の正方形は、+π/2回しても外側の輪郭が同じになる。  
それでも、同じ頂点番号の位置は変わっている。

![原点中心の正方形が+π/2回転しても外形は重なる一方、頂点(1,1,0)は(-1,1,0)へ移る図。同じ輪郭と同じ変換を区別する](images/23-symmetry.svg)

頂点 `(1,1,0)` は `(-1,1,0)` へ移るので、何もしない変換と回転は異なる。  
`GEOAT_COMPOSITION` では、この頂点の行き先も数値で表示する。

非対称な図形を使っても、XY平面上だけでは鏡映と半回転の違いを見逃すことがある。  
また、第4章の固定例Bの行列は転置・逆行列がたまたま同じなので、向きを取り違えても違いが隠れる。  
斜め法線の例、原点・3軸上の点、平面外の点、方向、行列全要素を組み合わせて調べよう。

前半で学んだ「同じ頂点でも座標の表し方は違う」と、後半の「同じ数値でも操作の基準が違う」を区別する。  
どのAPIでも、対象・座標系・基準点・軸・順序・条件を追えば、見慣れないコードを数学へ結び付けられる。

[この教材のトップへ戻る](README.md)

# 図形変換のSVG図解

全8章と練習問題の固定入力・途中計算に対応する静的なSVG図解を置く。

| 図解 | 本文との対応 |
| --- | --- |
| [同じPolyline頂点の座標](01-polyline-coordinates.svg) | 第1章。OCSの(3,2)と固定例A・BのWCS位置 |
| [OCS原点とElevation](02-origin-elevation.svg) | 第2章。共通原点O、C=5N、平面内の変位 |
| [法線と平面内の自由度](03-normal-freedom.svg) | 第2章。同じ法線でも基底を回せる |
| [任意軸の分岐と境界](04-arbitrary-axis-branches.svg) | 第3章。両成分の1/64未満と等号の区別 |
| [基底行列と配列の順序](05-basis-matrix-layout.svg) | 第4章。基底は列、Matrix3dの配列は行ごと |
| [往復変換と平面への射影](06-roundtrip-and-projection.svg) | 第5章。平面外の点の高さを保持する往復と、Zを捨てる操作の違い |
| [WCS・UCS・OCSで同じ点を測る](07-coordinate-system-roles.svg) | 第2章。図面・作業・図形の基準と、原点・軸・座標の違い |
| [NormalとElevationを分けて変える](08-normal-and-height.svg) | 第2章。同じOCS頂点で向きだけ・高さだけ・両方を変えた4例 |
| [外積からX・Y軸を作る](09-arbitrary-axis-steps.svg) | 第3章。補助軸、外積の順序、長さ1への正規化 |
| [境界前後での軸の切替え](10-boundary-axis-change.svg) | 第3章補足。近い法線でもX・Yが大きく変わる例 |
| [順変換の4方式の対応](11-four-forward-methods.svg) | 第4章。頂点取得と、同じOCS点から始める3つの計算経路 |
| [内積で3つの成分を取り出す](12-dot-product-components.svg) | 第5章。各単位軸方向に測った3・2・5と、順変換との対応 |
| [平行移動](13-translation.svg) | 第6.1節。点と方向、成分加算と行列 |
| [原点回転](14-origin-rotation.svg) | 第6.2節。正角・軸・中心 |
| [合成順序](15-composition-order.svg) | 第6.3・6.5節。TRとRT、逆順で戻す経路 |
| [ローカルとWCSの移動](16-local-movement.svg) | 第6.4節。Bの平面でどのXへ動かすか |
| [ローカルとWCSの回転](17-local-rotation.svg) | 第6.4節。中心と軸をWCSへ直す |
| [部材の配置](18-alignment.svg) | 第6.6節。原点・3軸の行き先と4方式 |
| [任意中心の回転](19-center-rotation.svg) | 第7.1節。中心を引く・回す・戻す |
| [一様スケール](20-scaling.svg) | 第7.2節。Cを固定して2倍 |
| [鏡映と半回転](21-mirroring.svg) | 第7.3節。平面外の点で違いを見る |
| [正射影と斜方向の射影](22-projection.svg) | 第7.4節。QからPまたはJへ動く |
| [対称な外形と頂点](23-symmetry.svg) | 第8章。輪郭が重なっても頂点は動く |
| [練習問題の共通入力](24-homework-inputs.svg) | 問1〜3。OCSに保存するXYと、N・高さhで定める図形平面。解答の座標は載せない |

図には座標系・軸・視点・変換方向を示す。
Markdownから参照するときは、図の内容が分かる代替テキストを付ける。
斜投影の図は見かけの長さ・角度を表すもので、座標値と右手系の規約で読む。
OCSの軸は実演と同じくXを赤、Yを緑、Nを青で区別し、軸名も付ける。  
高さの変位はマゼンタ、WCSの補助軸は灰色で示す。  
4方式の対応図は処理の模式図とし、3Dの図はWCSの同じ投影規則で描く。  
UCSの図だけは説明用に原点と軸を選んだ例であり、実演の固定入力やUCS設定を変更するものではない。

[詳細解説](../explanation.md) ／ [この教材のトップへ戻る](../README.md)

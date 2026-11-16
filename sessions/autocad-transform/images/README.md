# 図形変換のSVG図解

第1〜5章の固定入力・途中計算に対応する静的なSVG図解を置く。

| 図解 | 本文との対応 |
| --- | --- |
| [同じPolyline頂点の座標](01-polyline-coordinates.svg) | 第1章。OCSの(3,2)と固定例A・BのWCS位置 |
| [OCS原点とElevation](02-origin-elevation.svg) | 第2章。共通原点O、C=5N、平面内の変位 |
| [法線と平面内の自由度](03-normal-freedom.svg) | 第2章。同じ法線でも基底を回せる |
| [任意軸の分岐と境界](04-arbitrary-axis-branches.svg) | 第3章。両成分の1/64未満と等号の区別 |
| [基底行列と配列の順序](05-basis-matrix-layout.svg) | 第4章。基底は列、Matrix3dの配列は行ごと |
| [往復変換と平面への射影](06-roundtrip-and-projection.svg) | 第5章。平面外の点の高さを保持する往復と、Zを捨てる操作の違い |

図には座標系・軸・視点・変換方向を示す。
Markdownから参照するときは、図の内容が分かる代替テキストを付ける。
斜投影の図は見かけの長さ・角度を表すもので、座標値と右手系の規約で読む。

[詳細解説](../explanation.md) ／ [この教材のトップへ戻る](../README.md)

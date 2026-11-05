# 第5回：なぜ図形の内外や干渉を判定できるのか？

テーマはフィードバックによって変更する可能性がある。

## 到達目標

- 点の内部／境界／外部と、領域どうしの離隔／接触／正の面積・体積を伴う重なりを区別できる。
- 三角形の包含を2D外積の符号から導き、辺の延長線と有限な辺を区別できる。
- 単純な凹多角形のレイ法を説明し、境界の先行判定・半開区間・水平辺の扱いを理解する。
- AABBの各軸の区間から候補を絞り、実体の干渉はまだ確定しない理由を説明できる。
- 2D凸多角形のSATを内積の射影区間で説明し、3D凸箱では面の半空間条件を使えることと、その適用範囲を理解する。
- 退化・閉じていない形・自己交差・数値誤差を、通常の外部や離隔と分けられる。

## 前提知識

[第1回](../01-matrix/explanation.md) の座標系、[第2回](../02-vector/explanation.md) の2D外積・内積・法線、[第3回](../03-intersection/explanation.md) の線分交差と範囲条件、[第4回](../04-modeling/explanation.md) の閉じたメッシュと外向き法線を使う。2DはX右・Y上、3Dは右手系を引き継ぎ、座標の単位をそろえる。

## 教材への案内

| 教材 |
| --- |
| [詳細解説（Markdown）](explanation.md) |
| [詳細解説（PDF）](output/explanation.pdf) |
| [練習問題（PDF）](output/problems.pdf) |
| [解答・解説（PDF）](output/solutions.pdf) |
| [C#実装例の説明（PDF）](output/examples.pdf) |
| [SVG図解の目次](images/README.md) |
| [.NET・C#実装例](examples/README.md) |
| [AutoCAD専用：包囲箱・交点・ソリッド干渉](../../autocad-net/samples/05-geometry-check/README.md) |
| [C#自動チェック（CI）](../../.github/workflows/check-geometry-examples.yml) |
| [復習用の宿題](homework/README.md) |

[勉強会トップへ戻る](../../README.md)

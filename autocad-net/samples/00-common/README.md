# 00：コマンドとモデル空間の導入

**公式資料と静的照合した参考コード／ビルド・NETLOAD・実機動作は未検証。** [対象環境と設定](../../docs/setup.md) はAutoCAD 2026 Update 1.2以降／.NET 10／Visual Studio 2026。

[CommonCommands.cs](CommonCommands.cs) の `Hello` → `AddLine` の順に読む。最初は `Document` と `Editor` だけで案内を表示し、次に `Database`・`Transaction`・モデル空間への図形追加を追う。

| コマンド | 入力 | 期待動作と副作用 |
| --- | --- | --- |
| `GEO00_HELLO` | 引数なし、現在の図面 | Editorへ案内表示。図形変更なし |
| `GEO00_LINE` | WCSの始点(0,0,0)・終点(4,0,0)を固定 | モデル空間へLineを1本追加。長さ4作図単位。保存せず、繰り返すと重複追加 |

図面がない場合はどちらも戻る。期待動作を利用者が確認する場合は専用の空図面を使い、NETLOAD後にコマンド名を入力する。今回は実施していない。

## 数学教材との対応

- [第1回の学習の流れと表記](../../../sessions/01-matrix/explanation.md#学習の流れと表記)：座標系と点・変位の区別。Point3dの値は位置であり、2点の差(4,0,0)が変位。
- [第5回③の三角形ABC](../../../sessions/05-geometry-check/explanation.md#-ccw三角形辺の左側という条件を重ねる)：固定例AのA=(0,0)、B=(4,0)をZ=0へ置いた辺ABと同じ2点を使う。今回は線分の作成だけで、三角形包含や干渉判定は実装しない。

Point3dとLineはWCS座標で作り、UCS行列は使わない。mm等の物理単位、画面上の長さ、2D／3Dの表示方向を同一視しない。各章の数学・APIの説明は後続Issueで追加する。

## 静的に読む観点

`CommandMethod` 名 → 現在のDocument → Database／Editor → Transaction → BlockTable → ModelSpace → Line → AppendEntity → AddNewlyCreatedDBObject → Commit → usingの後始末を追う。[公式リファレンス](../../docs/references.md) で名前空間・署名・追加順序・所有権を照合できる。

TODO：ビルド・ロード・図形追加は未確認であり、動作済みとは扱わない。これらの実施は本Issueの完了条件に含めない。

[参考コードトップへ戻る](../../README.md) ／ [セットアップ](../../docs/setup.md)

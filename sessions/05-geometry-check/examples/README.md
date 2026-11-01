# 第5回：内外・AABB・SATのC#実装例

**準備中／レビュー待ちの下書き。** [詳細解説](../explanation.md) の固定例A〜Eと [宿題](../homework/README.md) を、独立した.NET 10コンソールで追う。7題材・全254チェック。追加NuGet、他回のプロジェクト参照、GUI、AutoCAD、DWG操作は不要。

## 必要環境とVisual Studioでの操作

対象は `net10.0` 。CLIでは.NET SDK 10、Visual Studioでは.NET 10に対応するVisual Studio 2026を使う。入力はソース内の固定値で、引数・標準入力はなく、出力はコンソールのみ。

1. [GeometryCheckExamples.sln](GeometryCheckExamples.sln) を開く。従来形式の `.sln` であり、プロジェクトは1件。
2. 構成をDebugにし、Ctrl+Shift+Bでビルドする。Ctrl+F5で全出力を読み、F5でデバッグ実行する。
3. [Program.cs](Program.cs) の `TriangleContainment` 内でPの外積を調べる行へF9を置く。F11で `EdgeValues` → `Cross2` に入り、辺の方向と点への変位、4・5・3の符号を追う。 `InTriangle` では `area2` 、 `outside` 、 `boundary` を見る。
4. `ConcaveRay` のRの検証から `ByRay` に入り、先に辺上判定を完了することを確認する。計数ループのa・b・t・xHitをウォッチし、上端と水平辺を除き、右向きだけを数える条件を追う。極小の同じ交点が2辺分になる例も確認する。
5. `SeparatingAxes` から `ConvexSat.Compare` に入り、両輪郭の辺からできる `axis` 、 `TryProject` のq・min・max、 `delta` を追う。軸(1,1)の説明用射影と、辺から得る符号・倍率の異なる法線が同じ結果になる理由を考える。
6. `BoxHalfspaces` から `TryHalfspaces` に入り、各面のanchor・normal・wと内積を確認する。法線は外向きで、−X・+X・−Y・+Y・−Z・+Zの順。最後に `Locate` が正の値を0より先に検査する理由を追う。

F5で終了前に止めるにはMain末尾の成功メッセージへF9を置く。GUIによるF5／Ctrl+F5／F9／F11の操作確認はTODOで、CLI・Visual Studio付属MSBuildによる検証とは区別する。

## CLIのDebug／Releaseビルドと実行

リポジトリのルートから実行する。

```sh
dotnet build sessions/05-geometry-check/examples/GeometryCheckExamples.sln -c Debug
dotnet run --project sessions/05-geometry-check/examples/GeometryCheckExamples.csproj -c Debug --no-build
dotnet build sessions/05-geometry-check/examples/GeometryCheckExamples.sln -c Release
dotnet run --project sessions/05-geometry-check/examples/GeometryCheckExamples.csproj -c Release --no-build
```

`examples/` 内からは `dotnet run` でも実行できる。全件成功時は終了コード0、不一致や想定外の例外では `FAIL` を標準エラーへ出力して終了コード1。数値の不一致にはラベル・成分番号・実値・期待値・許容誤差を表示する。

## ファイルと読む順序

| ファイル | 内容 |
| --- | --- |
| [GeometryCheckExamples.sln](GeometryCheckExamples.sln) | Visual Studioで開く入口。Debug／Release |
| [GeometryCheckExamples.csproj](GeometryCheckExamples.csproj) | net10.0のコンソール。外部パッケージ・ビルド参照なし |
| [Program.cs](Program.cs) | 固定入力・独立な期待値・7題材と自動チェック |
| [GeometryMath.cs](GeometryMath.cs) | doubleの2D／3D成分、分類型、外積・内積、有限な線分の範囲 |
| [PolygonChecks.cs](PolygonChecks.cs) | 単純閉輪郭の入力検査、三角形の左右条件、境界先行のレイ法 |
| [ConvexSat.cs](ConvexSat.cs) | 全頂点の射影区間、両輪郭の辺法線による2D凸SAT |
| [AabbChecks.cs](AabbChecks.cs) | 2D／3D箱の区間幅と共有面積・体積、用途別Boolean |
| [ConvexBoxChecks.cs](ConvexBoxChecks.cs) | 軸平行凸箱の6面の外向き法線と点包含 |

`Program.Main` → 題材の入力 → 各計算メソッド → 戻り値と期待値の順で読む。日本語XMLコメントに入力・規約・境界条件・結果の意味を記載している。[第2回の内積・外積](../../02-vector/explanation.md)、[第3回の有限な線分](../../03-intersection/explanation.md)、[第4回の外向き面法線](../../04-modeling/explanation.md) が数学の前提で、C#プロジェクト自体は独立している。

## 題材・固定入力・期待結果

| 題材 | 本文／宿題との対応 | 入力と主な期待結果 | チェック数 |
| --- | --- | --- | --- |
| 1. TriangleContainment | ③／基本1 | A=(0,0), B=(4,0), C=(0,3)。2倍面積12・面積6。P=(1,1)の外積4,5,3でInside、E=(2,0)はBoundary、O=(3,2)は8,−5,9でOutside。逆順・頂点・延長線・退化 | 25 |
| 2. ConcaveRay | ④／基本2 | L字6頂点。I=(0.5,2)は1回でInside、N=(2,2)は0回でOutside。D=(1,2)・V=(1,1)はBoundary、H=(3,0.5)はInside。R、水平辺、全頂点、極大・極小 | 35 |
| 3. Boxes2D | ⑤／基本3-1〜3 | A=[0,2]²。B=[2,4]×[1,3]はTouching、C=[2.1,4]×[1,3]はSeparated、D=[1,3]²は共有面積1のOverlapping。包含・一致・点接触 | 30 |
| 4. SeparatingAxes | ⑥／発展 | T1=(0,0),(3,0),(0,3)、T2=(2,2),(5,2),(2,5)。共通AABB面積1でも射影[0,3]／[4,7]でSeparated。T3=(1,1),(4,1),(1,4)はOverlapping、T4=(3,0),(5,0),(3,2)はTouching | 37 |
| 5. BoxHalfspaces | ⑦／基本3の3D前提 | K=[0,2]³。I=(1,1,1)は全6面−1でInside、O=(3,1,1)は+X面が1でOutside、E=(2,1,1)は+X面が0でBoundary。延長平面・辺・全頂点 | 20 |
| 6. Boxes3D | ⑦／基本3-4 | K2=[1,3]³とは体積1のOverlapping、K3=[2,3]×[1,2]²とは面積1の面を共有し体積0のTouching。辺・点接触、離隔、一致 | 31 |
| 7. InvalidInputsAndPrecision | ②・⑧／各問の注意 | 無効・非対応輪郭、非凸SAT、非有限値、無効箱、桁あふれ・一部のアンダーフロー。微小差がfloatで失われる例と数値比較器の失敗検査 | 76 |

L字の頂点順は `(0,0),(4,0),(4,1),(1,1),(1,4),(0,4)` 。2DはX右・Y上、3Dは右手系の同じ長さ単位を使う。P2・P3はdoubleの座標タプルで、点と方向はコメントと文脈で区別する。

出力例（抜粋）：

```text
  [OK] A: P=(1,1)のAB/BC/CA外積: 4, 5, 3
  [OK] A: Pの包含: Inside
  [OK] A: Eは有限なAB上: True
  [OK] B: D=(1,2): 分類: Boundary
  [OK] B: D=(1,2): 右向き計数: null
  [OK] C: A/B: 分類: Touching
  [OK] C: A/B: 接触込み: True
  [OK] C: A/B: 正の面積/体積のみ: False
  [OK] D: T1の斜め区間: 0, 3
  [OK] D: T2の斜め区間: 4, 7
  [OK] D: T1/T2の実体: Separated
  [OK] E: I=(1,1,1): 6面の内積: -1, -1, -1, -1, -1, -1
  [OK] E: K/K2: 共有面積/体積: 1
...
すべての 254 チェックに成功しました。
```

## 分類とAPIの前提

| 型・結果 | 意味 |
| --- | --- |
| PointLocation.Inside | 境界から内側へ離れた点 |
| PointLocation.Boundary | 有限な辺・頂点、または箱の境界面・辺・頂点上 |
| PointLocation.Outside | 領域の外 |
| RegionRelation.Separated | 共通点なし |
| RegionRelation.Touching | 共通点はあるが共有面積／体積0 |
| RegionRelation.Overlapping | 正の共有面積／体積がある。包含・一致も含む |
| 各型のInvalid | 無効・非対応入力、検出した非有限演算や退化など。外部や離隔とは別 |

輪郭は単一配列の点を順に結び、`closed: true` なら末尾から先頭へも辺を置く。始点の末尾重複は認めない。配列だけから閉じる意図は推定できないため、`closed: false` はInvalid。穴や複数輪郭を表す入力形式は用意していない。

`TryValidate` は全頂点の有限性・重複、ゼロ長辺、隣接辺の折返し、非隣接辺の交差・接触、符号付き面積0を検査する。面積が非0でも自己交差する例を拒否する。検査は頂点数の2乗の処理で、教材の小さな輪郭を対象とする。

`InTriangle` は3頂点限定でCW・CCWとも対応し、向きに合わせて全辺の内側条件を比較する。負の条件を0より先に扱うため、辺の延長線上をBoundaryにしない。`TryPointOnSegment` も外積0に加え、内積を0から辺長の2乗までに制限する。

`ByRay` は単純な凹多角形も受け取る。全辺のBoundary検査を先に行い、その後だけ `minY <= py && py < maxY` と `xHit > px` で計数する。水平辺は除算より前に除外する。通常の通過頂点は片側だけ、極大は0辺、極小は2辺が寄与し、同じ座標だからと交点を1つにまとめない。`RayResult.RightCrossings` はBoundary／Invalidではnullで、未計数と0回を区別する。

`ConvexSat.Compare` は両輪郭が **非退化で厳密に凸な2D閉多角形** であることを先に検査する。凹L字・自己交差・冗長な共線頂点はInvalid。両方の全辺から非ゼロ法線を作り、全頂点の内積の最小・最大を比較する。軸の正規化は不要で、負の倍率では最小・最大を取り直す。1軸でも分離すれば終了し、端点一致によるTouchingは全候補で分離がないことを確認してから返す。非凸形状や一般3DメッシュのSATではない。

`AabbChecks.Compare` は全軸で有限かつ正の元の幅を要求する。min>max、幅0、非有限座標・幅はInvalid。各軸の候補幅が負ならSeparated、全て非負で0を含めばTouching、全て正なら共有面積・体積を積で求める。正の幅の積が非有限や0になった場合もInvalidとする。`AabbResult.AxisDeltas` の負は離隔の候補差で、実際の負の長さではない。Invalidでは候補幅は空、共有量はnull。

`Inclusive` は接触を含む共通点のBoolean、`PositiveOverlap` は正の共有面積・体積のBooleanである。どちらもInvalidはnullを返し、falseと混同しない。`Try...` メソッドのboolは演算の有効性であり、falseを「図形の外」と解釈せず、成功時だけout値を使う。

`ConvexBoxChecks` は軸平行箱の6面だけを、外向きの単位法線と面上の点から手実装する。全内積が非正という半空間の共通条件を使い、正の値があれば他面で0でもOutside。箱の中身と境界を扱うもので、非凸ソリッド・開いた殻・任意の三角形メッシュの点包含ではない。

## 数値比較・誤差と対象外

幾何分類に固定epsilonは使わず、計算したdoubleの符号・0・不等号を比較する。これは **教材の固定入力の分類** であり、汎用CADの厳密述語ではない。浮動小数点で0になったことは幾何学的な厳密境界の証明ではなく、近境界・桁落ち・任意尺度での正しさは保証しない。

| 用途 | 設定と意味 |
| --- | --- |
| doubleの期待値照合 | 絶対誤差1e-12＋相対誤差1e-12×実値・期待値の絶対値の大きい方 |
| floatの小整数外積の照合 | 絶対誤差1e-5、相対誤差0 |
| 幾何分類の境界 | 外積・内積・区間幅の計算値が0。検算の許容誤差を流用しない |
| 入力・演算検査 | NaN／Infinity、ゼロ長・面積0、非有限な中間値や一部のアンダーフローを検出 |

数値の比較器は実値・期待値・許容誤差・差の有限性も確認する。0付近は絶対誤差、大きい値には相対誤差が役立つ。単位を変えれば絶対誤差も見直す。表示は通常G9、失敗時G17で、表示用の丸め値では再判定しない。

2D外積は長さの2乗、単位法線との内積は長さの尺度である。距離の許容差を導入するなら、外積の絶対値を辺長で、非単位SAT軸の内積差を軸長で割るなど、尺度を合わせる必要がある。単に全判定へ同じepsilonを足さない。頂点の半開条件を片側だけ緩めると偶奇も変わる。

題材7では `2+1e-10` がdoubleでは箱の外、floatへ移して戻すと境界の別入力になることを確認する。`System.Numerics.Vector2/Vector3` はこの比較に限って使い、分類本体はdoubleの成分式で追う。

検出できない丸めや桁落ちもあるため、全ての数値的な不確かさをInvalidへ変換できるわけではない。許容差による境界帯、判定保留の状態、誤差評価や頑健な述語、スケール調整は用途に応じた別設計になる。近境界の入力、非有限・退化・非対応入力を、無条件に正常なOutside／Separatedとして扱わない。

## 自動チェックとCI

実際に成功した `Equal` ／ `Numbers` の呼び出しを各1件と数える。配列の全成分比較は1件、反復の各呼び出しは別件。数値比較器が不一致・非有限値を拒否する検査も含め、25／35／30／37／20／31／76の合計254件。実際の `[OK]` 254行、成功要約、終了コード0を照合する。期待値は本文・宿題と独立検算をもとに記載し、分類本体の出力からは生成しない。

[Check Geometry Examples](../../../.github/workflows/check-geometry-examples.yml) は、第5回のC#コード（`.cs`）・プロジェクト（`.csproj`）・ソリューション（`.sln`）・ビルド設定（`.props`／`.targets`）、ルートの `global.json`／`Directory.Build.props`／`Directory.Build.targets`、ワークフロー自身の変更を、PR時とmainへのpush時に検証する。`examples/` 内のサブディレクトリも対象で、Markdownだけの変更では起動しない。

Ubuntu 24.04・.NET SDK 10.0.401で `GeometryCheckExamples.sln` をrestoreし、Releaseビルドして `GeometryCheckExamples.csproj` を実行する。プログラムの終了コード0、実際の `[OK]` 254行、完全一致する成功要約の3点を確認する。`set -euo pipefail` により、ログを `tee` で表示・保存している途中の実行失敗も伝播する。チェック内容を変更するときは、件数だけでなく各題材の判定内容もレビューし、このREADMEの件数・出力例とワークフローの `EXPECTED_CHECKS` を同じPRで更新する。

結果は [Actions → Check Geometry Examples](https://github.com/mkunori/geometry-study-group/actions/workflows/check-geometry-examples.yml)、またはPRのChecksから開く。**対象の実行 → GeometryCheckExamples (Release)** の `Restore solution`・`Build solution (Release)`・`Run GeometryCheckExamples and verify all 254 checks` のログを確認する。

正常時には、三角形P／E／OのInside／Boundary／Outside、レイの境界先行・半開規則、2D AABBとSATのSeparated／Touching／Overlapping、3Dの面・辺・点接触、退化・入力不正のInvalidが別々のラベルで表示されることを確認する。最後は `すべての 254 チェックに成功しました。` と `Verified all 254 checks.` が揃う。

失敗時は `FAIL` のラベルから実値・期待値を確認する。例えばT1／T4は点接触なのでTouchingを期待し、Overlappingに誤るとその設問で失敗する。数値比較は成分番号・許容誤差も表示する。件数や成功要約が一致しない場合は `::error::` を確認する。

権限は `contents: read` のみで、チェックアウトの認証情報を保持せず、PAT・Secret・書き込み権限は不要。push・自動コミット・PDF生成を行わない。第1〜4回の専用CIとPDF自動生成から独立したワークフローである。AutoCAD API参考コードは別ディレクトリで管理する別Issue #54の範囲で、このCIではビルド・実行しない。AutoCAD実機・ライセンス・SDK DLLも不要。

TODO：教材の通読レビューとVisual StudioのGUIデバッグ操作を確認する。F5／Ctrl+F5／F9／F11はこのCIでは検証できないため、上記のローカル手順で確認する。PDFと公開承認は後続作業で、第5回は準備中を維持する。AutoCAD 2026＋.NET 10の参考コードはこのコンソールへ組み込まない。

[この回のトップへ戻る](../README.md)

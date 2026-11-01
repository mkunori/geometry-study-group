# AutoCAD参考教材の総合レビュー

確認日：2026-11-01。対象は共通導入と第1〜5回の参考コード・README・共通設定。レビュー基点はmainのコミット [`e09ddc7`](https://github.com/mkunori/geometry-study-group/commit/e09ddc7e3226e5fa2fda7df7d0c86352919b6d82) 。着手条件の #69〜#74 は、[PR #76](https://github.com/mkunori/geometry-study-group/pull/76)・[PR #77](https://github.com/mkunori/geometry-study-group/pull/77)・[PR #78](https://github.com/mkunori/geometry-study-group/pull/78)・[PR #79](https://github.com/mkunori/geometry-study-group/pull/79)・[PR #80](https://github.com/mkunori/geometry-study-group/pull/80)・[PR #81](https://github.com/mkunori/geometry-study-group/pull/81) として、すべてmainへマージ済みであることを確認した。

**ソース・文書と公式資料の静的レビューのみ。ビルド・NETLOAD・AutoCAD起動・コマンド実行・自動テスト・CIは実施していない。** 以下の数値は数学上の期待値とソースの対応であり、AutoCADの観測結果ではない。親 [Issue #54](https://github.com/mkunori/geometry-study-group/issues/54) の参考教材を整理するもので、数学教材の公開承認は別件。

## 数学教材と第1〜5回の対応

各行の詳細解説・図解・宿題の問題と解答・C#数値例を、対応するAutoCAD章の固定入力と照合した。右手系、位置と方向、単位と座標系を維持しており、数学教材側の変更は行わない。

| 章・数学教材の参照先 | 確認した数値・数学的な意味 | AutoCAD側の対応と制約 |
| --- | --- | --- |
| 01：[解説](../../sessions/01-matrix/explanation.md)・[図解](../../sessions/01-matrix/images/README.md)・[宿題](../../sessions/01-matrix/homework/README.md)・[C#](../../sessions/01-matrix/examples/Program.cs) | 点(2,1,0)を+Z軸の周りに90°回転すると(-1,2,0)、変位(10,0,3)を加えて(9,2,3)。同次成分は点1・方向0 | [Matrix3dの章](../samples/01-matrix/README.md)。列方式のTRは回転→並進。ローカル原点(10,0,3)、軸+Y／−X／+Zも同じ配置。Pre／Postの戻り値を保持。System.Numericsの行方式は転置と積順序を区別 |
| 02：[解説](../../sessions/02-vector/explanation.md)・[図解](../../sessions/02-vector/images/README.md)・[宿題](../../sessions/02-vector/homework/README.md)・[C#](../../sessions/02-vector/examples/Program.cs) | a=(3,4)、b=(2,0)：内積6、長さ5と2、cosθ=3/5、約53.130102°、射影(3,0)。u=(2,0,0)×v=(0,3,0)は(0,0,6)、逆順は−Z | [Vector3dの章](../samples/02-vector/README.md)。三角形A=(1,1,0)、B=(4,1,0)、C=(3,3,0)の法線は+Z、逆順は−Z。零長・退化を先に除外。AutoCADはdouble、System.Numerics.Vector3はfloat |
| 03：[解説](../../sessions/03-intersection/explanation.md)・[図解](../../sessions/03-intersection/images/README.md)・[宿題](../../sessions/03-intersection/homework/README.md)・[C#](../../sessions/03-intersection/examples/Program.cs) | 対角線の交点(2,2)、t=u=1/2。範囲外はt=1.5、端点はt=1。同一直線の重複は区間[2,4]。z=2との交点(2,2,2)、分子12・分母24。z=2とx=1の交線は(1,t,2) | [交点の章](../samples/03-intersection/README.md)。数学の直線は無限、Lineは有限の図面Entity。4種類の延長指定と点コレクションを区別。重複区間・無数交点は数式で補助分類。平面例は無限平面で、三角形ABC内の判定ではない。2平面は固定例の手計算対応 |
| 04：[解説](../../sessions/04-modeling/explanation.md)・[図解](../../sessions/04-modeling/images/README.md)・[宿題](../../sessions/04-modeling/homework/README.md)・[C#](../../sessions/04-modeling/examples/Program.cs) | 幅2・高さ1、N=+Y／B=+Z／T=+X。ローカル(u,v,w)→(w,u,v)、長さ5、断面積2、数学上の体積10。共有頂点メッシュは8頂点・12三角形 | [モデリングの章](../samples/04-modeling/README.md)。ECSの閉じたPolyline→分解した4 Line→1 Region→Solid3d。生成後に同じ右手系フレームへ変換。Solid3dはB-repで、メッシュの頂点数をAPIへ要求しない。Sweepは最初の直線区間だけで、折れ曲がりの接合は対象外 |
| 05：[解説](../../sessions/05-geometry-check/explanation.md)・[図解](../../sessions/05-geometry-check/images/README.md)・[宿題](../../sessions/05-geometry-check/homework/README.md)・[C#](../../sessions/05-geometry-check/examples/Program.cs) | K=[0,2]³とK2=[1,3]³の共有部分は[1,2]³、体積1。K3=[2,3]×[1,2]×[1,2]は面接触、面積1・体積0。AABB候補だけでは三角形の重なりを確定できない | [干渉判定の章](../samples/05-geometry-check/README.md)。包囲範囲、IntersectWithの返却点、CheckInterferenceのbool、Boolean後のコピーを分ける。面接触のAPIフラグは断言しない。体積測定は行わず、Boolean後は範囲だけ表示。内外・SATの自作分類をSolid3dの契約と同一視しない |

AutoCADへの座標入力はWCSの固定値が基本で、Polyline.AddVertexAtはECSの2D座標で生成してから配置する。現在UCSから入力を変換する例ではない。角度APIにはラジアン、表示には必要に応じて度を使う。長さは作図単位でmm等を仮定せず、面積・体積はその2乗・3乗。外積は面積の尺度を持ち、単位法線とは区別する。各章の長さ・面積・角度・媒介変数の閾値は局所的な設定で、実務用の共通許容差ではない。

## API・署名・所有権の確認

採用したAutoCAD APIの名前空間、引数・戻り値、使用したオーバーロードは、以下の2026版参照表から確認できる。継承される基本メンバーも [共通参照表](references.md#全章で共有するプロパティ生成継承メンバー) に補った。C++資料はラップ元の制約・副作用を補う根拠であり、C#に同じ引数・出力があるとは解釈しない。

| 対象 | 公式ページをまとめた参照先 | 照合した契約・所有権 |
| --- | --- | --- |
| 共通・全章の取得処理 | [共通API](references.md#commoncommandsで使用するapi)・[共有メンバー](references.md#全章で共有するプロパティ生成継承メンバー)・[所有権](references.md#所有権例外呼出しコンテキスト) | CommandMethod(string)、WriteMessage(string)、Line(Point3d,Point3d)、Document／Database取得、Transaction生成・GetObjectの2引数版・DB登録順序。既存DBオブジェクトはTransaction管理、ホストのDocument／Databaseは破棄しない |
| 01：Matrix3d／Point3d／Vector3d | [第1回の公式参照表](../samples/01-matrix/README.md#2026版公式apiとの静的照合) | Rotationの3引数、Displacement、Pre／Post、AlignCoordinateSystemの8引数、点・方向のTransformByの戻り値を保持。Entity.TransformByはvoidで受け手を変更。値型のコピーとDB図形の変更を区別 |
| 02：Vector3d／Tolerance | [第2回の公式参照表](../samples/02-vector/README.md#2026版公式apiとの静的照合) | DotProduct→double、CrossProduct→Vector3d、GetAngleTo(Vector3d)→double、GetNormal(Tolerance)→Vector3d。ToleranceはequalVector→equalPointの順。EqualVector未満では入力を返す仕様を踏まえ、外積の長さを検査してから正規化 |
| 03：Line／Intersect／Point3dCollection | [第3回の公式参照表](../samples/03-intersection/README.md#2026版公式apiとの静的照合) | IntersectWithはEntity・Intersect・Point3dCollection・IntPtr×2の5引数でvoid。Planeなし・非Obsoleteの版を使用。呼出しごとに新しい点コレクション、一時Lineとともにusing。Plane／Line3dのAPIは生成しない |
| 04：Polyline／Region／Solid3d／SweepOptions | [第4回の公式参照表](../samples/04-modeling/README.md#2026版公式apiとの静的照合) | CreateFromCurves→DBObjectCollection。入力を未登録のLine群にして許可型と開き方を限定。複数Regionや途中失敗もfinallyで各要素を破棄。CreateExtrudedSolid(Entity,Vector3d,SweepOptions)／CreateSweptSolid(Entity,Entity,SweepOptions)はvoid。SweepはAlignSweepEntityToPath。SweepOptionsもusing、Builderは通常の設定オブジェクト |
| 05：Extents3d／Solid3d／RXObject | [第5回の公式参照表](../samples/05-geometry-check/README.md#2026版公式apiとの静的照合) | GeometricExtents→Extents3d、Min／Max→Point3d。CreateBox(double×3)、CheckInterference(Solid3d)→bool。Clone→objectを型検査し、BooleanOperation(BoolIntersect,Solid3d)→voidは両方のコピーだけに適用。引数側ShapeManagerの消費を考慮して再利用せず、双方をusingで後始末 |

図面に追加する00・01・04は、モデル空間をForWriteで開き、`AppendEntity` → `AddNewlyCreatedDBObject(..., true)` → `Commit()` の順になっている。追加用Entityは呼出し側のusingで管理し、Commit前の失敗はTransactionの破棄で未確定変更を戻す構成。Commit後の失敗まで取り消せるとは説明しない。通常の現在図面コマンドだけを対象とし、モデルレスUI・別図面・Sessionへ流用する際のロック設計は含めない。

## コマンド名と図面への副作用

6ファイルのCommandMethod属性17件に重複がなく、対応READMEの表記と一致する。次表はコードを読んだ期待動作で、実行結果ではない。DBへ追加する5コマンドはいずれもファイル保存を行わず、繰り返すと同位置に追加する。他プラグインとの名前の衝突は未確認。

| コマンド | 参照する章 | 図面DBへの変更・一時オブジェクト |
| --- | --- | --- |
| `GEO00_HELLO` | [00](../samples/00-common/README.md) | 表示のみ |
| `GEO00_LINE` | [00](../samples/00-common/README.md) | モデル空間へLineを1本追加 |
| `GEO01_TRANSFORM` | [01](../samples/01-matrix/README.md) | 比較用Lineを2本追加。変換前(0,0,0)→(2,1,0)、変換後(10,0,3)→(9,2,3) |
| `GEO01_LOCAL` | [01](../samples/01-matrix/README.md) | 点・方向の変換結果を表示 |
| `GEO01_ORDER` | [01](../samples/01-matrix/README.md) | 合成順序の違いを表示 |
| `GEO02_DOT` | [02](../samples/02-vector/README.md) | 内積・角度・射影を表示 |
| `GEO02_CROSS` | [02](../samples/02-vector/README.md) | 外積・向き・面積を表示 |
| `GEO02_NORMAL` | [02](../samples/02-vector/README.md) | 法線・退化例を表示 |
| `GEO03_LINES` | [03](../samples/03-intersection/README.md) | 未登録Lineと点コレクションで4種類の延長指定を表示、DB変更なし |
| `GEO03_OVERLAP` | [03](../samples/03-intersection/README.md) | 平行・重複区間を数式で分類、DB変更なし |
| `GEO03_PLANES` | [03](../samples/03-intersection/README.md) | 点・ベクトルによる平面の数値例を表示、DB変更なし |
| `GEO04_PROFILE` | [04](../samples/04-modeling/README.md) | X=0のYZ平面へ閉じたPolylineを1個追加 |
| `GEO04_EXTRUDE` | [04](../samples/04-modeling/README.md) | X範囲[0,5]、Y範囲[−1,1]、Z範囲[−0.5,0.5]のSolid3dを1個追加 |
| `GEO04_SWEEP` | [04](../samples/04-modeling/README.md) | 最初の直線区間だけで同じ範囲のSolid3dを1個追加。EXTRUDEと同位置に重なる |
| `GEO05_BOUNDS` | [05](../samples/05-geometry-check/README.md) | 未登録Polylineの範囲を表示し破棄、DB変更なし |
| `GEO05_INTERSECT` | [05](../samples/05-geometry-check/README.md) | 未登録Lineの返却点を列挙し一時オブジェクトを破棄、DB変更なし |
| `GEO05_SOLIDS` | [05](../samples/05-geometry-check/README.md) | 未登録の箱3個とBoolean用コピー2個で表示し破棄、DB変更なし |

## 共通設定・文書の整合

| 確認対象 | 静的確認の結果 |
| --- | --- |
| 対象環境 | [2026版の互換表](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-Customization/files/GUID-A6C680F2-DE2E-418A-A182-E4884073338A.htm) と [Visual Studio案内](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-450FD531-B6F6-4BAE-9A8C-8230AAC48CB4.htm) はUpdate 1.2以降／.NET 10／Visual Studio 2026 version 18.0で一致。初期版〜1.1の.NET 8と区別。2027版だけを根拠とした参照なし |
| ソリューション・参照 | [sln](../AutoCadGeometrySamples.sln) は1プロジェクトのDebug／Release x64。[csproj](../AutoCadGeometrySamples.csproj) はnet10.0のLibrary、samples/**/*.csに6ファイル、3 DLLすべてPrivate=false。[ローカル設定テンプレート](../AutoCadGeometrySamples.local.props.example) は実パスなし。Import条件・未設定／DLL不存在時のError定義を読み、実行はしていない |
| 配布物・CI | SDK・DLL・実行ファイルの追加なし。ローカルprops・SDK置き場・DLL等のignoreを確認。既存の数学C#ソリューション／CIからAutoCADへの参照なし。ワークフローの追加・変更・実行なし |
| 日本語・案内 | 6クラスと62メソッドに日本語XMLコメント。AutoCADトップ→各章・setup・references・本レビュー、各sessionsトップ→対応参考コードの導線を確認。「参考コード／実機未検証」と各章の期待動作・数式・制約を維持 |
| 数学教材・公開状態 | 詳細解説・SVG・宿題・コンソールC#・PDFを変更せず、第1回公開／第2〜5回準備中を維持。[PDF対象一覧](../../scripts/pdf-sessions.json) は01-matrixのみ。所要時間を追加しない |

## 残る不確実性と未実施事項

| 項目 | 確認できた範囲・残る制約 |
| --- | --- |
| 継承・個別署名 | DocumentManagerはCore.Applicationに掲載され、ApplicationServices.Application経由の用法は公式Guideにある。ModelSpace・2種類のインデクサー・Disposeの個別Referenceページは確認できず、公式Guideとクラスのインターフェースまでを根拠にした。実DLLのメタデータ・型解決は未照合 |
| Matrix3dの説明 | 公式Structureページ末尾の座標系説明には4×4の説明と整合しにくい記述がある。明示されたMp／Mvの同次成分・左積と、AlignCoordinateSystemを使う公式UCS例を根拠に照合。値型の更新・戻り値の実際の挙動は実行していない |
| IntersectWith | Managed Referenceの包囲箱の辺に触れる説明と、派生型の実装に依存する説明を区別。4延長モード、端点、返却点の件数・順序・重複は未観測。空の点コレクションや例外を、無条件に重複なし・干渉なしへ読み替えない |
| Region・Sweep・Boolean | 曲線の分解・Region生成数、押し出し／Sweepの成功、Booleanの成功や失敗後のネイティブ資源の状態は未観測。コレクション要素・オプション・コピーの後始末をコードで追った範囲。折れ曲がり全経路のSweepは実装対象外 |
| 接触と共有体積 | CheckInterferenceのManaged署名はboolだけで、C++の共有ソリッド出力を持たない。面・辺・点接触のtrue／falseは未確認。数学上の体積1／0はAPIの体積測定結果ではない |
| 対象環境・動作 | 参照DLLの製品年・更新版・依存関係、実コンパイル、NETLOAD、コマンド登録、表示・例外・図面状態はすべて未確認。静的照合を動作保証へ置き換えず、実機確認・自動検証を完了条件へ追加しない |

今回の修正は、本レビューの追加、共有APIの参照先と未確認範囲の補足、全コマンド一覧への導線、セットアップの図面変更コマンド案内と章追加前の古い記述の整理。ソースや数学教材の修正を必要とする不整合は、この静的レビューの範囲では見つからなかった。

[参考コードトップへ戻る](../README.md) ／ [セットアップ](setup.md) ／ [公式リファレンス](references.md) ／ [勉強会トップ](../../README.md)

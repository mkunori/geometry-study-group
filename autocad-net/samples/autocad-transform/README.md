# AutoCAD実践編：図形変換の実演コード

[同じ図形変換のいろいろな実現方法を見てみよう](../../../sessions/autocad-transform/README.md) の実演コードを管理する場所。
WCS・OCS・任意軸アルゴリズムを最優先に、異なるAPIや実装方法を数学で読み解く。
このページは実演の構成と管理方針を示すもので、動作確認済みのコードやコマンドの一覧ではない。

## 共通ソリューションとの関係

既存の [AutoCadGeometrySamples.sln](../../AutoCadGeometrySamples.sln) と [AutoCadGeometrySamples.csproj](../../AutoCadGeometrySamples.csproj) を使う。
プロジェクトは `samples/**/*.cs` を収録する構成で、テーマごとのコードをこのディレクトリに追加する。
セッション側には [実演の説明](../../../sessions/autocad-transform/examples/README.md) を置き、ソースや別ソリューションを二重管理しない。

## 題材ごとの分け方

| 題材 | 読み解く内容 |
| --- | --- |
| 座標系 | Polylineの頂点取得、Normal・Elevation、WCS・UCS・OCS |
| 任意軸アルゴリズム | 法線からOCS基底を生成する外積・正規化・分岐規則 |
| OCS/WCS相互変換 | 専用API、変換行列、基底の線形結合、正規直交基底との内積 |
| 基本変換・逆変換 | 座標系を考慮した移動・回転と、元へ戻す変換 |
| 合成と比較 | 順次適用、行列積、前後乗算、座標系の対応付け |

各題材を別のC#ファイルで管理し、コマンド名にはテーマ専用の `GEOAT_...` を使う。
既存の `GEO00_...`〜`GEO05_...` や、他のコマンドと衝突しないことを確認する。
各コードには、固定入力・座標系・単位・期待結果・適用条件・図面への副作用と、数学的な意図を記す。

## 対象環境と検証範囲

対象はAutoCAD 2026 Managed .NET APIとVisual Studio。
既存ソリューションのAutoCAD 2026 Update 1.2以降／.NET 10という対象を暫定的に維持する。
職場の更新版・実行環境を確認したうえで必要な対応を判断し、2024〜2025対応を前提にしない。

個々のAPIはAutoCAD 2026公式資料で仕様と適用条件を照合する。
共通の [公式リファレンス案内](../../docs/references.md) と [セットアップ](../../docs/setup.md) を参照する。
既存参考コードを含め、ビルド・NETLOAD・コマンド実行・実機表示は未検証である。

実演では完成済みのコードをVisual Studioに表示し、デバッガーで引数・途中計算・結果を追い、AutoCAD図面上の結果とも比較する。
実機確認は職場で行い、未実施の確認を成功済みと記載しない。
AutoCADを必要とするコードを、独立したC#教材・自動テスト・既存CIへ組み込まない。

[実践教材のトップへ戻る](../../../sessions/autocad-transform/README.md) ／ [AutoCAD参考コードのトップへ戻る](../../README.md)

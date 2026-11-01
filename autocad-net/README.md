# AutoCAD Managed .NET API参考コード

**公式資料とAPI仕様を静的に照合した参考コード／AutoCAD実機未検証。ビルド・NETLOAD・コマンド実行は未実施。** Windows版AutoCAD 2026 Update 1.2（2026.1.2）以降／.NET 10／Visual Studio 2026を対象にする。Update 1.1以前の.NET 8版やAutoCAD 2027向けへ読み替えない。

数学教材で学ぶ点・変位・座標系を、AutoCADの図面DBへ接続する入口。[数学教材のC#コンソール](../sessions/05-geometry-check/examples/README.md) とは独立した **1ソリューション・1 Class Library** で、各章のコードは同じプロジェクト内のファイルとコマンド名で分ける。00共通の2コマンドと、下の章別索引にある第1〜5回の各3コマンドを用意している。

## 読む順序とファイル

| 案内 | 内容 |
| --- | --- |
| [セットアップ](docs/setup.md) | 対象更新版、ローカルDLL参照、Visual Studio、NETLOAD、図面DBと後始末 |
| [公式リファレンス](docs/references.md) | AutoCAD 2026版のURL、対象API・署名と照合事項 |
| [総合レビュー](docs/review.md) | 共通基盤・第1〜5回の数学対応、全17コマンドの副作用、静的確認と残る不確実性 |
| [00：共通導入](samples/00-common/README.md) | コマンドの入力・期待動作と読む順序 |
| [CommonCommands.cs](samples/00-common/CommonCommands.cs) | メッセージ表示とLine追加の参考コード |
| [AutoCadGeometrySamples.sln](AutoCadGeometrySamples.sln) | Visual Studioで開く従来形式の.sln |
| [AutoCadGeometrySamples.csproj](AutoCadGeometrySamples.csproj) | net10.0・x64のClass Library。ビルド未検証 |
| [ローカル参照設定のテンプレート](AutoCadGeometrySamples.local.props.example) | 3つのAutoCAD DLLの参照先。実パスはコピー先だけに記入 |

AutoCADがない場合は、セットアップの前提と参考資料を読み、`Hello` → `AddLine` の順にソースを追う。DLLや製品を用意してビルドすることを、この教材の読解の前提にしない。

## 共通導入コマンドの期待動作

次は利用者が対象環境で確認する場合の期待であり、動作確認済みの結果ではない。章別を含む全17コマンドは [総合レビューのコマンド一覧](docs/review.md#コマンド名と図面への副作用) と各章のREADMEから参照できる。

| コマンド | 固定入力と期待動作 | 図面への変更 |
| --- | --- | --- |
| `GEO00_HELLO` | 現在の図面のEditorへ案内を表示 | なし |
| `GEO00_LINE` | WCS (0,0,0)→(4,0,0)のLineをモデル空間へ1本追加 | あり。保存は行わず、再実行で同位置へ重複追加 |

長さ4は作図単位で、mmなどを仮定しない。Lineの2点はWCSで、現在のUCSから変換する例ではない。実際に利用者が確認する場合は専用の空図面を使い、[セットアップの手順](docs/setup.md) に従う。

## 各章の案内

| ディレクトリ | 対応する数学教材 | 状態 |
| --- | --- | --- |
| [00-common](samples/00-common/README.md) | コマンド・図面DB・Transaction | 参考コード作成、ビルド・実機未検証 |
| [01-matrix](samples/01-matrix/README.md) | [第1回：座標・変換](../sessions/01-matrix/explanation.md) | 参考コード作成、ビルド・実機未検証 |
| [02-vector](samples/02-vector/README.md) | [第2回：内積・外積](../sessions/02-vector/explanation.md) | 参考コード作成、ビルド・実機未検証 |
| [03-intersection](samples/03-intersection/README.md) | [第3回：交点](../sessions/03-intersection/explanation.md) | 参考コード作成、ビルド・実機未検証 |
| [04-modeling](samples/04-modeling/README.md) | [第4回：モデル](../sessions/04-modeling/explanation.md) | 参考コード作成、ビルド・実機未検証 |
| [05-geometry-check](samples/05-geometry-check/README.md) | [第5回：内外・干渉](../sessions/05-geometry-check/explanation.md) | 参考コード作成、ビルド・実機未検証 |

## 追加・管理のルール

- コマンドは共通導入の `GEO00_...` 、各章の `GEO01_...` 〜 `GEO05_...` で区別する。組織内・他プラグインとの重複も利用者側で確認する。
- C#ソース、ビルド定義、文書とテンプレートを管理する。AutoCAD本体・SDK・DLL・CHM・ライセンス情報・PC固有の参照パスはGitへ含めない。
- 各章に日本語XMLコメント、数学教材の対応箇所と固定値、WCS／UCS・単位・入力・期待動作・副作用・所有権を記す。数学教材の数式・期待値は変更しない。
- AutoCAD 2026の公式資料でAPIを静的照合し、未確認の部分はTODO／注記にする。**ビルド・ロード・実行を自動検証しない。GitHub Actions・自動テスト・既存CIへの統合も追加しない。**
- `sessions/` のAutoCAD不要C#教材とそのCI、PDF公開状態・自動生成対象は維持する。本ディレクトリはそれらのプロジェクトから参照しない。

TODO：利用者が対象の製品・更新版・参照DLLを用意した場合のビルド／ロード／期待動作は未確認。参考コードの確認範囲は静的・文書上の整合性までとし、実機確認を要求しない。章別コードの追加・変更も、別Issue・別PRで行う。

[勉強会トップへ戻る](../README.md)

# geometry-study-group

幾何アルゴリズム勉強会

全5回を通して、図形を扱うアルゴリズムの数学的な仕組みを学ぶための社内教材です。このREADMEをトップページとして、GitHub上で各回の資料を閲覧できます。

## 基本方針

- 各回の本編は45分、質疑応答を含めて60分以内とします。
- 計算手順の暗記ではなく、数学的な仕組みの理解を重視します。
- .NET・C#での実装例と、静的なSVG図解を用意します。
- AutoCADとの連携は任意です。
- 説明資料・画像・発表用PDF・C#コードを社内教材として残します。
- GitHubのMarkdownを閲覧の入口とし、GitHub Pagesは使用しません。

## 各回の資料

テーマは初期案です。今後のフィードバックによって内容や順序を変更する可能性があります。

| 回 | 仮テーマ | 状態 |
| --- | --- | --- |
| 1 | [なぜ行列で図形を変換できるのか？](sessions/01-matrix/README.md) | TODO：教材未作成 |
| 2 | [なぜ内積・外積で角度や方向がわかるのか？](sessions/02-vector/README.md) | TODO：教材未作成 |
| 3 | [なぜ直線や平面の交点を計算できるのか？](sessions/03-intersection/README.md) | TODO：教材未作成 |
| 4 | [なぜ2Dの図形から3Dモデルを作れるのか？](sessions/04-modeling/README.md) | TODO：教材未作成 |
| 5 | [なぜ図形の内外や干渉を判定できるのか？](sessions/05-geometry-check/README.md) | TODO：教材未作成 |

[全5回の初期カリキュラム](docs/curriculum.md) ／ [資料制作ルール](AGENTS.md)

## 教材の構成と現在の状態

各回のREADMEから、次の教材ファイル・保管先を参照できます。

| パス（各回のディレクトリ内） | 用途 | 現在の状態 |
| --- | --- | --- |
| `README.md` | 各回のトップページと教材への案内 | 初期案内を作成済み |
| `explanation.md` | 詳細解説 | TODO付きのひな形 |
| `slides.md` | Marp用スライド原稿 | TODO付きのひな形 |
| `images/` | 静的なSVG図解 | 案内READMEのみ |
| `examples/` | .NET・C#の実装例 | 案内READMEのみ |
| `output/` | 発表用PDF | 案内READMEのみ |

今回はディレクトリ構成・ナビゲーション・制作ルールのみを整備しています。

TODO：各回の詳細解説、スライド、SVG図解、C#実装例、PDFを今後作成します。PDF生成環境は未構築です。

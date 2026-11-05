# 配布用PDF（4種類）

[詳細解説PDFを開く](explanation.pdf) ／ [ダウンロード](explanation.pdf?raw=true)

A4縦・ページ番号付き。[詳細解説Markdown](../explanation.md) の文章・表・数式・SVG8点・末尾の補足を省略せず収録している。本文は選択・検索できる。PDFはGitHubで閲覧・ダウンロードできる。

## 4種類の原本とPDF

| 教材 | Markdown原本 | PDF | ダウンロード |
| --- | --- | --- | --- |
| 詳細解説 | [explanation.md](../explanation.md) | [explanation.pdf](explanation.pdf) | [保存](explanation.pdf?raw=true) |
| 練習問題 | [problems.md](../homework/problems.md) | [problems.pdf](problems.pdf) | [保存](problems.pdf?raw=true) |
| 解答・解説 | [solutions.md](../homework/solutions.md) | [solutions.pdf](solutions.pdf) | [保存](solutions.pdf?raw=true) |
| C#実装例の説明 | [examples/README.md](../examples/README.md) | [examples.pdf](examples.pdf) | [保存](examples.pdf?raw=true) |

すべてA4縦・ページ番号付きで、本文を選択・検索できる。各PDFは対応するMarkdownだけから生成する。練習問題に解答を含めず、解答には途中計算・根拠・参照図を収録する。C#実装例の説明は実行環境・Visual Studio／CLIの手順・入力と期待結果・数学との対応・検証と適用範囲を収録し、C#ソース全体は印刷しない。本文・図・表・コードブロックは省略しない。

4種類をまとめて再生成する場合は、リポジトリのルートで次を実行する。

```sh
npm ci
npm run pdf:session -- sessions/02-vector
```

既存の4つのPDFを更新する。`--browser-path <実行ファイル>` も指定できる。全4種類の生成・比較が成功するまで出力を置き換えず、日時だけの差分なら既存ファイルを維持する。

## 原本と更新方法

- 本文原本：[explanation.md](../explanation.md)
- 参照する図：[images/](../images/README.md)
- 生成物：このディレクトリの `explanation.pdf`

PDFを別原稿として編集しない。修正はMarkdown・SVGへ行い、共通コマンドで再生成する。

この回は `pdf-publish.json` で自動更新を有効にしている。4原本・参照SVGをmainへ反映すると、GitHub Actionsが公開対象の全PDFを再生成し、全件成功後に内容の変わったPDFだけをコミットする。ローカルで表示確認したPDFを原本と同じPRに含めることもできる。対象一覧・手動実行・更新の仕組みは [共通運用文書](../../../docs/pdf-automation.md) を参照。手動生成したPDFをPRに含めることもできる。

## 必要環境と再生成

[共通の生成手順と必要環境](../../01-matrix/output/README.md) と同じNode.js 24.x・インストール済みChrome／Edge／Chromium・日本語フォントを使う。

リポジトリのルートで実行する。

```sh
npm ci
npm run pdf -- sessions/02-vector/explanation.md sessions/02-vector/output/explanation.pdf
```

PowerShellでは `npm.cmd` を使ってもよい。ブラウザーを自動検出できない場合は、生成コマンドに `--browser-path` と実行ファイルのパスを追加する。`--html-preview <file.html>` を加えると同じ本文の確認用HTMLも保存できる。HTMLとページの確認画像はリポジトリへ追加しない。

## 再生成後の確認

- 全ページを画像化し、日本語・数式・列ベクトル・SVG・表・改ページ・文字切れ・重なりを目視確認する。
- 本文・補足・図が欠けていないことを原本と照合し、テキストの選択・検索とページ番号を確認する。
- 別教材とGitHubへのリンクはPDF化の際だけ解除し、表示文字を残す。見出しジャンプ・外部の出典リンクを維持し、GitHub URLは末尾の元データ表記だけに1回存在することを確認する。[共通のリンクルール](../../../docs/pdf-automation.md#社内配布pdfのリンクと元データ表記) を参照。
- 数値例・符号・座標系を宿題・C#と照合し、C#をビルド・実行して73チェックと専用CIの成功も確認する。
- 原本のMarkdown・SVGをPRで更新する。mainへのマージ後にActionsの成功と生成PDFの全ページを確認する。手動生成PDFをPRに含める場合は同じ内容か照合し、生成コマンド・確認環境・表示結果を記載する。OS・ブラウザー・フォントが変わると紙面やページ数も変わりうる。

## 学習時の確認ポイント

内積を余弦定理から導く流れ、射影の分母、外積の成分と面積の導出を言葉でも追えるか確認する。2Dスカラー外積と3Dベクトル外積、左右の符号と頂点順序、ゼロ・一直線の場合の未定義も区別して読む。数値例は [宿題](../homework/README.md) と [C#実装例](../examples/README.md) で確かめられる。


[この回のトップへ戻る](../README.md)

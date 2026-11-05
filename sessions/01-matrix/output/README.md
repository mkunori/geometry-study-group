# 配布用PDF（4種類）

[詳細解説PDFを開く](explanation.pdf) ／ [ダウンロード](explanation.pdf?raw=true)

A4縦・ページ番号付き。[詳細解説Markdown](../explanation.md) の章立て・文章・表・数式・SVG11点・末尾の補足を含む全文PDF。本文は選択・検索できる。PDFはリポジトリに保存し、GitHubで直接閲覧・ダウンロードできる。

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
npm run pdf:session -- sessions/01-matrix
```

既存の4つのPDFを更新する。`--browser-path <実行ファイル>` も指定できる。全4種類の生成・比較が成功するまで出力を置き換えず、日時だけの差分なら既存ファイルを維持する。

## 本文原本と生成物

- 本文原本：[explanation.md](../explanation.md)
- 図解：[images/](../images/README.md)
- 生成物：このディレクトリの `explanation.pdf`

更新はMarkdownと参照するSVGに行い、PDFを再生成する。PDFを別の原稿として編集したり、本文を要約した別版を作ったりしない。

この回は `pdf-publish.json` で自動更新を有効にしている。4原本・参照SVGをmainへ反映すると、GitHub Actionsが公開対象の全PDFを再生成し、全件成功後に内容の変わったPDFだけをコミットする。ローカルで表示確認したPDFを原本と同じPRに含めることもできる。[自動生成・手動実行・新しい回の追加方法](../../../docs/pdf-automation.md) を参照。以下はローカルで表示確認するための手順。

## 必要環境

- Node.js 24.x（npmを同梱）
- インストール済みのGoogle Chrome、Microsoft Edge、Chromiumのいずれか
- 日本語フォント。Yu Gothic、Meiryo、Noto Sans CJK JP、sans-serifの順で利用する。SVGは元のフォント指定を使用する。

動作確認環境：Windows、Node.js 24.21.0、npm 11.19.0、Microsoft Edge 156.0.4314.50、Yu Gothic・Meiryo。

WindowsとmacOSの標準インストール先、LinuxのPATHからブラウザーを探す。自動検出できない場合は `--browser-path` で実行ファイルを指定する。ブラウザーを自動ダウンロードする構成にはしていない。

## 再生成

リポジトリのルートで実行する。

```sh
npm ci
npm run pdf:01
```

Windows PowerShellでは `npm.cmd ci`、`npm.cmd run pdf:01` でも実行できる。出力先は `sessions/01-matrix/output/explanation.pdf` で、既存ファイルを更新する。

第2回以降にも同じ共通コマンドを使える。本文が完成した回の入力・出力パスを指定する。

```sh
npm run pdf -- sessions/01-matrix/explanation.md sessions/01-matrix/output/explanation.pdf
```

ブラウザーを明示する例：

```sh
npm run pdf:01 -- --browser-path "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
```

必要な場合は `--html-preview <file.html>` を追加すると、同じ本文・数式・画像・フォントを含む確認用HTMLも保存できる。HTMLはローカル確認用で、公開や配信は行わない。

## 変換処理と固定依存

[生成スクリプト](../../../scripts/build-pdf.mjs) はMarkdownをHTMLにし、[印刷用CSS](../../../scripts/print.css) を適用してブラウザーでPDF化する。本文の要約や書き換えは行わない。画像と数式フォントはHTML内へ取り込み、外部CDNからの読み込みを不要にしている。

| 固定依存 | バージョン | 用途 |
| --- | --- | --- |
| markdown-it | 14.3.3 | 見出し・本文・表・リンク・画像のMarkdown変換 |
| markdown-it-texmath | 1.0.0 | 標準的な `$` / `$$` の数式の解釈 |
| KaTeX | 0.17.0 | `bmatrix`・`aligned` を含む数式の描画 |
| github-slugger | 2.0.0 | 見出しのアンカーID |
| puppeteer-core | 24.43.1 | インストール済みブラウザーの印刷機能でA4 PDFを生成 |
| @puppeteer/browsers | 2.13.2 | Actionsで固定バージョンのChrome for Testingを導入 |
| pdf-lib | 1.17.1 | 自動更新時の比較で日時・内部識別子のみの差分を抑制 |

[package.json](../../../package.json) と [package-lock.json](../../../package-lock.json) で依存を固定している。依存変更後は `npm ci` を実行する。

Markdownの相対リンクと画像パスは原本のまま維持する。PDF化の際だけ別教材とGitHubへのリンクを解除し、表示文字を残す。同一文書内の見出しジャンプと外部の出典リンクは維持する。ローカル画像は原稿のディレクトリを基準に解決する。末尾には元データのリポジトリURLを1回だけ自動付加する。[共通のリンクルール](../../../docs/pdf-automation.md#社内配布pdfのリンクと元データ表記) を参照。

## 再生成後の確認

- 本文・表・数式・SVG・末尾の補足がすべて含まれ、元Markdownと一致していること。
- 全ページを画像化して目視確認し、日本語・2×2/3×3/4×4の行列・図の欠け、横方向のはみ出し、重なり、不自然な改ページがないこと。
- 数式が生TeXになっておらず、行列・図・表が途中で分断されていないこと。
- 本文を選択・検索でき、見出しジャンプ・外部の出典リンク・ページ番号が機能すること。GitHub URLは末尾の元データ表記だけに1回存在すること。
- Markdownや図解を更新したら、上記の表示確認を行い、main反映後の自動生成結果も確認すること。ローカルで確認したPDFを同じPRに含めてもよい。

直接生成では日時などのメタデータが毎回変わるが、公開対象の自動更新では比較時にこの差分を抑制する。紙面・ページ数を揃える場合は原稿・依存に加え、OS・ブラウザー・フォントも揃える。

[この回のトップへ戻る](../README.md)

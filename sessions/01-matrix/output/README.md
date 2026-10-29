# 詳細解説PDF

[PDFを開く](explanation.pdf) ／ [ダウンロード](explanation.pdf?raw=true)

A4縦・13ページ。[詳細解説Markdown](../explanation.md) の章立て・文章・表・数式・SVG11点・末尾の補足を含む全文PDF。本文は選択・検索できる。PDFはリポジトリに保存し、GitHubで直接閲覧・ダウンロードできる。

## 本文原本と生成物

- 本文原本：[explanation.md](../explanation.md)
- 図解：[images/](../images/README.md)
- 生成物：このディレクトリの `explanation.pdf`

更新はMarkdownと参照するSVGに行い、PDFを再生成する。PDFを別の原稿として編集したり、本文を要約した別版を作ったりしない。

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

[package.json](../../../package.json) と [package-lock.json](../../../package-lock.json) で依存を固定している。依存変更後は `npm ci` を実行する。

Markdownの相対リンクと画像パスは原本のまま維持する。生成時に、PDF内の教材リンクはGitHubの `main` の対応ファイルへ向け、同一文書内の `#見出し` は内部リンクとして残す。ローカル画像は原稿のディレクトリを基準に解決する。

## 再生成後の確認

- 本文・表・数式・SVG・末尾の補足がすべて含まれ、元Markdownと一致していること。
- 全ページを画像化して目視確認し、日本語・2×2/3×3/4×4の行列・図の欠け、横方向のはみ出し、重なり、不自然な改ページがないこと。
- 数式が生TeXになっておらず、行列・図・表が途中で分断されていないこと。
- 本文を選択・検索でき、教材リンクとページ番号が機能すること。
- Markdownや図解を更新したら、生成PDFも同じPRに含めてコミットすること。

生成日時などのメタデータは再生成のたびに変わる。紙面を揃える場合は原稿・依存に加え、OS・ブラウザー・フォントも揃える。

[この回のトップへ戻る](../README.md)

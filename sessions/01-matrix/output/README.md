# 発表用PDFの保管先

[PDFを開く](slides.pdf) ／ [ダウンロード](slides.pdf?raw=true)

19ページ・16:9。本編45分＋質疑応答15分の発表用資料。PDFはリポジトリに保存しており、GitHubで閲覧・ダウンロードできる。閲覧だけなら以下の生成環境は不要。

## 元データ

- [Marp原稿](../slides.md)
- [既存SVG図解](../images/README.md)

内容を更新するときは原稿とSVGを編集してから再生成する。PDFを別の原稿として編集しない。詳しい導出は [詳細解説](../explanation.md) を参照。

## 生成に必要な環境

- Node.js 24.x（npmを同梱）
- Google Chrome、Microsoft Edge、Firefoxのいずれか。Marp CLIがインストール済みブラウザーを自動検出する。
- 日本語フォント。原稿とSVGはYu Gothic、Meiryo、sans-serifの順に指定している。確認環境と見た目を揃える場合は同じフォントを用意する。

動作確認環境：Windows、Node.js 24.21.0、npm 11.19.0、Microsoft Edge 156.0.4314.50。

## PDFの再生成

リポジトリのルートで実行する。

```sh
npm ci
npm run pdf:01
```

Windows PowerShellでは `npm.cmd ci`、`npm.cmd run pdf:01` でも実行できる。出力先はこのディレクトリの `slides.pdf` で、既存ファイルを更新する。

[package.json](../../../package.json) でMarp CLI 4.5.1とpdf-lib 1.17.1を指定し、[package-lock.json](../../../package-lock.json) で依存関係を固定している。初回と依存関係の更新後は `npm ci` を実行する。

[生成スクリプト](../../../scripts/build-pdf.mjs) はMarp CLIで原稿からPDFを生成し、PDF内のローカル教材リンクをGitHubの `main` ブランチのURLに置き換える。元のMarkdownの相対リンクや画像パスは維持する。pdf-libはこのリンク注釈の更新に使用しており、スライドの内容・数式・配置を変更しない。既存SVGの読み込みのため、Marp CLIの `--allow-local-files` を有効にしている。

ブラウザーを明示するときは、例えば次のように指定する。

```sh
npm run pdf:01 -- --browser edge
```

共通コマンドは `npm run pdf -- <原稿.md> <出力.pdf> [Marp CLIオプション]` 。次回以降も対応するパスを指定して使える。PDF内の教材リンクはこのリポジトリの `main` を参照するため、対応する教材もGitHubに保存する。

## 再生成後の確認

- 19ページ・16:9であり、全ページの文字・数式・図解に欠けや重なりがないこと。
- 日本語と2×2・3×3・4×4の行列が正しく表示され、TeXのソースが残っていないこと。
- 宿題・詳細解説などのリンクがGitHubの対応ページへ移動すること。
- 原稿・図解と生成PDFを同じPRに含め、PDFもコミットすること。

生成日時などのPDFメタデータは再生成のたびに変わる。ページの見た目を揃えるときは、原稿・依存バージョンに加えてOS・ブラウザー・フォントも揃える。

[この回のトップへ戻る](../README.md)

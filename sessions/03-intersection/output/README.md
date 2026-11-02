# 詳細解説PDF

[PDFを開く](explanation.pdf) ／ [ダウンロード](explanation.pdf?raw=true)

A4縦・ページ番号付き。[詳細解説Markdown](../explanation.md) の文章・表・数式・SVG10点・末尾の補足を省略せず収録している。本文は選択・検索できる。PDFはGitHubで閲覧・ダウンロードできる。

## 原本と更新方法

- 本文原本：[explanation.md](../explanation.md)
- 参照する図：[images/](../images/README.md)
- 生成物：このディレクトリの `explanation.pdf`

PDFを別原稿として編集しない。修正はMarkdown・SVGへ行い、共通コマンドで再生成する。

本文・SVGをmainへ反映した後、GitHub ActionsがPDFを再生成し、内容に変更のあるPDFだけをコミットする。対象一覧・手動実行・更新の仕組みは [共通運用文書](../../../docs/pdf-automation.md) を参照。手動生成したPDFをPRに含めることもできる。

## 必要環境と再生成

[共通の生成手順と必要環境](../../01-matrix/output/README.md) と同じNode.js 24.x・インストール済みChrome／Edge／Chromium・日本語フォントを使う。

リポジトリのルートで実行する。

```sh
npm ci
npm run pdf -- sessions/03-intersection/explanation.md sessions/03-intersection/output/explanation.pdf
```

PowerShellでは `npm.cmd` を使ってもよい。ブラウザーを自動検出できない場合は、生成コマンドに `--browser-path` と実行ファイルのパスを追加する。`--html-preview <file.html>` を加えると同じ本文の確認用HTMLも保存できる。HTMLとページの確認画像はリポジトリへ追加しない。

## 再生成後の確認

- 全ページを画像化し、日本語・数式・列ベクトル・SVG・表・改ページ・文字切れ・重なりを目視確認する。
- 本文・補足・図が欠けていないことを原本と照合し、テキストの選択・検索とページ番号を確認する。
- PDF内の教材リンクがGitHubの対応ファイルへ進むことを確認する。相対パスはMarkdownのまま維持し、生成時に共通処理がGitHubのmainへのリンクへ変換する。
- 固定例A〜Eの分母・符号・変数・交点／交線・結果分類を [宿題](../homework/README.md) と [C#実装例](../examples/README.md) に照らし合わせる。C#のDebug／Releaseビルド・155チェックと専用CIの成功も確認する。
- 原本のMarkdown・SVGをPRで更新する。mainへのマージ後にActionsの成功と生成PDFの全ページを確認する。手動生成PDFをPRに含める場合は同じ内容か照合し、生成コマンド・確認環境・表示結果を記載する。OS・ブラウザー・フォントが変わると紙面やページ数も変わりうる。

## 学習時の確認ポイント

共通点の条件からt・uの式が生まれる過程、uの負号が打ち消し合う理由、線分の両方の範囲と端点を追えるか確認する。同一直線の無限直線と有限な重複区間、零長・ゼロ法線と通常の平行／一致も区別して読む。

直線と平面の分母・位置条件、2平面の方向と通過点を別々に求める理由、交点・交線・平面一致の違いを確認する。数学では点として調べる零長線分を、サンプルコードでは入力制約として `Degenerate` にする方針と、数値的な判定保留の意味も照合する。


[この回のトップへ戻る](../README.md)

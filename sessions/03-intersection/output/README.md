# 第3回の詳細解説PDF（内容レビュー待ち）

[PDFを開く](explanation.pdf) ／ [ダウンロード](explanation.pdf?raw=true)

**教材PDFは公開中・自動更新対象（内容レビュー待ち）。** 利用者が全文を通読して手直しするための暫定PDFで、公開確定版ではない。[詳細解説Markdown](../explanation.md) の文章・表・数式・SVG10点・末尾の補足を省略せず収録する。A4縦・ページ番号付きで、本文は選択・検索できる。

## 原本と更新方法

- 本文原本：[explanation.md](../explanation.md)
- 参照する図：[images/](../images/README.md)
- 生成物：このディレクトリの `explanation.pdf`

PDFを別原稿として編集しない。修正はMarkdown・SVGへ行い、共通コマンドで再生成する。

**第3回PDFはGitHub Actionsによる自動更新対象です。** [対象一覧](../../../scripts/pdf-sessions.json) に登録済み。mainへのMarkdown・SVG反映後は [Build Explanation PDFs](../../../.github/workflows/build-explanation-pdfs.yml) が再生成し、内容に変更のあるPDFだけをコミットする。レビューPRでPDFを手動生成して添えることもできるが、必須ではない。

ルートREADMEの第3回は「準備中」を維持する。これは内容の最終レビュー・正式使用の準備状態であり、教材PDFの公開・自動更新とは別の扱い。正式承認とトップページの状態変更は後で判断する。

## 必要環境と再生成

[共通の生成手順と必要環境](../../01-matrix/output/README.md) と同じNode.js 24.x・インストール済みChrome／Edge／Chromium・日本語フォントを使う。動作確認環境はWindows、Node.js 24.21.0、npm 11.19.0、Microsoft Edge 156.0.4314.50、Yu Gothic・Meiryo。

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

今回の下書きは全16ページ。全ページを画像化して目視確認し、数式155個・SVG10点・末尾の補足、全37見出し、PDF内リンク9件、本文テキストの抽出とフォント8種類の埋め込みを確認した。更新後もその時点の全ページを再確認する。

## 通読レビューの観点

共通点の条件からt・uの式が生まれる過程、uの負号が打ち消し合う理由、線分の両方の範囲と端点を追えるか確認する。同一直線の無限直線と有限な重複区間、零長・ゼロ法線と通常の平行／一致も区別して読む。

直線と平面の分母・位置条件、2平面の方向と通過点を別々に求める理由、交点・交線・平面一致の違いを確認する。数学では点として調べる零長線分を、サンプルコードでは入力制約として `Degenerate` にする方針と、数値的な判定保留の意味も照合する。


[この回のトップへ戻る](../README.md)

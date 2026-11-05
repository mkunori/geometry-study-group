# 配布用PDF（4種類）

[詳細解説PDFを開く](explanation.pdf) ／ [ダウンロード](explanation.pdf?raw=true)

A4縦・ページ番号付き。[詳細解説Markdown](../explanation.md) の文章・表・数式・SVG10点・末尾の参考資料までを省略せず収録している。本文は選択・検索できる。PDFはGitHubで閲覧・ダウンロードできる。

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
npm run pdf:session -- sessions/05-geometry-check
```

既存の4つのPDFを更新する。`--browser-path <実行ファイル>` も指定できる。全4種類の生成・比較が成功するまで出力を置き換えず、日時だけの差分なら既存ファイルを維持する。

## 原本と更新方法

- 本文原本：[explanation.md](../explanation.md)
- 参照する図：[images/](../images/README.md)
- 生成物：このディレクトリの `explanation.pdf`

PDFを別原稿として編集しない。修正はMarkdown・SVGへ行い、共通コマンドで再生成する。

詳細解説PDFは、本文・SVGをmainへ反映した後にGitHub Actionsが再生成し、内容に変更がある場合だけコミットする。練習問題・解答・C#説明PDFを更新するときは、上記の共通コマンドで再生成し、原本と同じPRに含める。対象一覧・手動実行・更新の仕組みは [共通運用文書](../../../docs/pdf-automation.md) を参照。手動生成したPDFをPRに含めることもできる。

## 必要環境と再生成

[共通の生成手順と必要環境](../../01-matrix/output/README.md) と同じNode.js 24.x・インストール済みChrome／Edge／Chromium・日本語フォントを使う。

リポジトリのルートで実行する。

```sh
npm ci
npm run pdf -- sessions/05-geometry-check/explanation.md sessions/05-geometry-check/output/explanation.pdf
```

PowerShellでは `npm.cmd` を使ってもよい。ブラウザーを自動検出できない場合は `--browser-path` と実行ファイルのパスを追加する。`--html-preview <file.html>` を加えると同じ本文の確認用HTMLも保存できる。HTMLと確認画像はリポジトリへ追加しない。

## 再生成後の確認

- 全ページを画像化し、日本語・行列・数式・SVGのラベル・表・改ページ・文字切れ・重なりを目視確認する。
- 本文・補足・図・参考資料が欠けていないことを原本と照合し、テキストの選択・検索と全ページの番号を確認する。
- 別教材とGitHubへのリンクはPDF化の際だけ解除し、表示文字を残す。見出しジャンプ・外部の出典リンクを維持し、GitHub URLは末尾の元データ表記だけに1回存在することを確認する。[共通のリンクルール](../../../docs/pdf-automation.md#社内配布pdfのリンクと元データ表記) を参照。
- 固定例A〜Eの外積、境界先行のレイ計数、区間幅、内積射影、6面の外向き法線、共有面積・体積を [宿題](../homework/README.md) と [C#実装例](../examples/README.md) に照合する。C#のDebug／Release・全254チェックと専用CIの成功も確認する。
- 原本のMarkdown・SVGをPRで更新する。mainへのマージ後にActionsの成功と生成PDFの全ページを確認する。手動生成PDFをPRに含める場合は同じ内容か照合し、生成コマンド・確認環境・表示結果を記載する。OS・ブラウザー・フォントが変わると紙面やページ数も変わりうる。

末尾の短いまとめ段落には、その段落内の改ページを抑える書式を付けている。文章・数学的な内容は変えず、共通の生成スクリプトとCSSを利用する。

## 学習時の確認ポイント

点のInside／Boundary／Outsideと、領域どうしのSeparated／Touching／Overlappingを区別して読む。三角形の外積0だけで有限な辺上と決めないこと、逆順の符号、退化やInvalidの扱いを確認する。

凹L字では境界判定が先で、レイは下端を含め上端を除外する。通過頂点・極大・極小・水平辺の寄与を追い、AABB内でも形状外の点がある理由を確認する。

SATは両方の凸多角形の全辺法線が候補で、非単位軸の内積値を距離と混同しない。T1／T2の分離、T3との面積1/2、T4との点接触と、3D箱の体積1・面接触の違いを図と解答で確かめる。

3D半空間は正しく囲む凸形状が前提であり、今回のC#は軸平行凸箱に限定する。水密メッシュや外向き法線の説明を踏まえ、非凸・開いた一般メッシュやAutoCADの実際のソリッド干渉APIへ無条件に流用しない。許容誤差の境界帯と厳密な境界も区別する。


[この回のトップへ戻る](../README.md)

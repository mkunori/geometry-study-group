# 詳細解説PDF

[PDFを開く](explanation.pdf) ／ [ダウンロード](explanation.pdf?raw=true)

A4縦・ページ番号付き。[詳細解説Markdown](../explanation.md) の文章・表・数式・SVG10点・末尾の参考資料までを省略せず収録している。本文は選択・検索できる。PDFはGitHubで閲覧・ダウンロードできる。

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
npm run pdf -- sessions/05-geometry-check/explanation.md sessions/05-geometry-check/output/explanation.pdf
```

PowerShellでは `npm.cmd` を使ってもよい。ブラウザーを自動検出できない場合は `--browser-path` と実行ファイルのパスを追加する。`--html-preview <file.html>` を加えると同じ本文の確認用HTMLも保存できる。HTMLと確認画像はリポジトリへ追加しない。

## 再生成後の確認

- 全ページを画像化し、日本語・行列・数式・SVGのラベル・表・改ページ・文字切れ・重なりを目視確認する。
- 本文・補足・図・参考資料が欠けていないことを原本と照合し、テキストの選択・検索と全ページの番号を確認する。
- PDF内の教材リンクがGitHubの対応ファイルへ進むことを確認する。相対パスはMarkdownのまま維持し、生成時に共通処理がGitHubのmainへのリンクへ変換する。
- 固定例A〜Eの外積、境界先行のレイ計数、区間幅、内積射影、6面の外向き法線、共有面積・体積を [宿題](../homework/README.md) と [C#実装例](../examples/README.md) に照合する。C#のDebug／Release・全254チェックと専用CIの成功も確認する。
- 原本のMarkdown・SVGをPRで更新する。mainへのマージ後にActionsの成功と生成PDFの全ページを確認する。手動生成PDFをPRに含める場合は同じ内容か照合し、生成コマンド・確認環境・表示結果を記載する。OS・ブラウザー・フォントが変わると紙面やページ数も変わりうる。

末尾の短いまとめ段落には、その段落内の改ページを抑える書式を付けている。文章・数学的な内容は変えず、共通の生成スクリプトとCSSを利用する。

## 学習時の確認ポイント

点のInside／Boundary／Outsideと、領域どうしのSeparated／Touching／Overlappingを区別して読む。三角形の外積0だけで有限な辺上と決めないこと、逆順の符号、退化やInvalidの扱いを確認する。

凹L字では境界判定が先で、レイは下端を含め上端を除外する。通過頂点・極大・極小・水平辺の寄与を追い、AABB内でも形状外の点がある理由を確認する。

SATは両方の凸多角形の全辺法線が候補で、非単位軸の内積値を距離と混同しない。T1／T2の分離、T3との面積1/2、T4との点接触と、3D箱の体積1・面接触の違いを図と解答で確かめる。

3D半空間は正しく囲む凸形状が前提であり、今回のC#は軸平行凸箱に限定する。水密メッシュや外向き法線の説明を踏まえ、非凸・開いた一般メッシュやAutoCADの実際のソリッド干渉APIへ無条件に流用しない。許容誤差の境界帯と厳密な境界も区別する。


[この回のトップへ戻る](../README.md)

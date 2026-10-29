# 詳細解説PDFの保管先

TODO：[詳細解説Markdown](../explanation.md) の本文を完成させ、同じ内容のA4縦PDFを `explanation.pdf` に生成する。現在はPDF未生成。

本文原本は `explanation.md` とし、数式・表・SVG図解・補足を省略せずPDFに含める。PDFを別の原稿として編集しない。

[共通の生成手順と必要環境](../../01-matrix/output/README.md) を確認し、本文完成後にリポジトリのルートで実行する。

```sh
npm ci
npm run pdf -- sessions/03-intersection/explanation.md sessions/03-intersection/output/explanation.pdf
```

TODO：生成後は全ページの表示と本文の一致を確認し、この案内と各回のREADMEに実ファイルへの閲覧・ダウンロードリンクを追加する。生成したPDFもコミットする。

[この回のトップへ戻る](../README.md)

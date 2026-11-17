# 図形変換の配布用PDF

PDFは本文の別原本として編集せず、Markdownと参照するSVGから既存の共通処理で生成する。
社内配布の中心は詳細解説・練習問題・解答解説の3種類とし、実演コード自体はリポジトリで管理する。

## 原本と生成物の対応

| Markdown原本（セッション基準） | 生成物（セッション基準） |
| --- | --- |
| `explanation.md` | `output/explanation.pdf` |
| `homework/problems.md` | `output/problems.pdf` |
| `homework/solutions.md` | `output/solutions.pdf` |
| `examples/README.md` | `output/examples.pdf` |

表は完成時の保存先を示し、生成済みPDFの一覧ではない。
4種類目は実演の説明を収録し、実演コード全体の印刷は行わない。

[共通の生成手順と公開条件](../../../docs/pdf-automation.md) に従い、`npm ci` で固定依存を導入し、4原本から `npm run pdf:session -- sessions/autocad-transform` で生成する。
Node.js 24.x、日本語フォント、Chrome／Edge／Chromiumのいずれかを利用する。
全ページを画像化して、日本語・数式・SVG・表・コード・改ページ・文字切れを確認する。

表示確認だけの一時出力は、各原本を `npm run pdf -- <原本.md> <一時出力先.pdf>` に渡す。  
4種類をそれぞれ別の出力先へ生成し、原本を混ぜない。  
一時PDFの確認が終わっても、公開設定やルートの「準備中」は自動で変更しない。

この教材に `pdf-publish.json` は置かず、現段階ではPDF自動公開の対象にしない。
4原本の内容と全PDFの表示確認を終えてから、後続Issueで有効化する。
生成スクリプト・GitHub Actions・既存教材の公開マーカーは共通のものを維持する。

[詳細解説の本文原本](../explanation.md) ／ [実演の説明](../examples/README.md) ／ [この教材のトップへ戻る](../README.md)

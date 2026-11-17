# 図形変換の配布用PDF

社内配布の中心は、詳細解説・練習問題・解答解説の3種類である。  
実演の説明も補助資料として保存し、実演コード自体はリポジトリで管理する。

## PDFと本文原本

| 本文原本 | 配布用PDF |
| --- | --- |
| [詳細解説：全8章](../explanation.md) | [詳細解説PDF](explanation.pdf) |
| [練習問題](../homework/problems.md) | [練習問題PDF](problems.pdf) |
| [解答・解説](../homework/solutions.md) | [解答・解説PDF](solutions.pdf) |
| [実演の順序と確認項目](../examples/README.md) | [実演の説明PDF（補助資料）](examples.pdf) |

詳細解説は全8章を1冊に収録する。  
問題と解答は別冊とし、問題PDFには解答を含めない。  
実演の説明PDFは固定入力・期待値・停止候補・変数・適用条件を収録し、C#ソース全体の印刷は行わない。  
原本にあるAutoCAD参照DLLでのビルド・NETLOAD・実機確認の未検証事項は、PDFでも維持する。

## 生成と表示確認

PDFは本文の別原本として編集せず、Markdownと参照するSVGから既存の共通処理で生成する。  
[共通の生成手順と公開条件](../../../docs/pdf-automation.md) に従い、リポジトリのルートで実行する。

```sh
npm ci
npm run pdf:session -- sessions/autocad-transform
```

Node.js 24.x、日本語フォント、Chrome／Edge／Chromiumのいずれかを利用する。  
PowerShellでは `npm.cmd` でも実行できる。  
全ページを画像化して、日本語・句点改行・数式・SVG・表・コード・改ページ・文字切れを確認する。  
原本全文の収録、検索・選択可能な本文、問題と解答の分離、ページ番号、末尾の元データURLが1件であることも確かめる。

表示確認だけの一時出力は、各原本を `npm run pdf -- <原本.md> <一時出力先.pdf>` に渡す。  
4種類をそれぞれ別の出力先へ生成し、原本を混ぜない。

## PDFの自動更新

[公開マーカー](../pdf-publish.json) の `enabled: true` により、この教材の4種類を既存の自動更新対象に加える。  
mainへの反映後は、4原本と `images/` の更新を契機に、既存のBuild Explanation PDFsが再生成する。  
全対象の生成・比較に成功したときだけ、内容に差分があるPDFをコミットする。  
日時だけが変わる再生成では、既存のPDFをそのまま残す。

教材への案内はMarkdown原本へのリンクを維持し、配布・保存用のPDFはこのページから案内する。  
ルートREADMEの正式公開状態と、PDF自動更新の有効／無効は別に管理する。  
生成スクリプト・GitHub Actions・数学基礎編の公開マーカーは変更しない。

[この教材のトップへ戻る](../README.md)

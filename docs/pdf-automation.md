# 配布用PDFの生成と自動更新

## 各回4種類のPDF

各回の `output/` に詳細解説・練習問題・解答解説・C#実装例の説明を保存する。原本は分離したまま、すべて同じ [変換処理](../scripts/build-pdf.mjs) と印刷CSSで生成する。

| Markdown原本（セッション基準） | 出力（セッション基準） |
| --- | --- |
| `explanation.md` | `output/explanation.pdf` |
| `homework/problems.md` | `output/problems.pdf` |
| `homework/solutions.md` | `output/solutions.pdf` |
| `examples/README.md` | `output/examples.pdf` |

リポジトリのルートで、対象ディレクトリを指定する。回番号や総回数には依存しない。

```sh
npm ci
npm run pdf:session -- sessions/01-matrix
```

`sessions/01-matrix` は対象のセッションパスに置き換える。PowerShellでは `npm.cmd` でも実行できる。ブラウザーを明示する場合は `--browser-path <実行ファイル>` を追加する。

4種類を一時ファイルへ生成し、すべて成功してから内容に差分のあるPDFだけを出力する。生成・比較で失敗した場合は既存PDFを置き換えず、日時だけが異なる場合も既存のバイト列を維持する。1種類だけ生成する場合は従来の `npm run pdf -- <input.md> <output.pdf>` を使い、`--html-preview <file.html>` で表示確認用HTMLを保存できる。`pdf:01` は従来どおり第1回の詳細解説だけを生成する。

練習問題PDFは問題原本だけを収録し、解答を混入させない。解答PDFは小問番号・途中計算・根拠・注意点・参照SVGを原本と照合する。C#説明PDFはREADMEの固定入力と期待結果・数式とコードの対応・成立条件・誤差・検証と適用範囲を収録する。C#ソース全体やAutoCAD参考コードを別途PDF化しない。全種類ともA4縦・ページ番号付きで、本文の選択・検索、数式・SVG・表・コードブロックを維持する。表紙・版数・更新日・発行者情報は追加しない。

各回のREADMEと `output/README.md` から4種類へ直接移動できる。全ページを画像化して、日本語・数式・SVG・表・コード・改ページ・文字切れを確認する。問題と解答の番号・固定値の対応、原本全文の収録、末尾の元データURL1件も確認する。

自動更新を有効にした各回では、4原本と `images/` のSVGをPRで更新し、mainへマージする。`Build Explanation PDFs` が同じ変換基盤で4種類を生成し、内容に差分のある対象PDFだけを `github-actions[bot]` としてmainへコミットする。ローカルで表示確認したPDFを原本と同じPRに含めることもできる。

PDFはこれまでと同じ場所に保存するため、各回のREADMEから閲覧できる。ActionsのArtifactだけに置く運用にはしない。PDFを手で編集する必要はない。

## 自動実行と手動実行

[ワークフロー](../.github/workflows/build-explanation-pdfs.yml) はmainに次の変更が入ると起動する。

- `sessions/**/explanation.md`、`sessions/**/homework/problems.md`、`sessions/**/homework/solutions.md`
- `sessions/**/examples/README.md`、`sessions/**/images/**`、`sessions/**/pdf-publish.json`
- `scripts/**`（変換スクリプト、印刷CSS、対象検出、比較・コミット処理、テスト）
- `package.json`、`package-lock.json`
- ワークフロー自身

PDFだけの更新は起動対象に含めない。加えて、標準の `GITHUB_TOKEN` によるpushは通常、別のpushワークフローを起動しない。

手動の場合は、GitHubで **Actions → Build Explanation PDFs → Run workflow** を開き、**main** を選択して実行する。main以外を選んだ実行はジョブをスキップする。未信頼のPRコードはこのワークフローでは実行しない。

同じグループの処理は同時に走らせず、進行中の実行をキャンセルしない。各実行は開始時点の最新mainを取得する。生成中にmainが進んだ場合は、古い本文から作ったPDFをpushせずエラー終了する。最新mainで再実行する。

## 公開対象と新しい回の追加

[対象検出処理](../scripts/published-pdfs.mjs) が `sessions/` 直下のディレクトリを名前順で調べる。各回の `pdf-publish.json` が次の内容で、4原本の検査に成功した回だけを有効にする。原本と出力名の対応は [共通定義](../scripts/pdf-materials.mjs) を使い、一括生成と自動更新で揃える。

```json
{ "enabled": true }
```

マーカーがない回、または `{"enabled": false}` の回は対象外。READMEだけが存在する回や、4原本が揃っていても有効化していない回を自動公開しない。現在の第1〜5回はマーカーを配置済みで、計20PDFが対象となる。トップページの「準備中」は内容の最終レビュー・正式使用の準備状態を表し、PDF自動更新の有効／無効とは別に管理する。

- セッション名は英小文字・数字をハイフンで区切る。番号・総回数の前提はなく、`06-curves` や `advanced-topics` も使える。中央の固定一覧やスクリプトの変更は不要。
- マーカーは `enabled` という真偽値だけを持つJSONにする。不正JSON・未知の項目・文字列の `"true"` はエラーとする。
- 有効な回の4原本は通常ファイルで、見出しと本文が必要。欠落・空本文・見出しだけの原本、TODO/TBDを含む原本、未作成・作成予定・準備中・下書きというプレースホルダー行はエラーとする。数学・表示・リンクのレビューはPRで行う。
- 無効なディレクトリ名、シンボリックリンク／ジャンクション、ディレクトリ外を指す経路、重複する出力先は拒否する。原本・マーカー・既存の出力先もパスの各階層を検査する。
- 有効な回が0件なら生成・コミットせず正常終了する。無効化やマーカー削除によって、保存済みPDFを自動削除することはない。

新しい回を追加する手順：

1. `sessions/<session>/` に4原本・参照SVG・教材への案内を揃える。有効化マーカーはまだ置かない。
2. `npm run pdf:session -- sessions/<session>` で4種類を生成し、全ページ・原本全文・問題と解答の対応・リンクと末尾URLを確認する。このローカルコマンドは公開マーカーがなくても使える。
3. `pdf-publish.json` に `{"enabled": true}` を記載し、次の検出・全件生成コマンドとテストを実行する。原本の欠落・未完成を示すエラーがあれば修正する。
4. PDFが存在することを確認し、その回のREADMEと `output/README.md` に4種類のリンクを追加する。原本・図・マーカー・案内・表示確認したPDFを同じPRでレビューする。
5. レビュー後にmainへマージすると、対象を再検出して自動生成する。Actionsの生成対象ログ、20冊などその時点の全対象数、更新ファイル、全ページを確認する。同じ内容で再実行し、不要なコミットが生じないことも確認する。

```sh
npm ci
npm run test:pdf
npm run pdf:published
```

ブラウザーを明示する場合は `npm run pdf:published -- --browser-path <実行ファイル>` を使う。第1回だけを直接出力する既存の `npm run pdf:01`、任意の回の `npm run pdf -- <input.md> <output.pdf>` も引き続き利用できる。[必要環境と表示確認の手順](../sessions/01-matrix/output/README.md) も参照する。

## 社内配布PDFのリンクと元データ表記

原本Markdownのリンクは維持する。共通の [PDF用リンク処理](../scripts/pdf-links.mjs) が、4種類すべてのPDFの生成時に、本文だけに次のルールを適用する。

- 別の教材への相対リンクは解除し、表示文字や参照案内は本文に残す。元の画像は従来どおり埋め込む。
- GitHubとraw.githubusercontent.comへの参照先も解除する。URL自体が表示される場合は、回・教材の種類、Issue／PR番号などの読める参照に置き換える。HTMLリンクや本文・コード中のURL直書きも対象とする。
- 同一文書の `#見出し` と、同じファイルを明記した見出し参照はPDF内のジャンプとして維持する。外部の数学文献・Microsoft・Autodesk等の出典リンクは維持する。
- 「この回のトップへ戻る」等の戻る専用リンクは取り除く。本文と同じ段落にあっても、本文・図・他の参照内容は残す。

本文・補足・参考文献がすべて終わった後に、次の1行を各PDFへ1回だけ自動付加する。選択・検索・コピー可能で、URLからリポジトリを開ける。これ以外のGitHub URLはPDF本文・リンク注釈に残さない。Markdownに同じ案内を追記する必要はない。表示確認用HTMLにも、同じPDF用の変換結果が入る。

```text
資料の元データ（GitHubリポジトリ）： https://github.com/mkunori/geometry-study-group
```

再生成後は、本文と出典の欠落がないこと、末尾表記が重複しないこと、最後に空ページがないことを確認する。リンクの回帰テストは `npm run test:pdf` に含まれる。実PDFの本文・リンク注釈・しおり・末尾位置まで調べるテストには、使用するブラウザーを `PDF_TEST_BROWSER` で指定する。

```powershell
$env:PDF_TEST_BROWSER = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'
npm.cmd run test:pdf
```

ブラウザー未指定の場合は実PDFのテストだけをスキップし、既存の自動更新テストとリンクルールの単体テストを実行する。CIの実行順・サンドボックス設定は変更しない。

## 生成環境と不要な差分の抑制

- GitHub-hosted `ubuntu-24.04`、Node.js **24.21.0** を指定する。
- npm依存は `npm ci` とlockfileで固定する。Actions自体もコミットSHAで固定する。
- Chrome for Testingは、固定した `puppeteer-core` の `PUPPETEER_REVISIONS.chrome` が指定するバージョンをインストールする（現在 **148.0.7778.97**）。runnerに偶然入っているブラウザーには依存しない。
- `fonts-noto-cjk` と `fonts-liberation` を導入する。日本語の利用フォントを `fc-match` でログに出す。本文・数式・SVGは第1回と同じ変換処理を使う。

[公開対象の生成処理](../scripts/build-published-pdfs.mjs) は一時ファイルに生成し、[比較処理](../scripts/published-pdfs.mjs) で既存PDFと比較する。比較用コピーではCreationDate・ModDate・trailer IDを取り除き、ブラウザーが付け直すアクセシビリティ用のノード名を、表ヘッダーの参照先・名前一覧も含めて一貫した名前に揃える。本文、描画データ、リンク、見出し、タグの構造や参照関係は比較対象に残す。PDFの後処理用に固定バージョンのpdf-libを使うが、教材を別方式で描画し直すことはしない。

比較が一致したときは**既存PDFのバイト列をそのまま残す**ため、日時や内部識別子だけの変更ではコミットしない。実際に内容が異なったときだけ、ブラウザーが生成したPDFを保存する。比較用に正規化したPDFを公開することはない。全対象の生成・比較が成功するまで既存PDFを置き換えず、いずれかが失敗した実行ではコミットしない。

OS・日本語フォント・ブラウザーによって改ページや描画が変わるため、初回の自動実行でWindows製の既存PDFが更新される場合がある。フォントパッケージやrunnerの更新でも実質的な差分が生じ得る。その場合は生成された全ページを確認する。ページ数は環境によって変わり得る。

## UbuntuのChromeサンドボックス

Ubuntu 24.04ではAppArmorが未許可の実行ファイルによるuser namespaceの作成を制限する。runnerの一時ディレクトリに取得したChrome for Testingは、通常のChromeインストール先に付く許可の対象外で、`No usable sandbox!` により起動に失敗する場合がある。

ワークフローでは、取得したChromeの**完全な実行ファイルパスだけ**に `userns` を許可する、root所有のAppArmorプロファイルを一時runner内に作成・読み込みする。`flags=(unconfined)` はAppArmor側の追加のファイル制限を課さずにuser namespaceを許可するための指定であり、Chrome自身のサンドボックスを無効にする指定ではない。実行前に `chrome://sandbox` でLayer 1 SandboxがNamespaceであること、PID・Network namespacesおよびSeccomp-BPF sandboxが有効であることを検証し、不成立ならPDF生成・コミットへ進まない。

`--no-sandbox`、setuidヘルパーの追加、システム全体のuser namespace制限の解除は行わない。この設定は信頼済みmain専用のGitHub-hostedジョブ内だけに適用し、ローカルWindowsの起動設定は変更しない。対象パスの実行ファイルを置き換えられるコードも同じ許可を得るため、未信頼のPRをこのジョブで実行しないこと。GitHub側に新しいSecretや権限設定は不要で、runnerの `sudo` と `apparmor_parser` を使う。

AppArmorの読み込みで失敗した場合はそのステップのログを確認する。サンドボックスを無効にして処理を続行するフォールバックは用意しない。[Chromiumの解説と個別プロファイルの方式](https://chromium.googlesource.com/chromium/src/+/main/docs/security/apparmor-userns-restrictions.md) および [PuppeteerのUbuntu向け注意事項](https://pptr.dev/troubleshooting#issues-with-apparmor-on-ubuntu) を参照する。

## GitHub側で確認する設定

1. **Settings → Actions → General** でActionsを許可する。組織ポリシーがある場合は公式の `actions/checkout`、`actions/setup-node` が許可されているか確認する。
2. **Workflow permissions** は既定のRead-onlyを基本にする。書き込みはワークフローのmain専用ジョブにある `permissions: contents: write` と、checkoutが設定する `GITHUB_TOKEN` を使う。PATや追加Secretは不要。ポリシーで拒否される場合に限り、管理者が `Read and write permissions` への変更の要否を確認する。
3. **Settings → Rules → Rulesets**／**Branches** でmainへのBot pushが許可されるか確認する。保護設定をこの実装から変更・回避することはない。許可できない場合は運用に合った設定調整か、PDF更新をPRにする方式への変更を検討する。
4. この変更をmainへマージ後、**Actions → Build Explanation PDFs → Run workflow** で実行する。初回のPDF更新後に同じ内容で再実行し、不要な新規コミットがないことを確認する。

## ログとエラー時の確認

- `Published PDF targets:` に検出したPDF件数、`Build:` に原本とPDF種別が分かる出力パス、`PDF ready:` に生成結果、`Updated:` に更新先、`Unchanged` に比較一致したファイルが出る。最後に全対象の生成数と更新数を表示する。
- 差分なしなら `No PDF changes. No commit needed.` と表示して正常終了する。
- 変換や比較の失敗時は該当ステップと対象Markdownを確認する。TeXエラー、画像の欠落、横幅超過などを直し、再実行する。
- push拒否時はGitのエラーと、トークン権限・main保護・同時更新を確認する案内が出る。force pushやPATへの切り替えは行わない。原因を解消して最新mainで再実行する。
- コミット処理は公開対象のPDFパスだけをstageし、既存のstage済み変更やmain以外のブランチを拒否する。ソース・lockfile・未公開の回は自動コミットしない。

ローカルの回帰テストは `npm run test:pdf`。一時PDFと一時Gitリポジトリを使い、対象数の増減・未完成原本と無効経路の拒否・各原本単独の変更・メタデータ差分の抑制・後半の回での失敗・4PDFの追加・PDF限定コミット・main制約・push拒否を確認する。実際のリポジトリのmainにはpushしない。

GitHub Actionsの挙動については、[ワークフローの構文](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) と [GITHUB_TOKENでのトリガー](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow) を参照する。

[勉強会トップへ戻る](../README.md)

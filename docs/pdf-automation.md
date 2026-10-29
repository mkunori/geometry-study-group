# 詳細解説PDFの自動更新

本文原本は各回の `explanation.md`。通常はMarkdownと `images/` のSVGをPRで更新し、mainへマージする。`Build Explanation PDFs` が既存の変換スクリプトでPDFを生成し、変更された `output/explanation.pdf` だけを `github-actions[bot]` としてmainへコミットする。

PDFはこれまでと同じ場所に保存するため、各回のREADMEから閲覧できる。ActionsのArtifactだけに置く運用にはしない。PDFを手で編集する必要はない。

## 自動実行と手動実行

[ワークフロー](../.github/workflows/build-explanation-pdfs.yml) はmainに次の変更が入ると起動する。

- `sessions/**/explanation.md`、`sessions/**/images/**`
- `scripts/**`（変換スクリプト、印刷CSS、公開対象一覧、比較・コミット処理、テスト）
- `package.json`、`package-lock.json`
- ワークフロー自身

PDFだけの更新は起動対象に含めない。加えて、標準の `GITHUB_TOKEN` によるpushは通常、別のpushワークフローを起動しない。

手動の場合は、GitHubで **Actions → Build Explanation PDFs → Run workflow** を開き、**main** を選択して実行する。main以外を選んだ実行はジョブをスキップする。未信頼のPRコードはこのワークフローでは実行しない。

同じグループの処理は同時に走らせず、進行中の実行をキャンセルしない。各実行は開始時点の最新mainを取得する。生成中にmainが進んだ場合は、古い本文から作ったPDFをpushせずエラー終了する。最新mainで再実行する。

## 公開対象と新しい回の追加

[scripts/pdf-sessions.json](../scripts/pdf-sessions.json) が公開対象の明示的な一覧。現在は `01-matrix` だけを指定している。第2〜5回はTODOのひな形なので生成しない。

本文・SVGの制作と表示確認が済んだ回を追加する際は、次の手順を使う。

1. `sessions/<回>/explanation.md` と必要なSVGを完成させる。
2. 一覧に `02-vector` のようなディレクトリ名を追加する。入力は `sessions/<回>/explanation.md`、出力は `sessions/<回>/output/explanation.pdf` に自動対応する。
3. ローカルで次のコマンドを実行し、全ページの日本語・数式・図解・リンクを確認する。
4. PRをmainにマージすると、自動生成・コミットされる。PDFが存在することを確認してから、その回のREADMEと `output/README.md` に閲覧リンクを追加する。ローカルで確認したPDFを同じPRに含めることもできる。

```sh
npm ci
npm run test:pdf
npm run pdf:published
```

ブラウザーを明示する場合は `npm run pdf:published -- --browser-path <実行ファイル>` を使う。第1回だけを直接出力する既存の `npm run pdf:01`、任意の回の `npm run pdf -- <input.md> <output.pdf>` も引き続き利用できる。[必要環境と表示確認の手順](../sessions/01-matrix/output/README.md) も参照する。

## 生成環境と不要な差分の抑制

- GitHub-hosted `ubuntu-24.04`、Node.js **24.21.0** を指定する。
- npm依存は `npm ci` とlockfileで固定する。Actions自体もコミットSHAで固定する。
- Chrome for Testingは、固定した `puppeteer-core` の `PUPPETEER_REVISIONS.chrome` が指定するバージョンをインストールする（現在 **148.0.7778.97**）。runnerに偶然入っているブラウザーには依存しない。
- `fonts-noto-cjk` と `fonts-liberation` を導入する。日本語の利用フォントを `fc-match` でログに出す。本文・数式・SVGは第1回と同じ変換処理を使う。

[公開対象の生成処理](../scripts/build-published-pdfs.mjs) は一時ファイルに生成し、[比較処理](../scripts/published-pdfs.mjs) で既存PDFと比較する。比較用コピーではCreationDate・ModDate・trailer IDを取り除き、ブラウザーが付け直すアクセシビリティ用のノード名を、表ヘッダーの参照先・名前一覧も含めて一貫した名前に揃える。本文、描画データ、リンク、見出し、タグの構造や参照関係は比較対象に残す。PDFの後処理用に固定バージョンのpdf-libを使うが、教材を別方式で描画し直すことはしない。

比較が一致したときは**既存PDFのバイト列をそのまま残す**ため、日時や内部識別子だけの変更ではコミットしない。実際に内容が異なったときだけ、ブラウザーが生成したPDFを保存する。比較用に正規化したPDFを公開することはない。全対象の生成・比較が成功するまで既存PDFを置き換えず、いずれかが失敗した実行ではコミットしない。

OS・日本語フォント・ブラウザーによって改ページや描画が変わるため、初回の自動実行でWindows製の既存PDFが更新される場合がある。フォントパッケージやrunnerの更新でも実質的な差分が生じ得る。その場合は生成された全ページを確認する。ページ数は環境によって変わり得る。

## GitHub側で確認する設定

1. **Settings → Actions → General** でActionsを許可する。組織ポリシーがある場合は公式の `actions/checkout`、`actions/setup-node` が許可されているか確認する。
2. **Workflow permissions** は既定のRead-onlyを基本にする。書き込みはワークフローのmain専用ジョブにある `permissions: contents: write` と、checkoutが設定する `GITHUB_TOKEN` を使う。PATや追加Secretは不要。ポリシーで拒否される場合に限り、管理者が `Read and write permissions` への変更の要否を確認する。
3. **Settings → Rules → Rulesets**／**Branches** でmainへのBot pushが許可されるか確認する。保護設定をこの実装から変更・回避することはない。許可できない場合は運用に合った設定調整か、PDF更新をPRにする方式への変更を検討する。
4. この変更をmainへマージ後、**Actions → Build Explanation PDFs → Run workflow** で実行する。初回のPDF更新後に同じ内容で再実行し、不要な新規コミットがないことを確認する。

## ログとエラー時の確認

- `Build:` に処理対象、`Updated:` に更新先、`Unchanged` に比較一致したファイルが出る。最後に生成数と更新数を表示する。
- 差分なしなら `No PDF changes. No commit needed.` と表示して正常終了する。
- 変換や比較の失敗時は該当ステップと対象Markdownを確認する。TeXエラー、画像の欠落、横幅超過などを直し、再実行する。
- push拒否時はGitのエラーと、トークン権限・main保護・同時更新を確認する案内が出る。force pushやPATへの切り替えは行わない。原因を解消して最新mainで再実行する。
- コミット処理は公開対象のPDFパスだけをstageし、既存のstage済み変更やmain以外のブランチを拒否する。ソース・lockfile・未公開の回は自動コミットしない。

ローカルの回帰テストは `npm run test:pdf`。一時PDFと一時Gitリポジトリを使い、差分なし・本文やリンクの変更・生成失敗・PDF限定コミット・push拒否を確認する。実際のリポジトリのmainにはpushしない。

GitHub Actionsの挙動については、[ワークフローの構文](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) と [GITHUB_TOKENでのトリガー](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow) を参照する。

[勉強会トップへ戻る](../README.md)

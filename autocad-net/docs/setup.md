# AutoCAD参考コードのセットアップと読む順序

**参考コード／ビルド・NETLOAD・実機動作は未検証。** 以下は対象環境を持つ利用者が確認する場合の手順で、本リポジトリで実施済みの検証結果ではない。コード読解だけならAutoCADやDLLの配置は不要。

## 対象の製品・更新版を確認する

Windows版AutoCAD 2026で `ABOUT` を実行し、製品名・バージョン・更新情報を確認する。**Update 1.2（2026.1.2）以降**を対象とし、AutoCAD 2026という製品名だけで判断しない。表示内容をAutodeskの製品更新情報と照合する。ライセンス情報やシリアル番号はGitやPRへ貼らない。

[2026版互換表](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-Customization/files/GUID-A6C680F2-DE2E-418A-A182-E4884073338A.htm) はUpdate 1.2以降を.NET 10、初期版〜Update 1.1を.NET 8としている。後者はこの `net10.0` プロジェクトの対象外。関連Toolset等を共存させる場合も、各製品の対応更新版を個別に確認する。AutoCAD LT・Mac版・AutoCAD 2027用の例として扱わない。

[Visual Studioの2026版案内](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-450FD531-B6F6-4BAE-9A8C-8230AAC48CB4.htm) では、AutoCAD 2026.1.2以降にVisual Studio 2026 version 18.0と.NET 10を挙げている。2026-11-01の静的確認時点では本教材の想定と一致する。一方、更新前の行にはVisual Studio 2022／.NET 8があるため混同しない。公式の環境案内との一致は、この参考コードのビルド・ロード成功を意味しない。今後資料が変わった場合は差分を明記して再確認する。

## Visual StudioとローカルDLL参照を設定する

1. .NET 10 SDKと、それを扱えるVisual Studio 2026の.NET開発環境を用意する。
2. 対象AutoCAD／その更新版に一致するObjectARX／Managed .NET SDK、または対象製品の参照用DLLを正規の入手元から用意する。**3 DLLは同じ製品・更新版に対応する組**とし、別年・別更新版を混ぜない。製品・SDKはリポジトリ外に置く。
3. [テンプレート](../AutoCadGeometrySamples.local.props.example) を同じディレクトリの `AutoCadGeometrySamples.local.props` へコピーする。コピー先の `AutoCADManagedDir` に、3 DLLがあるディレクトリを記入する。空欄のままでは参照未設定。
4. 実パスを記入したファイルはGit管理外であることを `git status` 等で確認する。テンプレート・csprojへPC固有パスを書き戻さない。XMLの `&` などがパスに含まれる場合は適切にエスケープする。
5. [AutoCadGeometrySamples.sln](../AutoCadGeometrySamples.sln) を開き、構成をRelease・x64にする。単一のClass Libraryを使い、章ごとのプロジェクトは作らない。
6. 参照の `AcCoreMgd`・`AcDbMgd`・`AcMgd` が指定したDLLを指し、**Copy Local=false** であることを確認する。csprojでは各参照の `Private=false` に対応する。
7. 利用者がビルドする場合はCtrl+Shift+Bを使い、出力ウィンドウで実際の出力先を確認する。今回、この操作もビルドも実施していない。AutoCADの実行ファイルとしてF5や `dotnet run` で起動するプロジェクトではない。

| ローカル参照 | 主な役割 |
| --- | --- |
| `AcCoreMgd.dll` | コマンド・Editor等のコア機能 |
| `AcDbMgd.dll` | DatabaseServices・Geometry等の図面データ |
| `AcMgd.dll` | ApplicationServices等のアプリケーション機能 |

[DLL構成とCopy Localの公式案内](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-8657D153-0120-4881-A3C8-E00ED139E0D3.htm) に沿い、AutoCADが提供するDLLを成果物へ重複コピーしない。SDKの参照用DLLは製品本体の実行用DLLと同じ配布物とは限らず、NETLOAD先へSDK DLLを持ち込まない。

設定未記入なら `AutoCADManagedDirが未設定です` 、ファイルがなければ各DLL名を含むエラーをビルド定義で出す構成にしている。**このエラー処理も実行未検証**。存在確認だけでは製品年・更新版・DLL互換性を保証しない。参照警告、型解決エラー、読み込み失敗があれば、ローカル設定の読み込み、3 DLLの組、対象更新版、.NET SDK、x64を確認する。別年版への差し替えやセキュリティ無効化で回避しない。

## NETLOADと期待動作を確認する場合

1. 対象AutoCADで専用の空図面を開き、モデルタブを使う。`GEO00_LINE` は図面DBを変更するため、業務図面で試さない。
2. 自分のプラグインDLLだけを、組織の運用で承認されたロード用ディレクトリへ配置する。AutoCADの3 DLLやSDKを一緒にコピーしない。
3. `SECURELOAD` と `TRUSTEDPATHS` を確認し、管理者の方針に沿う信頼済みの場所から読み込む。`SECURELOAD` を0にしたり、ドライブ全体や不特定の書込可能ディレクトリを信頼したりする回避策は使わない。[NETLOADの公式案内](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-Customization/files/GUID-D790D6DA-4592-4C58-910F-42E4AA9EA982.htm) を参照する。
4. `NETLOAD` で出力した **AutoCadGeometrySamples.dll** を選ぶ。`AcMgd.dll` 等のホスト側DLLを選ばない。
5. `GEO00_HELLO` の期待は、コマンドラインへの案内表示で、図面変更はない。
6. `GEO00_LINE` の期待は、モデル空間のWCS (0,0,0)→(4,0,0)のLine 1本と追加メッセージ。長さは4作図単位で、mm等の単位を仮定しない。必要なら画面表示・プロパティで座標を確かめる。UCSの向きによって画面の見え方は変わるが、この例の入力点はWCSの固定値である。

これらは**期待動作のみで、実機での確認結果ではない**。再実行すると同じ場所へ図形が増える。コードは図面を自動保存せず、失敗時にも図面状態を確認する。ロード後のDLL差し替えができない場合はAutoCADを終了してから入れ替え、再起動して読み込む運用を利用者側で検討する。常駐AutoCADや別プロセスをこの教材から自動起動しない。

コマンドが見つからない場合は、ロード時のエラー、選んだDLL、製品更新版と参照DLL、公開クラスの `CommandMethod` 名を確認する。`GEO00_...` も他プラグインとの衝突がないか確認する。

## 図面DBと後始末を読む

`CommonCommands.AddLine` は、次の順で現在の図面のモデル空間を編集する。

1. `Application.DocumentManager.MdiActiveDocument` から `Document` を得る。図面がなければ戻る。`Document.Database` が図形データ、`Document.Editor` がメッセージの窓口。
2. `Database.TransactionManager.StartTransaction()` で処理範囲を作る。
3. `GetObject(..., OpenMode.ForRead)` で `BlockTable` を開き、その `BlockTableRecord.ModelSpace` をForWriteで開く。現在のペーパー空間へ追加する例ではない。
4. WCSの2点から新規 `Line` を作り、`AppendEntity` でモデル空間・DBへ追加する。
5. DBへ追加済みの新規オブジェクトを `AddNewlyCreatedDBObject(line, true)` でTransactionへ登録する。この順序を逆にしない。
6. `Commit()` でDB変更を確定し、`using` で新規LineのラッパーとTransactionを破棄する。

`Document`・`Database` はAutoCADが管理するものを利用し、ここでは破棄しない。`GetObject` で開いた既存BlockTable／BlockTableRecordはTransactionが管理する。新規LineとTransactionには `using` を付ける。LineのDisposeは、CommitしたDB上の図形を削除する操作ではない。

Commit前の例外では `using` を抜ける際のTransaction破棄で未確定変更が戻る。AutoCADの例外はメッセージに残すが、Commit後の処理の失敗まで「図面変更なし」と断定しない。その他の例外が伝播する場合も `using` による後始末は行われる。APIと所有権は [公式リファレンス](references.md) のTransaction／Disposeの項で照合できる。

この例はSessionフラグを使わない現在図面の通常コマンドなので、明示的な `DocumentLock` を追加していない。[ドキュメントロックの公式案内](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-A2CD7540-69C5-4085-BCE8-2A8ACE16BFDD.htm) が区別する、モデルレスUI・別図面・Sessionコマンド等へ流用するときは別のロック設計が必要になる。

## 各章を追加するとき

`GEO00_...` は共通導入、`GEO01_...` 〜 `GEO05_...` は対応章に予約する。後続Issueで `samples/NN-.../` にソースを追加し、同じClass Libraryの `samples/**/*.cs` として扱う。新たな章は数学教材への対応・固定値・単位・座標系・期待動作・副作用を記す。

後続でもビルド・ロード・実機実行・自動テスト・GitHub Actionsは行わず、公式資料への静的照合と未確認箇所の注記に限定する。AutoCAD DLL、SDK、CHM、ライセンス情報をコミットしない。既存 `sessions/` の数学教材とそのCIへ参照を追加しない。

[参考コードトップへ戻る](../README.md) ／ [公式リファレンス](references.md)

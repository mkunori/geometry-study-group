# AutoCAD 2026の公式リファレンスと静的照合

確認日：2026-11-01。以下はAutoCAD **2026版URL** で確認した対象・署名・規約の一覧。公式資料との静的照合であり、参照DLLを用いたビルド、NETLOAD、AutoCADでの実行は行っていない。URLが同じでも内容が更新されうるため、利用者の製品更新版と再照合する。

## 対象環境・参照DLL・ロード

| 2026版の公式ページ | 確認した事項 |
| --- | --- |
| [Managed .NET Compatibility](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-Customization/files/GUID-A6C680F2-DE2E-418A-A182-E4884073338A.htm) | AutoCAD 2026 Update 1.2以降は.NET 10、初期版〜Update 1.1は.NET 8。製品名だけでなく更新版を限定する根拠 |
| [Which Edition of Microsoft Visual Studio to Use](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-450FD531-B6F6-4BAE-9A8C-8230AAC48CB4.htm) | 2026.1.2以降の行にVisual Studio 2026 version 18.0／.NET 10。確認時点で想定環境と一致し、更新前のVS2022／.NET 8とは区別 |
| [Developer and ObjectARX Help](https://help.autodesk.com/view/OARX/2026/ENU/) | AutoCAD 2026のManaged .NET Developer's Guide／Reference GuideとSDK案内への入口 |
| [Components of the AutoCAD .NET API](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-8657D153-0120-4881-A3C8-E00ED139E0D3.htm) | AcCoreMgd／AcDbMgd／AcMgdの役割、製品・SDKからの参照、Copy Local=false。参照用DLLをプラグインへ重複コピーしない |
| [ABOUT](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-Core/files/GUID-CDBAD44E-F661-430C-A99E-192B83D41C10.htm) | 製品情報にバージョン・サービスパック等が含まれる。利用者が更新版を確認する入口 |
| [To Load a Managed .NET Application](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-Customization/files/GUID-D790D6DA-4592-4C58-910F-42E4AA9EA982.htm) | NETLOADで自作DLLを選ぶ手順。SECURELOADとTRUSTEDPATHSによる信頼済みの場所を利用し、保護を無効化しない |

上記の開発環境表との一致を、このプロジェクトの動作保証へ読み替えない。SDK内の3 DLLの実物・ファイル版・依存関係は未確認で、ローカル設定時に対象製品と一致する組を利用者が確認する。2027版のSDKや資料を2026の根拠にはしていない。

## CommonCommandsで使用するAPI

名前空間は `Autodesk.AutoCAD.Runtime` 、 `Autodesk.AutoCAD.ApplicationServices` 、 `Autodesk.AutoCAD.DatabaseServices` 、 `Autodesk.AutoCAD.Geometry` 、 `Autodesk.AutoCAD.EditorInput` 。コードの `AcApplication` はApplicationServicesの `Application` の別名で、別APIや別製品を指さない。

| 2026版の公式ページ | 対象とC#側の形 | 照合した内容 |
| --- | --- | --- |
| [CommandMethodAttribute(string)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Runtime_CommandMethodAttribute_CommandMethodAttribute_string.html) | `CommandMethodAttribute(string globalName)` | 引数1つの属性にGEO00_HELLO／GEO00_LINEを指定。公開クラスの公開static voidメソッドとして記述 |
| [Editor.WriteMessage(string)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_EditorInput_Editor_WriteMessage_string.html) | `void WriteMessage(string message)` | Editorへのメッセージ。文字列補間後もstringのオーバーロードを使う |
| [Create a Line Object](https://help.autodesk.com/cloudhelp/2026/PTB/OARX-DevGuide-Managed/files/GUID-47E8A12E-2ED4-4E78-ADA3-AAC9B4223C3C.htm) | DocumentManager／MdiActiveDocument、Document.Database、BlockTable、ModelSpace | 公式C#例の現在図面取得、読取／書込の開き方とLine追加順序。URLの言語はPTBだが2026版のC#例。教材は座標とコマンド名を独自に設定し、公式例の図形そのものは再現しない |
| [Line(Point3d, Point3d)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Line_Line_Point3d_Point3d.html) | `Line(Point3d pointer1, Point3d pointer2)` | 始点・終点はどちらもWCS。教材の(0,0,0)→(4,0,0)を直接渡し、UCS変換は行わない |
| [StartTransaction](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_TransactionManager_StartTransaction.html) | `Transaction StartTransaction()` | Database.TransactionManagerからTransactionを開始 |
| [GetObject(ObjectId, OpenMode)](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Transaction_GetObject_ObjectId_OpenMode.html) | `DBObject GetObject(ObjectId id, OpenMode mode)` | 戻り値をBlockTable／BlockTableRecordへキャストし、ForRead／ForWriteを使い分ける |
| [AppendEntity](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_BlockTableRecord_AppendEntity__CallerMustClose__Entity.html) | `ObjectId AppendEntity(Entity entity)` | EntityをDBとブロックテーブルレコードへ追加。Lineは新規Entityとして渡す |
| [AddNewlyCreatedDBObject](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Transaction_AddNewlyCreatedDBObject_DBObject__MarshalAsUnmanagedType_U1__bool.html) | `void AddNewlyCreatedDBObject(DBObject obj, bool add)` | objは新規かつDBへ追加済みが前提。AppendEntity後にtrueで登録する。公式署名のマーシャリング属性は呼出し引数へ書かない |
| [Commit](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Transaction_Commit.html) | `void Commit()` | Transaction中の変更を確定する。図面ファイルを保存する呼出しではない |

## 所有権・例外・呼出しコンテキスト

| 2026版の公式ページ | 照合した事項 |
| --- | --- |
| [Use Transactions to Access and Create Objects](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-50FD6118-B2D1-4313-A7D6-830794DFDEFA.htm) | GetObjectで開く既存オブジェクトはTransactionが管理し、CommitせずDisposeすると変更が戻る。DBへ追加した新規Entityの登録と確定の順序 |
| [Dispose Objects](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-9DFB5767-F8D6-4A88-87D6-9676C0189369.htm) | Transactionと新規DBObjectはusing等で破棄し、Transactionで開いた既存DBオブジェクトの破棄とは責務を区別する |
| [Lock and Unlock a Document](https://help.autodesk.com/cloudhelp/2026/ENU/OARX-DevGuide-Managed/files/GUID-A2CD7540-69C5-4085-BCE8-2A8ACE16BFDD.htm) | Sessionを使わない現在図面の通常コマンドと、モデルレスUI・別図面・Session等での明示的なロックを区別。今回のコードは前者だけ |

参考コードはAutoCADの例外をEditorへ表示し、Transactionと新規Lineはusingで後始末する。例外を発生させた実験やロールバックの実機確認は行っていない。Commit後の後処理の失敗まで巻き戻るとは説明しない。

TODO：利用者の実際のDLL・製品更新版との整合、Visual Studioでのビルド、NETLOAD、コマンド登録、メッセージ・Line追加・例外時の図面状態は未確認。本Issueでは静的照合までとし、これらの実機確認・自動テストは完了条件に含めない。

[参考コードトップへ戻る](../README.md) ／ [セットアップ](setup.md) ／ [共通コード](../samples/00-common/CommonCommands.cs)

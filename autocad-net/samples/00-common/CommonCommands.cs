using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCadGeometrySamples;

/// <summary>AutoCAD内で呼び出す導入コマンド。公式資料と静的照合した参考コードで、実機未検証。</summary>
/// <remarks>
/// 対象はWindows版AutoCAD 2026 Update 1.2以降と.NET 10。
/// 通常のドキュメントコマンドとして現在の図面を扱い、Session・モデルレス・別図面の操作は行わない。
/// </remarks>
public static class CommonCommands
{
    /// <summary>現在の図面のEditorへ案内を表示する。図形や図面設定は変更しない。</summary>
    /// <remarks>引数なし。図面がなければ戻る。期待動作はメッセージ表示だが、NETLOAD・実行とも未検証。</remarks>
    [CommandMethod("GEO00_HELLO")]
    public static void Hello()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;

        document.Editor.WriteMessage("\nGEO00_HELLO: 幾何アルゴリズム教材のAutoCAD参考コードです。");
    }

    /// <summary>WCSの(0,0,0)から(4,0,0)へのLineを、現在の図面のモデル空間へ1本追加する。</summary>
    /// <remarks>
    /// 引数なし。長さは4作図単位で、mmなどの物理単位は仮定しない。UCSからの座標変換は行わない。
    /// 成功時に図面DBが変わり、繰り返すと同じ位置へ追加される。図面の保存は行わない。
    /// ビルド・ロード・実行は未検証。利用者が確認する場合は専用の空図面を使う。
    /// </remarks>
    [CommandMethod("GEO00_LINE")]
    public static void AddLine()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;

        Database database = document.Database;
        Editor editor = document.Editor;
        try
        {
            // 現在図面の通常コマンド内なので、Sessionや別図面用のDocumentLockとは区別する。
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blocks = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 2点はWCSの位置。差(4,0,0)は変位で、第5回の三角形ABCの辺ABにも対応する。
                Point3d start = new(0, 0, 0);
                Point3d end = new(4, 0, 0);
                using (Line line = new(start, end))
                {
                    // DBへの追加を先に行い、その新規DBオブジェクトをTransactionへ登録する。
                    modelSpace.AppendEntity(line);
                    transaction.AddNewlyCreatedDBObject(line, true);
                    transaction.Commit();
                }
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        {
            // Commit前の例外ならusingによるTransactionの破棄でロールバックする。
            // Commit後の例外まで「必ず変更なし」とは扱わず、図面の状態を確認できる情報を残す。
            editor.WriteMessage($"\nGEO00_LINE: AutoCADの処理中に例外が発生しました。図面状態を確認してください。{error.Message}");
            return;
        }

        editor.WriteMessage("\nGEO00_LINE: モデル空間へWCS (0,0,0)→(4,0,0)の線分を追加しました。");
    }
}

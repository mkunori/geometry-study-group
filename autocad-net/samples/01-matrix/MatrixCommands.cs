using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCadGeometrySamples;

/// <summary>第1回の列方式の同次変換をAutoCAD APIへ対応させる参考コード。公式資料との静的照合のみ。</summary>
/// <remarks>
/// Windows版AutoCAD 2026 Update 1.2以降／.NET 10想定。ビルド未検証・NETLOAD未実施・AutoCAD実機未検証。
/// 固定入力は右手系・作図単位。現在のUCSや画面方向には依存させず、保存・既存図形の編集は行わない。
/// </remarks>
public static class MatrixCommands
{
    /// <summary>点と方向を原点周りに+90度回転して移動し、変換前後の新規Lineを2本追加する。</summary>
    /// <remarks>
    /// 引数なし。点(2,1,0)→(-1,2,0)→(9,2,3)、方向(2,1,0)→(-1,2,0)が数学上の期待値。
    /// モデル空間の既存図形は変更しない。再実行は重複追加となる。表示・図面への反映とも実機未検証。
    /// </remarks>
    [CommandMethod("GEO01_TRANSFORM")]
    public static void Transform()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;

        Point3d point = new(2, 1, 0);
        Vector3d direction = new(2, 1, 0);
        Matrix3d rotation = RotationAboutWorldZ();
        Matrix3d translation = Matrix3d.Displacement(new Vector3d(10, 0, 3));

        // 比較元を残すため値型をコピーし、各APIの返り値を保持する。
        Point3d rotated = point;
        rotated = rotated.TransformBy(rotation);
        Point3d placed = rotated;
        placed = placed.TransformBy(translation);
        Vector3d rotatedDirection = direction;
        rotatedDirection = rotatedDirection.TransformBy(rotation);
        Vector3d placedDirection = rotatedDirection;
        placedDirection = placedDirection.TransformBy(translation);

        // 列方式では先にR、次にT。PreMultiplyBy(T)は左からTを掛ける。
        Matrix3d placement = rotation;
        placement = placement.PreMultiplyBy(translation); // T R
        Point3d combined = point;
        combined = combined.TransformBy(placement);

        editor.WriteMessage("\nGEO01_TRANSFORM: 以下はAPI計算値。期待値はREADMEの数式を参照（実機未検証）。");
        WritePoint(editor, "入力点", point);
        WritePoint(editor, "回転後の点", rotated);
        WritePoint(editor, "回転→移動の点", placed);
        WritePoint(editor, "合成TRによる点", combined);
        WriteVector(editor, "入力方向", direction);
        WriteVector(editor, "回転後の方向", rotatedDirection);
        WriteVector(editor, "回転→移動の方向（移動は加わらない）", placedDirection);

        AddComparisonLines(document, point, placement);
    }

    /// <summary>断面上のローカル点(2,1,0)を、原点と3基底で定めた配置へ移し、WCS座標を表示する。</summary>
    /// <remarks>
    /// 引数なし。C=(10,0,3)、X基底=(0,1,0)、Y基底=(-1,0,0)、Z基底=(0,0,1)。
    /// 点の期待値は(9,2,3)、方向は(-1,2,0)。図形の追加や現在のUCSの変更は行わない。実機未検証。
    /// </remarks>
    [CommandMethod("GEO01_LOCAL")]
    public static void LocalToWorld()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;

        Point3d center = new(10, 0, 3);
        Vector3d localX = new(0, 1, 0);
        Vector3d localY = new(-1, 0, 0);
        Vector3d localZ = new(0, 0, 1);
        // fromは標準フレーム、toはWCS成分で表した配置先フレーム。
        // これによりローカルの数値(x,y,z)を C + x localX + y localY + z localZ へ写す。
        Matrix3d localToWorld = Matrix3d.AlignCoordinateSystem(
            Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
            center, localX, localY, localZ);

        Point3d localPoint = new(2, 1, 0);
        Point3d worldPoint = localPoint;
        worldPoint = worldPoint.TransformBy(localToWorld);
        Vector3d worldDirection = new(2, 1, 0);
        worldDirection = worldDirection.TransformBy(localToWorld);

        editor.WriteMessage("\nGEO01_LOCAL: ローカル→WCSのAPI計算値（実機未検証）。UCS設定・図面は変更しません。");
        WritePoint(editor, "ローカルの原点CをWCSで表した値", center);
        WriteVector(editor, "ローカルX基底（WCS成分）", localX);
        WriteVector(editor, "ローカルY基底（WCS成分）", localY);
        WriteVector(editor, "ローカルZ基底（WCS成分）", localZ);
        WritePoint(editor, "断面のローカル点", localPoint);
        WritePoint(editor, "配置後のWCS点", worldPoint);
        WriteVector(editor, "配置後のWCS方向", worldDirection);
    }

    /// <summary>既存教材のCompositionOrderと同じ点(1,0,0)、X方向への移動3で、変換順序を比較する。</summary>
    /// <remarks>
    /// 引数なし。回転→移動は(3,1,0)、移動→回転は(0,4,0)が数学上の期待値。
    /// 回転中心はWCS原点、軸は+Z。中間点と合成の計算値を表示するだけで、図面は変更しない。実機未検証。
    /// </remarks>
    [CommandMethod("GEO01_ORDER")]
    public static void CompositionOrder()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;

        Point3d point = new(1, 0, 0);
        Matrix3d rotation = RotationAboutWorldZ();
        Matrix3d translation = Matrix3d.Displacement(new Vector3d(3, 0, 0));
        Point3d afterRotation = point;
        afterRotation = afterRotation.TransformBy(rotation);
        Point3d rotateThenMove = afterRotation;
        rotateThenMove = rotateThenMove.TransformBy(translation);
        Point3d afterTranslation = point;
        afterTranslation = afterTranslation.TransformBy(translation);
        Point3d moveThenRotate = afterTranslation;
        moveThenRotate = moveThenRotate.TransformBy(rotation);

        // 元のRを各々コピーする。Pre/Postは呼出し順の名前ではなく積の左右を指定する。
        Matrix3d tr = rotation;
        tr = tr.PreMultiplyBy(translation); // T R：回転→移動
        Matrix3d rt = rotation;
        rt = rt.PostMultiplyBy(translation); // R T：移動→回転
        Point3d byTr = point;
        byTr = byTr.TransformBy(tr);
        Point3d byRt = point;
        byRt = byRt.TransformBy(rt);

        editor.WriteMessage("\nGEO01_ORDER: API計算値と数学上の期待値を読み比べます（実機未検証）。図面変更なし。");
        WritePoint(editor, "入力点", point);
        WritePoint(editor, "回転の直後", afterRotation);
        WritePoint(editor, "回転→移動（逐次）", rotateThenMove);
        WritePoint(editor, "移動の直後", afterTranslation);
        WritePoint(editor, "移動→回転（逐次）", moveThenRotate);
        WritePoint(editor, "PreMultiplyByで作ったTR", byTr);
        WritePoint(editor, "PostMultiplyByで作ったRT", byRt);
    }

    /// <summary>WCS原点を通る+Z軸周りの、右手系+90度の回転行列を返す。</summary>
    /// <returns>教材の列方式のRに対応するMatrix3d。角度はMath.PI/2ラジアン。</returns>
    private static Matrix3d RotationAboutWorldZ() =>
        Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);

    /// <summary>同じ端点から作った変換前後の新規Lineを、現在図面のモデル空間へ2本追加する。</summary>
    /// <param name="document">通常の非Sessionコマンドで取得した現在の図面。</param>
    /// <param name="end">変換前の終点。始点はWCS原点。</param>
    /// <param name="placement">新規Lineの一方へ適用する剛体変換TR。既存図形には適用しない。</param>
    /// <remarks>Commit前の例外ならTransactionの破棄でロールバックする。保存は行わない。実機未検証。</remarks>
    private static void AddComparisonLines(Document document, Point3d end, Matrix3d placement)
    {
        Database database = document.Database;
        try
        {
            // #69の通常コマンドと同じ所有権・Transactionの方式。別図面やモデルレスの操作ではない。
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blocks = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                using (Line before = new(Point3d.Origin, end))
                using (Line after = new(Point3d.Origin, end))
                {
                    // Entity.TransformByはそのEntity自身を変更する。ここでは未追加の新規Lineだけを変更。
                    // 端点だけでなく始点もTRで移るので、変換後はC→(9,2,3)になる。
                    after.TransformBy(placement);
                    modelSpace.AppendEntity(before);
                    transaction.AddNewlyCreatedDBObject(before, true);
                    modelSpace.AppendEntity(after);
                    transaction.AddNewlyCreatedDBObject(after, true);
                    transaction.Commit();
                }
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        {
            // Commit後の例外まで「変更なし」とは断定しない。
            document.Editor.WriteMessage($"\nGEO01_TRANSFORM: Line追加中に例外が発生しました。図面状態を確認してください。{error.Message}");
            return;
        }
        document.Editor.WriteMessage("\n変換前 (0,0,0)→(2,1,0)、変換後 (10,0,3)→(9,2,3) の新規Lineを2本追加しました（端点は数学上の期待値）。");
    }

    /// <summary>点の3成分をラベル付きでEditorへ表示する。UCSへの変換は行わない。</summary>
    /// <param name="editor">現在の図面の出力先。</param>
    /// <param name="label">WCS／ローカルや計算段階を区別する日本語ラベル。</param>
    /// <param name="point">表示する点。</param>
    private static void WritePoint(Editor editor, string label, Point3d point) =>
        editor.WriteMessage($"\n  {label}: {Coordinates(point.X, point.Y, point.Z)}");

    /// <summary>方向の3成分をラベル付きでEditorへ表示する。位置としては扱わない。</summary>
    /// <param name="editor">現在の図面の出力先。</param>
    /// <param name="label">基底や計算段階を区別する日本語ラベル。</param>
    /// <param name="vector">表示する方向ベクトル。</param>
    private static void WriteVector(Editor editor, string label, Vector3d vector) =>
        editor.WriteMessage($"\n  {label}: {Coordinates(vector.X, vector.Y, vector.Z)}");

    /// <summary>丸めてゼロに置換せず、3成分を有効数字17桁・小数点がピリオドのカルチャで表記する。</summary>
    /// <param name="x">X成分。</param>
    /// <param name="y">Y成分。</param>
    /// <param name="z">Z成分。</param>
    /// <returns>三角関数の微小な残差も読み取れる、括弧付きの座標文字列。</returns>
    private static string Coordinates(double x, double y, double z) =>
        string.Format(CultureInfo.InvariantCulture, "({0:G17}, {1:G17}, {2:G17})", x, y, z);
}

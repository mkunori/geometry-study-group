using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace AutoCadGeometrySamples;

/// <summary>実践編の表示・新規図形追加を共有する。変換方式の比較計算は各コマンド内に置く。</summary>
internal static class TransformDemoSupport
{
    /// <summary>成分計算の方向を、同じWCS成分のAutoCADベクトルにする。</summary>
    internal static Vector3d Vector(ArbitraryAxisMath.Vec v) => new(v.X, v.Y, v.Z);

    /// <summary>WCS原点からの位置ベクトルをWCS点として扱う。変換や平行移動はしない。</summary>
    internal static Point3d Point(ArbitraryAxisMath.Vec v) => new(v.X, v.Y, v.Z);

    /// <summary>AutoCADベクトルの成分を数学部分へ渡す。</summary>
    internal static ArbitraryAxisMath.Vec Components(Vector3d v) => new(v.X, v.Y, v.Z);

    /// <summary>表示の桁丸めを判定に流用せず、17桁の値をデバッガーの変数と対応させる。</summary>
    internal static void Write(Editor editor, string label, double value) =>
        editor.WriteMessage($"\n{label}: {value.ToString("G17", CultureInfo.InvariantCulture)}");

    /// <summary>3成分を同じ書式で出力する。labelには点／方向と座標系を記す。</summary>
    internal static void Write(Editor editor, string label, ArbitraryAxisMath.Vec v) =>
        editor.WriteMessage(string.Create(CultureInfo.InvariantCulture,
            $"\n{label}: ({v.X:G17}, {v.Y:G17}, {v.Z:G17})"));

    /// <summary>点の座標をG17で出力する。座標系を変換する関数ではない。</summary>
    internal static void WritePoint(Editor editor, string label, Point3d point) =>
        Write(editor, label, new ArbitraryAxisMath.Vec(point.X, point.Y, point.Z));

    /// <summary>ToArrayで得た16要素を、行ごとに表示する。積や転置の計算は隠さない。</summary>
    internal static void WriteMatrix(Editor editor, string label, double[] values)
    {
        editor.WriteMessage($"\n{label}（行ごと）");
        for (int row = 0; row < 4; row++)
            editor.WriteMessage(string.Create(CultureInfo.InvariantCulture,
                $"\n[{values[4 * row]:G17}, {values[4 * row + 1]:G17}, {values[4 * row + 2]:G17}, {values[4 * row + 3]:G17}]"));
    }

    /// <summary>同じ座標系の点どうしの距離残差を表示する。固定入力の小さな値を対象にする。</summary>
    internal static void WriteResidual(Editor editor, string label, Point3d a, Point3d b) =>
        Write(editor, label, ArbitraryAxisMath.Length(new(a.X - b.X, a.Y - b.Y, a.Z - b.Z)));

    /// <summary>現在図面の通常コマンドから、新規図形だけをモデル空間へ登録・確定する。</summary>
    /// <remarks>Commit前の例外は破棄でロールバック。Commit後の失敗まで変更なしとは断定しない。</remarks>
    internal static void InModelSpace(Document document, Action<BlockTableRecord, Transaction> add)
    {
        using Transaction transaction = document.Database.TransactionManager.StartTransaction();
        BlockTable blocks = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
        BlockTableRecord space = (BlockTableRecord)transaction.GetObject(
            blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        add(space, transaction);
        transaction.Commit();
    }

    /// <summary>新規Entityだけを登録する。呼出し元のusingで破棄し、既存DBオブジェクトには使わない。</summary>
    internal static void Append(BlockTableRecord space, Transaction transaction, Entity entity)
    {
        space.AppendEntity(entity);
        transaction.AddNewlyCreatedDBObject(entity, true);
    }

    /// <summary>WCSの始点と方向に従い新しいLineを描く。軸長は表示用で、数学上の基底長ではない。</summary>
    internal static void Axis(BlockTableRecord space, Transaction transaction,
        Point3d origin, Vector3d direction, double displayLength, int color)
    {
        Point3d end = new(origin.X + displayLength * direction.X,
            origin.Y + displayLength * direction.Y, origin.Z + displayLength * direction.Z);
        using Line line = new(origin, end) { ColorIndex = color };
        Append(space, transaction, line);
    }
}

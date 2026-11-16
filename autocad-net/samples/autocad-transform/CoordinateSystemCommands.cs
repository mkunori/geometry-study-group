using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>同じPolyline頂点のOCS値とWCS値を読む実演。AutoCAD 2026 Update 1.2以降／.NET 10想定。</summary>
/// <remarks>仕様を静的照合した参考コード。AutoCAD参照DLLでのビルド・NETLOAD・実機実行は未検証。</remarks>
public static class CoordinateSystemCommands
{
    /// <summary>同じ4頂点でXY平面と傾斜平面のPolylineを独立生成し、各頂点と基底を出力する。</summary>
    /// <remarks>
    /// 引数・選択なし。OCS頂点(0,0),(4,0),(3,2),(0,1)、bulge・幅0、閉輪郭。
    /// 平面AはN=(0,0,1),h=0、平面BはN=(0,3/5,4/5),h=5。
    /// 頂点2の数学上のWCS期待値はA=(3,2,0)、B=(-3,7/5,26/5)。実測値ではない。
    /// 新規Polyline2個とLine7本だけを追加。保存・既存図形・UCS・表示設定の変更はしない。再実行は重複追加。
    /// </remarks>
    [CommandMethod("GEOAT_COORDINATES")]
    public static void Coordinates()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;

        ArbitraryAxisMath.Frame flatFrame = ArbitraryAxisMath.Build(new(0, 0, 1));
        ArbitraryAxisMath.Frame tiltedFrame = ArbitraryAxisMath.Build(new(0, 3, 4));
        // 一方を変換して比較せず、同じOCS頂点からそれぞれ新しく作る。
        try
        {
            using Polyline flat = CreatePolyline(Vector(flatFrame.Normal), 0, 2);
            using Polyline tilted = CreatePolyline(Vector(tiltedFrame.Normal), 5, 4);
            editor.WriteMessage("\nGEOAT_COORDINATES: API値と数学上の期待値を照合します。実機動作は未検証。");
            Inspect(editor, "A：XY平面", flat);
            Inspect(editor, "B：傾斜平面", tilted);

            InModelSpace(document, (space, transaction) =>
            {
                Append(space, transaction, flat);
                Append(space, transaction, tilted);
                // 灰色はWCS原点からの3軸。表示長3を単位基底の長さと混同しない。
                Axis(space, transaction, Point3d.Origin, Vector3d.XAxis, 3, 8);
                Axis(space, transaction, Point3d.Origin, Vector3d.YAxis, 3, 8);
                Axis(space, transaction, Point3d.Origin, Vector3d.ZAxis, 3, 8);
                Point3d planeBase = Point(ArbitraryAxisMath.Position(tiltedFrame, 0, 0, 5));
                // C=5Nは図形平面の基準点で、OCS原点そのものを移動する処理ではない。
                Axis(space, transaction, Point3d.Origin, Vector(tiltedFrame.Normal), 5, 6);
                Axis(space, transaction, planeBase, Vector(tiltedFrame.XAxis), 3, 1);
                Axis(space, transaction, planeBase, Vector(tiltedFrame.YAxis), 3, 3);
                Axis(space, transaction, planeBase, Vector(tiltedFrame.Normal), 3, 5);
            });
            editor.WriteMessage("\n新規Polyline2個・Line7本を追加。黄色=A、シアン=B、灰=WCS、赤/緑/青=BのX/Y/N、マゼンタ=O→5N。保存なし。");
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        {
            editor.WriteMessage($"\nGEOAT_COORDINATES: AutoCAD例外。図面状態を確認してください。{error.Message}");
        }
    }

    /// <summary>頂点入力はOCSのPoint2d。NormalとElevationを先に明示し、現在UCSの点を流用しない。</summary>
    private static Polyline CreatePolyline(Vector3d normal, double elevation, int color)
    {
        Polyline polyline = new();
        try
        {
            polyline.Normal = normal;
            polyline.Elevation = elevation;
            polyline.ColorIndex = color;
            polyline.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
            polyline.AddVertexAt(1, new Point2d(4, 0), 0, 0, 0);
            polyline.AddVertexAt(2, new Point2d(3, 2), 0, 0, 0);
            polyline.AddVertexAt(3, new Point2d(0, 1), 0, 0, 0);
            polyline.Closed = true;
            return polyline;
        }
        catch
        {
            polyline.Dispose();
            throw;
        }
    }

    /// <summary>取得API、同じ入力点からのPlaneToWorld、自作基底の線形結合を独立して計算する。</summary>
    private static void Inspect(Editor editor, string label, Polyline polyline)
    {
        Vector3d normal = polyline.Normal;
        double elevation = polyline.Elevation;
        ArbitraryAxisMath.Frame frame = ArbitraryAxisMath.Build(Components(normal));
        Matrix3d planeToWorld = Matrix3d.PlaneToWorld(Vector(frame.Normal));
        editor.WriteMessage($"\n{label}（同一の頂点番号を比較）");
        Write(editor, "Normal：WCS方向", Components(normal));
        Write(editor, "Elevation：OCSの高さ", elevation);
        Write(editor, "自作X：WCS方向", frame.XAxis);
        Write(editor, "自作Y：WCS方向", frame.YAxis);
        Write(editor, "単位N：WCS方向", frame.Normal);
        editor.WriteMessage(frame.UsesWorldY ? "\n補助軸：WCS Y" : "\n補助軸：WCS Z");
        for (int index = 0; index < polyline.NumberOfVertices; index++)
        {
            Point2d ocs2d = polyline.GetPoint2dAt(index);
            Point3d wcsByGetter = polyline.GetPoint3dAt(index);
            Point3d ocs3d = new(ocs2d.X, ocs2d.Y, elevation);
            Point3d wcsByMatrix = ocs3d;
            wcsByMatrix = wcsByMatrix.TransformBy(planeToWorld);
            ArbitraryAxisMath.Vec wcsByBasis = ArbitraryAxisMath.Position(frame, ocs2d.X, ocs2d.Y, elevation);
            ArbitraryAxisMath.Vec getter = new(wcsByGetter.X, wcsByGetter.Y, wcsByGetter.Z);
            Write(editor, $"頂点{index} OCS(u,v,h)", new ArbitraryAxisMath.Vec(ocs2d.X, ocs2d.Y, elevation));
            Write(editor, "GetPoint3dAt：WCS点", getter);
            Write(editor, "PlaneToWorld：WCS点", new ArbitraryAxisMath.Vec(wcsByMatrix.X, wcsByMatrix.Y, wcsByMatrix.Z));
            Write(editor, "自作基底：WCS点", wcsByBasis);
            Write(editor, "取得値と自作基底の距離残差", ArbitraryAxisMath.Length(ArbitraryAxisMath.Difference(getter, wcsByBasis)));
            Write(editor, "行列と自作基底の距離残差", ArbitraryAxisMath.Length(ArbitraryAxisMath.Difference(
                new(wcsByMatrix.X, wcsByMatrix.Y, wcsByMatrix.Z), wcsByBasis)));
            Write(editor, "平面条件 N・P−h の残差", ArbitraryAxisMath.Dot(frame.Normal, getter) - elevation);
        }
    }
}

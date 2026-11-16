using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>第5章のWCS→OCS、往復と平面外の点を比較する。ビルド・NETLOAD・実機実行は未検証。</summary>
public static class WcsToOcsCommands
{
    /// <summary>4方式で同じWCS点を比較し、Bの平面上の頂点だけから新しいPolylineを作る。</summary>
    /// <remarks>
    /// Bの固定点P=(-3,7/5,26/5)はOCS(3,2,5)、平面外P+2NはOCS(3,2,7)が期待値。
    /// 射影せずに往復すれば平面外の点もそのまま戻る。新規Polyline1個とLine14本のみ追加。
    /// 引数・選択なし。保存・既存図形・UCS・ビュー・レイヤー設定の変更なし。再実行は重複追加。
    /// </remarks>
    [CommandMethod("GEOAT_WCS_TO_OCS")]
    public static void WcsToOcs()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        double root5 = Math.Sqrt(5);
        (string Label, ArbitraryAxisMath.Vec Normal, double Height, Point3d On, Point3d Off)[] cases =
        [
            ("A", new(0, 0, 1), 0, new(3, 2, 0), new(3, 2, 2)),
            ("B", new(0, 3, 4), 5, new(-3, 7.0 / 5, 26.0 / 5), new(-3, 13.0 / 5, 34.0 / 5)),
            ("斜め", new(1, 2, 2), 5,
                new(5.0 / 3 - 22 / (3 * root5), 10.0 / 3 + 1 / (3 * root5), (10 + 2 * root5) / 3),
                new(7.0 / 3 - 22 / (3 * root5), 14.0 / 3 + 1 / (3 * root5), (14 + 2 * root5) / 3)),
        ];
        try
        {
            foreach (var item in cases)
            {
                ArbitraryAxisMath.Frame frame = ArbitraryAxisMath.Build(item.Normal);
                Compare(editor, item.Label + "：平面上", item.On, frame, item.Height);
                Compare(editor, item.Label + "：平面外", item.Off, frame, item.Height);
            }
            ArbitraryAxisMath.Frame tilted = ArbitraryAxisMath.Build(new(0, 3, 4));
            Matrix3d worldToPlane = Matrix3d.WorldToPlane(Vector(tilted.Normal));
            Point3d[] worldVertices = [new(0, 3, 4), new(-4, 3, 4), new(-3, 7.0 / 5, 26.0 / 5), new(0, 11.0 / 5, 23.0 / 5)];
            Point3d outside = new(-3, 13.0 / 5, 34.0 / 5);
            // 実演の小さな固定値用の距離許容差。一般用途の精度保証や接触の定義ではない。
            const double vertexPlaneTolerance = 1e-10;
            using Polyline reconstructed = new() { Normal = Vector(tilted.Normal), Elevation = 5, ColorIndex = 4 };
            foreach (Point3d world in worldVertices)
            {
                Point3d ocs = world;
                ocs = ocs.TransformBy(worldToPlane);
                bool finite = double.IsFinite(ocs.X) && double.IsFinite(ocs.Y) && double.IsFinite(ocs.Z);
                if (!finite || Math.Abs(ocs.Z - reconstructed.Elevation) > vertexPlaneTolerance)
                    throw new System.InvalidOperationException("同じ図形平面上の有限な頂点ではありません。");
                reconstructed.AddVertexAt(reconstructed.NumberOfVertices, new Point2d(ocs.X, ocs.Y), 0, 0, 0);
            }
            reconstructed.Closed = true;
            Point3d outsideOcs = outside;
            outsideOcs = outsideOcs.TransformBy(worldToPlane);
            bool outsideAccepted = double.IsFinite(outsideOcs.X) && double.IsFinite(outsideOcs.Y) && double.IsFinite(outsideOcs.Z)
                && Math.Abs(outsideOcs.Z - reconstructed.Elevation) <= vertexPlaneTolerance;
            editor.WriteMessage($"\nBの平面外の点をh=5の頂点として受け入れるか：{outsideAccepted}（期待：False）");
            if (outsideAccepted) throw new System.InvalidOperationException("平面外の点を頂点に混入させないでください。");
            InModelSpace(document, (space, transaction) =>
            {
                Append(space, transaction, reconstructed);
                OcsWcsDemoDrawing.Frame(space, transaction, tilted, 5);
                using Line heightDifference = new(worldVertices[2], outside) { ColorIndex = 6 };
                Append(space, transaction, heightDifference);
                OcsWcsDemoDrawing.Marker(space, transaction, worldVertices[2], 3);
                OcsWcsDemoDrawing.Marker(space, transaction, outside, 1);
            });
            editor.WriteMessage("\n新規Polyline1個・Line14本。緑十字=平面上、赤十字=平面外、マゼンタ線=2N。平面外の点は頂点へ追加せず、保存もしません。");
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        {
            editor.WriteMessage($"\nGEOAT_WCS_TO_OCS: AutoCAD例外。図面状態を確認してください。{error.Message}");
        }
        catch (System.InvalidOperationException error)
        {
            editor.WriteMessage($"\nGEOAT_WCS_TO_OCS: 入力と平面の整合を確認してください。{error.Message}");
        }
    }

    /// <summary>専用逆変換、Inverse、内積、転置を並べる。入力点は手計算で固定し、順変換結果を比較元にしない。</summary>
    private static void Compare(Editor editor, string label, Point3d world, ArbitraryAxisMath.Frame frame, double elevation)
    {
        Matrix3d worldToPlane = Matrix3d.WorldToPlane(Vector(frame.Normal));
        Matrix3d planeToWorld = Matrix3d.PlaneToWorld(Vector(frame.Normal));
        Matrix3d inverse = planeToWorld.Inverse();
        double[] basisValues =
        [
            frame.XAxis.X, frame.YAxis.X, frame.Normal.X, 0,
            frame.XAxis.Y, frame.YAxis.Y, frame.Normal.Y, 0,
            frame.XAxis.Z, frame.YAxis.Z, frame.Normal.Z, 0,
            0, 0, 0, 1,
        ];
        Matrix3d basisMatrix = new(basisValues);
        // 原点共通・正規直交のこの行列だから転置を使える。平行移動を含む一般の4×4には使わない。
        Matrix3d transpose = basisMatrix.Transpose();
        Point3d byWorldToPlane = world;
        byWorldToPlane = byWorldToPlane.TransformBy(worldToPlane);
        Point3d byInverse = world;
        byInverse = byInverse.TransformBy(inverse);
        Vector3d displacement = new(world.X, world.Y, world.Z); // WCS原点からの変位。
        Point3d byDot = new(
            displacement.DotProduct(Vector(frame.XAxis)),
            displacement.DotProduct(Vector(frame.YAxis)),
            displacement.DotProduct(Vector(frame.Normal)));
        Point3d byTranspose = world;
        byTranspose = byTranspose.TransformBy(transpose);
        editor.WriteMessage($"\n--- {label} ---");
        WritePoint(editor, "固定WCS点", world);
        WritePoint(editor, "1 WorldToPlane：OCS", byWorldToPlane);
        WritePoint(editor, "2 Inverse：OCS", byInverse);
        WritePoint(editor, "3 内積：OCS", byDot);
        WritePoint(editor, "4 基底行列の転置：OCS", byTranspose);
        WriteResidual(editor, "1と3の距離残差", byWorldToPlane, byDot);
        WriteResidual(editor, "2と3の距離残差", byInverse, byDot);
        WriteResidual(editor, "4と3の距離残差", byTranspose, byDot);
        Write(editor, "OCS Z − Elevation", byDot.Z - elevation);
        WriteMatrix(editor, "WorldToPlane", worldToPlane.ToArray());
        WriteMatrix(editor, "PlaneToWorldのInverse", inverse.ToArray());
        WriteMatrix(editor, "自作基底行列の転置", transpose.ToArray());

        Point3d backByWorldToPlane = byWorldToPlane;
        backByWorldToPlane = backByWorldToPlane.TransformBy(planeToWorld);
        Point3d backByInverse = byInverse;
        backByInverse = backByInverse.TransformBy(planeToWorld);
        Point3d backByDot = new(
            byDot.X * frame.XAxis.X + byDot.Y * frame.YAxis.X + byDot.Z * frame.Normal.X,
            byDot.X * frame.XAxis.Y + byDot.Y * frame.YAxis.Y + byDot.Z * frame.Normal.Y,
            byDot.X * frame.XAxis.Z + byDot.Y * frame.YAxis.Z + byDot.Z * frame.Normal.Z);
        Point3d backByTranspose = byTranspose;
        backByTranspose = backByTranspose.TransformBy(basisMatrix);
        WritePoint(editor, "WorldToPlane経由の往復WCS", backByWorldToPlane);
        WritePoint(editor, "Inverse経由の往復WCS", backByInverse);
        WritePoint(editor, "内積経由の往復WCS", backByDot);
        WritePoint(editor, "転置経由の往復WCS", backByTranspose);
        WriteResidual(editor, "WorldToPlane往復残差", backByWorldToPlane, world);
        WriteResidual(editor, "Inverse往復残差", backByInverse, world);
        WriteResidual(editor, "内積往復残差", backByDot, world);
        WriteResidual(editor, "転置往復残差", backByTranspose, world);
        // Zを高さへ置き換える操作を意図的に分ける。座標変換ではなく、この平面への射影になる。
        Point3d forcedHeight = new(byDot.X, byDot.Y, elevation);
        Point3d projected = forcedHeight;
        projected = projected.TransformBy(planeToWorld);
        WritePoint(editor, "ZをElevationへ置換した後のWCS（射影）", projected);
        WriteResidual(editor, "射影によって動く距離", projected, world);
    }
}

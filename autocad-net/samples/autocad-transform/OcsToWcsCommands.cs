using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>第4章のOCS→WCSを4方式で比較する。AutoCAD参照DLLでのビルド・実機実行は未検証。</summary>
public static class OcsToWcsCommands
{
    /// <summary>A・B・斜め法線で同じ4頂点を比較し、Bの4方式の輪郭を同じ位置へ描く。</summary>
    /// <remarks>
    /// 引数・選択なし。AはN=+Z,h=0、BはN=(0,3/5,4/5),h=5、斜めはN=(1/3,2/3,2/3),h=5。
    /// Bの頂点2の期待値は(-3,7/5,26/5)。本文の期待値であり実測値ではない。
    /// 新規Polyline1個とLine23本。保存・既存図形・UCS・ビュー・レイヤー設定は変更せず、再実行は重複追加。
    /// </remarks>
    [CommandMethod("GEOAT_OCS_TO_WCS")]
    public static void OcsToWcs()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        (string Label, ArbitraryAxisMath.Vec Normal, double Height, bool Draw)[] cases =
        [
            ("A：XY平面", new(0, 0, 1), 0, false),
            ("B：傾斜平面", new(0, 3, 4), 5, true),
            ("斜め：逆向き・転置の誤りを見分ける対照", new(1, 2, 2), 5, false),
        ];
        Point3d[][]? tiltedResults = null;
        try
        {
            foreach (var item in cases)
            {
                ArbitraryAxisMath.Frame frame = ArbitraryAxisMath.Build(item.Normal);
                using Polyline source = OcsWcsDemoDrawing.CreateProfile(Vector(frame.Normal), item.Height);
                Point3d[][] results = Compare(editor, item.Label, source, frame);
                if (item.Draw) tiltedResults = results;
            }
            if (tiltedResults is null) return;
            ArbitraryAxisMath.Frame tilted = ArbitraryAxisMath.Build(new(0, 3, 4));
            using Polyline profile = OcsWcsDemoDrawing.CreateProfile(Vector(tilted.Normal), 5);
            InModelSpace(document, (space, transaction) =>
            {
                Append(space, transaction, profile);
                OcsWcsDemoDrawing.Frame(space, transaction, tilted, 5);
                int[] colors = [1, 3, 5, 2];
                for (int method = 0; method < 4; method++)
                    OcsWcsDemoDrawing.Outline(space, transaction, tiltedResults[method], colors[method]);
            });
            editor.WriteMessage("\n新規Polyline1個・Line23本。4方式は同じ位置で比較し、重なる見た目だけで一致とは判断しません。保存なし。");
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        {
            editor.WriteMessage($"\nGEOAT_OCS_TO_WCS: AutoCAD例外。図面状態を確認してください。{error.Message}");
        }
    }

    /// <summary>取得API、専用行列、自作基底行列、線形結合の計算をこの場に並べ、結果を個別に保持する。</summary>
    private static Point3d[][] Compare(Editor editor, string label, Polyline source, ArbitraryAxisMath.Frame frame)
    {
        Vector3d normal = source.Normal;
        double elevation = source.Elevation;
        Matrix3d planeToWorld = Matrix3d.PlaneToWorld(normal);
        // 基底は数学上の列。16要素の配列には行ごとに詰める。移動列は0。
        double[] basisValues =
        [
            frame.XAxis.X, frame.YAxis.X, frame.Normal.X, 0,
            frame.XAxis.Y, frame.YAxis.Y, frame.Normal.Y, 0,
            frame.XAxis.Z, frame.YAxis.Z, frame.Normal.Z, 0,
            0, 0, 0, 1,
        ];
        Matrix3d basisMatrix = new(basisValues);
        double[] apiValues = planeToWorld.ToArray();
        double[] constructedValues = basisMatrix.ToArray();
        editor.WriteMessage($"\n--- {label} ---");
        Write(editor, "Normal：WCS方向", Components(normal));
        Write(editor, "Elevation：OCSの高さ", elevation);
        WriteMatrix(editor, "PlaneToWorld", apiValues);
        WriteMatrix(editor, "自作基底Matrix3d", constructedValues);
        double maxElementDifference = 0;
        for (int index = 0; index < 16; index++)
            maxElementDifference = Math.Max(maxElementDifference, Math.Abs(apiValues[index] - constructedValues[index]));
        Write(editor, "行列16要素の最大絶対差", maxElementDifference);

        Point3d[][] results = [new Point3d[4], new Point3d[4], new Point3d[4], new Point3d[4]];
        for (int index = 0; index < source.NumberOfVertices; index++)
        {
            Point2d ocs2d = source.GetPoint2dAt(index);
            Point3d ocs3d = new(ocs2d.X, ocs2d.Y, elevation);
            // 4方式は同じ元頂点から独立に計算する。別方式の出力を入力にはしない。
            Point3d byGetter = source.GetPoint3dAt(index);
            Point3d byPlaneToWorld = ocs3d;
            byPlaneToWorld = byPlaneToWorld.TransformBy(planeToWorld);
            Point3d byBasisMatrix = ocs3d;
            byBasisMatrix = byBasisMatrix.TransformBy(basisMatrix);
            Point3d byCombination = new(
                ocs3d.X * frame.XAxis.X + ocs3d.Y * frame.YAxis.X + ocs3d.Z * frame.Normal.X,
                ocs3d.X * frame.XAxis.Y + ocs3d.Y * frame.YAxis.Y + ocs3d.Z * frame.Normal.Y,
                ocs3d.X * frame.XAxis.Z + ocs3d.Y * frame.YAxis.Z + ocs3d.Z * frame.Normal.Z);
            results[0][index] = byGetter;
            results[1][index] = byPlaneToWorld;
            results[2][index] = byBasisMatrix;
            results[3][index] = byCombination;
            WritePoint(editor, $"頂点{index} OCS(u,v,h)", ocs3d);
            WritePoint(editor, "1 GetPoint3dAt：WCS", byGetter);
            WritePoint(editor, "2 PlaneToWorld：WCS", byPlaneToWorld);
            WritePoint(editor, "3 基底行列：WCS", byBasisMatrix);
            WritePoint(editor, "4 線形結合：WCS", byCombination);
            WriteResidual(editor, "1と4の距離残差", byGetter, byCombination);
            WriteResidual(editor, "2と4の距離残差", byPlaneToWorld, byCombination);
            WriteResidual(editor, "3と4の距離残差", byBasisMatrix, byCombination);
            // 高さを捨てない往復。逆変換の詳細は第5章で比較する。
            Point3d backToOcs = byPlaneToWorld;
            backToOcs = backToOcs.TransformBy(planeToWorld.Inverse());
            WritePoint(editor, "OCS→WCS→OCS", backToOcs);
            WriteResidual(editor, "往復の距離残差", backToOcs, ocs3d);
            if (index == 2)
            {
                // ここからは誤りを見分ける診断例。描く4方式の結果には混ぜない。
                Point3d withoutElevation = new(ocs2d.X, ocs2d.Y, 0);
                withoutElevation = withoutElevation.TransformBy(planeToWorld);
                Point3d wrongDirection = ocs3d;
                wrongDirection = wrongDirection.TransformBy(Matrix3d.WorldToPlane(normal));
                Point3d twiceHeight = new(
                    byCombination.X + elevation * frame.Normal.X,
                    byCombination.Y + elevation * frame.Normal.Y,
                    byCombination.Z + elevation * frame.Normal.Z);
                // X'=Y、Y'=-Xは右手系でも、AutoCADが選ぶOCSとは異なる。
                Point3d differentBasis = new(
                    ocs3d.X * frame.YAxis.X - ocs3d.Y * frame.XAxis.X + ocs3d.Z * frame.Normal.X,
                    ocs3d.X * frame.YAxis.Y - ocs3d.Y * frame.XAxis.Y + ocs3d.Z * frame.Normal.Y,
                    ocs3d.X * frame.YAxis.Z - ocs3d.Y * frame.XAxis.Z + ocs3d.Z * frame.Normal.Z);
                WritePoint(editor, "診断例：Elevationを落とした結果", withoutElevation);
                WritePoint(editor, "診断例：逆向きの行列へOCS値を渡した結果", wrongDirection);
                WritePoint(editor, "診断例：高さを二重に足した結果", twiceHeight);
                WritePoint(editor, "診断例：法線は同じだが別の基底を使った結果", differentBasis);
            }
        }
        return results;
    }
}

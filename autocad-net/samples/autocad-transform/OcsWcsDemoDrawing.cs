using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>第4〜5章の固定輪郭と描画だけを共有する。相互変換の計算は各コマンドに記す。</summary>
internal static class OcsWcsDemoDrawing
{
    /// <summary>同じ非対称OCS輪郭を毎回新しく生成する。位置の変換や既存Entityの複製はしない。</summary>
    internal static Polyline CreateProfile(Vector3d normal, double elevation)
    {
        Polyline profile = new();
        try
        {
            profile.Normal = normal;
            profile.Elevation = elevation;
            profile.ColorIndex = 4;
            profile.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
            profile.AddVertexAt(1, new Point2d(4, 0), 0, 0, 0);
            profile.AddVertexAt(2, new Point2d(3, 2), 0, 0, 0);
            profile.AddVertexAt(3, new Point2d(0, 1), 0, 0, 0);
            profile.Closed = true;
            return profile;
        }
        catch
        {
            profile.Dispose();
            throw;
        }
    }

    /// <summary>WCSの3軸、CからのOCSの3軸、O→hNを合計7本のLineで描く。表示長3。</summary>
    internal static void Frame(BlockTableRecord space, Transaction transaction,
        ArbitraryAxisMath.Frame frame, double height)
    {
        Axis(space, transaction, Point3d.Origin, Vector3d.XAxis, 3, 8);
        Axis(space, transaction, Point3d.Origin, Vector3d.YAxis, 3, 8);
        Axis(space, transaction, Point3d.Origin, Vector3d.ZAxis, 3, 8);
        Point3d planeBase = new(height * frame.Normal.X, height * frame.Normal.Y, height * frame.Normal.Z);
        Axis(space, transaction, Point3d.Origin, Vector(frame.Normal), height, 6);
        Axis(space, transaction, planeBase, Vector(frame.XAxis), 3, 1);
        Axis(space, transaction, planeBase, Vector(frame.YAxis), 3, 3);
        Axis(space, transaction, planeBase, Vector(frame.Normal), 3, 5);
    }

    /// <summary>計算済みのWCS頂点を閉じたLine列として描く。表示位置をずらさず比較する。</summary>
    internal static void Outline(BlockTableRecord space, Transaction transaction, Point3d[] points, int color)
    {
        for (int index = 0; index < points.Length; index++)
        {
            using Line edge = new(points[index], points[(index + 1) % points.Length]) { ColorIndex = color };
            Append(space, transaction, edge);
        }
    }

    /// <summary>WCS点の位置を小さな十字3本で示す。表示幅0.24は幾何判定の許容差ではない。</summary>
    internal static void Marker(BlockTableRecord space, Transaction transaction, Point3d point, int color)
    {
        Axis(space, transaction, new Point3d(point.X - 0.12, point.Y, point.Z), Vector3d.XAxis, 0.24, color);
        Axis(space, transaction, new Point3d(point.X, point.Y - 0.12, point.Z), Vector3d.YAxis, 0.24, color);
        Axis(space, transaction, new Point3d(point.X, point.Y, point.Z - 0.12), Vector3d.ZAxis, 0.24, color);
    }
}

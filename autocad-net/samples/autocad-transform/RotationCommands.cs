using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>原点と任意中心の回転を、sin/cos・点API・行列で比較する。ビルド・実機未検証。</summary>
public static class RotationCommands
{
    /// <summary>+Z軸の右手系+π/2回転。頂点2=(3,2,0)の期待値は(-2,3,0)。</summary>
    /// <remarks>引数・選択なし、角度はラジアン。新規Line35本。保存・既存図形・UCS・表示設定は変更しない。</remarks>
    [CommandMethod("GEOAT_ROTATION")]
    public static void Rotation() => BasicTransformDrawing.Run("GEOAT_ROTATION", (document, editor) =>
    {
        double angle = Math.PI / 2;
        double cosine = Math.Cos(angle), sine = Math.Sin(angle);
        Matrix3d rotation = Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
        WriteMatrix(editor, "R：原点を通る+Z軸", rotation.ToArray());
        BasicTransformDrawing.Show(document, editor,
        [
            ("sin/cos", p => new Point3d(cosine * p.X - sine * p.Y, sine * p.X + cosine * p.Y, p.Z)),
            ("Point2d.RotateBy", p =>
            {
                Point2d xy = new(p.X, p.Y);
                xy = xy.RotateBy(angle, Point2d.Origin);
                return new Point3d(xy.X, xy.Y, p.Z);
            }),
            ("Point3d.RotateBy", p => p.RotateBy(angle, Vector3d.ZAxis, Point3d.Origin)),
            ("Rotation + TransformBy", p => p.TransformBy(rotation)),
        ]);
    });

    /// <summary>中心C=(1,1,0)を通る+Z軸周り。頂点2の期待値は(0,3,0)、C自身は固定。</summary>
    /// <remarks>引数・選択なし。新規Line42本。点APIとT(C)R T(-C)を独立に比較し、保存・既存図形は変更しない。</remarks>
    [CommandMethod("GEOAT_CENTER_ROTATION")]
    public static void CenterRotation() => BasicTransformDrawing.Run("GEOAT_CENTER_ROTATION", (document, editor) =>
    {
        double angle = Math.PI / 2;
        double cosine = Math.Cos(angle), sine = Math.Sin(angle);
        Point3d center = new(1, 1, 0);
        Matrix3d rotation = Matrix3d.Rotation(angle, Vector3d.ZAxis, center);
        Matrix3d toOrigin = Matrix3d.Displacement(new Vector3d(-center.X, -center.Y, -center.Z));
        Matrix3d fromOrigin = Matrix3d.Displacement(new Vector3d(center.X, center.Y, center.Z));
        Matrix3d originRotation = Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
        Matrix3d sandwich = fromOrigin * originRotation * toOrigin;
        WriteMatrix(editor, "中心指定", rotation.ToArray());
        WriteMatrix(editor, "T(C) R T(-C)", sandwich.ToArray());
        WritePoint(editor, "回転中心は動かない", center.TransformBy(sandwich));
        BasicTransformDrawing.Show(document, editor,
        [
            ("中心からのsin/cos", p => new Point3d(
                center.X + cosine * (p.X - center.X) - sine * (p.Y - center.Y),
                center.Y + sine * (p.X - center.X) + cosine * (p.Y - center.Y), p.Z)),
            ("Point2d.RotateBy：中心指定", p =>
            {
                Point2d xy = new(p.X, p.Y);
                xy = xy.RotateBy(angle, new Point2d(center.X, center.Y));
                return new Point3d(xy.X, xy.Y, p.Z);
            }),
            ("Point3d.RotateBy：中心指定", p => p.RotateBy(angle, Vector3d.ZAxis, center)),
            ("Rotation：中心指定", p => p.TransformBy(rotation)),
            ("T(C) R T(-C)", p => p.TransformBy(sandwich)),
        ]);
    });
}

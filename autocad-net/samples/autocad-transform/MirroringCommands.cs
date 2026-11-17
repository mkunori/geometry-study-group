using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>YZ平面の鏡映とY軸の半回転を区別する補足。平面上の輪郭だけでは違いが隠れる。ビルド・実機未検証。</summary>
public static class MirroringCommands
{
    /// <summary>頂点2=(3,2,0)の鏡映は(-3,2,0)。Z軸上の点は鏡映で+Z、Y軸半回転では-Z。</summary>
    /// <remarks>引数・選択なし、新規Line35本。方式4だけは異なる変換の診断例。保存・既存図形・UCS・表示設定は変更しない。</remarks>
    [CommandMethod("GEOAT_MIRRORING")]
    public static void Mirroring() => BasicTransformDrawing.Run("GEOAT_MIRRORING", (document, editor) =>
    {
        using Plane plane = new(Point3d.Origin, Vector3d.XAxis);
        Matrix3d mirror = Matrix3d.Mirroring(plane);
        Matrix3d halfTurn = Matrix3d.Rotation(Math.PI, Vector3d.YAxis, Point3d.Origin);
        WriteMatrix(editor, "YZ平面の鏡映", mirror.ToArray());
        WriteMatrix(editor, "Y軸の半回転：鏡映とは異なる", halfTurn.ToArray());
        BasicTransformDrawing.Show(document, editor,
        [
            ("X成分の符号反転", p => new Point3d(-p.X, p.Y, p.Z)),
            ("Point3d.Mirror(Plane)", p => p.Mirror(plane)),
            ("Mirroring(Plane) + TransformBy", p => p.TransformBy(mirror)),
            ("診断例：Y軸の半回転", p => p.TransformBy(halfTurn)),
        ]);
    });
}

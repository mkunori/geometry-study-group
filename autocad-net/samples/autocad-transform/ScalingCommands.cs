using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>正の一様スケールを、中心からの成分計算・点API・行列で比較する補足。ビルド・実機未検証。</summary>
public static class ScalingCommands
{
    /// <summary>C=(1,1,0)から2倍。頂点2=(3,2,0)は(5,3,0)、C自身は固定される。</summary>
    /// <remarks>引数・選択なし、新規Line28本。非一様・負・0スケールのEntityへの適用は実演しない。保存・既存図形等の変更なし。</remarks>
    [CommandMethod("GEOAT_SCALING")]
    public static void Scaling() => BasicTransformDrawing.Run("GEOAT_SCALING", (document, editor) =>
    {
        Point3d center = new(1, 1, 0);
        double scale = 2;
        Matrix3d scaling = Matrix3d.Scaling(scale, center);
        WriteMatrix(editor, "中心Cから一様2倍", scaling.ToArray());
        WritePoint(editor, "中心Cは固定", center.TransformBy(scaling));
        Point3d point = new(3, 2, 0);
        Point3d scaled = point.TransformBy(scaling);
        WritePoint(editor, "逆行列で戻す：期待(3,2,0)", scaled.TransformBy(scaling.Inverse()));
        BasicTransformDrawing.Show(document, editor,
        [
            ("中心からの成分計算", p => new Point3d(
                center.X + scale * (p.X - center.X),
                center.Y + scale * (p.Y - center.Y),
                center.Z + scale * (p.Z - center.Z))),
            ("Point3d.ScaleBy", p => p.ScaleBy(scale, center)),
            ("Scaling + TransformBy", p => p.TransformBy(scaling)),
        ]);
    });
}

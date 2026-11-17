using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>部材の配置を4方式で比較する補足。一般のOCSと任意ローカル配置を区別する。ビルド・実機未検証。</summary>
public static class AlignmentCommands
{
    /// <summary>C=(10,0,3)、X=(0,1,0)、Y=(-1,0,0)、Z=(0,0,1)への配置。頂点2は(8,3,3)。</summary>
    /// <remarks>引数・選択なし、右手系・長さ1の直交軸。新規Line35本。UCS設定は変えず、保存・既存図形の変更なし。</remarks>
    [CommandMethod("GEOAT_ALIGNMENT")]
    public static void Alignment() => BasicTransformDrawing.Run("GEOAT_ALIGNMENT", (document, editor) =>
    {
        Point3d center = new(10, 0, 3);
        Vector3d xAxis = new(0, 1, 0), yAxis = new(-1, 0, 0), zAxis = new(0, 0, 1);
        Matrix3d rotation = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);
        Matrix3d translation = Matrix3d.Displacement(new Vector3d(center.X, center.Y, center.Z));
        Matrix3d byProduct = translation * rotation;
        // 標準フレームを配置先フレームへ写す。任意fromの座標変換ならfromの逆も必要になる。
        Matrix3d byAlign = Matrix3d.AlignCoordinateSystem(
            Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
            center, xAxis, yAxis, zAxis);
        double[] values =
        [
            xAxis.X, yAxis.X, zAxis.X, center.X,
            xAxis.Y, yAxis.Y, zAxis.Y, center.Y,
            xAxis.Z, yAxis.Z, zAxis.Z, center.Z,
            0, 0, 0, 1,
        ];
        Matrix3d byElements = new(values);
        WriteMatrix(editor, "Rotation + Displacement", byProduct.ToArray());
        WriteMatrix(editor, "AlignCoordinateSystem", byAlign.ToArray());
        WriteMatrix(editor, "列基底と移動列を指定", byElements.ToArray());
        BasicTransformDrawing.Show(document, editor,
        [
            ("Rotation + Displacement", p => p.TransformBy(byProduct)),
            ("AlignCoordinateSystem", p => p.TransformBy(byAlign)),
            ("Matrix3dの成分指定", p => p.TransformBy(byElements)),
            ("Cと3軸を何倍かして足す", p => new Point3d(
                center.X + p.X * xAxis.X + p.Y * yAxis.X + p.Z * zAxis.X,
                center.Y + p.X * xAxis.Y + p.Y * yAxis.Y + p.Z * zAxis.Y,
                center.Z + p.X * xAxis.Z + p.Y * yAxis.Z + p.Z * zAxis.Z)),
        ]);
    });
}

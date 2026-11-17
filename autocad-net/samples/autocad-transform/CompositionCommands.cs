using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>列方式の合成順序と逆変換を比較する。AutoCAD参照DLLでのビルド・実機未検証。</summary>
public static class CompositionCommands
{
    /// <summary>頂点2を回転→移動で(8,3,3)、移動→回転で(-2,13,3)へ写す。前者を逆順で戻す。</summary>
    /// <remarks>Rは+Z・原点・+π/2、Tは(10,0,3)。引数・選択なし。新規Line42本。保存・既存図形等は変更しない。</remarks>
    [CommandMethod("GEOAT_COMPOSITION")]
    public static void Composition() => BasicTransformDrawing.Run("GEOAT_COMPOSITION", (document, editor) =>
    {
        Matrix3d rotation = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);
        Matrix3d translation = Matrix3d.Displacement(new Vector3d(10, 0, 3));
        Matrix3d product = translation * rotation;
        // 元のR/Tを残し、Pre/Postの返り値をそれぞれの変数へ保持する。
        Matrix3d byPre = rotation;
        byPre = byPre.PreMultiplyBy(translation); // T R
        Matrix3d byPost = translation;
        byPost = byPost.PostMultiplyBy(rotation); // T R
        Matrix3d reverseOrder = rotation * translation; // R T：異なる操作
        WriteMatrix(editor, "積 T R", product.ToArray());
        WriteMatrix(editor, "R.PreMultiplyBy(T)", byPre.ToArray());
        WriteMatrix(editor, "T.PostMultiplyBy(R)", byPost.ToArray());
        WriteMatrix(editor, "逆順 R T", reverseOrder.ToArray());
        Matrix3d inverse = product.Inverse();
        Matrix3d undoMove = Matrix3d.Displacement(new Vector3d(-10, 0, -3));
        Matrix3d undoRotate = Matrix3d.Rotation(-Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);
        WriteMatrix(editor, "Inverse(T R)", inverse.ToArray());
        WriteMatrix(editor, "Rの逆 Tの逆", (undoRotate * undoMove).ToArray());
        Point3d[] probes = [Point3d.Origin, new(1, 0, 0), new(0, 1, 0), new(0, 0, 1), new(3, 2, 0)];
        foreach (Point3d input in probes)
        {
            Point3d placed = input.TransformBy(product);
            Point3d byInverse = placed.TransformBy(inverse);
            Point3d byUndoSteps = placed.TransformBy(undoMove).TransformBy(undoRotate);
            WritePoint(editor, "逆行列で戻す：元のWCS座標", byInverse);
            WritePoint(editor, "移動を戻す→回転を戻す", byUndoSteps);
            WriteResidual(editor, "往復のずれ", input, byInverse);
            WriteResidual(editor, "逆順の個別操作のずれ", input, byUndoSteps);
        }
        Vector3d direction = new(3, 2, 1);
        Write(editor, "方向の変換：期待(-2,3,1)", Components(direction.TransformBy(product)));
        // 対称な輪郭の見かけが一致しても、この頂点は別の場所へ行く。
        WritePoint(editor, "正方形の頂点(1,1,0)の+90度回転", new Point3d(1, 1, 0).TransformBy(rotation));
        BasicTransformDrawing.Show(document, editor,
        [
            ("回転→移動：逐次", p => p.TransformBy(rotation).TransformBy(translation)),
            ("積 T R", p => p.TransformBy(product)),
            ("PreMultiplyBy", p => p.TransformBy(byPre)),
            ("PostMultiplyBy", p => p.TransformBy(byPost)),
            ("移動→回転：R T", p => p.TransformBy(reverseOrder)),
        ]);
    });
}

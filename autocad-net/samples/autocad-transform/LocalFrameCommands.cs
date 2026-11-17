using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>第1〜5章のBの平面上にローカル原点Cを置き、移動・回転の基準を比較する。ビルド・実機未検証。</summary>
public static class LocalFrameCommands
{
    /// <summary>ローカル頂点(3,2,0)の配置先P=(-3,7/5,26/5)から、6つの操作を比較する。</summary>
    /// <remarks>
    /// M=T(C)PlaneToWorld(N)、C=(0,3,4)、N=(0,3/5,4/5)。ここでの入力はOCS(u,v,5)ではなくC基準(u,v,0)。
    /// WCS Xへ2は(-1,7/5,26/5)、ローカルXへ2は(-5,7/5,26/5)。
    /// ローカル+Zの+π/2回転は(2,3/5,29/5)、WCS原点の+Z回転は(-7/5,-3,26/5)。
    /// 引数・選択なし。新規Line49本。灰色は配置済みB。保存・既存図形・UCS・表示設定は変更しない。
    /// </remarks>
    [CommandMethod("GEOAT_LOCAL_FRAME")]
    public static void LocalFrame() => BasicTransformDrawing.Run("GEOAT_LOCAL_FRAME", (document, editor) =>
    {
        Vector3d normal = new(0, 3.0 / 5, 4.0 / 5);
        Point3d center = new(0, 3, 4);
        Matrix3d localToWorld = Matrix3d.Displacement(new Vector3d(center.X, center.Y, center.Z))
            * Matrix3d.PlaneToWorld(normal);
        Vector3d localDisplacement = new(2, 0, 0);
        Matrix3d localMove = Matrix3d.Displacement(localDisplacement);
        Matrix3d worldMove = Matrix3d.Displacement(new Vector3d(2, 0, 0));
        // 方向の変換にはCを足さない。同じ数値(2,0,0)でもWCS Xとは逆向きになる。
        Vector3d worldDisplacement = localDisplacement.TransformBy(localToWorld);
        Matrix3d equivalentMove = Matrix3d.Displacement(worldDisplacement);
        Matrix3d localRotation = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);
        Matrix3d worldOriginRotation = Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);
        Vector3d worldAxis = Vector3d.ZAxis.TransformBy(localToWorld);
        Point3d worldCenter = Point3d.Origin.TransformBy(localToWorld);
        Matrix3d equivalentRotation = Matrix3d.Rotation(Math.PI / 2, worldAxis, worldCenter);
        Write(editor, "ローカルXへ2をWCS方向で表す：期待(-2,0,0)", Components(worldDisplacement));
        WritePoint(editor, "ローカル回転中心をWCSで表す", worldCenter);
        Write(editor, "ローカル回転軸をWCSで表す：N", Components(worldAxis));
        WriteMatrix(editor, "M：C基準のローカル→WCS", localToWorld.ToArray());
        WriteMatrix(editor, "WCS基準の移動 T_W M", (worldMove * localToWorld).ToArray());
        WriteMatrix(editor, "ローカル基準の移動 M T_L", (localToWorld * localMove).ToArray());
        WriteMatrix(editor, "変位をWCSへ直した移動", (equivalentMove * localToWorld).ToArray());
        WriteMatrix(editor, "WCS原点の回転 K_W M", (worldOriginRotation * localToWorld).ToArray());
        WriteMatrix(editor, "ローカル原点の回転 M K_L", (localToWorld * localRotation).ToArray());
        WriteMatrix(editor, "WCSで中心と軸を指定した同じ回転", (equivalentRotation * localToWorld).ToArray());
        Point3d[] placedB = [new(0, 3, 4), new(-4, 3, 4), new(-3, 7.0 / 5, 26.0 / 5), new(0, 11.0 / 5, 23.0 / 5)];
        BasicTransformDrawing.Show(document, editor,
        [
            ("WCS Xへ2：T_W M", p => p.TransformBy(worldMove * localToWorld)),
            ("ローカルXへ2：M T_L", p => p.TransformBy(localToWorld * localMove)),
            ("変位をWCSへ直す：方式2と同じ", p => p.TransformBy(localToWorld).TransformBy(equivalentMove)),
            ("WCS原点・+Zで回転", p => p.TransformBy(worldOriginRotation * localToWorld)),
            ("ローカル原点・+Zで回転", p => p.TransformBy(localToWorld * localRotation)),
            ("C・NでWCS回転：方式5と同じ", p => p.TransformBy(localToWorld).TransformBy(equivalentRotation)),
        ], reference: placedB, referenceProbe: new Point3d(0, 18.0 / 5, 24.0 / 5));
    });
}

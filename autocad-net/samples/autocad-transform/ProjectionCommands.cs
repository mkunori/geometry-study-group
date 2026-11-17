using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>第5章のQを、内積・正射影API・方向指定APIで平面へ移す補足。ビルド・実機未検証。</summary>
public static class ProjectionCommands
{
    /// <summary>Bの平面N・X=5へ射影。Q=(-3,13/5,34/5)の正射影はP=(-3,7/5,26/5)。</summary>
    /// <remarks>
    /// 引数・選択なし。斜方向D=(1,0,-1)では(-1/2,13/5,43/10)。平面と平行な方向はAPIへ渡さない。
    /// 新規Line42本。灰色はBを2N進めた平面外の輪郭。保存・既存図形・UCS・表示設定は変更しない。
    /// </remarks>
    [CommandMethod("GEOAT_PROJECTION")]
    public static void Projection() => BasicTransformDrawing.Run("GEOAT_PROJECTION", (document, editor) =>
    {
        Vector3d normal = new(0, 3.0 / 5, 4.0 / 5);
        Point3d center = new(0, 3, 4);
        using Plane plane = new(center, normal);
        Vector3d oblique = new(1, 0, -1);
        double denominator = normal.DotProduct(oblique);
        // この固定方向の内積は-4/5。方向を0にしたり、平行にしたりした入力はAPIへ流さない。
        if (!double.IsFinite(denominator) || Math.Abs(denominator) <= 1e-12)
            throw new System.InvalidOperationException("射影方向が平面にほぼ平行です。");
        Vector3d parallel = new(1, 0, 0);
        Write(editor, "平行方向のN・D：0なのでProjectへ渡さない", normal.DotProduct(parallel));
        Point3d[] outside =
        [
            new(0, 21.0 / 5, 28.0 / 5), new(-4, 21.0 / 5, 28.0 / 5),
            new(-3, 13.0 / 5, 34.0 / 5), new(0, 17.0 / 5, 31.0 / 5),
        ];
        // 3次元を2次元へ潰す射影行列をEntity.TransformByへは渡さず、結果の点からLineを作る。
        BasicTransformDrawing.Show(document, editor,
        [
            ("内積による正射影", p =>
            {
                double distance = normal.X * (p.X - center.X)
                    + normal.Y * (p.Y - center.Y) + normal.Z * (p.Z - center.Z);
                return new Point3d(p.X - distance * normal.X, p.Y - distance * normal.Y, p.Z - distance * normal.Z);
            }),
            ("Point3d.OrthoProject", p => p.OrthoProject(plane)),
            ("Point3d.Project：法線方向", p => p.Project(plane, normal)),
            ("斜方向の成分計算", p =>
            {
                double parameter = (normal.X * (center.X - p.X)
                    + normal.Y * (center.Y - p.Y) + normal.Z * (center.Z - p.Z)) / denominator;
                return new Point3d(p.X + parameter * oblique.X, p.Y + parameter * oblique.Y, p.Z + parameter * oblique.Z);
            }),
            ("Point3d.Project：斜方向", p => p.Project(plane, oblique)),
        ], source: outside);
    });
}

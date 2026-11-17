using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>平行移動の成分加算・ベクトル演算・行列を比較する。参照DLLでのビルド・実機は未検証。</summary>
public static class TranslationCommands
{
    /// <summary>WCSの頂点2=(3,2,0)を(10,0,3)だけ動かし、(13,2,3)を3方式で求める。</summary>
    /// <remarks>引数・選択なし、作図単位。新規Line28本。保存・既存図形・UCS・表示設定は変更しない。</remarks>
    [CommandMethod("GEOAT_TRANSLATION")]
    public static void Translation() => BasicTransformDrawing.Run("GEOAT_TRANSLATION", (document, editor) =>
    {
        Vector3d displacement = new(10, 0, 3);
        Matrix3d translation = Matrix3d.Displacement(displacement);
        WriteMatrix(editor, "T：WCSの移動", translation.ToArray());
        Vector3d direction = new(3, 2, 1);
        Vector3d translatedDirection = direction;
        translatedDirection = translatedDirection.TransformBy(translation);
        Write(editor, "方向(3,2,1)に移動は加わらない", Components(translatedDirection));
        // Entity.TransformByはvoidで図形自体を変更する。一時LineはDBへ登録せずusingで破棄する。
        using Line temporary = new(Point3d.Origin, new Point3d(3, 2, 0));
        temporary.TransformBy(translation);
        WritePoint(editor, "一時Lineの移動後の始点", temporary.StartPoint);
        WritePoint(editor, "一時Lineの移動後の終点：期待(13,2,3)", temporary.EndPoint);
        BasicTransformDrawing.Show(document, editor,
        [
            ("成分加算", p => new Point3d(p.X + displacement.X, p.Y + displacement.Y, p.Z + displacement.Z)),
            ("Point3d.Add", p => p.Add(displacement)),
            ("Displacement + TransformBy", p => p.TransformBy(translation)),
        ]);
    });
}

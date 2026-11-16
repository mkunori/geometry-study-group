using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>任意軸の分岐とPlaneToWorldの基底を比較する実演。ビルド・NETLOAD・実機実行は未検証。</summary>
public static class ArbitraryAxisCommands
{
    /// <summary>10個の固定法線で長さ・直交性・右手系・APIとの差を出力し、傾斜例の基底を重ねて描く。</summary>
    /// <remarks>
    /// 引数・選択なし。単位化後の|Nx|と|Ny|がともに1/64未満ならWCS Y、それ以外はWCS Zを選ぶ。
    /// 無効な法線は数学部分で拒否し、PlaneToWorldへ渡さない。新規Line9本のみ追加、保存・既存図形変更なし。
    /// 現在のUCS・表示を変更しない。期待される基底の一致を、AutoCADで実測済みとは扱わない。
    /// </remarks>
    [CommandMethod("GEOAT_ARBITRARY_AXIS")]
    public static void ArbitraryAxis()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        double boundary = ArbitraryAxisMath.AxisBoundary;
        double below = boundary * (1 - Math.Pow(2, -20));
        double above = boundary * (1 + Math.Pow(2, -20));
        (string Label, ArbitraryAxisMath.Vec Normal)[] cases =
        [
            ("+Z", new(0, 0, 1)),
            ("−Z", new(0, 0, -1)),
            ("+X", new(1, 0, 0)),
            ("傾斜：第1章の固定例B", new(0, 3, 4)),
            ("Z近傍", new(0, 1.0 / 128, Math.Sqrt(16383) / 128)),
            ("X境界直前", WithX(below)),
            ("X境界ちょうど", WithX(boundary)),
            ("X境界直後", WithX(above)),
            ("Y境界ちょうど", new(0, boundary, Math.Sqrt(1 - boundary * boundary))),
            ("斜め：3成分とも非ゼロ", new(1, 2, 2)),
        ];

        try
        {
            editor.WriteMessage("\nGEOAT_ARBITRARY_AXIS: 自作基底とPlaneToWorldのAPI計算値を比較（実機未検証）。");
            foreach (var item in cases) Inspect(editor, item.Label, item.Normal);
            ShowRejectedInputs(editor);
            ArbitraryAxisMath.Frame tilted = ArbitraryAxisMath.Build(new(0, 3, 4));
            Matrix3d api = Matrix3d.PlaneToWorld(Vector(tilted.Normal));
            // API側も標準基底から独立に変換し、自作側の軸を再利用しない。
            Vector3d apiX = Vector3d.XAxis.TransformBy(api);
            Vector3d apiY = Vector3d.YAxis.TransformBy(api);
            Vector3d apiZ = Vector3d.ZAxis.TransformBy(api);
            InModelSpace(document, (space, transaction) =>
            {
                Axis(space, transaction, Point3d.Origin, Vector3d.XAxis, 3, 8);
                Axis(space, transaction, Point3d.Origin, Vector3d.YAxis, 3, 8);
                Axis(space, transaction, Point3d.Origin, Vector3d.ZAxis, 3, 8);
                Axis(space, transaction, Point3d.Origin, Vector(tilted.XAxis), 3, 1);
                Axis(space, transaction, Point3d.Origin, Vector(tilted.YAxis), 3, 3);
                Axis(space, transaction, Point3d.Origin, Vector(tilted.Normal), 3, 5);
                Axis(space, transaction, Point3d.Origin, apiX, 3, 4);
                Axis(space, transaction, Point3d.Origin, apiY, 3, 6);
                Axis(space, transaction, Point3d.Origin, apiZ, 3, 2);
            });
            editor.WriteMessage("\nLine9本追加。全てWCS原点から表示長3。灰=WCS、自作X/Y/N=赤/緑/青、API X/Y/Z=シアン/マゼンタ/黄。一致する軸は重なります。保存なし。");
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        {
            editor.WriteMessage($"\nGEOAT_ARBITRARY_AXIS: AutoCAD例外。図面状態を確認してください。{error.Message}");
        }
    }

    /// <summary>Y=0、Zが正の単位法線。境界の比較はBuildで単位化した後の成分に対して行う。</summary>
    private static ArbitraryAxisMath.Vec WithX(double x) => new(x, 0, Math.Sqrt(1 - x * x));

    /// <summary>同じ法線から作った自作基底とAPI基底の各成分・距離残差を出力する。</summary>
    private static void Inspect(Editor editor, string label, ArbitraryAxisMath.Vec input)
    {
        ArbitraryAxisMath.Frame frame = ArbitraryAxisMath.Build(input);
        Matrix3d planeToWorld = Matrix3d.PlaneToWorld(Vector(frame.Normal));
        Vector3d apiX = Vector3d.XAxis.TransformBy(planeToWorld);
        Vector3d apiY = Vector3d.YAxis.TransformBy(planeToWorld);
        Vector3d apiZ = Vector3d.ZAxis.TransformBy(planeToWorld);
        editor.WriteMessage($"\n--- {label} ---");
        Write(editor, "入力法線：WCS方向", input);
        Write(editor, "単位N：分岐で使う値", frame.Normal);
        Write(editor, "|Nx|", Math.Abs(frame.Normal.X));
        Write(editor, "|Ny|", Math.Abs(frame.Normal.Y));
        editor.WriteMessage(frame.UsesWorldY ? "\n補助軸：WCS Y（両成分が境界未満）" : "\n補助軸：WCS Z（等号を含む）");
        Write(editor, "自作X", frame.XAxis);
        Write(editor, "自作Y", frame.YAxis);
        Write(editor, "|X|", ArbitraryAxisMath.Length(frame.XAxis));
        Write(editor, "|Y|", ArbitraryAxisMath.Length(frame.YAxis));
        Write(editor, "|N|", ArbitraryAxisMath.Length(frame.Normal));
        Write(editor, "X・Y", ArbitraryAxisMath.Dot(frame.XAxis, frame.YAxis));
        Write(editor, "Y・N", ArbitraryAxisMath.Dot(frame.YAxis, frame.Normal));
        Write(editor, "N・X", ArbitraryAxisMath.Dot(frame.Normal, frame.XAxis));
        Write(editor, "(X×Y)・N：右手系なら1", ArbitraryAxisMath.Dot(ArbitraryAxisMath.Cross(frame.XAxis, frame.YAxis), frame.Normal));
        Write(editor, "API X", Components(apiX));
        Write(editor, "API Y", Components(apiY));
        Write(editor, "API Z", Components(apiZ));
        Write(editor, "Xの距離残差", ArbitraryAxisMath.Length(ArbitraryAxisMath.Difference(frame.XAxis, Components(apiX))));
        Write(editor, "Yの距離残差", ArbitraryAxisMath.Length(ArbitraryAxisMath.Difference(frame.YAxis, Components(apiY))));
        Write(editor, "Nの距離残差", ArbitraryAxisMath.Length(ArbitraryAxisMath.Difference(frame.Normal, Components(apiZ))));
    }

    /// <summary>幾何的に方向を定められない入力を、APIへ渡す前に拒否する例。</summary>
    private static void ShowRejectedInputs(Editor editor)
    {
        ArbitraryAxisMath.Vec[] invalid = [new(0, 0, 0), new(double.NaN, 0, 1), new(0, double.PositiveInfinity, 1)];
        foreach (var input in invalid)
        {
            try
            {
                _ = ArbitraryAxisMath.Build(input);
            }
            catch (System.ArgumentException error)
            {
                editor.WriteMessage($"\n無効入力を拒否（API呼出し・描画なし）：{error.Message}");
            }
        }
    }
}

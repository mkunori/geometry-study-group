using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using static AutoCadGeometrySamples.TransformDemoSupport;

namespace AutoCadGeometrySamples;

/// <summary>第6〜7章の固定入力・表示・描画だけを共有する。学習対象の変換式は各コマンドへ残す。</summary>
internal static class BasicTransformDrawing
{
    /// <summary>第1章と同じ非対称輪郭を、標準XY平面のWCS点として毎回新しく用意する。</summary>
    internal static Point3d[] Profile() =>
        [new(0, 0, 0), new(4, 0, 0), new(3, 2, 0), new(0, 1, 0)];

    /// <summary>現在図面がある通常コマンドだけを対象に、実行とAutoCAD例外の表示を共通化する。</summary>
    /// <remarks>保存・UCS・ビュー・既存図形は変更しない。Commit後の失敗までロールバックとは断定しない。</remarks>
    internal static void Run(string command, Action<Document, Editor> execute)
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        try
        {
            document.Editor.WriteMessage($"\n{command}: 固定入力のAPI計算値。実機未検証。期待値は詳細解説・READMEを参照。");
            execute(document, document.Editor);
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        {
            document.Editor.WriteMessage($"\n{command}: AutoCAD例外。図面状態を確認してください。{error.Message}");
        }
    }

    /// <summary>同じ入力を各方式へ独立に渡し、原点・3軸上の点・頂点2と非対称輪郭を比較する。</summary>
    /// <param name="document">現在図面。新規の閉輪郭Line列だけを登録する。</param>
    /// <param name="editor">WCSの計算座標と方式1との差をG17で表示する。</param>
    /// <param name="methods">入力点は値型のコピー。異なる操作も比較できるよう0差を一律には要求しない。</param>
    /// <param name="source">変換前の固定4頂点。省略時は標準XY平面の輪郭。</param>
    /// <param name="reference">灰色の比較元をWCSで指定。ローカル入力のときは配置済みの位置を渡す。</param>
    /// <param name="referenceProbe">灰色のZ軸上の比較点。ローカル入力では配置後のWCS位置を渡す。</param>
    internal static void Show(Document document, Editor editor,
        (string Label, Func<Point3d, Point3d> Apply)[] methods,
        Point3d[]? source = null, Point3d[]? reference = null, Point3d? referenceProbe = null)
    {
        Point3d[] input = source ?? Profile();
        Point3d[] probes = [Point3d.Origin, new(1, 0, 0), new(0, 1, 0), new(0, 0, 1), input[2]];
        string[] names = ["原点", "X軸上の点", "Y軸上の点", "Z軸上の点", "頂点2"];
        for (int index = 0; index < probes.Length; index++)
        {
            WritePoint(editor, $"入力：{names[index]}", probes[index]);
            Point3d first = methods[0].Apply(probes[index]);
            for (int method = 0; method < methods.Length; method++)
            {
                Point3d result = methods[method].Apply(probes[index]);
                WritePoint(editor, methods[method].Label + "：WCS結果", result);
                WriteResidual(editor, "方式1との距離（同じ操作の組だけ0を期待）", result, first);
            }
        }
        // 変換済みの点を別方式へ渡さず、すべて元の4頂点から計算する。
        Point3d[][] outlines = methods.Select(m => input.Select(p => m.Apply(p)).ToArray()).ToArray();
        InModelSpace(document, (space, transaction) =>
        {
            OcsWcsDemoDrawing.Outline(space, transaction, reference ?? input, 8);
            OcsWcsDemoDrawing.Marker(space, transaction, referenceProbe ?? probes[3], 8);
            int[] colors = [1, 3, 5, 2, 6, 4];
            for (int method = 0; method < methods.Length; method++)
            {
                OcsWcsDemoDrawing.Outline(space, transaction, outlines[method], colors[method]);
                OcsWcsDemoDrawing.Marker(space, transaction, methods[method].Apply(probes[3]), colors[method]);
            }
        });
        editor.WriteMessage($"\n新規Line {7 * (methods.Length + 1)}本。閉輪郭4辺とZ軸上の比較点の十字3本ずつ。灰色は比較元、方式順に赤・緑・青・黄・マゼンタ・シアン。保存なし。再実行は重複追加。");
    }
}

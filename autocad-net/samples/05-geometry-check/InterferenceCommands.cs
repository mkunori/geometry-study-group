using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCadGeometrySamples;

/// <summary>AABB候補・曲線の交点・ソリッドの干渉フラグを区別する参考コード。公式資料との静的照合のみ。</summary>
/// <remarks>
/// Windows版AutoCAD 2026 Update 1.2以降／.NET 10想定。ビルド未検証・NETLOAD未実施・AutoCAD実機未検証。
/// 全てWCS・作図単位。生成物とBoolean用コピーはDB未登録の一時オブジェクトで、usingで破棄する。
/// 図面DBの登録・既存図形の変更・保存・UCS変更は行わず、Transactionも作成しない。
/// </remarks>
public static class InterferenceCommands
{
    /// <summary>第5回のA/B/C/Dと、AABBだけでは実体の重なりが決まらない2三角形を比較する。</summary>
    /// <remarks>引数なし。閉じた平面輪郭の範囲をXYだけで比較し、Z=0を3D体積の重なりと混同しない。</remarks>
    [CommandMethod("GEO05_BOUNDS")]
    public static void BoundsCandidates()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        editor.WriteMessage("\nGEO05_BOUNDS: APIの範囲と包囲箱の候補判定（実機未検証）。図面変更なし。");
        try
        {
            using (Polyline a = Rectangle(0, 0, 2, 2))
            using (Polyline b = Rectangle(2, 1, 4, 3))
            using (Polyline c = Rectangle(2.1, 1, 4, 3))
            using (Polyline d = Rectangle(1, 1, 3, 3))
            {
                ShowBounds2D(editor, "A/B：手計算では接触", a, b);
                ShowBounds2D(editor, "A/C：手計算では離隔", a, c);
                ShowBounds2D(editor, "A/D：手計算では正の共有面積", a, d);
            }
            using (Polyline t1 = CreateLoop([new(0, 0), new(3, 0), new(0, 3)]))
            using (Polyline t2 = CreateLoop([new(2, 2), new(5, 2), new(2, 5)]))
            {
                ShowBounds2D(editor, "T1/T2：AABBの重なりと実体を分ける", t1, t2);
                editor.WriteMessage("\n  数学上はT1がx+y<=3、T2がx+y>=4なので離隔。AABB候補から実体の重なりを断定しません。");
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        { editor.WriteMessage($"\nGEO05_BOUNDS: 範囲を取得できません。候補外とは扱いません。{error.Message}"); }
    }

    /// <summary>有限な曲線の返却点と、包囲箱の候補・領域の重なりが別であることを比較する。</summary>
    /// <remarks>引数なし。詳細な線分のt/u・端点・重複区間の分類は第3回を参照。ここでは点集合の意味を読む。</remarks>
    [CommandMethod("GEO05_INTERSECT")]
    public static void CurveIntersectionPoints()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        editor.WriteMessage("\nGEO05_INTERSECT: IntersectWithの点コレクション（実機未検証）。図面変更なし。");
        using (Line first = new(new Point3d(0, 0, 0), new Point3d(2, 2, 0)))
        using (Line crossing = new(new Point3d(0, 2, 0), new Point3d(2, 0, 0)))
        using (Line parallel = new(new Point3d(0, 0.5, 0), new Point3d(2, 2.5, 0)))
        {
            ShowCurvePoints(editor, "交差例：手計算の交点は(1,1,0)", first, crossing);
            ShowCurvePoints(editor, "平行例：AABBは重なるが線分は離れる", first, parallel);
        }
    }

    /// <summary>共通体積1と面接触の箱で干渉フラグを表示し、別途コピー上のBoolIntersectを示す。</summary>
    /// <remarks>引数なし。K/K3のAPIフラグがtrue/falseのどちらになるかは断言しない。全箱・コピーを破棄し、図面へ追加しない。</remarks>
    [CommandMethod("GEO05_SOLIDS")]
    public static void SolidInterference()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        editor.WriteMessage("\nGEO05_SOLIDS: APIの干渉フラグと数学上の共有体積を区別（実機未検証）。図面変更なし。");
        try
        {
            using (Solid3d k = CreateBoxAt(new(0, 0, 0), new(2, 2, 2)))
            using (Solid3d k2 = CreateBoxAt(new(1, 1, 1), new(3, 3, 3)))
            using (Solid3d k3 = CreateBoxAt(new(2, 1, 1), new(3, 2, 2)))
            {
                WriteBounds(editor, "K", k.GeometricExtents);
                WriteBounds(editor, "K2", k2.GeometricExtents);
                WriteBounds(editor, "K3", k3.GeometricExtents);
                ShowInterference(editor, "K/K2：数学上の共通体積1", k, k2);
                ShowInterference(editor, "K/K3：数学上は面接触、共通体積0・接触面積1", k, k3);
                // フラグから三値分類を推測せず、別の操作として既知の正体積例だけをコピー上で処理する。
                ShowBooleanIntersection(editor, k, k2);
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        { editor.WriteMessage($"\nGEO05_SOLIDS: 生成・移動・範囲取得に失敗しました。離隔や体積0へ読み替えません。{error.Message}"); }
        catch (InvalidOperationException error)
        { editor.WriteMessage($"\nGEO05_SOLIDS: 固定した箱の条件に一致しないため中止。{error.Message}"); }
    }

    /// <summary>取得した2つのWCS包囲箱をXYの区間で比較する。実体の関係へは変換しない。</summary>
    /// <param name="editor">出力先。</param><param name="label">比較する固定例。</param>
    /// <param name="first">第1図形。</param><param name="second">第2図形。</param>
    /// <remarks>この例はZ=0の曲線。ゼロのZ幅は比較しない。例外は呼出し側で結果未取得として扱う。</remarks>
    private static void ShowBounds2D(Editor editor, string label, Entity first, Entity second)
    {
        editor.WriteMessage($"\n\n{label}");
        Extents3d a = first.GeometricExtents, b = second.GeometricExtents;
        WriteBounds(editor, "第1図形", a);
        WriteBounds(editor, "第2図形", b);
        if (!ValidBounds(a) || !ValidBounds(b))
        { editor.WriteMessage("\n  範囲が無効のため判定保留。離隔とは扱いません。"); return; }
        double dx = Math.Min(a.MaxPoint.X, b.MaxPoint.X) - Math.Max(a.MinPoint.X, b.MinPoint.X);
        double dy = Math.Min(a.MaxPoint.Y, b.MaxPoint.Y) - Math.Max(a.MinPoint.Y, b.MinPoint.Y);
        if (!double.IsFinite(dx) || !double.IsFinite(dy))
        { editor.WriteMessage("\n  候補幅が非有限のため判定保留。"); return; }
        WriteScalar(editor, "包囲箱の候補幅Δx", dx);
        WriteScalar(editor, "包囲箱の候補幅Δy", dy);
        editor.WriteMessage(dx < 0 || dy < 0 ? "\n  XYの包囲箱の候補外。"
            : dx == 0 || dy == 0 ? "\n  XYの包囲箱が境界で重なる候補。実体の接触は未判定。"
            : "\n  XYの包囲箱が正幅で重なる候補。実体の重なりは未判定。");
    }

    /// <summary>延長しない有限Lineの交点を、毎回新しい点コレクションへ取得して全点を表示する。</summary>
    /// <param name="editor">出力先。</param><param name="label">固定例。</param>
    /// <param name="first">第1の非零長Line。</param><param name="second">第2の非零長Line。</param>
    private static void ShowCurvePoints(Editor editor, string label, Line first, Line second)
    {
        try
        {
            ShowBounds2D(editor, label, first, second);
            using (Point3dCollection points = new())
            {
                first.IntersectWith(second, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                editor.WriteMessage($"\n  API返却要素数={points.Count}。面積・体積・重複区間を返す値ではありません。");
                foreach (Point3d point in points) WritePoint(editor, "API返却点（WCS）", point);
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        { editor.WriteMessage($"\n  曲線の範囲・交点の結果未取得。交点なしとは扱いません。{error.Message}"); }
    }

    /// <summary>boolだけを返すManaged版CheckInterferenceを呼び、教材の三値分類へ変換しない。</summary>
    /// <param name="editor">出力先。</param><param name="label">数学上の比較条件。</param>
    /// <param name="first">第1一時ソリッド。</param><param name="second">第2一時ソリッド。</param>
    /// <remarks>採用したC#署名には共有ソリッドの戻り値やout引数はない。</remarks>
    private static void ShowInterference(Editor editor, string label, Solid3d first, Solid3d second)
    {
        try
        {
            bool flag = first.CheckInterference(second);
            editor.WriteMessage($"\n  {label}: CheckInterferenceのAPIフラグ={flag}。面接触の意味を含め、実機未検証。");
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        { editor.WriteMessage($"\n  {label}: API結果未取得。falseへ読み替えません。{error.Message}"); }
    }

    /// <summary>元の2箱を残し、両方をコピーしてから共通体積の形を作る。結果もDBへ登録しない。</summary>
    /// <param name="editor">出力先。</param><param name="k">Kの原本。</param><param name="k2">K2の原本。</param>
    /// <remarks>Booleanは受け手を変更し、引数側のモデルも消費しうる。失敗時も両コピーを破棄し、再利用しない。</remarks>
    private static void ShowBooleanIntersection(Editor editor, Solid3d k, Solid3d k2)
    {
        try
        {
            using (Solid3d result = CloneSolid(k))
            using (Solid3d operand = CloneSolid(k2))
            {
                result.BooleanOperation(BooleanOperationType.BoolIntersect, operand);
                // 共有形状は新しい戻り値ではなく、変更された受け手result。operandの形は読まない。
                WriteBounds(editor, "BoolIntersect後のコピーの範囲", result.GeometricExtents);
                editor.WriteMessage("\n  共通形状の数学上の期待値は[1,2]^3、体積1。この範囲表示は体積測定ではありません。");
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        { editor.WriteMessage($"\n  Booleanまたは結果範囲の取得に失敗。共通体積0とは扱いません。{error.Message}"); }
        catch (InvalidOperationException error)
        { editor.WriteMessage($"\n  Boolean用コピーを準備できません。{error.Message}"); }
    }

    /// <summary>Cloneのobject型の戻り値と失敗時のnullを検査し、呼出し側へ所有権を渡す。</summary>
    /// <param name="source">変更しない一時ソリッド。</param><returns>呼出し側のusingで破棄するSolid3dコピー。</returns>
    private static Solid3d CloneSolid(Solid3d source)
    {
        object? copy = source.Clone();
        if (copy is Solid3d solid) return solid;
        if (copy is IDisposable disposable) disposable.Dispose();
        throw new InvalidOperationException("Cloneの結果がSolid3dではありません。");
    }

    /// <summary>軸平行な箱を作り、生成直後のMinPointを指定した下端へ移動する。</summary>
    /// <param name="min">希望するWCS下端。</param><param name="max">希望するWCS上端。</param>
    /// <returns>DB未登録の一時Solid3d。呼出し側のusingで破棄する。</returns>
    /// <remarks>CreateBoxの原点を下端だと仮定しない。固定入力向けであり、任意スケールのカーネル成功は保証しない。</remarks>
    private static Solid3d CreateBoxAt(Point3d min, Point3d max)
    {
        double x = max.X - min.X, y = max.Y - min.Y, z = max.Z - min.Z;
        if (!Finite(min) || !Finite(max) || !double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z) || x <= 0 || y <= 0 || z <= 0)
            throw new InvalidOperationException("有限で正の3辺を持つ箱だけを扱います。");
        Solid3d solid = new();
        try
        {
            solid.CreateBox(x, y, z);
            Extents3d initial = solid.GeometricExtents;
            if (!ValidBounds(initial)) throw new InvalidOperationException("生成直後の箱の範囲が無効です。");
            solid.TransformBy(Matrix3d.Displacement(initial.MinPoint.GetVectorTo(min)));
            return solid;
        }
        catch { solid.Dispose(); throw; }
    }

    /// <summary>固定した長方形領域の境界を表す一時Polylineを作る。内部を面エンティティとして作る処理ではない。</summary>
    /// <param name="minX">X下端。</param><param name="minY">Y下端。</param><param name="maxX">X上端。</param><param name="maxY">Y上端。</param>
    /// <returns>呼出し側のusingで破棄する閉じた輪郭。</returns>
    private static Polyline Rectangle(double minX, double minY, double maxX, double maxY) =>
        CreateLoop([new(minX, minY), new(maxX, minY), new(maxX, maxY), new(minX, maxY)]);

    /// <summary>固定した三角形・長方形の頂点から、ECSとWCSのXYが一致する閉輪郭を作る。</summary>
    /// <param name="vertices">Z=0の単純な固定輪郭の頂点。汎用輪郭検証は対象外。</param>
    /// <returns>DB未登録のPolyline。設定途中の例外ではここで破棄し、成功時は呼出し側へ所有権を渡す。</returns>
    private static Polyline CreateLoop(Point2d[] vertices)
    {
        Polyline loop = new();
        try
        {
            loop.Normal = Vector3d.ZAxis;
            loop.Elevation = 0;
            for (int i = 0; i < vertices.Length; i++) loop.AddVertexAt(i, vertices[i], 0, 0, 0);
            loop.Closed = true;
            return loop;
        }
        catch { loop.Dispose(); throw; }
    }

    /// <summary>範囲の点が有限で、各軸の下端と上端が逆転していないかを調べる。平面のZ幅0は許す。</summary>
    /// <param name="bounds">SDKから取得したWCSの範囲。</param><returns>有限な順序付き範囲ならtrue。</returns>
    private static bool ValidBounds(Extents3d bounds) => Finite(bounds.MinPoint) && Finite(bounds.MaxPoint) &&
        bounds.MinPoint.X <= bounds.MaxPoint.X && bounds.MinPoint.Y <= bounds.MaxPoint.Y && bounds.MinPoint.Z <= bounds.MaxPoint.Z;

    /// <summary>点の全成分にNaNやInfinityがないかを検査する。</summary>
    /// <param name="point">検査点。</param><returns>有限ならtrue。</returns>
    private static bool Finite(Point3d point) => double.IsFinite(point.X) && double.IsFinite(point.Y) && double.IsFinite(point.Z);

    /// <summary>範囲の両端を表示する。範囲を実体や共通体積と呼び換えない。</summary>
    /// <param name="editor">出力先。</param><param name="label">対象名。</param><param name="bounds">WCSの包囲箱。</param>
    private static void WriteBounds(Editor editor, string label, Extents3d bounds)
    {
        WritePoint(editor, $"{label}のMinPoint", bounds.MinPoint);
        WritePoint(editor, $"{label}のMaxPoint", bounds.MaxPoint);
    }

    /// <summary>点の3成分を有効数字17桁で表示する。</summary>
    /// <param name="editor">出力先。</param><param name="label">点の役割。</param><param name="point">表示点。</param>
    private static void WritePoint(Editor editor, string label, Point3d point) =>
        editor.WriteMessage(string.Format(CultureInfo.InvariantCulture, "\n  {0}: ({1:G17}, {2:G17}, {3:G17})", label, point.X, point.Y, point.Z));

    /// <summary>候補幅などのスカラーを有効数字17桁で表示し、微小な差を丸めて接触と扱わない。</summary>
    /// <param name="editor">出力先。</param><param name="label">量の名前。</param><param name="value">値。</param>
    private static void WriteScalar(Editor editor, string label, double value) =>
        editor.WriteMessage($"\n  {label}: {value.ToString("G17", CultureInfo.InvariantCulture)}");
}

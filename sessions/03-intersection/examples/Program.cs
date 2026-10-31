using System.Globalization;
using System.Numerics;
using System.Text;
using static IntersectionMath;

/// <summary>第3回の固定例と境界条件を、7題材で手計算・API・残差と比較する。</summary>
/// <remarks>引数・標準入力・GUI・外部パッケージは不要。入力はソース内の固定値。</remarks>
internal static class Program
{
    private const double DoubleTolerance = 1e-12;
    private const double FloatTolerance = 1e-5;
    private static int checks;

    /// <summary>全題材を実行し、失敗ラベルを表示して非0で終了する。</summary>
    /// <returns>全件成功なら0、チェック失敗なら1。</returns>
    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            Run("1. 媒介変数と範囲", ParameterDomains);
            Run("2. 2D無限直線の交点", Lines2D);
            Run("3. 2D線分・端点・重複区間", Segments2D);
            Run("4. 三角形から平面の法線", TrianglePlane);
            Run("5. 3D直線と平面", LineAndPlane);
            Run("6. 2平面の交線", TwoPlanes);
            Run("7. 誤差・例外・尺度・境界", PrecisionAndExceptions);
            Console.WriteLine($"\nすべての {checks} チェックに成功しました。");
            return 0;
        }
        catch (InvalidOperationException error)
        {
            Console.Error.WriteLine($"FAIL: {error.Message}");
            return 1;
        }
    }

    /// <summary>題材を区切り、実際に成功したチェック数を表示する。</summary>
    /// <param name="title">題材名。</param><param name="sample">実行する題材。</param>
    private static void Run(string title, Action sample)
    {
        Console.WriteLine($"\n{title}");
        int before = checks;
        sample();
        Console.WriteLine($"  この題材: {checks - before} チェック");
    }

    /// <summary>同じ式でも、変数の範囲で点の集合が変わることを確認する。</summary>
    private static void ParameterDomains()
    {
        // 解説②／基本問題1-1,4。方向は変位で、正規化して終点を変えない。
        V2 p = (0, 0), r = (4, 4);
        foreach (var (t, x, y) in new[] { (0.0, 0.0, 0.0), (1.0, 4.0, 4.0), (.5, 2.0, 2.0), (1.5, 6.0, 6.0), (-.5, -2.0, -2.0) })
        {
            var point = Add2(p, Scale2(r, t));
            Check($"P+t r (t={t})", [point.X, point.Y], [x, y]);
        }
        Assert("線分の両端を含む", InClosedUnitInterval(0) && InClosedUnitInterval(1));
        double outside = 1.5, negative = -.5;
        Assert("t=1.5は直線と半直線に属し線分外", outside >= 0 && !InClosedUnitInterval(outside));
        Assert("t=-.5は直線上だが半直線・線分外", negative < 0 && !InClosedUnitInterval(negative));
    }

    /// <summary>固定例Aの符号・t/u・共有点を成分式とAPIで検算する。</summary>
    private static void Lines2D()
    {
        // 解説③／基本問題1-2,3。F11でCross2、LineLine2の分子と分母を追える。
        V2 p = (0, 0), r = (4, 4), q = (0, 4), s = (4, -4);
        var w = Sub2(q, p);
        double denominator = Cross2(r, s), numeratorT = Cross2(w, s), numeratorU = Cross2(w, r);
        Check("A: 分母・分子T・分子U", [denominator, numeratorT, numeratorU], [-32, -16, -16]);
        var result = LineLine2(p, r, q, s);
        Kind2("A: 唯一の交点", result, IntersectionKind.Point);
        Check("A: t/u/交点", [result.T!.Value, result.U!.Value, result.Point!.Value.X, result.Point.Value.Y], [.5, .5, 2, 2]);
        Residual2("A", result, p, r, q, s);
        // 2D外積はスカラー。Vector3にZ=0で埋め込み、そのZ成分と比較する。
        Check("A: Vector3.CrossのZ", [Vector3.Cross(new(4, 4, 0), new(4, -4, 0)).Z], [denominator], FloatTolerance);
        Check("A: Vector2による両式の位置", [(new Vector2(0, 4) + .5f * new Vector2(4, -4)).X,
            (new Vector2(0, 4) + .5f * new Vector2(4, -4)).Y], [2, 2], FloatTolerance);
        var origin = LineLine2((0, 0), (1, 0), (0, -1), (0, 1));
        Kind2("原点が正しい共有点である場合", origin, IntersectionKind.Point);
        Check("原点の共有点", [origin.Point!.Value.X, origin.Point.Value.Y], [0, 0]);
    }

    /// <summary>固定例B/Cを含む閉線分の共有部分を分類する。</summary>
    private static void Segments2D()
    {
        // 解説④／基本問題2。無限直線を解くことと、両方の[0,1]を調べることを分ける。
        var inside = SegmentSegment2((0, 0), (4, 4), (0, 4), (4, -4));
        Kind2("A: 線分内部", inside, IntersectionKind.Point);
        Assert("A: 両変数が内部", inside.T > 0 && inside.T < 1 && inside.U > 0 && inside.U < 1);
        Residual2("A線分", inside, (0, 0), (4, 4), (0, 4), (4, -4));

        var extension = LineLine2((0, 0), (2, 0), (3, -1), (0, 2));
        Kind2("C: 無限直線は一点で交差", extension, IntersectionKind.Point);
        Check("C: 範囲外のt/u/位置", [extension.T!.Value, extension.U!.Value, extension.Point!.Value.X, extension.Point.Value.Y], [1.5, .5, 3, 0]);
        Residual2("C延長", extension, (0, 0), (2, 0), (3, -1), (0, 2));
        Kind2("C: 線分は交点なし", SegmentSegment2((0, 0), (2, 0), (3, -1), (0, 2)), IntersectionKind.None);

        var endpoint = SegmentSegment2((0, 0), (2, 0), (2, -1), (0, 2));
        Kind2("C: 端点接触", endpoint, IntersectionKind.Point);
        Check("C: 端点のt/u", [endpoint.T!.Value, endpoint.U!.Value], [1, .5]);
        Residual2("C端点", endpoint, (0, 0), (2, 0), (2, -1), (0, 2));

        Kind2("B: 平行な別直線", LineLine2((0, 0), (4, 0), (0, 2), (2, 0)), IntersectionKind.None);
        Kind2("B: 同一直線", LineLine2((0, 0), (4, 0), (1, 0), (2, 0)), IntersectionKind.Coincident);
        Kind2("B: 平行な別線分", SegmentSegment2((0, 0), (4, 0), (0, 2), (2, 0)), IntersectionKind.None);
        Kind2("同一直線・離れた線分", SegmentSegment2((0, 0), (4, 0), (5, 0), (2, 0)), IntersectionKind.None);
        var touch = SegmentSegment2((0, 0), (4, 0), (4, 0), (2, 0));
        Kind2("同一直線・端点1点", touch, IntersectionKind.Point);
        Check("同一直線・端点のt/u", [touch.T!.Value, touch.U!.Value], [1, 0]);
        Residual2("同一直線接触", touch, (0, 0), (4, 0), (4, 0), (2, 0));

        var overlap = SegmentSegment2((0, 0), (4, 0), (2, 0), (4, 0));
        Kind2("C: 重複区間", overlap, IntersectionKind.OverlapSegment);
        var start = overlap.OverlapStart!.Value; var end = overlap.OverlapEnd!.Value;
        Check("C: 重複区間の両端", [start.X, start.Y, end.X, end.Y], [2, 0, 4, 0]);
        OnBothSegments("重複始端", start, (0, 0), (4, 0), (2, 0), (4, 0));
        OnBothSegments("重複終端", end, (0, 0), (4, 0), (2, 0), (4, 0));
        var reverse = SegmentSegment2((0, 0), (4, 0), (6, 0), (-4, 0));
        Kind2("逆向きの第2線分も重複", reverse, IntersectionKind.OverlapSegment);
        Check("逆向きでも同じ両端", [reverse.OverlapStart!.Value.X, reverse.OverlapEnd!.Value.X], [2, 4]);
        Kind2("第1線分が零長", SegmentSegment2((2, 0), (0, 0), (0, 0), (4, 0)), IntersectionKind.Degenerate);
        Kind2("第2線分が零長", SegmentSegment2((0, 0), (4, 0), (2, 0), (0, 0)), IntersectionKind.Degenerate);
        Kind2("両方が零長", SegmentSegment2((2, 0), (0, 0), (2, 0), (0, 0)), IntersectionKind.Degenerate);
    }

    /// <summary>固定例Dで法線と平面条件を作り、退化三角形を拒否する。</summary>
    private static void TrianglePlane()
    {
        // 解説⑤／基本問題3-1。法線は単位長にせず、(0,0,6)を次の題材でも使う。
        V3 a = (0, 0, 2), b = (2, 0, 2), c = (0, 3, 2);
        Assert("D: 三角形の法線を定義できる", TryTriangleNormal(a, b, c, out var n));
        Check("D: 法線", [n.X, n.Y, n.Z], [0, 0, 6]);
        Check("D: 両辺と法線の直交", [Dot3(n, Sub3(b, a)), Dot3(n, Sub3(c, a))], [0, 0]);
        var api = Vector3.Cross(new(2, 0, 0), new(0, 3, 0));
        Check("D: Vector3.Cross", [api.X, api.Y, api.Z], [0, 0, 6], FloatTolerance);
        foreach (var vertex in new[] { a, b, c }) Check("D: 頂点の平面残差", [Dot3(n, Sub3(vertex, a))], [0]);
        Assert("一直線の3点は法線未定義", !TryTriangleNormal(a, b, (4, 0, 2), out _));
        Assert("重複頂点は法線未定義", !TryTriangleNormal(a, a, c, out _));
        Assert("面積の下限以下の三角形", !TryTriangleNormal((0, 0, 0), (1e-7, 0, 0), (0, 1e-7, 0), out _));
    }

    /// <summary>固定例Dの一点・なし・直線全体を、内積と残差で区別する。</summary>
    private static void LineAndPlane()
    {
        // 解説⑥／基本問題3-2〜5。分母0の割り算を行わず、位置の条件へ分岐する。
        V3 p = (1, 2, 0), d = (2, 0, 4), a = (0, 0, 2), n = (0, 0, 6);
        double numerator = Dot3(n, Sub3(a, p)), denominator = Dot3(n, d);
        Check("D: 分子・分母", [numerator, denominator], [12, 24]);
        var result = LinePlane3(p, d, a, n);
        Kind3("D: 唯一の交点", result, IntersectionKind.Point);
        var x = result.Point!.Value;
        Check("D: tと交点", [result.T!.Value, x.X, x.Y, x.Z], [.5, 2, 2, 2]);
        var onLine = Add3(p, Scale3(d, result.T.Value));
        Check("D: 直線の残差", [x.X - onLine.X, x.Y - onLine.Y, x.Z - onLine.Z], [0, 0, 0]);
        Check("D: 平面の残差", [Dot3(n, Sub3(x, a))], [0]);
        Assert("D: 線分内部でも交差", result.T > 0 && result.T < 1);
        var end = Add3(p, d);
        Check("D: 線分終点", [end.X, end.Y, end.Z], [3, 2, 4]);
        Check("D: Vector3.Dotの分子・分母", [Vector3.Dot(new(0, 0, 6), new(-1, -2, 2)),
            Vector3.Dot(new(0, 0, 6), new(2, 0, 4))], [12, 24], FloatTolerance);
        var apiX = new Vector3(1, 2, 0) + .5f * new Vector3(2, 0, 4);
        Check("D: APIで同じ位置", [apiX.X, apiX.Y, apiX.Z], [2, 2, 2], FloatTolerance);
        Kind3("D: 平面と平行で離れる", LinePlane3(p, (1, 0, 0), a, n), IntersectionKind.None);
        Kind3("D: 直線全体が平面内", LinePlane3((1, 2, 2), (1, 0, 0), a, n), IntersectionKind.Coincident);

        var outside = LinePlane3(p, (.5, 0, .5), a, n);
        Kind3("無限直線では交差するが線分外", outside, IntersectionKind.Point);
        Check("線分範囲外のt", [outside.T!.Value], [4]);
        Assert("t=4を線分の交点としない", outside.T > 1);
        var endpoint = LinePlane3(p, (1, 0, 2), a, n);
        Kind3("線分の終点が平面上", endpoint, IntersectionKind.Point);
        Check("平面に届く終点t=1", [endpoint.T!.Value], [1]);
        var initial = LinePlane3((1, 2, 2), d, a, n);
        Kind3("線分の始点が平面上", initial, IntersectionKind.Point);
        Check("平面にある始点t=0", [initial.T!.Value], [0]);
    }

    /// <summary>固定例Eと追加の任意法線例で、方向と通過点を別に検証する。</summary>
    private static void TwoPlanes()
    {
        // 解説⑦／発展問題。交線方向を求めるだけでなく、2元連立式へF11で入る。
        V3 a1 = (0, 0, 2), n1 = (0, 0, 1), a2 = (1, 0, 0), n2 = (1, 0, 0);
        var result = PlanePlane3(a1, n1, a2, n2);
        Kind3("E: 交わりは交線", result, IntersectionKind.Line);
        var x = result.Point!.Value; var v = result.Direction!.Value;
        Check("E: 通過点", [x.X, x.Y, x.Z], [1, 0, 2]);
        Check("E: 交線方向", [v.X, v.Y, v.Z], [0, 1, 0]);
        Check("E: 両法線と方向の直交", [Dot3(n1, v), Dot3(n2, v)], [0, 0]);
        foreach (double t in new[] { -2.0, 0, 3 })
        {
            var point = Add3(x, Scale3(v, t));
            Check("E: 両平面の残差", [Dot3(n1, Sub3(point, a1)), Dot3(n2, Sub3(point, a2))], [0, 0]);
        }
        var api = Vector3.Cross(new(0, 0, 1), new(1, 0, 0));
        Check("E: APIによる方向", [api.X, api.Y, api.Z], [v.X, v.Y, v.Z], FloatTolerance);
        Kind3("平行で別の平面", PlanePlane3(a1, n1, (0, 0, 5), n1), IntersectionKind.None);
        Kind3("同一平面", PlanePlane3(a1, n1, a1, n1), IntersectionKind.Coincident);
        Kind3("逆向き法線でも同一平面", PlanePlane3(a1, n1, (2, 3, 2), (0, 0, -2)), IntersectionKind.Coincident);

        // 自由座標の3分岐と、軸に平行でない法線でも両方の条件を満たすことを確認する。
        foreach (var sample in new (V3 A, V3 N, V3 B, V3 M, V3 Expected)[] {
            ((1,0,0), (1,0,0), (0,2,0), (0,1,0), (1,2,0)),
            ((0,2,0), (0,1,0), (0,0,3), (0,0,1), (0,2,3)),
            ((1,0,0), (1,2,0), (0,-1,0), (0,1,3), (0,.5,-.5)) })
        {
            var line = PlanePlane3(sample.A, sample.N, sample.B, sample.M);
            Kind3("追加平面の交線", line, IntersectionKind.Line);
            var point = line.Point!.Value; var direction = line.Direction!.Value;
            Check("連立式から求めた通過点", [point.X, point.Y, point.Z], [sample.Expected.X, sample.Expected.Y, sample.Expected.Z]);
            var moved = Add3(point, Scale3(direction, 2));
            Check("追加平面の通過点と移動後の残差", [Dot3(sample.N, Sub3(point, sample.A)),
                Dot3(sample.M, Sub3(point, sample.B)), Dot3(sample.N, Sub3(moved, sample.A)),
                Dot3(sample.M, Sub3(moved, sample.B))], [0, 0, 0, 0]);
        }
    }

    /// <summary>数値的な保留を交点なしと分け、尺度・型・端点・無効入力を確認する。</summary>
    private static void PrecisionAndExceptions()
    {
        // 解説⑧。角度と距離と無次元パラメータを、異なる条件で検査する。
        Kind2("ゼロ方向の無限直線", LineLine2((0, 0), (0, 0), (0, 1), (1, 0)), IntersectionKind.Degenerate);
        Kind2("方向長の下限以下", LineLine2((0, 0), (1e-13, 0), (0, 1), (0, 1)), IntersectionKind.Degenerate);
        Kind3("ゼロ方向の3D直線", LinePlane3((0, 0, 2), (0, 0, 0), (0, 0, 2), (0, 0, 1)), IntersectionKind.Degenerate);
        Kind3("直線×平面のゼロ法線", LinePlane3((0, 0, 0), (0, 0, 1), (0, 0, 2), (0, 0, 0)), IntersectionKind.Degenerate);
        Kind3("第1平面のゼロ法線", PlanePlane3((0, 0, 0), (0, 0, 0), (0, 0, 2), (0, 0, 1)), IntersectionKind.Degenerate);
        Kind3("第2平面のゼロ法線", PlanePlane3((0, 0, 0), (0, 0, 1), (0, 0, 2), (0, 0, 0)), IntersectionKind.Degenerate);
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity })
        {
            Kind2("非有限な点", LineLine2((invalid, 0), (1, 0), (0, 1), (0, 1)), IntersectionKind.InvalidInput);
            Kind2("非有限な方向", SegmentSegment2((0, 0), (1, 0), (0, 1), (0, invalid)), IntersectionKind.InvalidInput);
            Kind3("非有限な直線点", LinePlane3((invalid, 0, 0), (0, 0, 1), (0, 0, 2), (0, 0, 1)), IntersectionKind.InvalidInput);
            Kind3("非有限な方向d", LinePlane3((0, 0, 0), (0, invalid, 1), (0, 0, 2), (0, 0, 1)), IntersectionKind.InvalidInput);
            Kind3("非有限な平面点", PlanePlane3((0, 0, 2), (0, 0, 1), (invalid, 0, 0), (1, 0, 0)), IntersectionKind.InvalidInput);
            Kind3("非有限な法線", LinePlane3((0, 0, 0), (0, 0, 1), (0, 0, 2), (0, 0, invalid)), IntersectionKind.InvalidInput);
            Kind3("2平面の非有限法線", PlanePlane3((0, 0, 2), (0, 0, invalid), (1, 0, 0), (1, 0, 0)), IntersectionKind.InvalidInput);
            Assert("非有限な三角形", !TryTriangleNormal((0, 0, invalid), (1, 0, 0), (0, 1, 0), out _));
        }
        V2 almost = (1, 1 + 1e-13);
        double denominator = Cross2((1, 1), almost);
        Assert("doubleに残る小さな分母", denominator > 0 && denominator < 1e-12);
        Kind2("ほぼ平行な2D直線は保留", LineLine2((0, 0), (1, 1), (0, 1), almost), IntersectionKind.Uncertain);
        Check("floatでは差が消えて分母0", [Vector3.Cross(new(1, 1, 0), new(1, (float)almost.Y, 0)).Z], [0], FloatTolerance);
        Console.WriteLine($"  doubleの小さな分母: {denominator.ToString("G17", CultureInfo.InvariantCulture)}");
        Kind3("ほぼ平行な直線と平面は保留", LinePlane3((0, 0, 0), (1, 0, 1e-13), (0, 0, 2), (0, 0, 6)), IntersectionKind.Uncertain);
        Kind3("ほぼ平行な2平面は保留", PlanePlane3((0, 0, 2), (0, 0, 1), (1, 0, 0), (1e-13, 0, 1)), IntersectionKind.Uncertain);
        Kind2("距離が小さい別の平行線は保留", LineLine2((0, 0), (1, 0), (0, 1e-10), (1, 0)), IntersectionKind.Uncertain);
        Kind3("距離が小さい平行な直線と平面", LinePlane3((0, 0, 1e-10), (1, 0, 0), (0, 0, 0), (0, 0, 1)), IntersectionKind.Uncertain);
        Kind3("距離が小さい平行2平面", PlanePlane3((0, 0, 0), (0, 0, 1), (0, 0, 1e-10), (0, 0, 1)), IntersectionKind.Uncertain);

        foreach (double k in new[] { 1e-6, 1e6 })
        {
            var scaled = LineLine2((0, 0), Scale2((4, 4), k), (0, 4), Scale2((4, -4), k));
            Kind2("方向の倍率を変えても同じ交点", scaled, IntersectionKind.Point);
            Check("倍率で変わるt/uと不変の位置", [scaled.T!.Value * k, scaled.U!.Value * k, scaled.Point!.Value.X, scaled.Point.Value.Y], [.5, .5, 2, 2]);
        }
        foreach (double k in new[] { 1e-6, 1e6, -2 })
        {
            var scaled = LinePlane3((1, 2, 0), (2, 0, 4), (0, 0, 2), Scale3((0, 0, 6), k));
            Kind3("法線の倍率・符号を変えても交差", scaled, IntersectionKind.Point);
            Check("法線倍率でtと交点は不変", [scaled.T!.Value, scaled.Point!.Value.X, scaled.Point.Value.Y, scaled.Point.Value.Z], [.5, 2, 2, 2]);
        }
        foreach (double k in new[] { 1e-6, 1e6 })
        {
            var scaled = PlanePlane3((0, 0, 2), (0, 0, k), (1, 0, 0), (-k, 0, 0));
            Kind3("2平面も法線倍率・符号に依存しない", scaled, IntersectionKind.Line);
            Check("法線反転で進む向きだけ逆", [scaled.Point!.Value.X, scaled.Point.Value.Y, scaled.Point.Value.Z,
                scaled.Direction!.Value.X, scaled.Direction.Value.Y, scaled.Direction.Value.Z], [1, 0, 2, 0, -1, 0]);
        }
        var millimetersToMeters = LineLine2((0, 0), (.004, .004), (0, .004), (.004, -.004));
        Kind2("座標単位をmmからmへ", millimetersToMeters, IntersectionKind.Point);
        Check("単位変更でもt/uは不変", [millimetersToMeters.T!.Value, millimetersToMeters.U!.Value,
            millimetersToMeters.Point!.Value.X, millimetersToMeters.Point.Value.Y], [.5, .5, .002, .002]);
        // ごく小さい範囲外は端点へ丸めず保留。十分に範囲外ならNone。
        Kind2("1のわずか外側を保留", SegmentSegment2((0, 0), (1, 0), (1 + 5e-13, -1), (0, 2)), IntersectionKind.Uncertain);
        Kind2("1より十分外側はなし", SegmentSegment2((0, 0), (1, 0), (1 + 1e-8, -1), (0, 2)), IntersectionKind.None);
        Kind2("1の内側は一点", SegmentSegment2((0, 0), (1, 0), (1 - 5e-13, -1), (0, 2)), IntersectionKind.Point);
        Kind2("0のわずか外側を保留", SegmentSegment2((0, 0), (1, 0), (-5e-13, -1), (0, 2)), IntersectionKind.Uncertain);
        Kind2("同一直線のわずかな隙間を保留", SegmentSegment2((0, 0), (1, 0), (1 + 5e-13, 0), (1, 0)), IntersectionKind.Uncertain);
        Kind2("有限入力でも長さの桁あふれ", LineLine2((0, 0), (double.MaxValue, 1), (0, 1), (0, 1)), IntersectionKind.Uncertain);
        Kind2("有限な点どうしの差の桁あふれ", LineLine2((double.MaxValue, 0), (1, 0), (-double.MaxValue, 1), (0, 1)), IntersectionKind.Uncertain);
        Kind3("法線の長さの桁あふれ", LinePlane3((0, 0, 0), (0, 0, 1), (0, 0, 2), (0, 0, double.MaxValue)), IntersectionKind.Uncertain);
        Kind3("非ゼロ法線の長さのアンダーフロー", PlanePlane3((0, 0, 2), (0, 0, 1e-200), (1, 0, 0), (1, 0, 0)), IntersectionKind.Uncertain);
        Kind3("非ゼロ外積の長さのアンダーフローも保留", PlanePlane3((0, 0, 2), (0, 0, 1), (1, 0, 0), (1e-200, 0, 1)), IntersectionKind.Uncertain);
    }

    /// <summary>計算で得た点を両方の媒介変数式へ戻し、残差0を確認する。</summary>
    /// <param name="label">例の名前。</param><param name="result">Pointの結果。</param>
    /// <param name="p">第1始点。</param><param name="r">第1方向。</param>
    /// <param name="q">第2始点。</param><param name="s">第2方向。</param>
    private static void Residual2(string label, Intersection2 result, V2 p, V2 r, V2 q, V2 s)
    {
        var x = result.Point!.Value;
        var first = Sub2(x, Add2(p, Scale2(r, result.T!.Value)));
        var second = Sub2(x, Add2(q, Scale2(s, result.U!.Value)));
        Check(label + ": 第1式の残差", [first.X, first.Y], [0, 0]);
        Check(label + ": 第2式の残差", [second.X, second.Y], [0, 0]);
    }

    /// <summary>重複区間の端点が両方の直線上かつ閉線分範囲内にあることを確認する。</summary>
    /// <param name="label">端点名。</param><param name="x">確認する端点。</param>
    /// <param name="p">第1始点。</param><param name="r">第1方向。</param>
    /// <param name="q">第2始点。</param><param name="s">第2方向。</param>
    private static void OnBothSegments(string label, V2 x, V2 p, V2 r, V2 q, V2 s)
    {
        double t = Dot2(Sub2(x, p), r) / Dot2(r, r), u = Dot2(Sub2(x, q), s) / Dot2(s, s);
        Assert(label + ": 両区間内", double.IsFinite(t) && double.IsFinite(u) && t >= 0 && t <= 1 && u >= 0 && u <= 1);
        Residual2(label, new(IntersectionKind.Point, x, t, u), p, r, q, s);
    }

    /// <summary>2D分類と、分類に対応する値だけが存在することを確認する。</summary>
    /// <param name="label">検証名。</param><param name="r">実際の結果。</param><param name="expected">期待分類。</param>
    private static void Kind2(string label, Intersection2 r, IntersectionKind expected)
    {
        bool point = r.Point.HasValue && Finite2(r.Point.Value) && r.T.HasValue && double.IsFinite(r.T.Value) &&
            r.U.HasValue && double.IsFinite(r.U.Value);
        bool overlap = r.OverlapStart.HasValue && r.OverlapEnd.HasValue && Finite2(r.OverlapStart.Value) &&
            Finite2(r.OverlapEnd.Value) && r.OverlapStart != r.OverlapEnd;
        bool fields = r.Kind switch {
            IntersectionKind.Point => point && !r.OverlapStart.HasValue && !r.OverlapEnd.HasValue,
            IntersectionKind.OverlapSegment => overlap && !r.Point.HasValue && !r.T.HasValue && !r.U.HasValue,
            _ => !r.Point.HasValue && !r.T.HasValue && !r.U.HasValue && !r.OverlapStart.HasValue && !r.OverlapEnd.HasValue
        };
        Assert(label, r.Kind == expected && fields, $"actual={r.Kind}, expected={expected}");
    }

    /// <summary>3D分類と、点・直線の値の有無を確認する。</summary>
    /// <param name="label">検証名。</param><param name="r">実際の結果。</param><param name="expected">期待分類。</param>
    private static void Kind3(string label, Intersection3 r, IntersectionKind expected)
    {
        bool fields = r.Kind switch {
            IntersectionKind.Point => r.Point.HasValue && Finite3(r.Point.Value) && r.T.HasValue && double.IsFinite(r.T.Value) && !r.Direction.HasValue,
            IntersectionKind.Line => r.Point.HasValue && Finite3(r.Point.Value) && r.Direction.HasValue && Finite3(r.Direction.Value) && r.Direction.Value != (0, 0, 0) && !r.T.HasValue,
            _ => !r.Point.HasValue && !r.T.HasValue && !r.Direction.HasValue
        };
        Assert(label, r.Kind == expected && fields, $"actual={r.Kind}, expected={expected}");
    }

    /// <summary>未丸めの実値と期待値を、両側の有限性と絶対誤差で比較する。</summary>
    /// <param name="label">失敗位置のラベル。</param><param name="actual">実値の成分。</param>
    /// <param name="expected">数学から別に求めた期待値。</param><param name="tolerance">この入力用の絶対誤差。</param>
    private static void Check(string label, double[] actual, double[] expected, double tolerance = DoubleTolerance)
    {
        if (actual.Length != expected.Length) throw new InvalidOperationException(label + ": 成分数が不一致");
        for (int i = 0; i < actual.Length; i++)
            if (!double.IsFinite(actual[i]) || !double.IsFinite(expected[i]) || Math.Abs(actual[i] - expected[i]) > tolerance)
                throw new InvalidOperationException($"{label}[{i}]: actual={actual[i]:G17}, expected={expected[i]:G17}, tolerance={tolerance}");
        Passed(label, string.Join(", ", actual.Select(x => x.ToString("G9", CultureInfo.InvariantCulture))));
    }

    /// <summary>分類・範囲・有限性などの条件を検査する。</summary>
    /// <param name="label">検証名。</param><param name="condition">期待する条件。</param><param name="detail">表示する補足。</param>
    private static void Assert(string label, bool condition, string? detail = null)
    {
        if (!condition) throw new InvalidOperationException(label + ": " + (detail ?? "条件を満たしません。"));
        Passed(label, detail ?? "確認済み");
    }

    /// <summary>1つの検証呼び出しの成功を1件として数える。</summary>
    /// <param name="label">成功した検証名。</param><param name="detail">未丸め値から作った表示用の結果。</param>
    private static void Passed(string label, string detail)
    {
        checks++;
        Console.WriteLine($"  [OK] {label}: {detail}");
    }
}

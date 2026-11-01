using System.Globalization;
using System.Numerics;
using System.Text;
using static GeometryMath;

/// <summary>第5回の固定例A〜Eを7題材で実行し、入力・計算・分類を確認する。</summary>
/// <remarks>引数・標準入力・GUI・外部ファイル・AutoCADを使わない。期待値は本文・宿題から独立に記す。</remarks>
internal static class Program
{
    private static int checks;
    private static readonly P2[] TriangleA = [(0, 0), (4, 0), (0, 3)];
    private static readonly P2[] L = [(0, 0), (4, 0), (4, 1), (1, 1), (1, 4), (0, 4)];
    private static readonly P2[] T1 = [(0, 0), (3, 0), (0, 3)];
    private static readonly P2[] T2 = [(2, 2), (5, 2), (2, 5)];
    private static readonly P2[] T3 = [(1, 1), (4, 1), (1, 4)];
    private static readonly P2[] T4 = [(3, 0), (5, 0), (3, 2)];
    private static readonly Box2 A = new((0, 0), (2, 2));
    private static readonly Box3 K = new((0, 0, 0), (2, 2, 2));

    /// <summary>全題材を実行し、不一致・想定外の例外を非0終了にする。</summary>
    /// <returns>全成功なら0、失敗なら1。</returns>
    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            Run("1. 三角形の内外・向き・有限な辺", TriangleContainment);
            Run("2. 凹L字の境界先行・半開レイ", ConcaveRay);
            Run("3. 2D AABBの離隔・接触・共有面積", Boxes2D);
            Run("4. 凸2D多角形の内積射影とSAT", SeparatingAxes);
            Run("5. 3D凸箱の外向き面法線と点包含", BoxHalfspaces);
            Run("6. 3D AABBの共有体積と接触", Boxes3D);
            Run("7. 無効入力・非対応・数値精度", InvalidInputsAndPrecision);
            Console.WriteLine($"\nすべての {checks} チェックに成功しました。");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL: {error.GetType().Name}: {error.Message}");
            return 1;
        }
    }

    /// <summary>題材ごとに入力の意味と成功件数を追える区切りを付ける。</summary>
    /// <param name="title">題材名。</param><param name="sample">再現可能な固定入力の検証。</param>
    private static void Run(string title, Action sample)
    {
        Console.WriteLine($"\n{title}");
        int before = checks;
        sample();
        Console.WriteLine($"  この題材: {checks - before} チェック");
    }

    /// <summary>固定例Aで、全辺の左右と線分範囲を組み合わせる理由を確かめる。</summary>
    private static void TriangleContainment()
    {
        Equal("A: 有効な閉三角形", PolygonChecks.TryValidate(TriangleA, true, out double area2), true);
        Numbers("A: 符号付き2倍面積・面積", [area2, Math.Abs(area2) / 2], [12, 6]);
        Numbers("A: P=(1,1)のAB/BC/CA外積", EdgeValues(TriangleA, (1, 1)), [4, 5, 3]);
        Equal("A: Pの包含", PolygonChecks.InTriangle(TriangleA, true, (1, 1)), PointLocation.Inside);
        Numbers("A: E=(2,0)のAB/BC/CA外積", EdgeValues(TriangleA, (2, 0)), [0, 6, 6]);
        Equal("A: Eの包含", PolygonChecks.InTriangle(TriangleA, true, (2, 0)), PointLocation.Boundary);
        Equal("A: Eの線分検査が有効", TryPointOnSegment((0, 0), (4, 0), (2, 0), out bool eOn), true);
        Equal("A: Eは有限なAB上", eOn, true);
        Numbers("A: Eの射影・辺長の2乗", [Dot((2, 0), (4, 0)), Dot((4, 0), (4, 0))], [8, 16]);
        Numbers("A: O=(3,2)のAB/BC/CA外積", EdgeValues(TriangleA, (3, 2)), [8, -5, 9]);
        Equal("A: Oの包含", PolygonChecks.InTriangle(TriangleA, true, (3, 2)), PointLocation.Outside);
        // 延長線上の外積0よりも、他辺の外側という条件が優先する。
        Numbers("A: F=(5,0)の外積", EdgeValues(TriangleA, (5, 0)), [0, -3, 15]);
        Equal("A: Fの線分検査が有効", TryPointOnSegment((0, 0), (4, 0), (5, 0), out bool fOn), true);
        Equal("A: Fは延長線だけ", fOn, false);
        Equal("A: Fは境界でなく外部", PolygonChecks.InTriangle(TriangleA, true, (5, 0)), PointLocation.Outside);
        foreach (P2 vertex in TriangleA)
            Equal($"A: 頂点{vertex}は境界", PolygonChecks.InTriangle(TriangleA, true, vertex), PointLocation.Boundary);
        P2[] reverse = [(0, 0), (0, 3), (4, 0)];
        Equal("A: 逆順も有効", PolygonChecks.TryValidate(reverse, true, out double reverseArea2), true);
        Numbers("A: 逆順の2倍面積・面積", [reverseArea2, Math.Abs(reverseArea2) / 2], [-12, 6]);
        Numbers("A: 逆順PのAC/CB/BA", EdgeValues(reverse, (1, 1)), [-3, -5, -4]);
        Equal("A: 逆順Pも内部", PolygonChecks.InTriangle(reverse, true, (1, 1)), PointLocation.Inside);
        Equal("A: 逆順Eも境界", PolygonChecks.InTriangle(reverse, true, (2, 0)), PointLocation.Boundary);
        Equal("A: 逆順Oも外部", PolygonChecks.InTriangle(reverse, true, (3, 2)), PointLocation.Outside);
        Equal("A: 面積0はInvalid", PolygonChecks.InTriangle([(0, 0), (1, 0), (2, 0)], true, (1, 0)), PointLocation.Invalid);
    }

    /// <summary>固定例Bで、箱の内側と実体の内側、境界と偶奇を分ける。</summary>
    private static void ConcaveRay()
    {
        Equal("B: L字は単純閉輪郭", PolygonChecks.TryValidate(L, true, out double area2), true);
        Numbers("B: L字面積", [area2 / 2], [7]);
        CheckRay("B: I=(0.5,2)", L, (.5, 2), PointLocation.Inside, 1);
        CheckRay("B: N=(2,2)", L, (2, 2), PointLocation.Outside, 0);
        P2 notch = (2, 2);
        Equal("B: NはAABB内", 0 <= notch.X && notch.X <= 4 && 0 <= notch.Y && notch.Y <= 4, true);
        CheckRay("B: D=(1,2)", L, (1, 2), PointLocation.Boundary, null);
        CheckRay("B: V=(1,1)", L, (1, 1), PointLocation.Boundary, null);
        CheckRay("B: H=(3,0.5)", L, (3, .5), PointLocation.Inside, 1);
        CheckRay("B: R=(0.5,1)の半開規則", L, (.5, 1), PointLocation.Inside, 1);
        CheckRay("B: 水平辺上(2,0)", L, (2, 0), PointLocation.Boundary, null);
        CheckRay("B: くぼみの水平辺上(2,1)", L, (2, 1), PointLocation.Boundary, null);
        CheckRay("B: 上側水平辺上(0.5,4)", L, (.5, 4), PointLocation.Boundary, null);
        foreach (P2 vertex in L)
            Equal($"B: 頂点{vertex}は境界", PolygonChecks.ByRay(L, true, vertex).Location, PointLocation.Boundary);
        CheckRay("B: 逆順I", L.Reverse().ToArray(), (.5, 2), PointLocation.Inside, 1);
        // 半開規則は全頂点を1回にする規則ではない。極小では同じ位置を2辺が寄与する。
        P2[] diamond = [(0, 1), (1, 0), (2, 1), (1, 2)];
        CheckRay("B: 非水平2辺の極小は2寄与", diamond, (-1, 0), PointLocation.Outside, 2);
        CheckRay("B: 非水平2辺の極大は0寄与", diamond, (-1, 2), PointLocation.Outside, 0);
        CheckRay("B: 通過頂点は各1寄与", diamond, (-1, 1), PointLocation.Outside, 2);
    }

    /// <summary>固定例Cの区間幅、共有集合の次元、用途別Booleanを調べる。</summary>
    private static void Boxes2D()
    {
        CheckBox("C: A/B", AabbChecks.Compare(A, new Box2((2, 1), (4, 3))), RegionRelation.Touching, [0, 1], 0);
        CheckBox("C: A/C", AabbChecks.Compare(A, new Box2((2.1, 1), (4, 3))), RegionRelation.Separated, [-.1, 1], 0);
        CheckBox("C: A/D", AabbChecks.Compare(A, new Box2((1, 1), (3, 3))), RegionRelation.Overlapping, [1, 1], 1);
        CheckBox("C: 完全包含", AabbChecks.Compare(A, new Box2((.5, .5), (1.5, 1.5))), RegionRelation.Overlapping, [1, 1], 1);
        CheckBox("C: 同じ箱", AabbChecks.Compare(A, A), RegionRelation.Overlapping, [2, 2], 4);
        CheckBox("C: 点だけの接触", AabbChecks.Compare(A, new Box2((2, 2), (3, 3))), RegionRelation.Touching, [0, 0], 0);
    }

    /// <summary>固定例DでAABB候補を残した後、斜め軸と全辺法線で実体を判定する。</summary>
    private static void SeparatingAxes()
    {
        CheckBox("D: T1/T2のAABB", AabbChecks.Compare(new Box2((0, 0), (3, 3)), new Box2((2, 2), (5, 5))),
            RegionRelation.Overlapping, [1, 1], 1);
        Numbers("D: T1のq=x+y", T1.Select(p => Dot((1, 1), p)).ToArray(), [0, 3, 3]);
        Numbers("D: T2のq=x+y", T2.Select(p => Dot((1, 1), p)).ToArray(), [4, 7, 7]);
        Project("D: T1の斜め区間", T1, (1, 1), [0, 3]);
        Project("D: T2の斜め区間", T2, (1, 1), [4, 7]);
        Project("D: T1の正倍率軸", T1, (2, 2), [0, 6]);
        Project("D: T2の負倍率軸", T2, (-2, -2), [-14, -8]);
        Equal("D: T1/T2の実体", ConvexSat.Compare(T1, true, T2, true), RegionRelation.Separated);
        Equal("D: T1/T3の実体", ConvexSat.Compare(T1, true, T3, true), RegionRelation.Overlapping);
        Equal("D: T1/T4の実体", ConvexSat.Compare(T1, true, T4, true), RegionRelation.Touching);
        Equal("D: 順番交換", ConvexSat.Compare(T2, true, T1, true), RegionRelation.Separated);
        Equal("D: CW輪郭も同じ結果", ConvexSat.Compare(T1.Reverse().ToArray(), true, T3.Reverse().ToArray(), true), RegionRelation.Overlapping);
        Equal("D: 完全包含も重なり", ConvexSat.Compare(T1, true, [(.5, .5), (1, .5), (.5, 1)], true), RegionRelation.Overlapping);
        Equal("D: 同一三角形も重なり", ConvexSat.Compare(T1, true, T1, true), RegionRelation.Overlapping);
        Numbers("D: Q=(1.1,1.1)のT1外積", EdgeValues(T1, (1.1, 1.1)), [3.3, 2.4, 3.3]);
        Numbers("D: QのT3外積", EdgeValues(T3, (1.1, 1.1)), [.3, 8.4, .3]);
        Equal("D: QはT1の厳密な内部", PolygonChecks.InTriangle(T1, true, (1.1, 1.1)), PointLocation.Inside);
        Equal("D: QはT3の厳密な内部", PolygonChecks.InTriangle(T3, true, (1.1, 1.1)), PointLocation.Inside);
        Project("D: T4のX端点一致", T4, (1, 0), [3, 5]);
        Project("D: T4のY区間", T4, (0, 1), [0, 2]);
        Project("D: T4の斜め端点一致", T4, (1, 1), [3, 5]);
        Equal("D: 共有点(3,0)はT1の境界", PolygonChecks.InTriangle(T1, true, (3, 0)), PointLocation.Boundary);
        Equal("D: 共有点(3,0)はT4の境界", PolygonChecks.InTriangle(T4, true, (3, 0)), PointLocation.Boundary);
        Equal("D: X端点一致でもYで分離", ConvexSat.Compare(T1, true, [(3, 4), (5, 4), (3, 6)], true), RegionRelation.Separated);
        // 三角形の3方向だけに実装を固定しない。片方にしかない斜め法線も必要な例。
        P2[] square = [(0, 0), (2, 0), (2, 2), (0, 2)];
        P2[] diagonal = [(1.8, 2.5), (2.5, 1.8), (3, 3)];
        Equal("D: 相手だけの辺法線で分離", ConvexSat.Compare(square, true, diagonal, true), RegionRelation.Separated);
        Equal("D: 相手だけの辺法線・交換後", ConvexSat.Compare(diagonal, true, square, true), RegionRelation.Separated);
    }

    /// <summary>固定例Eの6面の内積を、外向き法線と面上の点から求める。</summary>
    private static void BoxHalfspaces()
    {
        CheckHalfspaces("E: I=(1,1,1)", (1, 1, 1), [-1, -1, -1, -1, -1, -1], PointLocation.Inside);
        CheckHalfspaces("E: O=(3,1,1)", (3, 1, 1), [-3, 1, -1, -1, -1, -1], PointLocation.Outside);
        CheckHalfspaces("E: E=(2,1,1)", (2, 1, 1), [-2, 0, -1, -1, -1, -1], PointLocation.Boundary);
        Equal("E: 面の延長上(2,3,1)は外部", ConvexBoxChecks.Locate(K, (2, 3, 1)), PointLocation.Outside);
        Equal("E: 辺(2,2,1)は境界", ConvexBoxChecks.Locate(K, (2, 2, 1)), PointLocation.Boundary);
        foreach (double x in new[] { 0d, 2d })
            foreach (double y in new[] { 0d, 2d })
                foreach (double z in new[] { 0d, 2d })
                    Equal($"E: 頂点({x},{y},{z})は境界", ConvexBoxChecks.Locate(K, (x, y, z)), PointLocation.Boundary);
        Equal("E: 原点以外の箱でも内部", ConvexBoxChecks.Locate(new Box3((10, 20, 30), (12, 22, 32)), (11, 21, 31)), PointLocation.Inside);
    }

    /// <summary>固定例Eの体積共有と、面・辺・点だけの接触を区別する。</summary>
    private static void Boxes3D()
    {
        CheckBox("E: K/K2", AabbChecks.Compare(K, new Box3((1, 1, 1), (3, 3, 3))), RegionRelation.Overlapping, [1, 1, 1], 1);
        CheckBox("E: K/K3の面接触", AabbChecks.Compare(K, new Box3((2, 1, 1), (3, 2, 2))), RegionRelation.Touching, [0, 1, 1], 0);
        CheckBox("E: 辺だけの接触", AabbChecks.Compare(K, new Box3((2, 2, 1), (3, 3, 3))), RegionRelation.Touching, [0, 0, 1], 0);
        CheckBox("E: 点だけの接触", AabbChecks.Compare(K, new Box3((2, 2, 2), (3, 3, 3))), RegionRelation.Touching, [0, 0, 0], 0);
        CheckBox("E: 離隔", AabbChecks.Compare(K, new Box3((2.1, 1, 1), (3, 3, 3))), RegionRelation.Separated, [-.1, 1, 1], 0);
        CheckBox("E: 同じ箱", AabbChecks.Compare(K, K), RegionRelation.Overlapping, [2, 2, 2], 8);
        AabbResult faceContact = AabbChecks.Compare(K, new Box3((2, 1, 1), (3, 2, 2)));
        Numbers("E: K/K3の共有面積（体積とは別）", [faceContact.AxisDeltas[1] * faceContact.AxisDeltas[2]], [1]);
    }

    /// <summary>不正・非対応入力を正常分類から分け、計算精度と許容差の意味を確かめる。</summary>
    private static void InvalidInputsAndPrecision()
    {
        (string Name, P2[] Points, bool Closed)[] invalidContours = [
            ("空", [], true), ("2点", [(0, 0), (1, 0)], true),
            ("開いた輪郭", TriangleA, false), ("面積0", [(0, 0), (1, 0), (2, 0)], true),
            ("零長辺", [(0, 0), (0, 0), (0, 3)], true),
            ("末尾に始点重複", [(0, 0), (4, 0), (0, 3), (0, 0)], true),
            ("非隣接重複", [(0, 0), (4, 0), (4, 4), (4, 0), (0, 4)], true),
            ("隣接辺の折返し", [(0, 0), (3, 0), (1, 0), (1, 2), (0, 2)], true),
            ("自己交差・符号付き面積は非0", [(0, 0), (4, 3), (0, 4), (3, 0)], true),
            ("NaN", [(0, 0), (double.NaN, 0), (0, 3)], true),
            ("Infinity", [(0, 0), (4, 0), (0, double.PositiveInfinity)], true),
            ("長さ計算の桁あふれ", [(0, 0), (1e200, 0), (0, 1e200)], true),
            ("長さ計算のアンダーフロー", [(0, 0), (1e-200, 0), (0, 1e-200)], true)];
        foreach (var input in invalidContours)
        {
            Equal($"無効輪郭/{input.Name}: レイ", PolygonChecks.ByRay(input.Points, input.Closed, (.5, .5)).Location, PointLocation.Invalid);
            Equal($"無効輪郭/{input.Name}: SAT", ConvexSat.Compare(input.Points, input.Closed, T1, true), RegionRelation.Invalid);
        }
        Equal("null輪郭: レイ", PolygonChecks.ByRay(null, true, (0, 0)).Location, PointLocation.Invalid);
        Equal("null輪郭: SAT", ConvexSat.Compare(T1, true, null, true), RegionRelation.Invalid);
        Equal("開いた第2輪郭: SAT", ConvexSat.Compare(T1, true, T2, false), RegionRelation.Invalid);
        Equal("凹L字はSAT非対応", ConvexSat.Compare(T1, true, L, true), RegionRelation.Invalid);
        P2[] collinearCorner = [(0, 0), (1, 0), (2, 0), (2, 2), (0, 2)];
        Equal("冗長な共線頂点: レイは対応", PolygonChecks.ByRay(collinearCorner, true, (1, 1)).Location, PointLocation.Inside);
        Equal("冗長な共線頂点: 厳密凸SATは非対応", ConvexSat.Compare(T1, true, collinearCorner, true), RegionRelation.Invalid);
        Equal("三角形APIに4点はInvalid", PolygonChecks.InTriangle([(0, 0), (2, 0), (2, 2), (0, 2)], true, (1, 1)), PointLocation.Invalid);
        Equal("三角形APIにNaN点", PolygonChecks.InTriangle(TriangleA, true, (double.NaN, 0)), PointLocation.Invalid);
        Equal("レイAPIにInfinity点", PolygonChecks.ByRay(L, true, (0, double.NegativeInfinity)).Location, PointLocation.Invalid);
        Equal("線分の零長辺は検査無効", TryPointOnSegment((0, 0), (0, 0), (0, 0), out _), false);
        Equal("線分の非有限点は検査無効", TryPointOnSegment((0, 0), (1, 0), (double.NaN, 0), out _), false);
        Equal("射影のゼロ軸は無効", ConvexSat.TryProject(T1, (0, 0), out _), false);
        Equal("射影の非有限軸は無効", ConvexSat.TryProject(T1, (double.PositiveInfinity, 0), out _), false);
        Equal("射影の空入力は無効", ConvexSat.TryProject([], (1, 1), out _), false);
        Equal("射影の桁あふれは無効", ConvexSat.TryProject([(1e200, 1e200)], (1e200, 1e200), out _), false);
        Box2[] invalid2 = [new((2, 0), (1, 2)), new((0, 0), (0, 2)),
            new((double.NaN, 0), (2, 2)), new((0, 0), (2, double.PositiveInfinity)),
            new((-1e308, 0), (1e308, 2))];
        foreach (Box2 box in invalid2)
        {
            AabbResult result = AabbChecks.Compare(A, box);
            Equal($"無効2D箱{box}", result.Relation, RegionRelation.Invalid);
            Equal<double?>($"無効2D箱の面積は未定義{box}", result.SharedMeasure, null);
        }
        Box3[] invalid3 = [new((0, 0, 2), (2, 2, 1)), new((0, 0, 0), (2, 0, 2)),
            new((0, double.NaN, 0), (2, 2, 2)), new((0, 0, 0), (2, 2, double.PositiveInfinity))];
        foreach (Box3 box in invalid3)
        {
            Equal($"無効3D箱{box}: 関係", AabbChecks.Compare(K, box).Relation, RegionRelation.Invalid);
            Equal($"無効3D箱{box}: 包含", ConvexBoxChecks.Locate(box, (1, 1, 1)), PointLocation.Invalid);
        }
        Equal("3D点のNaNはInvalid", ConvexBoxChecks.Locate(K, (1, double.NaN, 1)), PointLocation.Invalid);
        Equal("3D点のInfinityはInvalid", ConvexBoxChecks.Locate(K, (1, 1, double.PositiveInfinity)), PointLocation.Invalid);
        Equal<bool?>("Invalidをinclusiveのfalseにしない", AabbChecks.Inclusive(RegionRelation.Invalid), null);
        Equal<bool?>("Invalidを面積ありのfalseにしない", AabbChecks.PositiveOverlap(RegionRelation.Invalid), null);
        Box2 huge = new((0, 0), (1e200, 1e200)), tiny = new((0, 0), (1e-200, 1e-200));
        Equal("共有面積の桁あふれはInvalid", AabbChecks.Compare(huge, huge).Relation, RegionRelation.Invalid);
        Equal("正幅の積が0ならInvalid", AabbChecks.Compare(tiny, tiny).Relation, RegionRelation.Invalid);

        // 許容誤差は検算用の数値比較だけに使い、分類の等号を勝手に緩めない。
        double smallGap = 1e-10;
        Equal("微小な正の隙間は教材計算では離隔", AabbChecks.Compare(A, new Box2((2 + smallGap, 1), (3, 3))).Relation, RegionRelation.Separated);
        Equal("doubleの微小差は残る", 2 + smallGap > 2, true);
        Vector3 rounded = new((float)(2 + smallGap), 1, 1);
        Equal("floatへ移すと微小差が消える", rounded.X == 2, true);
        Equal("doubleの点は箱の外部", ConvexBoxChecks.Locate(K, (2 + smallGap, 1, 1)), PointLocation.Outside);
        Equal("floatから戻した点は既に別入力", ConvexBoxChecks.Locate(K, (rounded.X, rounded.Y, rounded.Z)), PointLocation.Boundary);
        Vector2 edge = new(4, 0), w = new(1, 1);
        Numbers("floatの小整数外積をdoubleと照合", [edge.X * w.Y - edge.Y * w.X], [4], 1e-5, 0);
        Numbers("絶対誤差で0付近の数値比較", [1e-13], [0], 1e-12, 0);
        Numbers("相対誤差で大きい数値の比較", [1e9 + .0001], [1e9], 0, 1e-12);
        RejectComparison("実値NaNを比較成功にしない", () => RequireNear([double.NaN], [0], 1e-12, 0));
        RejectComparison("期待値Infinityを比較成功にしない", () => RequireNear([0], [double.PositiveInfinity], 1e-12, 0));
        RejectComparison("誤った期待値を拒否", () => RequireNear([1], [2], 1e-12, 0));
    }

    /// <summary>各辺から点への外積を、輪郭の記載順に得る。</summary>
    /// <param name="polygon">固定された検算用輪郭。</param><param name="point">検査点。</param><returns>辺ごとの外積配列。</returns>
    private static double[] EdgeValues(P2[] polygon, P2 point) => polygon.Select((a, i) =>
        Cross2(Sub(polygon[(i + 1) % polygon.Length], a), Sub(point, a))).ToArray();

    /// <summary>レイの分類と計数を別々の独立期待値へ照合する。</summary>
    /// <param name="label">入力を含むラベル。</param><param name="polygon">閉輪郭。</param><param name="p">検査点。</param>
    /// <param name="location">期待分類。</param><param name="crossings">境界ならnull、他は期待回数。</param>
    private static void CheckRay(string label, P2[] polygon, P2 p, PointLocation location, int? crossings)
    {
        RayResult result = PolygonChecks.ByRay(polygon, true, p);
        Equal(label + ": 分類", result.Location, location);
        Equal(label + ": 右向き計数", result.RightCrossings, crossings);
    }

    /// <summary>箱の分類・各軸の幅・共有量と、2つの用途のBooleanを検算する。</summary>
    /// <param name="label">箱の組。</param><param name="result">計算結果。</param><param name="relation">期待分類。</param>
    /// <param name="deltas">期待する候補幅。</param><param name="measure">期待共有面積または体積。</param>
    private static void CheckBox(string label, AabbResult result, RegionRelation relation, double[] deltas, double measure)
    {
        Equal(label + ": 分類", result.Relation, relation);
        Numbers(label + ": 各軸の候補幅", result.AxisDeltas, deltas);
        Numbers(label + ": 共有面積/体積", [result.SharedMeasure ?? double.NaN], [measure]);
        // Booleanの期待値は期待する状態から写像する。実際の分類結果からは生成しない。
        Equal<bool?>(label + ": 接触込み", AabbChecks.Inclusive(result.Relation), relation != RegionRelation.Separated);
        Equal<bool?>(label + ": 正の面積/体積のみ", AabbChecks.PositiveOverlap(result.Relation), relation == RegionRelation.Overlapping);
    }

    /// <summary>内積射影の有効性を確かめてから区間を照合する。</summary>
    /// <param name="label">射影名。</param><param name="polygon">頂点。</param><param name="axis">共通の非ゼロ軸。</param>
    /// <param name="expected">独立な最小・最大の期待値。</param>
    private static void Project(string label, P2[] polygon, P2 axis, double[] expected)
    {
        Equal(label + ": 有効", ConvexSat.TryProject(polygon, axis, out Interval interval), true);
        Numbers(label, [interval.Min, interval.Max], expected);
    }

    /// <summary>6面の符号付き内積と、それらを合わせた分類を照合する。</summary>
    /// <param name="label">点名。</param><param name="p">Kに対する検査点。</param>
    /// <param name="expected">−X・+X・−Y・+Y・−Z・+Z面の期待値。</param><param name="location">期待分類。</param>
    private static void CheckHalfspaces(string label, P3 p, double[] expected, PointLocation location)
    {
        Equal(label + ": 6面演算が有効", ConvexBoxChecks.TryHalfspaces(K, p, out double[] values), true);
        Numbers(label + ": 6面の内積", values, expected);
        Equal(label + ": 分類", ConvexBoxChecks.Locate(K, p), location);
    }

    /// <summary>状態・整数・Boolean・nullable値を等値比較し、成功した呼び出しを1件とする。</summary>
    /// <typeparam name="T">比較する型。</typeparam><param name="label">検査名。</param>
    /// <param name="actual">実値。</param><param name="expected">独立な期待値。</param>
    private static void Equal<T>(string label, T actual, T expected)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new InvalidOperationException($"{label}: 実値={actual}, 期待値={expected}");
        checks++;
        Console.WriteLine($"  [OK] {label}: {actual?.ToString() ?? "null"}");
    }

    /// <summary>未丸めの数値配列を照合し、全成分成功を1チェックと数える。</summary>
    /// <param name="label">検査名。</param><param name="actual">実値。</param><param name="expected">独立な期待値。</param>
    /// <param name="absolute">絶対許容誤差。値と同じ単位。</param><param name="relative">無次元の相対許容誤差。</param>
    private static void Numbers(string label, double[] actual, double[] expected, double absolute = 1e-12, double relative = 1e-12)
    {
        try { RequireNear(actual, expected, absolute, relative); }
        catch (InvalidOperationException error) { throw new InvalidOperationException(label + ": " + error.Message, error); }
        checks++;
        Console.WriteLine($"  [OK] {label}: {string.Join(", ", actual.Select(x => x.ToString("G9", CultureInfo.InvariantCulture)))}");
    }

    /// <summary>絶対＋相対許容誤差で比較する。幾何学的な境界分類には使用しない。</summary>
    /// <param name="actual">有限な実値配列。</param><param name="expected">同じ次元・単位の有限な期待値。</param>
    /// <param name="absolute">0以上の有限な絶対許容誤差。</param><param name="relative">0以上の有限な相対許容誤差。</param>
    /// <remarks>不一致・無効な比較条件はInvalidOperationException。NaNで比較が抜けないよう先に有限性を調べる。</remarks>
    private static void RequireNear(double[] actual, double[] expected, double absolute, double relative)
    {
        if (actual.Length != expected.Length || !double.IsFinite(absolute) || !double.IsFinite(relative) || absolute < 0 || relative < 0)
            throw new InvalidOperationException("比較次元または許容誤差が無効です。");
        for (int i = 0; i < actual.Length; i++)
        {
            double tolerance = absolute + relative * Math.Max(Math.Abs(actual[i]), Math.Abs(expected[i]));
            double difference = Math.Abs(actual[i] - expected[i]);
            if (!double.IsFinite(actual[i]) || !double.IsFinite(expected[i]) || !double.IsFinite(tolerance) ||
                !double.IsFinite(difference) || difference > tolerance)
                throw new InvalidOperationException(FormattableString.Invariant(
                    $"成分{i}: 実値={actual[i]:G17}, 期待値={expected[i]:G17}, 許容誤差={tolerance:G17}"));
        }
    }

    /// <summary>数値比較器自体が、不一致や非有限値を成功にしないことを検査する。</summary>
    /// <param name="label">検査名。</param><param name="comparison">必ず失敗すべき比較。</param>
    private static void RejectComparison(string label, Action comparison)
    {
        bool rejected = false;
        try { comparison(); } catch (InvalidOperationException) { rejected = true; }
        Equal(label, rejected, true);
    }
}

using System.Globalization;
using System.Numerics;
using System.Text;
using static ModelingMath;
using static MeshBuilder;

/// <summary>第4回の固定値と制約を、配置から閉じた境界まで7題材で追う。</summary>
/// <remarks>引数・標準入力・GUI・外部パッケージ・ファイル出力は使わない。</remarks>
internal static class Program
{
    private const double DoubleTolerance = 1e-12;
    private const double FloatTolerance = 1e-5;
    private static int checks;
    private static readonly SectionFrame Start = new((0, 0, 0), (0, 1, 0), (0, 0, 1), (1, 0, 0));
    private static readonly V2[] Rectangle = [(-1, -.5), (1, -.5), (1, .5), (-1, .5)];

    // 生成アルゴリズムから期待値を作らず、本文・宿題の表を独立に転記する。
    private static readonly V3[] ExpectedVertices = [(0, -1, -.5), (0, 1, -.5), (0, 1, .5), (0, -1, .5),
        (5, -1, -.5), (5, 1, -.5), (5, 1, .5), (5, -1, .5)];
    private static readonly Triangle[] ExpectedTriangles = [new(0, 1, 5), new(0, 5, 4), new(1, 2, 6), new(1, 6, 5),
        new(2, 3, 7), new(2, 7, 6), new(3, 0, 4), new(3, 4, 7), new(0, 2, 1), new(0, 3, 2), new(4, 5, 6), new(4, 6, 7)];

    /// <summary>全題材を実行し、失敗位置を表示して非0で終了する。</summary>
    /// <returns>全成功なら0、検算や入力検査が期待と異なれば1。</returns>
    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            Run("1. 局所断面から世界座標", LocalPlacement);
            Run("2. 4×4列方式とSystem.Numerics", HomogeneousMatrix);
            Run("3. 始端・終端の頂点生成", ExtrusionVertices);
            Run("4. 側面・キャップと外向き法線", FacesAndNormals);
            Run("5. 辺の接続と体積検算", ClosureAndVolume);
            Run("6. 経路上のフレームと回転点", StationFrames);
            Run("7. 退化・逆向き・数値精度", InvalidInputsAndPrecision);
            Console.WriteLine($"\nすべての {checks} チェックに成功しました。");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL: {error.GetType().Name}: {error.Message}");
            return 1;
        }
    }

    /// <summary>題材を区切り、その題材で成功した検証呼び出し数を表示する。</summary>
    /// <param name="title">題材名。</param><param name="sample">固定入力から始まる題材。</param>
    private static void Run(string title, Action sample)
    {
        Console.WriteLine($"\n{title}");
        int before = checks;
        sample();
        Console.WriteLine($"  この題材: {checks - before} チェック");
    }

    /// <summary>固定例Aの配置式と内積・外積を、成分式とfloatのAPIで照合する。</summary>
    private static void LocalPlacement()
    {
        // 解説②／基本1。NとBは断面内、Tはその法線である。
        SectionFrame frame = Start with { Center = (10, 20, 30) };
        V3 point = Place(frame, (2, -1));
        Check("A: C+2N−B", Components(point), [10, 22, 29]);
        Check("A: 基底の内積", [Dot(frame.N, frame.B), Dot(frame.N, frame.T), Dot(frame.B, frame.T)], [0, 0, 0]);
        Check("A: 基底の長さ", [Length(frame.N), Length(frame.B), Length(frame.T)], [1, 1, 1]);
        Check("A: N×B=T", Components(Cross(frame.N, frame.B)), [1, 0, 0]);
        Check("A: 局所原点はC", Components(Place(frame, (0, 0))), [10, 20, 30]);
        Vector3 apiPoint = new Vector3(10, 20, 30) + 2 * Vector3.UnitY - Vector3.UnitZ;
        Check("A: Vector3の配置", [apiPoint.X, apiPoint.Y, apiPoint.Z], [10, 22, 29], FloatTolerance);
        Vector3 apiCross = Vector3.Cross(Vector3.UnitY, Vector3.UnitZ);
        Check("A: Vector3.Cross", [apiCross.X, apiCross.Y, apiCross.Z], [1, 0, 0], FloatTolerance);
        Check("A: Vector3.Dot", [Vector3.Dot(Vector3.UnitY, Vector3.UnitZ)], [0], FloatTolerance);
    }

    /// <summary>本文の列方式の行列を手実装で掛け、転置後の行方式のAPIと照合する。</summary>
    private static void HomogeneousMatrix()
    {
        SectionFrame frame = Start with { Center = (10, 20, 30) };
        double[,] column = ColumnMatrix(frame);
        Check("A: 列N/B/T/Cの全16要素", column.Cast<double>().ToArray(), [0, 0, 1, 10, 1, 0, 0, 20, 0, 1, 0, 30, 0, 0, 0, 1]);
        Check("A: w=0/h=1の列積", TransformColumn(column, [2, -1, 0, 1]), [10, 22, 29, 1]);
        Check("A: h=0は平行移動を受けない", TransformColumn(column, [2, -1, 0, 0]), [0, 2, -1, 0]);
        Check("A: wはT方向の局所座標", TransformColumn(column, [2, -1, 3, 1]), [13, 22, 29, 1]);
        Matrix4x4 row = NumericsRowMatrix(column);
        Vector4 api = Vector4.Transform(new Vector4(2, -1, 0, 1), row);
        Check("A: 転置した行方式Vector4", [api.X, api.Y, api.Z, api.W], [10, 22, 29, 1], FloatTolerance);
        Vector3 api3 = Vector3.Transform(new Vector3(2, -1, 0), row);
        Check("A: Vector3.Transform", [api3.X, api3.Y, api3.Z], [10, 22, 29], FloatTolerance);
        Check("A: 行方式の平行移動は第4行", [row.M41, row.M42, row.M43, row.M44], [10, 20, 30, 1]);
        Vector4 direction = Vector4.Transform(new Vector4(2, -1, 0, 0), row);
        Check("A: APIのh=0", [direction.X, direction.Y, direction.Z, direction.W], [0, 2, -1, 0], FloatTolerance);
    }

    /// <summary>固定例Bの各局所点を始終端に配置し、本文の8座標と照合する。</summary>
    private static void ExtrusionVertices()
    {
        // 解説③／基本2。C0は原点で、例Aの中心とは異なる。
        V3[] vertices = CreateVertices(Rectangle, true, Start, 5);
        Check("B: 共有頂点数", [vertices.Length], [8]);
        for (int i = 0; i < vertices.Length; i++)
            Check($"B: 頂点{i}", Components(vertices[i]), Components(ExpectedVertices[i]));
    }

    /// <summary>接続を実際に生成し、全三角形の順序・面積・外積・法線を確認する。</summary>
    private static void FacesAndNormals()
    {
        Mesh mesh = ExtrudeConvexQuad(Rectangle, true, Start, 5);
        Check("B: 三角形数", [mesh.Triangles.Length], [12]);
        Check("B: インデックス数", [mesh.Triangles.Length * 3], [36]);
        V3[] expectedCrosses = [(0, 0, -10), (0, 5, 0), (0, 0, 10), (0, -5, 0), (-2, 0, 0), (2, 0, 0)];
        V3[] expectedNormals = [(0, 0, -1), (0, 1, 0), (0, 0, 1), (0, -1, 0), (-1, 0, 0), (1, 0, 0)];
        double[] expectedAreas = [5, 2.5, 5, 2.5, 1, 1];
        for (int i = 0; i < mesh.Triangles.Length; i++)
        {
            Triangle face = mesh.Triangles[i], expected = ExpectedTriangles[i];
            Check($"B: 三角形{i}の順序", [face.A, face.B, face.C], [expected.A, expected.B, expected.C]);
            Console.WriteLine($"    3点: {Text(mesh.Vertices[face.A])} / {Text(mesh.Vertices[face.B])} / {Text(mesh.Vertices[face.C])}");
            FaceGeometry geometry = AnalyzeTriangle(mesh.Vertices, face);
            Check($"B: 三角形{i}の外積", Components(geometry.AreaVector), Components(expectedCrosses[i / 2]));
            Check($"B: 三角形{i}の単位法線", Components(geometry.UnitNormal), Components(expectedNormals[i / 2]));
            Check($"B: 三角形{i}の面積/2倍面積/単位長", [geometry.Area, Length(geometry.AreaVector), Length(geometry.UnitNormal)],
                [expectedAreas[i / 2], 2 * expectedAreas[i / 2], 1]);
            V3 centroid = Scale(Add(Add(mesh.Vertices[face.A], mesh.Vertices[face.B]), mesh.Vertices[face.C]), 1.0 / 3);
            Assert($"B: 三角形{i}は直柱中心から外向き", Dot(geometry.AreaVector, Sub(centroid, (2.5, 0, 0))) > 0);
        }
    }

    /// <summary>断面積×長さを先に検算し、辺の対と符号付き体積を別に調べる。</summary>
    private static void ClosureAndVolume()
    {
        Mesh mesh = ExtrudeConvexQuad(Rectangle, true, Start, 5);
        double area = SignedArea(Rectangle);
        Check("B: 断面積", [area], [2]);
        Check("B: 断面積×法線方向の長さ", [area * 5], [10]);
        Assert("B: 全辺が逆向きの対", HasPairedOppositeEdges(mesh));
        int edges = mesh.Triangles.SelectMany(f => new[] { (f.A, f.B), (f.B, f.C), (f.C, f.A) })
            .Select(e => (Math.Min(e.Item1, e.Item2), Math.Max(e.Item1, e.Item2))).Distinct().Count();
        Check("B: 対角線も含む辺数", [edges], [18]);
        Check("B: 符号付き体積", [SignedVolume(mesh)], [10]);
        double surfaceArea = mesh.Triangles.Sum(f => AnalyzeTriangle(mesh.Vertices, f).Area);
        Check("B: 表面積は体積と別", [surfaceArea], [34]);
        Mesh translated = mesh with { Vertices = mesh.Vertices.Select(p => Add(p, (10, 20, 30))).ToArray() };
        Check("B: 平行移動後も体積10", [SignedVolume(translated)], [10]);
        Console.WriteLine("    辺の対の検査だけでは自己交差・頂点周り・有効ソリッドを保証しない。");
    }

    /// <summary>固定例Cの2フレームを比較し、角の接続を勝手に補わない。Dの回転点も照合する。</summary>
    private static void StationFrames()
    {
        SectionFrame end = new((5, 4, 0), (-1, 0, 0), (0, 0, 1), (0, 1, 0));
        ValidateFrame(end);
        Check("C: 終点N×B=T", Components(Cross(end.N, end.B)), [0, 1, 0]);
        Check("C: 終点の直交性", [Dot(end.N, end.B), Dot(end.N, end.T), Dot(end.B, end.T)], [0, 0, 0]);
        Check("C: 終点の単位長", [Length(end.N), Length(end.B), Length(end.T)], [1, 1, 1]);
        Check("C: 始点の局所(1,0)", Components(Place(Start, (1, 0))), [0, 1, 0]);
        Check("C: 終点の局所(1,0)", Components(Place(end, (1, 0))), [4, 4, 0]);
        Check("C: 終点の列行列", TransformColumn(ColumnMatrix(end), [1, 0, 0, 1]), [4, 4, 0, 1]);
        Vector3 api = Vector3.Transform(new Vector3(1, 0, 0), NumericsRowMatrix(ColumnMatrix(end)));
        Check("C: 終点の行方式API", [api.X, api.Y, api.Z], [4, 4, 0], FloatTolerance);
        Check("C: 姿勢を固定した場合", Components(Place(Start with { Center = end.Center }, (1, 0))), [5, 5, 0]);
        Check("C: 角の左側フレーム", Components(Place(Start with { Center = (5, 0, 0) }, (1, 0))), [5, 1, 0]);
        Check("C: 角の右側フレーム", Components(Place(end with { Center = (5, 0, 0) }, (1, 0))), [4, 0, 0]);
        Console.WriteLine("    角で接線は+Xから+Yへ不連続。留め継ぎ・丸めなどの接続条件は未定義。");
        double theta = Math.PI / 2;
        Check("D: 半径2・高さ1・90度", [2 * Math.Cos(theta), 2 * Math.Sin(theta), 1], [0, 2, 1]);
        Console.WriteLine("    回転点の照合であり、汎用スイープ・回転体メッシュの生成ではない。");
    }

    /// <summary>無効な入力を拒否し、向きの限界とfloatで失われる差を確かめる。</summary>
    private static void InvalidInputsAndPrecision()
    {
        foreach (double length in new[] { 0.0, -5, 1e-13, double.NaN, double.PositiveInfinity })
            Reject($"押し出し長{length:G9}を拒否", () => ExtrudeConvexQuad(Rectangle, true, Start, length));
        Reject("開いた輪郭を拒否", () => ExtrudeConvexQuad(Rectangle, false, Start, 5));
        Reject("3頂点を拒否", () => ExtrudeConvexQuad(Rectangle[..3], true, Start, 5));
        Reject("始点の重複保存を拒否", () => ExtrudeConvexQuad([.. Rectangle, Rectangle[0]], true, Start, 5));
        foreach (var (name, section) in new (string, V2[])[] {
            ("連続重複・ゼロ辺", [(-1,-.5), (-1,-.5), (1,.5), (-1,.5)]),
            ("非隣接重複", [(-1,-.5), (1,-.5), (-1,-.5), (-1,.5)]),
            ("一直線・ゼロ面積", [(0,0), (1,0), (2,0), (3,0)]),
            ("下限以下の辺", [(0,0), (1e-13,0), (1,1), (0,1)]),
            ("自己交差", [(-1,-.5), (1,.5), (1,-.5), (-1,.5)]),
            ("非凸", [(0,0), (2,0), (.5,.5), (0,2)]),
            ("NaN", [(double.NaN,0), (1,0), (1,1), (0,1)]),
            ("無限大", [(0,0), (double.PositiveInfinity,0), (1,1), (0,1)]) })
            Reject(name + "の輪郭を拒否", () => ExtrudeConvexQuad(section, true, Start, 5));
        Reject("時計回りを拒否", () => ExtrudeConvexQuad(Rectangle.Reverse().ToArray(), true, Start, 5));
        Reject("ゼロ基底を拒否", () => Place(Start with { N = (0, 0, 0) }, (1, 0)));
        Reject("非単位基底を拒否", () => Place(Start with { N = (0, 2, 0) }, (1, 0)));
        Reject("非直交基底を拒否", () => Place(Start with { B = (0, 1, 0) }, (1, 0)));
        Reject("左手系を拒否", () => Place(Start with { T = (-1, 0, 0) }, (1, 0)));
        Reject("非有限中心を拒否", () => Place(Start with { Center = (0, double.NaN, 0) }, (1, 0)));
        Reject("非有限基底を拒否", () => Place(Start with { N = (0, double.PositiveInfinity, 0) }, (1, 0)));
        Reject("非有限局所点を拒否", () => Place(Start, (double.NaN, 0)));
        Reject("配置の桁あふれを拒否", () => Place(Start with { Center = (0, double.MaxValue, 0) }, (double.MaxValue, 0)));
        Reject("世界座標でつぶれる断面を拒否", () => ExtrudeConvexQuad(Rectangle, true, Start with { Center = (0, 1e20, 0) }, 5));
        Reject("重複インデックスを拒否", () => AnalyzeTriangle(ExpectedVertices, new(0, 0, 1)));
        Reject("負のインデックスを拒否", () => AnalyzeTriangle(ExpectedVertices, new(-1, 0, 1)));
        Reject("範囲外インデックスを拒否", () => AnalyzeTriangle(ExpectedVertices, new(0, 1, 8)));
        Reject("一直線の三角形を単位化しない", () => AnalyzeTriangle([(0, 0, 0), (1, 0, 0), (2, 0, 0)], new(0, 1, 2)));
        Reject("微小三角形を単位化しない", () => AnalyzeTriangle([(0, 0, 0), (1e-8, 0, 0), (0, 1e-8, 0)], new(0, 1, 2)));
        Reject("非有限な三角形を拒否", () => AnalyzeTriangle([(0, 0, 0), (double.NaN, 0, 0), (0, 1, 0)], new(0, 1, 2)));
        Reject("外積の桁あふれを拒否", () => AnalyzeTriangle([(0, 0, 0), (1e200, 0, 0), (0, 1e200, 0)], new(0, 1, 2)));
        Mesh mesh = ExtrudeConvexQuad(Rectangle, true, Start, 5);
        Mesh open = mesh with { Triangles = mesh.Triangles.Where((_, i) => i != 8 && i != 9).ToArray() };
        Assert("片側キャップなしは辺の対が崩れる", !HasPairedOppositeEdges(open));
        Reject("開いた境界に体積式を適用しない", () => SignedVolume(open));
        Mesh oneReversed = mesh with { Triangles = (Triangle[])mesh.Triangles.Clone() };
        oneReversed.Triangles[0] = new(0, 5, 1);
        Assert("1枚だけ反転すると辺の向きが不一致", !HasPairedOppositeEdges(oneReversed));
        Mesh allReversed = mesh with { Triangles = mesh.Triangles.Select(f => new Triangle(f.A, f.C, f.B)).ToArray() };
        Assert("全反転は辺の対だけでは検出不能", HasPairedOppositeEdges(allReversed));
        Check("全反転の符号付き体積", [SignedVolume(allReversed)], [-10]);
        Check("反転した底面の外積", Components(AnalyzeTriangle(oneReversed.Vertices, oneReversed.Triangles[0]).AreaVector), [0, 0, 10]);
        Assert("空メッシュを閉じた境界としない", !HasPairedOppositeEdges(new([], [])));
        Assert("範囲外番号は辺検査でも拒否", !HasPairedOppositeEdges(new(mesh.Vertices, [new(0, 1, 99)])));
        Reject("行列の寸法不一致を拒否", () => TransformColumn(new double[3, 3], [1, 2, 3, 1]));
        Reject("入力列の寸法不一致を拒否", () => TransformColumn(ColumnMatrix(Start), [1, 2, 3]));
        Reject("非有限な列を拒否", () => TransformColumn(ColumnMatrix(Start), [double.NaN, 0, 0, 1]));
        Reject("float変換範囲外を拒否", () => NumericsRowMatrix(ColumnMatrix(Start with { Center = (double.MaxValue, 0, 0) })));
        double tinyDifference = (1.0 + 1e-10) - 1.0;
        double lostDifference = (double)(float)(1.0 + 1e-10) - 1.0;
        Check("doubleには小さな差が残る", [tinyDifference], [1e-10]);
        Check("float変換で差が消える", [lostDifference], [0]);
        Assert("doubleへ戻しても精度は戻らない", tinyDifference > 0 && lostDifference == 0);
    }

    /// <summary>タプルの成分を比較用の配列へ並べる。</summary>
    /// <param name="p">点またはベクトル。</param><returns>X・Y・Zの順の配列。</returns>
    private static double[] Components(V3 p) => [p.X, p.Y, p.Z];

    /// <summary>表示専用に座標を整形する。検証はこの文字列ではなく元のdoubleを使う。</summary>
    /// <param name="p">表示する点。</param><returns>丸括弧付きの座標。</returns>
    private static string Text(V3 p) => "(" + string.Join(",", Components(p).Select(x => x.ToString("G9", CultureInfo.InvariantCulture))) + ")";

    /// <summary>実値と独立な期待値の両方の有限性を調べ、未丸め値を絶対誤差で比較する。</summary>
    /// <param name="label">検証名。</param><param name="actual">実値。</param><param name="expected">数学から求めた期待値。</param>
    /// <param name="tolerance">固定入力に合わせた有限で非負の許容誤差。</param>
    private static void Check(string label, double[] actual, double[] expected, double tolerance = DoubleTolerance)
    {
        if (actual.Length != expected.Length || !double.IsFinite(tolerance) || tolerance < 0)
            throw new InvalidOperationException(label + ": 比較の成分数・許容誤差が無効です。");
        for (int i = 0; i < actual.Length; i++)
            if (!double.IsFinite(actual[i]) || !double.IsFinite(expected[i]) || Math.Abs(actual[i] - expected[i]) > tolerance)
                throw new InvalidOperationException($"{label}[{i}]: actual={actual[i]:G17}, expected={expected[i]:G17}, tolerance={tolerance:G17}");
        Passed(label, string.Join(", ", actual.Select(x => x.ToString("G9", CultureInfo.InvariantCulture))));
    }

    /// <summary>非数値の条件を検証する。</summary>
    /// <param name="label">検証名。</param><param name="condition">期待する条件。</param>
    private static void Assert(string label, bool condition)
    {
        if (!condition) throw new InvalidOperationException(label + ": 条件を満たしません。");
        Passed(label, "確認済み");
    }

    /// <summary>無効入力がArgumentExceptionで明示的に拒否されることを検証する。</summary>
    /// <param name="label">入力の問題点。</param><param name="action">拒否されるはずの呼び出し。</param>
    private static void Reject(string label, Action action)
    {
        try { action(); }
        catch (ArgumentException error) { Passed(label, error.Message); return; }
        throw new InvalidOperationException(label + ": 入力が拒否されませんでした。");
    }

    /// <summary>成功した検証呼び出し1件を数え、題材ごとに[OK]を出力する。</summary>
    /// <param name="label">検証名。</param><param name="detail">表示用の値や説明。</param>
    private static void Passed(string label, string detail)
    {
        checks++;
        Console.WriteLine($"  [OK] {label}: {detail}");
    }
}

using System.Globalization;
using System.Numerics;
using System.Text;

/// <summary>第2回の共有数値例を6題材で実行し、手実装と.NET APIを期待値と比較する。</summary>
/// <remarks>
/// 入力は固定値。2DはX右・Y上、3Dは右手系で、同じ直交座標系を使う。
/// 各題材の通常コメントは、解説・宿題との対応と検証意図を示す。
/// </remarks>
internal static class Program
{
    // この教材の小さな固定値に対する絶対誤差。floatを含む比較には広い許容誤差を使う。
    private const double DoubleTolerance = 1e-12;
    private const double FloatTolerance = 1e-5;
    private static int checks;

    /// <summary>6題材を順に実行し、成功なら0、チェック失敗なら1で終了する。</summary>
    /// <returns>プロセスの終了コード。</returns>
    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            DotLengthAndAngle();
            ProjectionAndOrthogonality();
            CrossProduct3D();
            OrientationAndArea2D();
            TriangleNormal3D();
            DegenerateAndPrecision();
            Console.WriteLine($"\nすべての {checks} チェックに成功しました。");
            return 0;
        }
        catch (InvalidOperationException error)
        {
            Console.Error.WriteLine($"FAIL: {error.Message}");
            return 1;
        }
    }

    /// <summary>内積・長さから求めた角度を、成分計算と.NET APIで確認する。</summary>
    private static void DotLengthAndAngle()
    {
        // 解説②・基本問題1：積の和が長さの積×cosθになることを共有例で確かめる。
        // 手実装はdouble、.NETのVector2はfloat。度への変換はラジアンを求めてから行う。
        Console.WriteLine("1. 内積・長さ・角度（解説②／基本問題1）");
        var a = (X: 3.0, Y: 4.0);
        var b = (X: 2.0, Y: 0.0);
        double dot = VectorMath.Dot2(a, b);
        double lengthA = VectorMath.Length2(a);
        double lengthB = VectorMath.Length2(b);
        Check("a・b", [dot], [6]);
        Check("長さ |a|,|b|", [lengthA, lengthB], [5, 2]);
        Check("cosθ", [dot / (lengthA * lengthB)], [3.0 / 5]);
        CheckCondition("非ゼロの2本の角度を定義できる", VectorMath.TryAngle2(a, b, out double radians));
        Check("角度（ラジアン）", [radians], [0.9272952180016122]);
        Check("角度（度）", [radians * 180 / Math.PI], [53.13010235415598]);
        Console.WriteLine($"  角度（表示用・小数6桁）: {(radians * 180 / Math.PI).ToString("F6", CultureInfo.InvariantCulture)}°");

        var apiA = new Vector2(3, 4);
        var apiB = new Vector2(2, 0);
        Check("Vector2.Dot", [Vector2.Dot(apiA, apiB)], [dot], FloatTolerance);
        Check("Vector2.Length", [apiA.Length(), apiB.Length()], [lengthA, lengthB], FloatTolerance);
        // 固定入力が非ゼロなのを確認済み。APIのfloatの比もクランプしてからacosに渡す。
        double apiCosine = Vector2.Dot(apiA, apiB) / (apiA.Length() * apiB.Length());
        Check("floatの値から求めた角度", [Math.Acos(Math.Clamp(apiCosine, -1.0, 1.0))], [radians], FloatTolerance);
    }

    /// <summary>非単位ベクトルへの射影と、残りの垂直成分を確認する。</summary>
    private static void ProjectionAndOrthogonality()
    {
        // 解説③・基本問題2：q=tbの残りがbと垂直となる係数tは(a・b)/(b・b)。
        // 基本問題1の直交例も比較し、内積0の意味を数値で確かめる。
        Console.WriteLine("\n2. 射影と直交（解説③／基本問題1・2）");
        var a = (X: 3.0, Y: 4.0);
        var b = (X: 2.0, Y: 0.0);
        CheckCondition("射影先の方向を定義できる", VectorMath.TryProject2(a, b, out var projection));
        Check("正射影", [projection.X, projection.Y], [3, 0]);
        var residual = (X: a.X - projection.X, Y: a.Y - projection.Y);
        Check("残り a-q", [residual.X, residual.Y], [0, 4]);
        Check("残りとbの内積", [VectorMath.Dot2(residual, b)], [0]);
        Check("(3,4)・(4,-3)", [VectorMath.Dot2(a, (4, -3))], [0]);

        var apiA = new Vector2(3, 4);
        var apiB = new Vector2(2, 0);
        // bは長さ2。分母を1とせず、b・b=4で割る。
        var apiProjection = Vector2.Dot(apiA, apiB) / Vector2.Dot(apiB, apiB) * apiB;
        var apiResidual = apiA - apiProjection;
        Check(".NET APIによる射影", [apiProjection.X, apiProjection.Y], [projection.X, projection.Y], FloatTolerance);
        Check(".NET APIによる残りの直交", [Vector2.Dot(apiResidual, apiB)], [0], FloatTolerance);
    }

    /// <summary>外積の向き・面積・反交換性と、両入力との直交を確認する。</summary>
    private static void CrossProduct3D()
    {
        // 解説④：右手系ではX方向×Y方向が+Z。外積の長さは平行四辺形の面積。
        // 一般の3成分の例は発展問題の任意補足にも対応し、Vector3.Dotで直交を検算する。
        Console.WriteLine("\n3. 3D外積（解説④／発展問題の補足）");
        var u = (X: 2.0, Y: 0.0, Z: 0.0);
        var v = (X: 0.0, Y: 3.0, Z: 0.0);
        var cross = VectorMath.Cross3(u, v);
        var reverse = VectorMath.Cross3(v, u);
        Check("u×v", [cross.X, cross.Y, cross.Z], [0, 0, 6]);
        Check("v×u", [reverse.X, reverse.Y, reverse.Z], [0, 0, -6]);
        Check("平行四辺形の面積", [VectorMath.Length3(cross)], [6]);
        Check("三角形の面積", [VectorMath.Length3(cross) / 2], [3]);
        var apiCross = Vector3.Cross(new Vector3(2, 0, 0), new Vector3(0, 3, 0));
        Check("Vector3.Cross", [apiCross.X, apiCross.Y, apiCross.Z], [cross.X, cross.Y, cross.Z], FloatTolerance);

        var a = (X: 1.0, Y: 2.0, Z: 3.0);
        var b = (X: 4.0, Y: 5.0, Z: 6.0);
        var general = VectorMath.Cross3(a, b);
        Check("一般のa×b", [general.X, general.Y, general.Z], [-3, 6, -3]);
        Check("aとの直交", [VectorMath.Dot3(a, general)], [0]);
        Check("bとの直交", [VectorMath.Dot3(b, general)], [0]);
        var apiGeneral = Vector3.Cross(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
        Check("一般のVector3.Cross", [apiGeneral.X, apiGeneral.Y, apiGeneral.Z], [general.X, general.Y, general.Z], FloatTolerance);
        Check("Vector3.Dotでaとの直交", [Vector3.Dot(new Vector3(1, 2, 3), apiGeneral)], [0], FloatTolerance);
        Check("Vector3.Dotでbとの直交", [Vector3.Dot(new Vector3(4, 5, 6), apiGeneral)], [0], FloatTolerance);
    }

    /// <summary>2Dスカラー外積の符号から左右を読み、面積と区別する。</summary>
    private static void OrientationAndArea2D()
    {
        // 解説⑤・基本問題3：位置ベクトルをそのまま掛けず、同じ点Aから2辺を作る。
        // Cross2は自作の成分式。Z成分0のVector3.Crossと比べ、結果がスカラーだと確認する。
        Console.WriteLine("\n4. 2Dの左右と面積（解説⑤／基本問題3）");
        var a = (X: 1.0, Y: 1.0);
        var b = (X: 4.0, Y: 1.0);
        var c = (X: 3.0, Y: 3.0);
        var u = (X: b.X - a.X, Y: b.Y - a.Y);
        var v = (X: c.X - a.X, Y: c.Y - a.Y);
        Check("ABとAC", [u.X, u.Y, v.X, v.Y], [3, 0, 2, 2]);
        double left = VectorMath.Cross2(u, v);
        Check("Cのスカラー外積", [left], [6]);
        Check("符号付き三角形面積", [left / 2], [3]);
        Check("通常の三角形面積", [Math.Abs(left) / 2], [3]);
        var cRight = (X: 3.0, Y: -1.0);
        var cLine = (X: 2.0, Y: 1.0);
        double right = VectorMath.Cross2(u, (cRight.X - a.X, cRight.Y - a.Y));
        double collinear = VectorMath.Cross2(u, (cLine.X - a.X, cLine.Y - a.Y));
        Check("C′のスカラー外積", [right], [-6]);
        Check("C″のスカラー外積", [collinear], [0]);
        CheckCondition("Y上：Cは左、C′は右、C″は直線上", left > 0 && right < 0 && collinear == 0);
        Check("C′の符号付き面積と通常の面積", [right / 2, Math.Abs(right) / 2], [-3, 3]);
        var embedded = Vector3.Cross(new Vector3((float)u.X, (float)u.Y, 0), new Vector3((float)v.X, (float)v.Y, 0));
        Check("3Dへ埋め込んだ外積のZ成分", [embedded.Z], [left], FloatTolerance);
        // 同じ図をY下向きの座標値で表し直すと符号が反転する。幾何的な左右と座標の向きを区別する。
        Check("Yを反転した座標の符号", [VectorMath.Cross2((u.X, -u.Y), (v.X, -v.Y))], [-6]);
    }

    /// <summary>頂点順序による単位法線の反転と、退化時に正規化しないことを確認する。</summary>
    private static void TriangleNormal3D()
    {
        // 解説⑥・発展問題：三角形の法線は(B-A)×(C-A)。BとCの交換で向きが反転する。
        // 一般の法線変換の話ではなく、頂点から面の向きを作る話として扱う。
        Console.WriteLine("\n5. 3D三角形の法線（解説⑥／発展問題）");
        var a = (X: 1.0, Y: 1.0, Z: 0.0);
        var b = (X: 4.0, Y: 1.0, Z: 0.0);
        var c = (X: 3.0, Y: 3.0, Z: 0.0);
        var ab = (X: b.X - a.X, Y: b.Y - a.Y, Z: b.Z - a.Z);
        var ac = (X: c.X - a.X, Y: c.Y - a.Y, Z: c.Z - a.Z);
        var cross = VectorMath.Cross3(ab, ac);
        Check("AB×AC", [cross.X, cross.Y, cross.Z], [0, 0, 6]);
        Check("三角形ABCの面積", [VectorMath.Length3(cross) / 2], [3]);
        CheckCondition("ABCの法線を定義できる", VectorMath.TryTriangleNormal3(a, b, c, out var normal));
        Check("ABCの単位法線", [normal.X, normal.Y, normal.Z], [0, 0, 1]);
        Check("単位法線の長さ", [VectorMath.Length3(normal)], [1]);
        // 上の成否確認で非退化と分かってから.NETのNormalizeを使う。退化時には呼ばない。
        var apiNormal = Vector3.Normalize(Vector3.Cross(new Vector3(3, 0, 0), new Vector3(2, 2, 0)));
        Check(".NET APIの単位法線", [apiNormal.X, apiNormal.Y, apiNormal.Z], [normal.X, normal.Y, normal.Z], FloatTolerance);
        CheckCondition("ACBの法線を定義できる", VectorMath.TryTriangleNormal3(a, c, b, out var reversed));
        Check("ACBの単位法線", [reversed.X, reversed.Y, reversed.Z], [0, 0, -1]);
        Check("頂点逆順の面積", [VectorMath.Length3(VectorMath.Cross3(ac, ab)) / 2], [3]);
        CheckCondition("一直線の法線は未定義", !VectorMath.TryTriangleNormal3(a, b, (2, 1, 0), out _));
        CheckCondition("C=Aの重複点の法線は未定義", !VectorMath.TryTriangleNormal3(a, b, a, out _));
        CheckCondition("B=Aの重複点の法線は未定義", !VectorMath.TryTriangleNormal3(a, a, c, out _));
        Console.WriteLine("  外積0の三角形は正規化しない。未定義を単位法線(0,0,0)で代用しない。");
    }

    /// <summary>ゼロ長さ・丸め・非有限値と、許容誤差を使う条件を確認する。</summary>
    private static void DegenerateAndPrecision()
    {
        // 解説③・⑦：向きがない入力は未定義。絶対誤差と、長さで割った無次元の尺度を区別する。
        // 追加の小数例は数値誤差を示す補足であり、講義・宿題の共有例を置き換えるものではない。
        Console.WriteLine("\n6. 退化・誤差・規約（解説③・⑦）");
        CheckCondition("ゼロのaとの角度は未定義", !VectorMath.TryAngle2((0, 0), (2, 0), out _));
        CheckCondition("ゼロのbとの角度は未定義", !VectorMath.TryAngle2((3, 4), (0, 0), out _));
        CheckCondition("ゼロの射影先は未定義", !VectorMath.TryProject2((3, 4), (0, 0), out _));
        CheckCondition("ゼロの射影元は扱える", VectorMath.TryProject2((0, 0), (2, 0), out var zeroProjection));
        Check("ゼロの射影元の結果", [zeroProjection.X, zeroProjection.Y], [0, 0]);

        var a = (X: 1.0, Y: 2.0, Z: 3.0);
        var b = (X: 2.0, Y: 4.0, Z: 6.0);
        var parallelCross = VectorMath.Cross3(a, b);
        Check("平行な2本の外積", [parallelCross.X, parallelCross.Y, parallelCross.Z], [0, 0, 0]);
        const double angularTolerance = 1e-12; // 無次元。長さや面積の下限とは別の条件。
        double sineScale = VectorMath.Length3(parallelCross) / (VectorMath.Length3(a) * VectorMath.Length3(b));
        CheckCondition("非ゼロの2本は平行（sinの尺度）", double.IsFinite(sineScale) && sineScale <= angularTolerance);

        var almostPerpendicular = (X: 0.1 + 0.2, Y: 1.0);
        var reference = (X: 1.0, Y: -0.3);
        double residualDot = VectorMath.Dot2(almostPerpendicular, reference);
        CheckCondition("丸めで内積は厳密な0にならない", residualDot != 0 && double.IsFinite(residualDot));
        Check("許容誤差内の内積", [residualDot], [0]);
        double cosineScale = Math.Abs(residualDot) / (VectorMath.Length2(almostPerpendicular) * VectorMath.Length2(reference));
        CheckCondition("ほぼ垂直（cosの尺度）", double.IsFinite(cosineScale) && cosineScale <= angularTolerance);
        Console.WriteLine($"  丸めの内積残差（未丸めの表示）: {residualDot.ToString("G17", CultureInfo.InvariantCulture)}");

        // sqrt(13)の丸めで同方向のcosが1をわずかに超えることがある。TryAngle2内でクランプする。
        CheckCondition("同方向の角度を計算できる", VectorMath.TryAngle2((2, 3), (2, 3), out double sameAngle));
        Check("同方向の角度", [sameAngle], [0]);
        CheckCondition("逆方向の角度を計算できる", VectorMath.TryAngle2((2, 3), (-2, -3), out double oppositeAngle));
        Check("逆方向の角度", [oppositeAngle], [Math.PI]);

        var tinyCross = VectorMath.Cross3((1, 1, 0), (1, 1 + 1e-13, 0));
        CheckCondition("doubleに残る小さな外積", tinyCross.Z > 0 && tinyCross.Z < 1e-12);
        var floatCross = Vector3.Cross(new Vector3(1, 1, 0), new Vector3(1, (float)(1 + 1e-13), 0));
        Check("floatへ丸めると消える差", [floatCross.Z], [0], FloatTolerance);
        CheckCondition("面積の下限以下では法線を作らない", !VectorMath.TryTriangleNormal3((0, 0, 0), (1, 1, 0), (1, 1 + 1e-13, 0), out _));

        CheckCondition("NaNの角度入力を拒否", !VectorMath.TryAngle2((double.NaN, 4), (2, 0), out _));
        CheckCondition("Infinityの角度入力を拒否", !VectorMath.TryAngle2((double.PositiveInfinity, 4), (2, 0), out _));
        CheckCondition("NaNの射影入力を拒否", !VectorMath.TryProject2((double.NaN, 4), (2, 0), out _));
        CheckCondition("Infinityの射影先を拒否", !VectorMath.TryProject2((3, 4), (double.PositiveInfinity, 0), out _));
        CheckCondition("NaNの頂点を拒否", !VectorMath.TryTriangleNormal3((double.NaN, 0, 0), (1, 0, 0), (0, 1, 0), out _));
        CheckCondition("Infinityの頂点を拒否", !VectorMath.TryTriangleNormal3((0, 0, 0), (double.PositiveInfinity, 0, 0), (0, 1, 0), out _));
        CheckCondition("長さの桁あふれを拒否", !VectorMath.TryAngle2((double.MaxValue, 1), (2, 0), out _));
        Console.WriteLine("  長さ・面積の下限は教材の単位に依存する。任意スケールや厳密な位相判定は対象外。");
    }

    /// <summary>表示で丸める前の有限値を、成分ごとの絶対誤差で比較する。</summary>
    /// <param name="label">失敗箇所を示すラベル。</param>
    /// <param name="actual">実値。</param>
    /// <param name="expected">数学から別に求めた期待値。</param>
    /// <param name="tolerance">この固定入力に使う絶対誤差の上限。</param>
    private static void Check(string label, double[] actual, double[] expected, double tolerance = DoubleTolerance)
    {
        if (actual.Length != expected.Length)
            throw new InvalidOperationException($"{label}: 成分数が一致しません。");
        for (int i = 0; i < actual.Length; i++)
        {
            // NaNの大小比較やInfinity同士の引き算で不一致を見逃さないよう、両側の有限性も検査する。
            if (!double.IsFinite(actual[i]) || !double.IsFinite(expected[i]) || Math.Abs(actual[i] - expected[i]) > tolerance)
                throw new InvalidOperationException($"{label}[{i}]: actual={Format(actual)}, expected={Format(expected)}, tolerance={tolerance}");
        }
        Passed(label, Format(actual));
    }

    /// <summary>定義可能・未定義・符号などの条件を自動チェックする。</summary>
    /// <param name="label">検証する条件名。</param>
    /// <param name="condition">期待する条件を満たせばtrue。</param>
    private static void CheckCondition(string label, bool condition)
    {
        if (!condition)
            throw new InvalidOperationException($"{label}: 条件を満たしません。");
        Passed(label, "確認済み");
    }

    /// <summary>数値・条件のどちらの成功も1件として数え、同じ形式で表示する。</summary>
    /// <param name="label">成功したチェック名。</param>
    /// <param name="detail">表示用の結果。</param>
    private static void Passed(string label, string detail)
    {
        checks++;
        Console.WriteLine($"  [OK] {label}: {detail}");
    }

    /// <summary>値を不変カルチャーの有効数字9桁で表示する。比較にはこの文字列を使わない。</summary>
    /// <param name="values">表示する成分。</param>
    /// <returns>括弧で囲んだ成分の文字列。</returns>
    private static string Format(double[] values) =>
        $"({string.Join(", ", values.Select(value => value.ToString("G9", CultureInfo.InvariantCulture)))})";
}

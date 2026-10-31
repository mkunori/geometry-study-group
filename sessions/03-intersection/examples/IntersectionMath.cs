/// <summary>直交座標系・2DのX右Y上・3D右手系に対する、教材用double成分計算。</summary>
/// <remarks>
/// 小さな固定入力用。厳密述語や任意スケール用の幾何ライブラリではない。
/// 生の成分演算は補正せず、交差メソッドが有限性・退化・角度・残差を検査する。
/// </remarks>
internal static class IntersectionMath
{
    // 同じ数値でも尺度は別。法線の大きさには方向長の下限を適用しない。
    internal const double MinimumDirectionLength = 1e-12;
    internal const double MinimumTriangleArea2 = 1e-12;
    internal const double AngularTolerance = 1e-12;
    internal const double DistanceTolerance = 1e-9;
    internal const double ParameterTolerance = 1e-12;

    /// <summary>2Dの点または変位に、変位を足す。</summary>
    /// <param name="a">元の点または変位。</param><param name="b">加える変位。</param>
    /// <returns>成分の和。有限性は呼び出し側で確認する。</returns>
    public static V2 Add2(V2 a, V2 b) => (a.X + b.X, a.Y + b.Y);

    /// <summary>2Dの2点の差、または変位の差を求める。</summary>
    /// <param name="a">到着側。</param><param name="b">基準側。</param>
    /// <returns>bからaへの変位。</returns>
    public static V2 Sub2(V2 a, V2 b) => (a.X - b.X, a.Y - b.Y);

    /// <summary>2D変位を媒介変数倍する。</summary>
    /// <param name="a">変位。</param><param name="t">無次元の係数。</param>
    /// <returns>各成分をt倍した変位。</returns>
    public static V2 Scale2(V2 a, double t) => (a.X * t, a.Y * t);

    /// <summary>2D内積を成分の積の和から求める。</summary>
    /// <param name="a">第1のベクトル。</param><param name="b">第2のベクトル。</param>
    /// <returns>a.X*b.X+a.Y*b.Y。</returns>
    public static double Dot2(V2 a, V2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>2Dスカラー外積を求める。順序を逆にすると符号が反転する。</summary>
    /// <param name="a">第1の方向。</param><param name="b">第2の方向。</param>
    /// <returns>a.X*b.Y-a.Y*b.X。符号付き面積、または3D外積のZ成分。</returns>
    public static double Cross2(V2 a, V2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>2Dベクトルの長さを三平方の定理から求める。</summary>
    /// <param name="a">入力ベクトル。</param><returns>長さ。極端な値では桁あふれ・桁落ちがあり得る。</returns>
    public static double Length2(V2 a) => Math.Sqrt(Dot2(a, a));

    /// <summary>3Dの点または変位に、変位を足す。</summary>
    /// <param name="a">元の点または変位。</param><param name="b">加える変位。</param>
    /// <returns>3成分の和。</returns>
    public static V3 Add3(V3 a, V3 b) => (a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>3Dの点または変位の差を求める。</summary>
    /// <param name="a">到着側。</param><param name="b">基準側。</param><returns>bからaへの変位。</returns>
    public static V3 Sub3(V3 a, V3 b) => (a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>3D変位を係数倍する。</summary>
    /// <param name="a">変位。</param><param name="t">係数。</param><returns>各成分をt倍した変位。</returns>
    public static V3 Scale3(V3 a, double t) => (a.X * t, a.Y * t, a.Z * t);

    /// <summary>3D内積を成分の積の和から求める。</summary>
    /// <param name="a">第1のベクトル。</param><param name="b">第2のベクトル。</param><returns>スカラーの内積。</returns>
    public static double Dot3(V3 a, V3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>右手系の3D外積を成分式から求める。</summary>
    /// <param name="a">第1の方向。</param><param name="b">第2の方向。</param><returns>両入力に垂直なベクトル。</returns>
    public static V3 Cross3(V3 a, V3 b) =>
        (a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    /// <summary>3Dベクトルの長さを求める。</summary>
    /// <param name="a">入力ベクトル。</param><returns>長さ。極端な値へのスケーリングは行わない。</returns>
    public static double Length3(V3 a) => Math.Sqrt(Dot3(a, a));

    /// <summary>2D入力または中間点の全成分が有限か調べる。</summary>
    /// <param name="a">調べる点・方向。</param><returns>NaN・Infinityを含まなければtrue。</returns>
    public static bool Finite2(V2 a) => double.IsFinite(a.X) && double.IsFinite(a.Y);

    /// <summary>3D入力または中間点の全成分が有限か調べる。</summary>
    /// <param name="a">調べる点・方向・法線。</param><returns>全成分が有限ならtrue。</returns>
    public static bool Finite3(V3 a) => double.IsFinite(a.X) && double.IsFinite(a.Y) && double.IsFinite(a.Z);

    /// <summary>有限な媒介変数が閉線分の範囲に含まれるか調べる。</summary>
    /// <param name="t">方向を終点までの変位とした無次元の媒介変数。</param>
    /// <returns>0と1を含む範囲内ならtrue。許容誤差による範囲拡張はしない。</returns>
    public static bool InClosedUnitInterval(double t) => double.IsFinite(t) && t >= 0 && t <= 1;

    /// <summary>2D無限直線P+tr、Q+usの共有部分を外積で求める。</summary>
    /// <param name="p">第1直線上の点。</param><param name="r">第1方向。長さは座標と同じ単位。</param>
    /// <param name="q">第2直線上の点。</param><param name="s">第2方向。</param>
    /// <returns>一点（t・u付き）、なし、一致、退化、無効入力、または判定保留。</returns>
    /// <remarks>分母非ゼロでも角度の尺度が小さければUncertain。厳密平行へ読み替えない。</remarks>
    public static Intersection2 LineLine2(V2 p, V2 r, V2 q, V2 s)
    {
        if (!Finite2(p) || !Finite2(r) || !Finite2(q) || !Finite2(s))
            return new(IntersectionKind.InvalidInput);
        double nr = Length2(r), ns = Length2(s);
        if (!double.IsFinite(nr) || !double.IsFinite(ns)) return new(IntersectionKind.Uncertain);
        if (nr <= MinimumDirectionLength || ns <= MinimumDirectionLength)
            return new(IntersectionKind.Degenerate);

        var w = Sub2(q, p);
        double denominator = Cross2(r, s);
        double numeratorT = Cross2(w, s), numeratorU = Cross2(w, r);
        double scale = nr * ns;
        if (!Finite2(w) || !double.IsFinite(denominator) || !double.IsFinite(numeratorT) ||
            !double.IsFinite(numeratorU) || !double.IsFinite(scale)) return new(IntersectionKind.Uncertain);
        if (denominator == 0)
        {
            if (numeratorU == 0) return new(IntersectionKind.Coincident);
            return new(Math.Abs(numeratorU) / nr <= DistanceTolerance
                ? IntersectionKind.Uncertain : IntersectionKind.None);
        }
        if (Math.Abs(denominator) / scale <= AngularTolerance) return new(IntersectionKind.Uncertain);

        // w=tr-usにrを掛けた負号は、cross(s,r)=-cross(r,s)で打ち消される。
        double t = numeratorT / denominator, u = numeratorU / denominator;
        var x = Add2(p, Scale2(r, t));
        var other = Add2(q, Scale2(s, u));
        if (!double.IsFinite(t) || !double.IsFinite(u) || !Finite2(x) || !Finite2(other) ||
            !(Length2(Sub2(x, other)) <= DistanceTolerance)) return new(IntersectionKind.Uncertain);
        return new(IntersectionKind.Point, Point: x, T: t, U: u);
    }

    /// <summary>閉線分P〜P+r、Q〜Q+sの共有部分を求める。</summary>
    /// <param name="p">第1始点。</param><param name="r">第1終点までの変位。</param>
    /// <param name="q">第2始点。</param><param name="s">第2終点までの変位。</param>
    /// <returns>一点、なし、両端付き重複区間、退化、無効入力、判定保留。</returns>
    /// <remarks>零長は点-線分処理へ進めずDegenerate。境界付近の範囲外を端点に丸めない。</remarks>
    public static Intersection2 SegmentSegment2(V2 p, V2 r, V2 q, V2 s)
    {
        var lines = LineLine2(p, r, q, s);
        if (lines.Kind == IntersectionKind.Point)
        {
            double t = lines.T!.Value, u = lines.U!.Value;
            if (t < -ParameterTolerance || t > 1 + ParameterTolerance ||
                u < -ParameterTolerance || u > 1 + ParameterTolerance) return new(IntersectionKind.None);
            if (!InClosedUnitInterval(t) || !InClosedUnitInterval(u)) return new(IntersectionKind.Uncertain);
            return lines;
        }
        if (lines.Kind != IntersectionKind.Coincident) return lines;

        // 同一直線では第2線分の両端を第1線分のt座標へ射影し、区間どうしを比べる。
        var end = Add2(q, s);
        double t0 = Dot2(Sub2(q, p), r) / Dot2(r, r);
        double t1 = Dot2(Sub2(end, p), r) / Dot2(r, r);
        if (!Finite2(end) || !double.IsFinite(t0) || !double.IsFinite(t1)) return new(IntersectionKind.Uncertain);
        double low = Math.Max(0, Math.Min(t0, t1)), high = Math.Min(1, Math.Max(t0, t1));
        if (low > high)
            return new(low - high <= ParameterTolerance ? IntersectionKind.Uncertain : IntersectionKind.None);
        var start = Add2(p, Scale2(r, low));
        var finish = Add2(p, Scale2(r, high));
        if (!Finite2(start) || !Finite2(finish)) return new(IntersectionKind.Uncertain);
        if (low == high)
        {
            double u = Dot2(Sub2(start, q), s) / Dot2(s, s);
            if (!double.IsFinite(u)) return new(IntersectionKind.Uncertain);
            return new(IntersectionKind.Point, Point: start, T: low, U: u);
        }
        return new(IntersectionKind.OverlapSegment, OverlapStart: start, OverlapEnd: finish);
    }

    /// <summary>三角形ABCから、正規化していない法線(B-A)×(C-A)を求める。</summary>
    /// <param name="a">基準頂点。</param><param name="b">第2頂点。</param><param name="c">第3頂点。</param>
    /// <param name="normal">成功時の法線。失敗時の既定値を平面に使わない。</param>
    /// <returns>有限で、辺長・2倍面積・角度が教材の下限を超えればtrue。</returns>
    /// <remarks>面積の単位は長さの2乗。既存法線の任意のスケーリングの可否とは別の検査。</remarks>
    public static bool TryTriangleNormal(V3 a, V3 b, V3 c, out V3 normal)
    {
        normal = default;
        if (!Finite3(a) || !Finite3(b) || !Finite3(c)) return false;
        var ab = Sub3(b, a); var ac = Sub3(c, a); var cross = Cross3(ab, ac);
        double nb = Length3(ab), nc = Length3(ac), area2 = Length3(cross), scale = nb * nc;
        if (!Finite3(cross) || !double.IsFinite(scale) || !double.IsFinite(area2) ||
            nb <= MinimumDirectionLength || nc <= MinimumDirectionLength ||
            area2 <= MinimumTriangleArea2 || area2 / scale <= AngularTolerance) return false;
        normal = cross;
        return true;
    }

    /// <summary>3D無限直線P+tdと、点A・非ゼロ法線nの無限平面の共有部分を求める。</summary>
    /// <param name="p">直線上の点。</param><param name="d">方向。長さは座標と同じ単位。</param>
    /// <param name="a">平面上の点。</param><param name="n">平面法線。単位長でなくてもよい。</param>
    /// <returns>一点、なし、直線全体（Coincident）、退化、無効入力、判定保留。</returns>
    /// <remarks>有限線分の場合は呼び出し側でtの範囲も確認する。ゼロ方向はDegenerate。</remarks>
    public static Intersection3 LinePlane3(V3 p, V3 d, V3 a, V3 n)
    {
        if (!Finite3(p) || !Finite3(d) || !Finite3(a) || !Finite3(n)) return new(IntersectionKind.InvalidInput);
        if (n == (0, 0, 0)) return new(IntersectionKind.Degenerate);
        double nd = Length3(d), nn = Length3(n);
        if (!double.IsFinite(nd) || !double.IsFinite(nn) || nn == 0) return new(IntersectionKind.Uncertain);
        if (nd <= MinimumDirectionLength) return new(IntersectionKind.Degenerate);
        double denominator = Dot3(n, d), numerator = Dot3(n, Sub3(a, p)), scale = nn * nd;
        if (!double.IsFinite(denominator) || !double.IsFinite(numerator) || !double.IsFinite(scale))
            return new(IntersectionKind.Uncertain);
        if (denominator == 0)
        {
            if (numerator == 0) return new(IntersectionKind.Coincident);
            return new(Math.Abs(numerator) / nn <= DistanceTolerance
                ? IntersectionKind.Uncertain : IntersectionKind.None);
        }
        if (Math.Abs(denominator) / scale <= AngularTolerance) return new(IntersectionKind.Uncertain);
        double t = numerator / denominator;
        var x = Add3(p, Scale3(d, t));
        double residual = Dot3(n, Sub3(x, a)) / nn;
        if (!double.IsFinite(t) || !Finite3(x) || !(Math.Abs(residual) <= DistanceTolerance))
            return new(IntersectionKind.Uncertain);
        return new(IntersectionKind.Point, Point: x, T: t);
    }

    /// <summary>任意の非ゼロ法線で表した2無限平面の共有部分を求める。</summary>
    /// <param name="a1">第1平面上の点。</param><param name="n1">第1法線。</param>
    /// <param name="a2">第2平面上の点。</param><param name="n2">第2法線。</param>
    /// <returns>交線（通過点と方向）、なし、平面一致、退化、無効入力、判定保留。</returns>
    /// <remarks>法線を単位化し、外積の最大絶対成分に対応する座標を0として残る2元連立式を解く。</remarks>
    public static Intersection3 PlanePlane3(V3 a1, V3 n1, V3 a2, V3 n2)
    {
        if (!Finite3(a1) || !Finite3(n1) || !Finite3(a2) || !Finite3(n2))
            return new(IntersectionKind.InvalidInput);
        if (n1 == (0, 0, 0) || n2 == (0, 0, 0)) return new(IntersectionKind.Degenerate);
        double nn1 = Length3(n1), nn2 = Length3(n2);
        if (!double.IsFinite(nn1) || !double.IsFinite(nn2) || nn1 == 0 || nn2 == 0)
            return new(IntersectionKind.Uncertain);
        var a = Scale3(n1, 1 / nn1); var b = Scale3(n2, 1 / nn2);
        var v = Cross3(a, b);
        double sine = Length3(v);
        if (v == (0, 0, 0))
        {
            double distance = Dot3(a, Sub3(a2, a1));
            if (!double.IsFinite(distance)) return new(IntersectionKind.Uncertain);
            if (distance == 0) return new(IntersectionKind.Coincident);
            return new(Math.Abs(distance) <= DistanceTolerance ? IntersectionKind.Uncertain : IntersectionKind.None);
        }
        if (!(sine > AngularTolerance)) return new(IntersectionKind.Uncertain);

        // vの最大成分なら、残る2元連立式の行列式も比較的大きく、0ではない。
        double[] left = [a.X, a.Y, a.Z], right = [b.X, b.Y, b.Z];
        double[] direction = [v.X, v.Y, v.Z], coordinates = new double[3];
        int free = 0;
        for (int k = 1; k < 3; k++)
            if (Math.Abs(direction[k]) > Math.Abs(direction[free])) free = k;
        int i = (free + 1) % 3, j = (free + 2) % 3;
        double c1 = Dot3(a, a1), c2 = Dot3(b, a2);
        double determinant = left[i] * right[j] - left[j] * right[i];
        coordinates[i] = (c1 * right[j] - left[j] * c2) / determinant;
        coordinates[j] = (left[i] * c2 - c1 * right[i]) / determinant;
        V3 x = (coordinates[0], coordinates[1], coordinates[2]);
        if (!Finite3(x) || !(Math.Abs(Dot3(a, Sub3(x, a1))) <= DistanceTolerance) ||
            !(Math.Abs(Dot3(b, Sub3(x, a2))) <= DistanceTolerance)) return new(IntersectionKind.Uncertain);
        return new(IntersectionKind.Line, Point: x, Direction: v);
    }
}

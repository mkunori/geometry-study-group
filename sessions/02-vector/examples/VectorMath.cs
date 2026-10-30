/// <summary>内積・外積・射影の成分式を、doubleのタプルで直接計算する教材用の手実装。</summary>
/// <remarks>
/// 同じ直交座標系の標準基底を使う。2DはX右・Y上、3Dは右手系。
/// 小さな固定入力の学習用で、任意スケールに対応する汎用の数値計算ライブラリではない。
/// Dot・Cross・Lengthは成分式そのもので、非有限値や桁あふれを補正しない。
/// Tryメソッドは計算結果の有限性と下限を検査し、未定義・扱えない入力をfalseで返す。
/// </remarks>
internal static class VectorMath
{
    // この教材の共通単位に対する下限。長さと外積の長さ（面積の次元）は別の尺度として持つ。
    private const double MinimumLength = 1e-12;
    private const double MinimumCrossLength = 1e-12;

    /// <summary>2D内積を、対応する成分の積の和として求める。</summary>
    /// <param name="a">第1のベクトル (x, y)。</param>
    /// <param name="b">同じ座標系の第2のベクトル (x, y)。</param>
    /// <returns>スカラー a.X*b.X + a.Y*b.Y。</returns>
    public static double Dot2((double X, double Y) a, (double X, double Y) b) =>
        a.X * b.X + a.Y * b.Y;

    /// <summary>2Dベクトルの長さを、自分自身との内積の平方根から求める。</summary>
    /// <param name="a">長さを求めるベクトル。</param>
    /// <returns>sqrt(x²+y²)。単位は入力の長さと同じ。</returns>
    public static double Length2((double X, double Y) a) => Math.Sqrt(Dot2(a, a));

    /// <summary>非ゼロの2Dベクトルのなす角を、内積とacosから求める。</summary>
    /// <param name="a">第1のベクトル。</param>
    /// <param name="b">第2のベクトル。</param>
    /// <param name="radians">成功時は0からπまでの角度（ラジアン）。失敗時は既定値であり角度として使わない。</param>
    /// <returns>長さ・積・比が有限で、両方の長さが1e-12を超える場合はtrue。それ以外はfalse。</returns>
    /// <remarks>
    /// 除算前に長さを検査する。有限なcos値を[-1,1]へクランプし、丸めによるacosの定義域外を避ける。
    /// クランプでゼロ長さやNaNを隠さない。非常に大きい・小さい入力は別途スケーリングが必要。
    /// </remarks>
    public static bool TryAngle2((double X, double Y) a, (double X, double Y) b, out double radians)
    {
        radians = default;
        double lengthA = Length2(a);
        double lengthB = Length2(b);
        if (!double.IsFinite(lengthA) || !double.IsFinite(lengthB) ||
            lengthA <= MinimumLength || lengthB <= MinimumLength)
            return false;

        double denominator = lengthA * lengthB;
        double numerator = Dot2(a, b);
        if (!double.IsFinite(denominator) || !double.IsFinite(numerator))
            return false;
        double cosine = numerator / denominator;
        if (!double.IsFinite(cosine))
            return false;

        radians = Math.Acos(Math.Clamp(cosine, -1.0, 1.0));
        return true;
    }

    /// <summary>非ゼロのbが張る直線への、aの正射影を求める。</summary>
    /// <param name="a">射影するベクトル。ゼロでもよい。</param>
    /// <param name="b">射影先の方向。単位ベクトルでなくてもよい。</param>
    /// <param name="projection">成功時の射影ベクトル。失敗時の既定値は射影の答えとして使わない。</param>
    /// <returns>射影先の長さが1e-12を超え、式の各計算結果が有限ならtrue。それ以外はfalse。</returns>
    /// <remarks>係数は(a・b)/(b・b)。分母の長さの2乗がbの大きさを補正する。射影先は線分ではない。</remarks>
    public static bool TryProject2(
        (double X, double Y) a, (double X, double Y) b, out (double X, double Y) projection)
    {
        projection = default;
        double denominator = Dot2(b, b);
        double numerator = Dot2(a, b);
        if (!double.IsFinite(denominator) || !double.IsFinite(numerator) ||
            denominator <= MinimumLength * MinimumLength)
            return false;

        double factor = numerator / denominator;
        var result = (X: factor * b.X, Y: factor * b.Y);
        if (!double.IsFinite(result.X) || !double.IsFinite(result.Y))
            return false;
        projection = result;
        return true;
    }

    /// <summary>2Dスカラー外積を、3Dへ埋め込んだ外積のZ成分として求める。</summary>
    /// <param name="u">基準になる第1のベクトル。</param>
    /// <param name="v">第2のベクトル。</param>
    /// <returns>スカラー u.X*v.Y - u.Y*v.X。2Dベクトルは返さない。</returns>
    /// <remarks>X右・Y上では正が反時計回り、負が時計回り。絶対値は平行四辺形の面積。</remarks>
    public static double Cross2((double X, double Y) u, (double X, double Y) v) =>
        u.X * v.Y - u.Y * v.X;

    /// <summary>3D内積を、対応する3成分の積の和として求める。</summary>
    /// <param name="a">第1のベクトル。</param>
    /// <param name="b">第2のベクトル。</param>
    /// <returns>スカラー a.X*b.X + a.Y*b.Y + a.Z*b.Z。</returns>
    public static double Dot3((double X, double Y, double Z) a, (double X, double Y, double Z) b) =>
        a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>3Dベクトルの長さを、自分自身との内積の平方根から求める。</summary>
    /// <param name="a">長さを求めるベクトル。</param>
    /// <returns>sqrt(x²+y²+z²)。単位は入力の長さと同じ。</returns>
    public static double Length3((double X, double Y, double Z) a) => Math.Sqrt(Dot3(a, a));

    /// <summary>右手系の3D外積を、基底の外積から得られる3成分の式で求める。</summary>
    /// <param name="a">第1のベクトル。</param>
    /// <param name="b">第2のベクトル。aからbへ右手の指を曲げる順序で使う。</param>
    /// <returns>両方と直交するベクトル。長さは平行四辺形の面積。平行・ゼロ入力ではゼロになる。</returns>
    /// <remarks>順序を逆にすると符号が反転する。ゼロの結果には方向がなく、正規化できない。</remarks>
    public static (double X, double Y, double Z) Cross3(
        (double X, double Y, double Z) a, (double X, double Y, double Z) b) =>
        (a.Y * b.Z - a.Z * b.Y,
         a.Z * b.X - a.X * b.Z,
         a.X * b.Y - a.Y * b.X);

    /// <summary>3D三角形の頂点順序A→B→Cに対応する単位法線を求める。</summary>
    /// <param name="a">基準の頂点A。</param>
    /// <param name="b">頂点B。</param>
    /// <param name="c">頂点C。</param>
    /// <param name="normal">成功時の単位法線。失敗時の既定値は法線として使わない。</param>
    /// <returns>外積の長さが有限で1e-12を超える場合はtrue。それ以外はfalse。</returns>
    /// <remarks>
    /// (B-A)×(C-A)を、その長さで割る。一直線・重複点・下限以下の小さな面は正規化しない。
    /// 外積の長さの単位は面積。しきい値はこの教材のスケール用で、厳密な退化判定ではない。
    /// 頂点からの法線算出であり、既存法線の一般の変換（逆転置など）は扱わない。
    /// </remarks>
    public static bool TryTriangleNormal3(
        (double X, double Y, double Z) a, (double X, double Y, double Z) b,
        (double X, double Y, double Z) c, out (double X, double Y, double Z) normal)
    {
        normal = default;
        var ab = (X: b.X - a.X, Y: b.Y - a.Y, Z: b.Z - a.Z);
        var ac = (X: c.X - a.X, Y: c.Y - a.Y, Z: c.Z - a.Z);
        var cross = Cross3(ab, ac);
        double length = Length3(cross);
        if (!double.IsFinite(length) || length <= MinimumCrossLength)
            return false;

        normal = (cross.X / length, cross.Y / length, cross.Z / length);
        return true;
    }
}

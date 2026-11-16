namespace AutoCadGeometrySamples;

/// <summary>AutoCAD型に依存せず、任意軸アルゴリズムの成分計算を追うための数学部分。</summary>
/// <remarks>有限な非ゼロ法線を受け取り、WCS成分で右手系正規直交基底を返す。描画やDB操作は行わない。</remarks>
internal static class ArbitraryAxisMath
{
    /// <summary>Autodeskの分岐定数。誤差判定のepsilonではなく、単位法線に対する軸選択規則。</summary>
    internal const double AxisBoundary = 1.0 / 64.0;

    /// <summary>点の位置と区別して使う、3成分の方向・変位。</summary>
    internal readonly record struct Vec(double X, double Y, double Z);

    /// <summary>WCS成分で表したOCSの単位軸と、補助軸の選択結果。</summary>
    internal readonly record struct Frame(Vec XAxis, Vec YAxis, Vec Normal, bool UsesWorldY);

    /// <summary>有限な非ゼロ方向を単位化する。尺度を先に除き、二乗和の桁あふれ・アンダーフローを避ける。</summary>
    /// <param name="v">WCS成分。長さは任意だが、方向が必要なのでゼロと非有限値は認めない。</param>
    /// <returns>入力と同じ向きの単位ベクトル。極端な成分比の丸めまで解消するものではない。</returns>
    /// <exception cref="System.ArgumentException">ゼロ、NaN、無限大の成分を含む場合。</exception>
    internal static Vec Unit(Vec v)
    {
        if (!double.IsFinite(v.X) || !double.IsFinite(v.Y) || !double.IsFinite(v.Z))
            throw new System.ArgumentException("法線・外積の成分は有限でなければならない。");
        double scale = Math.Max(Math.Abs(v.X), Math.Max(Math.Abs(v.Y), Math.Abs(v.Z)));
        if (scale == 0) throw new System.ArgumentException("ゼロ方向は単位化できない。");
        double x = v.X / scale, y = v.Y / scale, z = v.Z / scale;
        double length = Math.Sqrt(x * x + y * y + z * z);
        return new Vec(x / length, y / length, z / length);
    }

    /// <summary>単位化した法線で厳密な小なりを比較し、補助軸との外積からOCS基底を作る。</summary>
    /// <param name="normal">WCSで表した有限な非ゼロ法線。大きさをOCSの尺度に使わない。</param>
    /// <returns>X×Y=Nとなる右手系。分岐境界での軸の連続性は保証しない。</returns>
    internal static Frame Build(Vec normal)
    {
        Vec n = Unit(normal);
        bool usesWorldY = Math.Abs(n.X) < AxisBoundary && Math.Abs(n.Y) < AxisBoundary;
        Vec helper = usesWorldY ? new Vec(0, 1, 0) : new Vec(0, 0, 1);
        // helper×Nの順序でXを作る。逆にするとXもYも反転し、同じOCS規約ではなくなる。
        Vec x = Unit(Cross(helper, n));
        Vec y = Unit(Cross(n, x));
        return new Frame(x, y, n, usesWorldY);
    }

    /// <summary>右手系の外積。2方向に垂直で、長さは張る平行四辺形の面積。</summary>
    internal static Vec Cross(Vec a, Vec b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    /// <summary>内積。単位基底なら方向の直交性や座標への射影を確認できる。</summary>
    internal static double Dot(Vec a, Vec b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>方向の成分差。基底や同じ原点からの位置の残差を調べる。</summary>
    internal static Vec Difference(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>小さな固定値・残差の長さ。任意の巨大入力を測る汎用ノルムではない。</summary>
    internal static double Length(Vec v) => Math.Sqrt(Dot(v, v));

    /// <summary>OCSの(u,v,h)を、WCS原点からの位置ベクトルuX+vY+hNへ写す。</summary>
    /// <param name="frame">任意軸アルゴリズムで得た単位基底。</param>
    /// <param name="u">OCSのX座標。</param>
    /// <param name="v">OCSのY座標。</param>
    /// <param name="height">OCSのZ座標。PolylineではElevation。</param>
    internal static Vec Position(Frame frame, double u, double v, double height) => new(
        u * frame.XAxis.X + v * frame.YAxis.X + height * frame.Normal.X,
        u * frame.XAxis.Y + v * frame.YAxis.Y + height * frame.Normal.Y,
        u * frame.XAxis.Z + v * frame.YAxis.Z + height * frame.Normal.Z);
}

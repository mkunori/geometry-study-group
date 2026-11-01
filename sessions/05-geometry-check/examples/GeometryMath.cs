global using P2 = (double X, double Y);
global using P3 = (double X, double Y, double Z);

/// <summary>点と領域の関係。無効・非対応入力や非有限な演算はInvalidに分ける。</summary>
internal enum PointLocation { Invalid, Inside, Boundary, Outside }

/// <summary>正の面積・体積を持つ領域の関係。Touchingは非空だが共有面積・体積0。</summary>
internal enum RegionRelation { Invalid, Separated, Touching, Overlapping }

/// <summary>境界の先行検査を含むレイ法の結果。</summary>
/// <param name="Location">点の分類。</param>
/// <param name="RightCrossings">内部・外部の計数。境界・無効なら未計数のnull。</param>
internal readonly record struct RayResult(PointLocation Location, int? RightCrossings);

/// <summary>1本の軸での閉じた射影区間。</summary>
/// <param name="Min">内積の最小値。</param><param name="Max">内積の最大値。</param>
internal readonly record struct Interval(double Min, double Max);

/// <summary>第5回のdouble成分式。座標は2DでX右・Y上、3Dで右手系。</summary>
internal static class GeometryMath
{
    /// <summary>2点の差から変位を得る。</summary>
    /// <param name="a">到達点。</param><param name="b">基準点。</param><returns>bからaへの変位。</returns>
    internal static P2 Sub(P2 a, P2 b) => (a.X - b.X, a.Y - b.Y);

    /// <summary>2D外積を成分で求める。正は第1ベクトルから左側。</summary>
    /// <param name="a">辺などの方向。</param><param name="b">同じ単位の変位。</param>
    /// <returns>符号付き平行四辺形面積。単位は長さの2乗。</returns>
    /// <remarks>生の成分式なので、非有限な結果や丸めの扱いは呼び出し側で検査する。</remarks>
    internal static double Cross2(P2 a, P2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>対応成分の積を足し、辺方向の位置や射影を求める。</summary>
    /// <param name="a">第1ベクトル。</param><param name="b">第2ベクトル。</param><returns>内積。</returns>
    internal static double Dot(P2 a, P2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>2成分の有限性を調べる。</summary>
    /// <param name="p">点またはベクトル。</param><returns>NaN・無限大がなければtrue。</returns>
    internal static bool Finite(P2 p) => double.IsFinite(p.X) && double.IsFinite(p.Y);

    /// <summary>3成分の有限性を調べる。</summary>
    /// <param name="p">点またはベクトル。</param><returns>NaN・無限大がなければtrue。</returns>
    internal static bool Finite(P3 p) => double.IsFinite(p.X) && double.IsFinite(p.Y) && double.IsFinite(p.Z);

    /// <summary>非ゼロの有限な辺上に点があるか、外積0と内積の両端込み範囲で調べる。</summary>
    /// <param name="a">線分の始点。</param><param name="b">線分の終点。</param><param name="p">検査点。</param>
    /// <param name="onSegment">検査成功時、有限な線分上ならtrue。</param>
    /// <returns>演算が有効ならtrue。無効・非有限演算・長さの2乗0ならfalseで、包含のfalseとは別。</returns>
    /// <remarks>許容差による境界帯は作らない。浮動小数点の厳密述語ではない。</remarks>
    internal static bool TryPointOnSegment(P2 a, P2 b, P2 p, out bool onSegment)
    {
        onSegment = false;
        if (!Finite(a) || !Finite(b) || !Finite(p)) return false;
        P2 e = Sub(b, a), w = Sub(p, a);
        double length2 = Dot(e, e), cross = Cross2(e, w), projection = Dot(w, e);
        if (!Finite(e) || !Finite(w) || !double.IsFinite(length2) || length2 <= 0 ||
            !double.IsFinite(cross) || !double.IsFinite(projection)) return false;
        onSegment = cross == 0 && 0 <= projection && projection <= length2;
        return true;
    }
}

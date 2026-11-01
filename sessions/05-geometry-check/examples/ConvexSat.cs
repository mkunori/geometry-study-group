using static GeometryMath;

/// <summary>非退化で厳密に凸な2D閉多角形どうしの分離軸判定。</summary>
internal static class ConvexSat
{
    /// <summary>同じ非ゼロ軸と全頂点の内積から、最小・最大を得る。</summary>
    /// <param name="vertices">有限な点の集合。空は不可。</param><param name="axis">正規化不要の有限な非ゼロ軸。</param>
    /// <param name="interval">成功時の閉射影区間。距離の尺度には軸長で割る。</param>
    /// <returns>演算が有効ならtrue。falseではintervalを使用しない。</returns>
    internal static bool TryProject(IReadOnlyList<P2>? vertices, P2 axis, out Interval interval)
    {
        interval = default;
        if (vertices is null || vertices.Count == 0 || !Finite(axis) || axis == (0, 0)) return false;
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        foreach (P2 p in vertices)
        {
            double q = Dot(axis, p);
            if (!Finite(p) || !double.IsFinite(q)) return false;
            min = Math.Min(min, q);
            max = Math.Max(max, q);
        }
        interval = new(min, max);
        return true;
    }

    /// <summary>両輪郭の全辺法線を候補にし、離隔・接触・正の面積の重なりを分ける。</summary>
    /// <param name="a">第1の厳密に凸な2D多角形。</param><param name="aClosed">第1輪郭を閉じる意図。</param>
    /// <param name="b">第2の厳密に凸な2D多角形。</param><param name="bClosed">第2輪郭を閉じる意図。</param>
    /// <returns>幾何的な3状態、または無効・非対応・非有限演算のInvalid。</returns>
    /// <remarks>非凸・冗長な共線点・自己交差・穴・一般3Dメッシュには適用しない。</remarks>
    internal static RegionRelation Compare(IReadOnlyList<P2>? a, bool aClosed, IReadOnlyList<P2>? b, bool bClosed)
    {
        if (!PolygonChecks.IsStrictlyConvex(a, aClosed) || !PolygonChecks.IsStrictlyConvex(b, bClosed))
            return RegionRelation.Invalid;
        bool touching = false;
        foreach (var polygon in new[] { a!, b! })
            for (int i = 0; i < polygon.Count; i++)
            {
                P2 edge = Sub(polygon[(i + 1) % polygon.Count], polygon[i]);
                P2 axis = (-edge.Y, edge.X);
                if (!TryProject(a, axis, out Interval first) || !TryProject(b, axis, out Interval second))
                    return RegionRelation.Invalid;
                double delta = Math.Min(first.Max, second.Max) - Math.Max(first.Min, second.Min);
                if (!double.IsFinite(delta)) return RegionRelation.Invalid;
                if (delta < 0) return RegionRelation.Separated;
                touching |= delta == 0;
            }
        // 1軸の一致だけでは接触と決まらない。全候補に分離がないことを先に確認する。
        return touching ? RegionRelation.Touching : RegionRelation.Overlapping;
    }
}

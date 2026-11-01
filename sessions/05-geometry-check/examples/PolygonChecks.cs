using static GeometryMath;

/// <summary>穴のない単純閉輪郭の検査と、三角形・凹多角形の点包含。</summary>
internal static class PolygonChecks
{
    /// <summary>有限点・重複・折返し・自己交差・非ゼロ面積を検査する。</summary>
    /// <param name="vertices">3点以上。先頭を末尾に重複保存しない。</param>
    /// <param name="closed">trueなら末尾から先頭へも辺を置く。falseは非対応。</param>
    /// <param name="area2">成功時は符号付き面積の2倍。CCWで正。</param>
    /// <returns>この教材の単純閉輪郭として有効ならtrue。</returns>
    /// <remarks>二重ループの教材用検査。穴や複数輪郭を表す形式ではなく、丸めに頑健な厳密述語ではない。</remarks>
    internal static bool TryValidate(IReadOnlyList<P2>? vertices, bool closed, out double area2)
    {
        area2 = 0;
        if (!closed || vertices is null || vertices.Count < 3 || vertices.Any(p => !Finite(p))) return false;
        int count = vertices.Count;
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++) if (vertices[i] == vertices[j]) return false;
            P2 a = vertices[i], b = vertices[(i + 1) % count], c = vertices[(i + 2) % count];
            if (!TryPointOnSegment(a, b, a, out _)) return false;
            P2 e = Sub(b, a), next = Sub(c, b);
            double turn = Cross2(e, next), along = Dot(e, next);
            if (!Finite(next) || !double.IsFinite(turn) || !double.IsFinite(along)) return false;
            // 隣接辺を交差検査から除く前に、重なって折り返す辺を拒否する。
            if (turn == 0 && along <= 0) return false;
            double term = Cross2(Sub(a, vertices[0]), Sub(b, vertices[0]));
            area2 += term;
            if (!double.IsFinite(term) || !double.IsFinite(area2)) return false;
        }
        if (area2 == 0) return false;
        for (int i = 0; i < count; i++)
            for (int j = i + 1; j < count; j++)
            {
                if (j == i + 1 || (i == 0 && j == count - 1)) continue;
                if (!TrySegmentsIntersect(vertices[i], vertices[(i + 1) % count],
                    vertices[j], vertices[(j + 1) % count], out bool intersects) || intersects) return false;
            }
        return true;
    }

    /// <summary>非隣接2辺の交差を、4つの外積符号と共線時の有限範囲から検査する。</summary>
    /// <param name="a">第1線分の始点。</param><param name="b">第1線分の終点。</param>
    /// <param name="c">第2線分の始点。</param><param name="d">第2線分の終点。</param>
    /// <param name="intersects">検査成功時、端点接触・共有線分を含む交差があればtrue。</param>
    /// <returns>全ての辺上検査・外積演算が有限で有効ならtrue。</returns>
    private static bool TrySegmentsIntersect(P2 a, P2 b, P2 c, P2 d, out bool intersects)
    {
        intersects = false;
        if (!TryPointOnSegment(a, b, c, out bool cOn) || !TryPointOnSegment(a, b, d, out bool dOn) ||
            !TryPointOnSegment(c, d, a, out bool aOn) || !TryPointOnSegment(c, d, b, out bool bOn)) return false;
        double ac = Cross2(Sub(b, a), Sub(c, a)), ad = Cross2(Sub(b, a), Sub(d, a));
        double ca = Cross2(Sub(d, c), Sub(a, c)), cb = Cross2(Sub(d, c), Sub(b, c));
        // 符号の積を取ると大きな値であふれるため、符号自体を比較する。
        intersects = cOn || dOn || aOn || bOn ||
            (Math.Sign(ac) != Math.Sign(ad) && ac != 0 && ad != 0 &&
             Math.Sign(ca) != Math.Sign(cb) && ca != 0 && cb != 0);
        return true;
    }

    /// <summary>非退化三角形の全辺の内側条件を重ねる。CW・CCWの両方に対応する。</summary>
    /// <param name="vertices">3頂点の単純閉輪郭。</param><param name="closed">末尾から先頭へ辺を置く意図。</param>
    /// <param name="p">有限な検査点。</param><returns>内部・境界・外部、または無効。</returns>
    internal static PointLocation InTriangle(IReadOnlyList<P2>? vertices, bool closed, P2 p)
    {
        if (vertices is null || vertices.Count != 3 || !Finite(p) || !TryValidate(vertices, closed, out double area2))
            return PointLocation.Invalid;
        bool boundary = false, outside = false;
        for (int i = 0; i < 3; i++)
        {
            double value = Cross2(Sub(vertices[(i + 1) % 3], vertices[i]), Sub(p, vertices[i]));
            if (!double.IsFinite(value)) return PointLocation.Invalid;
            // CWでは内部が辺の右側になる。向きの符号でCCWと同じ比較へそろえる。
            if (area2 < 0) value = -value;
            outside |= value < 0;
            boundary |= value == 0;
        }
        // 外部を先に確定することで、辺の延長線上を境界と誤らない。
        return outside ? PointLocation.Outside : boundary ? PointLocation.Boundary : PointLocation.Inside;
    }

    /// <summary>辺上を先に確定し、その後に+Xレイの偶奇を用いる。</summary>
    /// <param name="vertices">穴・自己交差のない単純閉輪郭。凹形状も可。</param>
    /// <param name="closed">trueなら最後から最初へ接続。falseはInvalid。</param><param name="p">検査点。</param>
    /// <returns>分類と右方向の計数。Boundary・Invalidでは回数はnull。</returns>
    internal static RayResult ByRay(IReadOnlyList<P2>? vertices, bool closed, P2 p)
    {
        if (!Finite(p) || !TryValidate(vertices, closed, out _)) return new(PointLocation.Invalid, null);
        var polygon = vertices!;
        for (int i = 0; i < polygon.Count; i++)
        {
            if (!TryPointOnSegment(polygon[i], polygon[(i + 1) % polygon.Count], p, out bool on))
                return new(PointLocation.Invalid, null);
            if (on) return new(PointLocation.Boundary, null);
        }
        int crossings = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            P2 a = polygon[i], b = polygon[(i + 1) % polygon.Count];
            // 下端を含み上端を含めない。水平辺はこの条件を満たさず、除算にも進まない。
            if (!(Math.Min(a.Y, b.Y) <= p.Y && p.Y < Math.Max(a.Y, b.Y))) continue;
            double t = (p.Y - a.Y) / (b.Y - a.Y);
            double xHit = a.X + t * (b.X - a.X);
            if (!double.IsFinite(t) || !double.IsFinite(xHit)) return new(PointLocation.Invalid, null);
            if (xHit > p.X) crossings++;
        }
        return new(crossings % 2 == 1 ? PointLocation.Inside : PointLocation.Outside, crossings);
    }

    /// <summary>単純閉輪郭で、全ての曲がりが同じ向きかつ0でないことを検査する。</summary>
    /// <param name="vertices">凸SATへ渡す輪郭。</param><param name="closed">閉じる意図。</param>
    /// <returns>非退化で厳密に凸ならtrue。CW・CCWとも可。</returns>
    /// <remarks>冗長な共線頂点もこの教材のSATでは非対応とする。凹L字はfalse。</remarks>
    internal static bool IsStrictlyConvex(IReadOnlyList<P2>? vertices, bool closed)
    {
        if (!TryValidate(vertices, closed, out double area2)) return false;
        var polygon = vertices!;
        for (int i = 0; i < polygon.Count; i++)
        {
            P2 a = polygon[i], b = polygon[(i + 1) % polygon.Count], c = polygon[(i + 2) % polygon.Count];
            double turn = Cross2(Sub(b, a), Sub(c, b));
            if (!double.IsFinite(turn) || turn == 0 || Math.Sign(turn) != Math.Sign(area2)) return false;
        }
        return true;
    }
}

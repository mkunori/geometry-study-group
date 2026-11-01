using static GeometryMath;

/// <summary>2Dの軸平行閉長方形。Min・Maxは対応成分の下端・上端。</summary>
/// <param name="Min">X・Yの下端。</param><param name="Max">X・Yの上端。</param>
internal readonly record struct Box2(P2 Min, P2 Max);

/// <summary>3Dの軸平行閉箱。中身と境界を含むソリッド。</summary>
/// <param name="Min">X・Y・Zの下端。</param><param name="Max">X・Y・Zの上端。</param>
internal readonly record struct Box3(P3 Min, P3 Max);

/// <summary>箱どうしの関係と、各軸の候補幅・共有面積または体積。</summary>
/// <param name="Relation">幾何的な関係またはInvalid。</param>
/// <param name="AxisDeltas">上端のmin−下端のmax。負は離隔であり、実在する負の長さではない。Invalidなら空。</param>
/// <param name="SharedMeasure">共有面積／体積。離隔・接触は0、Invalidはnull。</param>
internal readonly record struct AabbResult(RegionRelation Relation, double[] AxisDeltas, double? SharedMeasure);

/// <summary>正の幅を持つ2D／3D AABBの共通区間を調べる。</summary>
internal static class AabbChecks
{
    /// <summary>有限で正の幅を持つ2D長方形かを調べる。</summary>
    /// <param name="box">検査する箱。</param><returns>全軸の幅が有限かつ正ならtrue。</returns>
    internal static bool IsValid(Box2 box) => Finite(box.Min) && Finite(box.Max) &&
        PositiveWidth(box.Min.X, box.Max.X) && PositiveWidth(box.Min.Y, box.Max.Y);

    /// <summary>有限で正の幅を持つ3D箱かを調べる。</summary>
    /// <param name="box">検査する箱。</param><returns>全軸の幅が有限かつ正ならtrue。</returns>
    internal static bool IsValid(Box3 box) => Finite(box.Min) && Finite(box.Max) &&
        PositiveWidth(box.Min.X, box.Max.X) && PositiveWidth(box.Min.Y, box.Max.Y) && PositiveWidth(box.Min.Z, box.Max.Z);

    /// <summary>区間の向き・幅の非退化・有限性をまとめて調べる。</summary>
    /// <param name="min">下端。</param><param name="max">上端。</param><returns>差が有限かつ正ならtrue。</returns>
    private static bool PositiveWidth(double min, double max) => double.IsFinite(max - min) && max - min > 0;

    /// <summary>2D箱どうしの共通区間と共有面積を求める。</summary>
    /// <param name="a">第1箱。</param><param name="b">第2箱。</param><returns>分類、X・Y候補幅、面積。</returns>
    internal static AabbResult Compare(Box2 a, Box2 b) => !IsValid(a) || !IsValid(b) ? Invalid() : Classify([
        Math.Min(a.Max.X, b.Max.X) - Math.Max(a.Min.X, b.Min.X),
        Math.Min(a.Max.Y, b.Max.Y) - Math.Max(a.Min.Y, b.Min.Y)]);

    /// <summary>3D箱どうしの共通区間と共有体積を求める。</summary>
    /// <param name="a">第1箱。</param><param name="b">第2箱。</param><returns>分類、X・Y・Z候補幅、体積。</returns>
    internal static AabbResult Compare(Box3 a, Box3 b) => !IsValid(a) || !IsValid(b) ? Invalid() : Classify([
        Math.Min(a.Max.X, b.Max.X) - Math.Max(a.Min.X, b.Min.X),
        Math.Min(a.Max.Y, b.Max.Y) - Math.Max(a.Min.Y, b.Min.Y),
        Math.Min(a.Max.Z, b.Max.Z) - Math.Max(a.Min.Z, b.Min.Z)]);

    /// <summary>1軸の負は離隔、0を含む非負は接触、全て正なら重なりとする。</summary>
    /// <param name="deltas">共通区間の候補幅。</param><returns>分類と共有量。積の非有限・正幅の積の0もInvalid。</returns>
    private static AabbResult Classify(double[] deltas)
    {
        if (deltas.Any(d => !double.IsFinite(d))) return Invalid();
        if (deltas.Any(d => d < 0)) return new(RegionRelation.Separated, deltas, 0);
        if (deltas.Any(d => d == 0)) return new(RegionRelation.Touching, deltas, 0);
        double measure = 1;
        foreach (double delta in deltas) measure *= delta;
        if (!double.IsFinite(measure) || measure <= 0) return Invalid();
        return new(RegionRelation.Overlapping, deltas, measure);
    }

    /// <summary>無効時に偽の幅や面積を使わないための結果を作る。</summary>
    /// <returns>空の候補幅とnullの共有量を持つInvalid。</returns>
    private static AabbResult Invalid() => new(RegionRelation.Invalid, [], null);

    /// <summary>接触込みのBooleanへ明示的に変換する。</summary>
    /// <param name="relation">元の3状態またはInvalid。</param><returns>接触・重なりはtrue、離隔はfalse、無効はnull。</returns>
    internal static bool? Inclusive(RegionRelation relation) => relation switch
    {
        RegionRelation.Touching or RegionRelation.Overlapping => true,
        RegionRelation.Separated => false,
        _ => null
    };

    /// <summary>正の共有面積・体積だけをBooleanへ変換する。</summary>
    /// <param name="relation">元の3状態またはInvalid。</param><returns>重なりはtrue、接触・離隔はfalse、無効はnull。</returns>
    internal static bool? PositiveOverlap(RegionRelation relation) => relation switch
    {
        RegionRelation.Overlapping => true,
        RegionRelation.Touching or RegionRelation.Separated => false,
        _ => null
    };
}

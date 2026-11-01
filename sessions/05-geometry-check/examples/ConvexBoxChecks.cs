using static GeometryMath;

/// <summary>軸平行凸箱の点包含を、6面の外向き法線の半空間条件から求める。</summary>
internal static class ConvexBoxChecks
{
    /// <summary>±X・±Y・±Zの各外向き法線と、面から点への変位の内積を求める。</summary>
    /// <param name="box">有限な正の幅を持つ閉箱。</param><param name="p">有限な検査点。</param>
    /// <param name="values">成功時は−X・+X・−Y・+Y・−Z・+Z面の順の値。無効なら空。</param>
    /// <returns>全6面の演算が有効ならtrue。</returns>
    /// <remarks>単位法線なので値は符号付き距離の尺度。一般の非凸・開いたメッシュを受け取るAPIではない。</remarks>
    internal static bool TryHalfspaces(Box3 box, P3 p, out double[] values)
    {
        values = [];
        if (!AabbChecks.IsValid(box) || !Finite(p)) return false;
        (P3 Anchor, P3 Normal)[] faces = [
            (box.Min, (-1, 0, 0)), (box.Max, (1, 0, 0)),
            (box.Min, (0, -1, 0)), (box.Max, (0, 1, 0)),
            (box.Min, (0, 0, -1)), (box.Max, (0, 0, 1))];
        double[] result = new double[6];
        for (int i = 0; i < faces.Length; i++)
        {
            var (anchor, normal) = faces[i];
            P3 w = (p.X - anchor.X, p.Y - anchor.Y, p.Z - anchor.Z);
            double value = normal.X * w.X + normal.Y * w.Y + normal.Z * w.Z;
            if (!Finite(w) || !double.IsFinite(value)) return false;
            result[i] = value;
        }
        values = result;
        return true;
    }

    /// <summary>全外向き面について内積が負なら内部、非正で0を含めば境界、正があれば外部。</summary>
    /// <param name="box">境界と中身を含む軸平行凸箱。</param><param name="p">検査点。</param>
    /// <returns>内部・境界・外部、または無効。</returns>
    internal static PointLocation Locate(Box3 box, P3 p)
    {
        if (!TryHalfspaces(box, p, out double[] values)) return PointLocation.Invalid;
        // ある面の延長平面上でも、他面から外へ出ていれば境界ではない。
        if (values.Any(v => v > 0)) return PointLocation.Outside;
        return values.Any(v => v == 0) ? PointLocation.Boundary : PointLocation.Inside;
    }
}

global using V2 = (double U, double V);
global using V3 = (double X, double Y, double Z);
using System.Numerics;

/// <summary>断面の中心と右手系の直交単位フレーム。成分は世界XYZ座標。</summary>
/// <param name="Center">断面の原点。座標と同じ長さ単位。</param>
/// <param name="N">断面のu方向の無次元単位基底。</param>
/// <param name="B">断面のv方向の無次元単位基底。</param>
/// <param name="T">N×Bと一致する断面法線・押し出し方向。</param>
internal readonly record struct SectionFrame(V3 Center, V3 N, V3 B, V3 T);

/// <summary>第4回の配置・行列・断面積の成分式。外部CADや他の教材に依存しない。</summary>
internal static class ModelingMath
{
    // 長さ・面積・無次元の判定値を混ぜない。教材の固定単位向けの下限である。
    internal const double MinimumLength = 1e-12;
    internal const double MinimumArea2 = 1e-12;
    internal const double FrameTolerance = 1e-12;

    /// <summary>点へ変位を足す、または2つの変位を合成する。</summary>
    /// <param name="a">点または変位。</param><param name="b">同じ単位の変位。</param>
    /// <returns>成分ごとの和。</returns>
    internal static V3 Add(V3 a, V3 b) => (a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>同じ座標系の2点の差から変位を求める。</summary>
    /// <param name="a">到達点。</param><param name="b">基準点。</param><returns>bからaへの変位。</returns>
    internal static V3 Sub(V3 a, V3 b) => (a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>ベクトルを実数倍する。</summary>
    /// <param name="a">対象ベクトル。</param><param name="s">倍率。</param><returns>各成分をs倍した値。</returns>
    internal static V3 Scale(V3 a, double s) => (a.X * s, a.Y * s, a.Z * s);

    /// <summary>対応する成分の積を足し、長さや直交性を調べる。</summary>
    /// <param name="a">第1ベクトル。</param><param name="b">第2ベクトル。</param><returns>内積。</returns>
    internal static double Dot(V3 a, V3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>右手系の外積を成分式で求める。引数の交換で符号が逆転する。</summary>
    /// <param name="a">第1ベクトル。</param><param name="b">第2ベクトル。</param>
    /// <returns>両方に垂直なベクトル。長さは平行四辺形の面積。</returns>
    internal static V3 Cross(V3 a, V3 b) =>
        (a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    /// <summary>ベクトルのユークリッド長を求める。</summary>
    /// <param name="a">対象ベクトル。</param><returns>内積の平方根。</returns>
    /// <remarks>極端な値での桁あふれ・アンダーフローを補正しない。利用側で有限性と下限を確認する。</remarks>
    internal static double Length(V3 a) => Math.Sqrt(Dot(a, a));

    /// <summary>3成分すべてが有限かを調べる。</summary>
    /// <param name="a">対象の点またはベクトル。</param><returns>NaN・無限大を含まなければtrue。</returns>
    internal static bool Finite(V3 a) => double.IsFinite(a.X) && double.IsFinite(a.Y) && double.IsFinite(a.Z);

    /// <summary>有限な中心、直交単位基底、N×B=Tを検査する。</summary>
    /// <param name="frame">世界座標の断面フレーム。</param>
    /// <remarks>自動的な正規化や左右反転はしない。無効入力はArgumentException。</remarks>
    internal static void ValidateFrame(SectionFrame frame)
    {
        if (!Finite(frame.Center) || !Finite(frame.N) || !Finite(frame.B) || !Finite(frame.T))
            throw new ArgumentException("フレームに非有限値があります。", nameof(frame));
        double[] errors = [Dot(frame.N, frame.N) - 1, Dot(frame.B, frame.B) - 1, Dot(frame.T, frame.T) - 1,
            Dot(frame.N, frame.B), Dot(frame.N, frame.T), Dot(frame.B, frame.T),
            Length(Sub(Cross(frame.N, frame.B), frame.T))];
        if (errors.Any(x => !double.IsFinite(x) || Math.Abs(x) > FrameTolerance))
            throw new ArgumentException("N・B・TはN×B=Tの直交単位基底が必要です。", nameof(frame));
    }

    /// <summary>断面内の距離u・vを、C+uN+vBで世界座標へ配置する。</summary>
    /// <param name="frame">右手系の直交単位フレーム。</param><param name="point">長さ単位の局所(u,v)。wは0。</param>
    /// <returns>世界座標の点。</returns><remarks>非有限入力・演算結果、無効フレームはArgumentException。</remarks>
    internal static V3 Place(SectionFrame frame, V2 point)
    {
        ValidateFrame(frame);
        if (!double.IsFinite(point.U) || !double.IsFinite(point.V)) throw new ArgumentException("局所点が非有限です。", nameof(point));
        V3 result = Add(frame.Center, Add(Scale(frame.N, point.U), Scale(frame.B, point.V)));
        if (!Finite(result)) throw new ArgumentException("配置結果が非有限です。", nameof(point));
        return result;
    }

    /// <summary>本文の列方式に従い、基底N・B・Tと中心Cを列に並べる。</summary>
    /// <param name="frame">右手系の断面フレーム。</param><returns>最後の行が(0,0,0,1)のdoubleの4×4配列。</returns>
    internal static double[,] ColumnMatrix(SectionFrame frame)
    {
        ValidateFrame(frame);
        var (c, n, b, t) = (frame.Center, frame.N, frame.B, frame.T);
        return new double[,] { { n.X, b.X, t.X, c.X }, { n.Y, b.Y, t.Y, c.Y },
            { n.Z, b.Z, t.Z, c.Z }, { 0, 0, 0, 1 } };
    }

    /// <summary>4×4配列と4成分の列を、行と列の内積で掛ける。</summary>
    /// <param name="matrix">本文の列方式の4×4配列。</param><param name="local">(u,v,w,h)。断面上の点はw=0、h=1。</param>
    /// <returns>4成分の変換結果。</returns><remarks>寸法不一致・非有限入力・結果はArgumentException。</remarks>
    internal static double[] TransformColumn(double[,] matrix, double[] local)
    {
        if (matrix.GetLength(0) != 4 || matrix.GetLength(1) != 4 || local.Length != 4 ||
            matrix.Cast<double>().Any(x => !double.IsFinite(x)) || local.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("有限な4×4行列と4成分の列が必要です。");
        double[] result = new double[4];
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++) result[row] += matrix[row, col] * local[col];
        if (result.Any(x => !double.IsFinite(x))) throw new ArgumentException("行列積が非有限です。");
        return result;
    }

    /// <summary>列方式の配列をfloatへ変換し、System.Numericsの行方式へ転置する。</summary>
    /// <param name="column">有限な4×4配列。</param><returns>vMの順で使える転置済み行列。</returns>
    /// <remarks>float範囲外を拒否するが丸めや微小値の消失は起こる。doubleの精度は保てない。</remarks>
    internal static Matrix4x4 NumericsRowMatrix(double[,] column)
    {
        _ = TransformColumn(column, [0, 0, 0, 1]);
        float[] m = column.Cast<double>().Select(x => (float)x).ToArray();
        if (m.Any(x => !float.IsFinite(x))) throw new ArgumentException("floatの有限範囲を超える行列です。", nameof(column));
        Matrix4x4 asColumns = new(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7],
            m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
        return Matrix4x4.Transpose(asColumns);
    }

    /// <summary>輪郭の符号付き面積を、最初の点を基準とした外積の和で求める。</summary>
    /// <param name="section">最後に始点を重複保存しない、3点以上の局所輪郭。末尾から先頭へ閉じる。</param>
    /// <returns>反時計回りで正となる面積。</returns>
    /// <remarks>自己交差輪郭の内部面積を定義するAPIではない。有限性と点数のみを検査する。</remarks>
    internal static double SignedArea(IReadOnlyList<V2> section)
    {
        if (section.Count < 3 || section.Any(p => !double.IsFinite(p.U) || !double.IsFinite(p.V)))
            throw new ArgumentException("有限な3点以上が必要です。", nameof(section));
        double sum = 0;
        V2 o = section[0];
        for (int i = 0; i < section.Count; i++)
        {
            V2 a = section[i], b = section[(i + 1) % section.Count];
            sum += (a.U - o.U) * (b.V - o.V) - (a.V - o.V) * (b.U - o.U);
        }
        if (!double.IsFinite(sum)) throw new ArgumentException("面積計算が非有限です。", nameof(section));
        return sum / 2;
    }
}

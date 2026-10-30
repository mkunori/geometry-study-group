using System.Numerics;

/// <summary>
/// 教材の列ベクトル方式（p' = Mp）を直接計算し、同じ変換を.NETの行ベクトル方式へ対応させる。
/// </summary>
/// <remarks>
/// 行列の配列は [行, 列] を表し、添字は0から始まる。手計算には double を使う。
/// 本教材の固定サイズの行列とアフィン変換だけを扱い、配列サイズやアフィン条件の検証は行わない。
/// 透視投影や汎用行列ライブラリとしての利用は対象外。
/// </remarks>
internal static class ColumnMath
{
    /// <summary>2×2行列を列ベクトルに左から掛け、変換後の2成分を求める。</summary>
    /// <param name="m">[行, 列] で参照する2×2の線形変換行列。</param>
    /// <param name="p">変換前の列ベクトル (x, y)。</param>
    /// <returns>m の各行と p の成分の積を足した (x', y')。</returns>
    /// <remarks>
    /// 第1列はX基底、第2列はY基底の変換先。p.X 倍の第1列と p.Y 倍の第2列を足す計算でもある。
    /// </remarks>
    public static (double X, double Y) Multiply2(double[,] m, (double X, double Y) p) =>
        (m[0, 0] * p.X + m[0, 1] * p.Y,
         m[1, 0] * p.X + m[1, 1] * p.Y);

    /// <summary>原点周りの2D回転を表す、列ベクトル方式の2×2行列を作る。</summary>
    /// <param name="radians">回転角（ラジアン）。度で与える場合は、度 × Math.PI / 180 に変換する。</param>
    /// <returns>第1列が (cosθ, sinθ)、第2列が (-sinθ, cosθ) の回転行列。</returns>
    /// <remarks>
    /// X軸を右、Y軸を上とし、正の角度は反時計回り。各列に回転後の基底ベクトルを置くため、
    /// <see cref="Multiply2"/> で掛けると任意のベクトルも同じ角度だけ回転する。
    /// </remarks>
    public static double[,] Rotation2(double radians)
    {
        double c = Math.Cos(radians);
        double s = Math.Sin(radians);
        // 第1列 (c,s) は回転後のX基底、第2列 (-s,c) は回転後のY基底。
        return new double[,] { { c, -s }, { s, c } };
    }

    /// <summary>3×3の2D同次行列を列ベクトルに掛け、同次成分も含めて計算する。</summary>
    /// <param name="m">[行, 列] で参照する3×3行列。本教材では最下行が (0, 0, 1) のアフィン変換。</param>
    /// <param name="p">同次座標 (x, y, w)。点は w=1、方向ベクトルは w=0 とする。</param>
    /// <returns>3行すべてと p の積から求めた (x', y', w')。</returns>
    /// <remarks>
    /// 右端の移動列に w を掛けるため、点には移動が加わり、方向には加わらない。
    /// 本教材のアフィン変換では w は保たれる。w による除算や透視投影は行わない。
    /// </remarks>
    public static (double X, double Y, double W) Transform2(
        double[,] m, (double X, double Y, double W) p) =>
        (m[0, 0] * p.X + m[0, 1] * p.Y + m[0, 2] * p.W,
         m[1, 0] * p.X + m[1, 1] * p.Y + m[1, 2] * p.W,
         m[2, 0] * p.X + m[2, 1] * p.Y + m[2, 2] * p.W);

    /// <summary>4×4の3D同次行列を列ベクトルに掛け、同次成分も含めて計算する。</summary>
    /// <param name="m">[行, 列] で参照する4×4行列。本教材では最下行が (0, 0, 0, 1) のアフィン変換。</param>
    /// <param name="p">同次座標 (x, y, z, w)。点は w=1、方向ベクトルは w=0 とする。</param>
    /// <returns>4行すべてと p の積から求めた (x', y', z', w')。</returns>
    /// <remarks>
    /// 左上3×3が線形変換、右端の上3成分が移動量を表す。移動量への w の掛け算で点と方向を区別する。
    /// 本教材のアフィン変換では w は保たれる。w による除算や透視投影は行わない。
    /// </remarks>
    public static (double X, double Y, double Z, double W) Transform3(
        double[,] m, (double X, double Y, double Z, double W) p) =>
        (m[0, 0] * p.X + m[0, 1] * p.Y + m[0, 2] * p.Z + m[0, 3] * p.W,
         m[1, 0] * p.X + m[1, 1] * p.Y + m[1, 2] * p.Z + m[1, 3] * p.W,
         m[2, 0] * p.X + m[2, 1] * p.Y + m[2, 2] * p.Z + m[2, 3] * p.W,
         m[3, 0] * p.X + m[3, 1] * p.Y + m[3, 2] * p.Z + m[3, 3] * p.W);

    /// <summary>2つの3×3行列を、left × right の順に掛ける。</summary>
    /// <param name="left">積の左側の3×3行列。列ベクトルへの適用は right の後になる。</param>
    /// <param name="right">積の右側の3×3行列。列ベクトルへ先に適用する。</param>
    /// <returns>各成分 C[i,j] が Σ left[i,k] × right[k,j] となる、新しい3×3配列。</returns>
    /// <remarks>
    /// 入力・出力とも配列の添字は [行, 列]。列ベクトル方式では TRp = T(Rp) なので、
    /// 回転 R の後に移動 T を適用する行列は Multiply3(T, R) で求める。一般に TR と RT は異なる。
    /// </remarks>
    public static double[,] Multiply3(double[,] left, double[,] right)
    {
        var result = new double[3, 3];
        // C[i,j] = Σ left[i,k] * right[k,j]。左の行と右の列を掛けて足す。
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                for (int k = 0; k < 3; k++)
                    result[i, j] += left[i, k] * right[k, j];
        return result;
    }

    /// <summary>列ベクトル方式の2Dアフィン変換を、同じ変換を表す.NETの行ベクトル方式へ配置し直す。</summary>
    /// <param name="m">最下行が (0, 0, 1) の3×3行列。[行, 列] で参照する。</param>
    /// <returns>転置後の固定された最後の列 (0, 0, 1) を省略した <see cref="Matrix3x2"/>。</returns>
    /// <remarks>
    /// p' = M_col p に対応する行方式は p_row' = p_row M_row、M_row = M_col^T。
    /// 元の右端の移動列 m[0,2], m[1,2] は M31, M32 へ移る。
    /// double から float へキャストするため丸め誤差が生じうる。アフィン条件の検証や透視投影への対応は行わない。
    /// </remarks>
    public static Matrix3x2 ToRow3x2(double[,] m) => new(
        (float)m[0, 0], (float)m[1, 0],
        (float)m[0, 1], (float)m[1, 1],
        (float)m[0, 2], (float)m[1, 2]);

    /// <summary>列ベクトル方式の4×4行列を転置し、.NETの行ベクトル方式の配置へ変換する。</summary>
    /// <param name="m">[行, 列] で参照する4×4行列。本教材では3Dアフィン変換を渡す。</param>
    /// <returns>m の転置の全16成分を float に変換した <see cref="Matrix4x4"/>。</returns>
    /// <remarks>
    /// 同じ変換を p_row' = p_row M_row、M_row = M_col^T として計算するための配置変更。
    /// 元の右端の移動列は M41, M42, M43 へ移る。メモリの格納順序の変換を意味しない。
    /// double から float への丸めで精度が変わる。本教材では点・方向のアフィン変換だけを扱い、透視投影は扱わない。
    /// </remarks>
    public static Matrix4x4 ToRow4x4(double[,] m) => new(
        (float)m[0, 0], (float)m[1, 0], (float)m[2, 0], (float)m[3, 0],
        (float)m[0, 1], (float)m[1, 1], (float)m[2, 1], (float)m[3, 1],
        (float)m[0, 2], (float)m[1, 2], (float)m[2, 2], (float)m[3, 2],
        (float)m[0, 3], (float)m[1, 3], (float)m[2, 3], (float)m[3, 3]);
}

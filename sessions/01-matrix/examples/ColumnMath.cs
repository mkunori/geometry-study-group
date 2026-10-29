using System.Numerics;

// 本文の列ベクトル規約をそのまま書く。配列の添字は [行, 列]。
// この教材で使う固定サイズの行列とアフィン変換だけを扱う。
internal static class ColumnMath
{
    public static (double X, double Y) Multiply2(double[,] m, (double X, double Y) p) =>
        (m[0, 0] * p.X + m[0, 1] * p.Y,
         m[1, 0] * p.X + m[1, 1] * p.Y);

    public static double[,] Rotation2(double radians)
    {
        double c = Math.Cos(radians);
        double s = Math.Sin(radians);
        // 第1列 (c,s) は回転後のX基底、第2列 (-s,c) は回転後のY基底。
        return new double[,] { { c, -s }, { s, c } };
    }

    public static (double X, double Y, double W) Transform2(
        double[,] m, (double X, double Y, double W) p) =>
        (m[0, 0] * p.X + m[0, 1] * p.Y + m[0, 2] * p.W,
         m[1, 0] * p.X + m[1, 1] * p.Y + m[1, 2] * p.W,
         m[2, 0] * p.X + m[2, 1] * p.Y + m[2, 2] * p.W);

    public static (double X, double Y, double Z, double W) Transform3(
        double[,] m, (double X, double Y, double Z, double W) p) =>
        (m[0, 0] * p.X + m[0, 1] * p.Y + m[0, 2] * p.Z + m[0, 3] * p.W,
         m[1, 0] * p.X + m[1, 1] * p.Y + m[1, 2] * p.Z + m[1, 3] * p.W,
         m[2, 0] * p.X + m[2, 1] * p.Y + m[2, 2] * p.Z + m[2, 3] * p.W,
         m[3, 0] * p.X + m[3, 1] * p.Y + m[3, 2] * p.Z + m[3, 3] * p.W);

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

    // 列方式の3×3を転置し、固定された最後の列 (0,0,1) を省く。
    // m は最下行が (0,0,1) の2Dアフィン変換に限定する。
    public static Matrix3x2 ToRow3x2(double[,] m) => new(
        (float)m[0, 0], (float)m[1, 0],
        (float)m[0, 1], (float)m[1, 1],
        (float)m[0, 2], (float)m[1, 2]);

    // 列方式の4×4を転置する。右端の移動列は M41,M42,M43 へ移る。
    public static Matrix4x4 ToRow4x4(double[,] m) => new(
        (float)m[0, 0], (float)m[1, 0], (float)m[2, 0], (float)m[3, 0],
        (float)m[0, 1], (float)m[1, 1], (float)m[2, 1], (float)m[3, 1],
        (float)m[0, 2], (float)m[1, 2], (float)m[2, 2], (float)m[3, 2],
        (float)m[0, 3], (float)m[1, 3], (float)m[2, 3], (float)m[3, 3]);
}

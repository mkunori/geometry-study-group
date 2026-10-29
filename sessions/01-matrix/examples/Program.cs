using System.Globalization;
using System.Numerics;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
const double DoubleTolerance = 1e-12;
const double FloatTolerance = 1e-5;
int checks = 0;

try
{
    MatrixAndBasis();
    Rotation2D();
    Homogeneous2D();
    LocalToWorld3D();
    CompositionOrder();
    NumericsAndPrecision();
    Console.WriteLine($"\nすべての {checks} チェックに成功しました。");
    return 0;
}
catch (InvalidOperationException error)
{
    Console.Error.WriteLine($"FAIL: {error.Message}");
    return 1;
}

void MatrixAndBasis()
{
    Console.WriteLine("1. 行列×ベクトルと基底（解説②）");
    double[,] a = { { 2, 1 }, { 1, 2 } };
    var p = ColumnMath.Multiply2(a, (2, 1)); // (2*2 + 1*1, 1*2 + 2*1)
    var e1 = ColumnMath.Multiply2(a, (1, 0));
    var e2 = ColumnMath.Multiply2(a, (0, 1));
    Check("Ap", [p.X, p.Y], [5, 4]);
    Check("Ae1 = 第1列", [e1.X, e1.Y], [2, 1]);
    Check("Ae2 = 第2列", [e2.X, e2.Y], [1, 2]);
    Check("2Ae1 + Ae2 = Ap", [2 * e1.X + e2.X, 2 * e1.Y + e2.Y], [p.X, p.Y]);

    // .NETでは転置した配置を使う。引数順は M11,M12,M21,M22,M31,M32。
    var row = new Matrix3x2(2, 1, 1, 2, 0, 0);
    var api = Vector2.Transform(new Vector2(2, 1), row);
    Check(".NETのAp", [api.X, api.Y], [5, 4], FloatTolerance);
}

void Rotation2D()
{
    Console.WriteLine("\n2. 2D回転（解説③／基本問題1）");
    double[,] exact90 = { { 0, -1 }, { 1, 0 } };
    var p = ColumnMath.Multiply2(exact90, (2, 1));
    var e1 = ColumnMath.Multiply2(exact90, (1, 0));
    var e2 = ColumnMath.Multiply2(exact90, (0, 1));
    Check("90度の点", [p.X, p.Y], [-1, 2]);
    Check("90度のX基底", [e1.X, e1.Y], [0, 1]);
    Check("90度のY基底", [e2.X, e2.Y], [-1, 0]);
    var api90 = Vector2.Transform(new Vector2(2, 1), Matrix3x2.CreateRotation(MathF.PI / 2));
    Check(".NETの90度", [api90.X, api90.Y], [-1, 2], FloatTolerance);

    // 任意角度は度からラジアンに変え、cos・sinで2本の基底の行き先を作る。
    double degrees = 30;
    double radians = degrees * Math.PI / 180;
    var rotated = ColumnMath.Multiply2(ColumnMath.Rotation2(radians), (2, 1));
    double expectedX = Math.Sqrt(3) - 0.5;
    double expectedY = 1 + Math.Sqrt(3) / 2;
    Check("30度の点", [rotated.X, rotated.Y], [expectedX, expectedY]);
    var api30 = Vector2.Transform(new Vector2(2, 1), Matrix3x2.CreateRotation((float)radians));
    Check(".NETの30度", [api30.X, api30.Y], [rotated.X, rotated.Y], FloatTolerance);
    Check("回転前後の長さの2乗", [rotated.X * rotated.X + rotated.Y * rotated.Y], [5]);

    // 正方向・負方向・一周でも同じ式を使い、既知の座標で検証する。
    foreach (var (angle, x, y) in new[] { (0, 2, 1), (90, -1, 2), (180, -2, -1), (-90, 1, -2), (360, 2, 1) })
    {
        var result = ColumnMath.Multiply2(ColumnMath.Rotation2(angle * Math.PI / 180), (2, 1));
        Check($"sin・cosで{angle}度", [result.X, result.Y], [x, y]);
    }
}

void Homogeneous2D()
{
    Console.WriteLine("\n3. 同次座標と平行移動（解説④／基本問題2）");
    double[,] t = { { 1, 0, 3 }, { 0, 1, -2 }, { 0, 0, 1 } };
    var point = ColumnMath.Transform2(t, (2, 1, 1));
    var direction = ColumnMath.Transform2(t, (2, 1, 0));
    // x' = x + 3w、y' = y - 2w。w=0なら移動列の寄与がゼロになる。
    Check("点 w=1", [point.X, point.Y, point.W], [5, -1, 1]);
    Check("方向 w=0", [direction.X, direction.Y, direction.W], [2, 1, 0]);
    var origin = ColumnMath.Transform2(t, (0, 0, 1));
    Check("移動した原点", [origin.X, origin.Y, origin.W], [3, -2, 1]);

    var row = ColumnMath.ToRow3x2(t);
    var apiPoint = Vector2.Transform(new Vector2(2, 1), row);
    // TransformNormalは移動を足さず、線形部分だけを掛ける。
    var apiDirection = Vector2.TransformNormal(new Vector2(2, 1), row);
    Check(".NETの点", [apiPoint.X, apiPoint.Y], [point.X, point.Y], FloatTolerance);
    Check(".NETの方向", [apiDirection.X, apiDirection.Y], [direction.X, direction.Y], FloatTolerance);
    Check(".NETの移動成分 M31,M32", [row.M31, row.M32], [3, -2], FloatTolerance);
}

void LocalToWorld3D()
{
    Console.WriteLine("\n4. 3Dのローカル→ワールド（解説⑤／発展問題）");
    double[,] m =
    {
        { 0, -1, 0, 10 },
        { 1, 0, 0, 0 },
        { 0, 0, 1, 3 },
        { 0, 0, 0, 1 }
    };
    var point = ColumnMath.Transform3(m, (2, 1, 0, 1));
    var direction = ColumnMath.Transform3(m, (2, 1, 0, 0));
    Check("ワールドの点", [point.X, point.Y, point.Z, point.W], [9, 2, 3, 1]);
    Check("ワールドの方向", [direction.X, direction.Y, direction.Z, direction.W], [-1, 2, 0, 0]);

    var row = ColumnMath.ToRow4x4(m);
    var apiPoint = Vector3.Transform(new Vector3(2, 1, 0), row);
    var apiDirection = Vector3.TransformNormal(new Vector3(2, 1, 0), row);
    Check(".NETのワールド点", [apiPoint.X, apiPoint.Y, apiPoint.Z], [9, 2, 3], FloatTolerance);
    Check(".NETのワールド方向", [apiDirection.X, apiDirection.Y, apiDirection.Z], [-1, 2, 0], FloatTolerance);
    Check(".NETの移動成分 M41,M42,M43", [row.M41, row.M42, row.M43], [10, 0, 3], FloatTolerance);

    // 列方式の T*R に対応する.NETの合成は Rrow*Trow。
    var apiPlacement = Matrix4x4.CreateRotationZ(MathF.PI / 2) * Matrix4x4.CreateTranslation(10, 0, 3);
    var placed = Vector3.Transform(new Vector3(2, 1, 0), apiPlacement);
    Check(".NETの回転→移動", [placed.X, placed.Y, placed.Z], [point.X, point.Y, point.Z], FloatTolerance);
    var zBasis = Vector3.TransformNormal(Vector3.UnitZ, apiPlacement);
    Check("Z基底はそのまま", [zBasis.X, zBasis.Y, zBasis.Z], [0, 0, 1], FloatTolerance);
}

void CompositionOrder()
{
    Console.WriteLine("\n5. 合成の順序（解説⑥／基本問題3）");
    double[,] r = { { 0, -1, 0 }, { 1, 0, 0 }, { 0, 0, 1 } };
    double[,] t = { { 1, 0, 3 }, { 0, 1, 0 }, { 0, 0, 1 } };
    var afterRotation = ColumnMath.Transform2(r, (1, 0, 1));
    var rotateThenMove = ColumnMath.Transform2(t, afterRotation);
    var afterTranslation = ColumnMath.Transform2(t, (1, 0, 1));
    var moveThenRotate = ColumnMath.Transform2(r, afterTranslation);
    Check("回転の直後", [afterRotation.X, afterRotation.Y], [0, 1]);
    Check("回転→移動", [rotateThenMove.X, rotateThenMove.Y], [3, 1]);
    Check("移動の直後", [afterTranslation.X, afterTranslation.Y], [4, 0]);
    Check("移動→回転", [moveThenRotate.X, moveThenRotate.Y], [0, 4]);

    var tr = ColumnMath.Multiply3(t, r);
    var rt = ColumnMath.Multiply3(r, t);
    Check("TRの全成分", tr.Cast<double>().ToArray(), [0, -1, 3, 1, 0, 0, 0, 0, 1]);
    Check("RTの全成分", rt.Cast<double>().ToArray(), [0, -1, 0, 1, 0, 3, 0, 0, 1]);
    var byTr = ColumnMath.Transform2(tr, (1, 0, 1));
    var byRt = ColumnMath.Transform2(rt, (1, 0, 1));
    Check("TRp = T(Rp)", [byTr.X, byTr.Y, byTr.W], [rotateThenMove.X, rotateThenMove.Y, 1]);
    Check("RTp = R(Tp)", [byRt.X, byRt.Y, byRt.W], [moveThenRotate.X, moveThenRotate.Y, 1]);

    var rowR = Matrix3x2.CreateRotation(MathF.PI / 2);
    var rowT = Matrix3x2.CreateTranslation(3, 0);
    var apiTr = Vector2.Transform(new Vector2(1, 0), rowR * rowT);
    var apiRt = Vector2.Transform(new Vector2(1, 0), rowT * rowR);
    Check(".NETのRrow*Trow", [apiTr.X, apiTr.Y], [3, 1], FloatTolerance);
    Check(".NETのTrow*Rrow", [apiRt.X, apiRt.Y], [0, 4], FloatTolerance);
}

void NumericsAndPrecision()
{
    Console.WriteLine("\n6. .NETの転置配置と精度（解説⑥・補足）");
    // 非対称な係数で、行と列を取り違えていないことを確認する。
    double[,] column = { { 2, -1, 5 }, { 3, 4, -2 }, { 0, 0, 1 } };
    var manual = ColumnMath.Transform2(column, (2, 1, 1));
    var row = ColumnMath.ToRow3x2(column);
    Check("M11,M12,M21,M22,M31,M32", [row.M11, row.M12, row.M21, row.M22, row.M31, row.M32], [2, 3, -1, 4, 5, -2], FloatTolerance);
    Check("非対称行列の手計算", [manual.X, manual.Y], [8, 8]);
    var api = Vector2.Transform(new Vector2(2, 1), row);
    Check("非対称行列の.NET", [api.X, api.Y], [manual.X, manual.Y], FloatTolerance);

    // 三角関数やπの近似により厳密な0にならない例。ここは値を丸めず表示する。
    Console.WriteLine($"  Math.Cos(Math.PI / 2)   = {Math.Cos(Math.PI / 2).ToString("G17", CultureInfo.InvariantCulture)}");
    Console.WriteLine($"  MathF.Cos(MathF.PI / 2) = {MathF.Cos(MathF.PI / 2).ToString("G9", CultureInfo.InvariantCulture)}");
    Console.WriteLine("  手実装(double): 許容誤差1e-12、.NET API(float): 許容誤差1e-5");
}

void Check(string label, double[] actual, double[] expected, double tolerance = DoubleTolerance)
{
    if (actual.Length != expected.Length)
        throw new InvalidOperationException($"{label}: 成分数が一致しません。");
    for (int i = 0; i < actual.Length; i++)
    {
        // NaNは通常の大小比較をすり抜けるため、有限値であることも確認する。
        if (!double.IsFinite(actual[i]) || !double.IsFinite(expected[i]) || Math.Abs(actual[i] - expected[i]) > tolerance)
            throw new InvalidOperationException($"{label}[{i}]: actual={Format(actual)}, expected={Format(expected)}, tolerance={tolerance}");
    }
    checks++;
    Console.WriteLine($"  [OK] {label}: {Format(actual)}");
}

string Format(double[] values) => $"({string.Join(", ", values.Select(value => value.ToString("G9", CultureInfo.InvariantCulture)))})";

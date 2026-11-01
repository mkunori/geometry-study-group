using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCadGeometrySamples;

/// <summary>第2回の内積・外積・射影・三角形法線をAutoCAD APIへ対応させる参考コード。公式資料との静的照合のみ。</summary>
/// <remarks>
/// Windows版AutoCAD 2026 Update 1.2以降／.NET 10想定。ビルド未検証・NETLOAD未実施・AutoCAD実機未検証。
/// 固定入力はWCSの同じ直交座標系で、X右・Y上・3Dは右手系。長さは作図単位で、mmなどを仮定しない。
/// 全コマンドはEditorへの表示のみ。図形・UCS・保存先を変更せず、TransactionやDBObjectは作成しない。
/// </remarks>
public static class VectorCommands
{
    // VectorMath.csと同じ小さな固定入力向け。長さと面積の次元を混同しない。
    private const double MinimumLength = 1e-12;
    private const double MinimumCrossLength = 1e-12;

    /// <summary>非単位のbへの射影と、内積・長さ・角度を表示する。図面は変更しない。</summary>
    /// <remarks>
    /// 引数なし。a=(3,4,0)、b=(2,0,0)。数学上は内積6、長さ5と2、角度約53.130102度、射影(3,0,0)。
    /// ゼロの角度入力・射影先を拒否し、ゼロの射影元は扱える例も表示する。出力は実機未検証。
    /// </remarks>
    [CommandMethod("GEO02_DOT")]
    public static void DotAndProjection()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        Vector3d a = new(3, 4, 0);
        Vector3d b = new(2, 0, 0);
        editor.WriteMessage("\nGEO02_DOT: API計算値（実機未検証）。数学上の期待値はREADMEを参照。図面変更なし。");
        WriteVector(editor, "a", a);
        WriteVector(editor, "b（単位ベクトルではない）", b);
        WriteScalar(editor, "a・b", a.DotProduct(b));
        WriteScalar(editor, "aの長さ", a.Length);
        WriteScalar(editor, "bの長さ", b.Length);

        if (TryAngle(a, b, out double radians))
        {
            WriteScalar(editor, "cosθ（内積／長さの積）", a.DotProduct(b) / (a.Length * b.Length));
            WriteScalar(editor, "acosからの角度（ラジアン）", radians);
            WriteScalar(editor, "acosからの角度（度）", radians * 180 / Math.PI);
            // 同じ入力を検査してからAPIの[0,π]の角度とも読み比べる。符号付き角度ではない。
            double apiRadians = a.GetAngleTo(b);
            WriteScalar(editor, "GetAngleTo（ラジアン）", apiRadians);
            WriteScalar(editor, "GetAngleTo（度）", apiRadians * 180 / Math.PI);
        }
        else editor.WriteMessage("\n  角度：未定義または教材で扱う範囲外。");

        if (TryProject(a, b, out Vector3d projection))
        {
            Vector3d residual = a;
            residual = residual.Subtract(projection);
            WriteScalar(editor, "b・b（射影の分母）", b.DotProduct(b));
            WriteScalar(editor, "射影の係数", a.DotProduct(b) / b.DotProduct(b));
            WriteVector(editor, "bへの正射影q", projection);
            WriteVector(editor, "残差a-q", residual);
            WriteScalar(editor, "残差とbの内積", residual.DotProduct(b));
        }
        else editor.WriteMessage("\n  射影：未定義または教材で扱う範囲外。");

        Vector3d zero = new(0, 0, 0);
        editor.WriteMessage($"\n  ゼロのaとの角度を定義可能: {TryAngle(zero, b, out _)}");
        editor.WriteMessage($"\n  ゼロのbとの角度を定義可能: {TryAngle(a, zero, out _)}");
        editor.WriteMessage($"\n  ゼロの射影先を扱える: {TryProject(a, zero, out _)}");
        if (TryProject(zero, b, out Vector3d zeroProjection))
            WriteVector(editor, "ゼロの射影元から非ゼロのbへの射影", zeroProjection);
    }

    /// <summary>外積の順序・面積と、一般の3成分の外積が両入力に直交することを数値で表示する。</summary>
    /// <remarks>
    /// 引数なし。u=(2,0,0)、v=(0,3,0)の外積は(0,0,6)、逆順は(0,0,-6)が期待値。
    /// 一般例(1,2,3)×(4,5,6)の期待値は(-3,6,-3)。図面変更なし、出力は実機未検証。
    /// </remarks>
    [CommandMethod("GEO02_CROSS")]
    public static void CrossAndArea()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        Vector3d u = new(2, 0, 0);
        Vector3d v = new(0, 3, 0);
        Vector3d cross = u.CrossProduct(v);
        editor.WriteMessage("\nGEO02_CROSS: API計算値（実機未検証）。右手系の外積。図面変更なし。");
        WriteVector(editor, "u", u);
        WriteVector(editor, "v", v);
        WriteVector(editor, "u×v", cross);
        WriteVector(editor, "v×u", v.CrossProduct(u));
        WriteScalar(editor, "平行四辺形面積（作図単位の2乗）", cross.Length);
        WriteScalar(editor, "三角形面積（作図単位の2乗）", cross.Length / 2);

        Vector3d a = new(1, 2, 3);
        Vector3d b = new(4, 5, 6);
        Vector3d general = a.CrossProduct(b);
        WriteVector(editor, "一般例a", a);
        WriteVector(editor, "一般例b", b);
        WriteVector(editor, "一般例a×b", general);
        WriteScalar(editor, "a・(a×b)", a.DotProduct(general));
        WriteScalar(editor, "b・(a×b)", b.DotProduct(general));
    }

    /// <summary>共通始点からの辺で2Dの左右と三角形法線を求め、逆順・一直線・重複・小さい面を表示する。</summary>
    /// <remarks>
    /// 引数なし。A=(1,1,0)、B=(4,1,0)、C=(3,3,0)。Z成分+6はX右・Y上で左を表す。
    /// ABCの単位法線は+Z、ACBは-Zが数学上の期待値。下限以下の外積にはGetNormalを呼ばない。
    /// 法線の可視化や図形の追加は行わず、すべて実機未検証。
    /// </remarks>
    [CommandMethod("GEO02_NORMAL")]
    public static void OrientationAndNormal()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        Point3d a = new(1, 1, 0);
        Point3d b = new(4, 1, 0);
        Point3d c = new(3, 3, 0);
        // 位置をそのまま外積せず、同じ始点AからB/Cへの変位を作る。
        Vector3d ab = a.GetVectorTo(b);
        Vector3d ac = a.GetVectorTo(c);
        Vector3d cross = ab.CrossProduct(ac);
        editor.WriteMessage("\nGEO02_NORMAL: API計算値（実機未検証）。WCSのXY平面、X右・Y上。図面変更なし。");
        WriteVector(editor, "AB=B-A", ab);
        WriteVector(editor, "AC=C-A", ac);
        WriteVector(editor, "AB×AC（3Dベクトル）", cross);
        WriteScalar(editor, "2Dの左右に使うZ成分（スカラー）", cross.Z);
        // 整数の固定例の符号を読む。一般のほぼ一直線の形状を厳密に分類する処理ではない。
        editor.WriteMessage($"\n  この固定例のCはA→Bの{(cross.Z > 0 ? "左" : cross.Z < 0 ? "右" : "直線上")}。");
        WriteScalar(editor, "符号付き三角形面積", cross.Z / 2);
        WriteScalar(editor, "通常の三角形面積", cross.Length / 2);

        WriteTriangleNormal(editor, "ABC", a, b, c);
        WriteTriangleNormal(editor, "ACB（B/C交換）", a, c, b);
        WriteTriangleNormal(editor, "一直線 C=(2,1,0)", a, b, new Point3d(2, 1, 0));
        WriteTriangleNormal(editor, "重複 C=A", a, b, a);
        WriteTriangleNormal(editor, "重複 B=A", a, a, c);
        WriteTriangleNormal(editor, "小さい外積の例", Point3d.Origin,
            new Point3d(1, 1, 0), new Point3d(1, 1 + 1e-13, 0));
    }

    /// <summary>有限で十分な長さを持つ2本のなす角を、内積とacosから求める。</summary>
    /// <param name="a">WCS成分の第1ベクトル。</param>
    /// <param name="b">同じ座標系の第2ベクトル。</param>
    /// <param name="radians">成功時は0からπのラジアン。失敗時は使用しない。</param>
    /// <returns>長さが1e-12を超え、長さ・積・比が有限ならtrue。それ以外はfalse。</returns>
    private static bool TryAngle(Vector3d a, Vector3d b, out double radians)
    {
        radians = default;
        if (!IsFinite(a) || !IsFinite(b)) return false;
        double lengthA = a.Length;
        double lengthB = b.Length;
        if (!double.IsFinite(lengthA) || !double.IsFinite(lengthB) ||
            lengthA <= MinimumLength || lengthB <= MinimumLength) return false;
        double denominator = lengthA * lengthB;
        double numerator = a.DotProduct(b);
        if (!double.IsFinite(denominator) || denominator <= 0 || !double.IsFinite(numerator)) return false;
        double cosine = numerator / denominator;
        if (!double.IsFinite(cosine)) return false;
        // クランプは有限な比の丸め誤差用。ゼロ長やNaNを有効な角度へ置き換えない。
        radians = Math.Acos(Math.Clamp(cosine, -1.0, 1.0));
        return true;
    }

    /// <summary>非単位でもよいbが張る直線へaを正射影する。ゼロのaは許す。</summary>
    /// <param name="a">射影元の有限ベクトル。</param>
    /// <param name="b">射影先の有限ベクトル。長さは1e-12を超えること。</param>
    /// <param name="projection">成功時の射影。失敗時の既定値は答えとして使わない。</param>
    /// <returns>分母・係数・結果が扱える有限値ならtrue。それ以外はfalse。</returns>
    private static bool TryProject(Vector3d a, Vector3d b, out Vector3d projection)
    {
        projection = default;
        if (!IsFinite(a) || !IsFinite(b)) return false;
        double denominator = b.DotProduct(b);
        double numerator = a.DotProduct(b);
        // b・b=|b|²なので、長さの下限も2乗してから比較する。
        if (!double.IsFinite(denominator) || !double.IsFinite(numerator) ||
            denominator <= MinimumLength * MinimumLength) return false;
        double factor = numerator / denominator;
        if (!double.IsFinite(factor)) return false;
        Vector3d result = b.MultiplyBy(factor);
        if (!IsFinite(result)) return false;
        projection = result;
        return true;
    }

    /// <summary>三角形の辺の外積を検査してから、ABCの向きの単位法線を作る。</summary>
    /// <param name="a">WCSの頂点A。</param>
    /// <param name="b">同じ座標系の頂点B。</param>
    /// <param name="c">同じ座標系の頂点C。</param>
    /// <param name="normal">成功時の単位法線。falseのときは使用しない。</param>
    /// <returns>頂点・辺・外積が有限で、外積の長さが1e-12を超える場合に正規化してtrue。</returns>
    /// <remarks>外積の下限は面積の次元。APIのGlobal許容誤差に暗黙依存せず、明示したToleranceを渡す。</remarks>
    private static bool TryTriangleNormal(Point3d a, Point3d b, Point3d c, out Vector3d normal)
    {
        normal = default;
        if (!IsFinite(a) || !IsFinite(b) || !IsFinite(c)) return false;
        Vector3d ab = a.GetVectorTo(b);
        Vector3d ac = a.GetVectorTo(c);
        if (!IsFinite(ab) || !IsFinite(ac)) return false;
        Vector3d cross = ab.CrossProduct(ac);
        if (!IsFinite(cross)) return false;
        double length = cross.Length;
        if (!double.IsFinite(length) || length <= MinimumCrossLength) return false;

        // GetNormalはEqualVector未満なら入力をそのまま返す。下限を超えたことを先に確認する。
        // 第1引数はこの外積の数値に対する閾値、第2引数は点の許容誤差（この呼出しでは未使用）。
        Tolerance tolerance = new(MinimumCrossLength, MinimumLength);
        Vector3d result = cross.GetNormal(tolerance);
        if (!IsFinite(result)) return false;
        normal = result;
        return true;
    }

    /// <summary>法線が作れた場合だけ成分と長さを表示し、失敗時に既定値を法線として表示しない。</summary>
    /// <param name="editor">現在の図面の出力先。</param>
    /// <param name="label">頂点順序や退化条件の名前。</param>
    /// <param name="a">WCSの頂点A。</param>
    /// <param name="b">WCSの頂点B。</param>
    /// <param name="c">WCSの頂点C。</param>
    private static void WriteTriangleNormal(Editor editor, string label, Point3d a, Point3d b, Point3d c)
    {
        if (TryTriangleNormal(a, b, c, out Vector3d normal))
        {
            WriteVector(editor, $"{label}の単位法線", normal);
            WriteScalar(editor, $"{label}の単位法線の長さ", normal.Length);
        }
        else editor.WriteMessage($"\n  {label}: 法線は未定義、下限以下、または非有限値のため扱いません。");
    }

    /// <summary>SDKの演算へ渡す前に、方向の全成分が有限かを調べる。</summary>
    /// <param name="vector">検査対象の方向。</param>
    /// <returns>NaNと正負のInfinityを含まない場合はtrue。</returns>
    private static bool IsFinite(Vector3d vector) =>
        double.IsFinite(vector.X) && double.IsFinite(vector.Y) && double.IsFinite(vector.Z);

    /// <summary>辺を引き算する前に、位置の全成分が有限かを調べる。</summary>
    /// <param name="point">検査対象の位置。</param>
    /// <returns>NaNと正負のInfinityを含まない場合はtrue。</returns>
    private static bool IsFinite(Point3d point) =>
        double.IsFinite(point.X) && double.IsFinite(point.Y) && double.IsFinite(point.Z);

    /// <summary>スカラーを有効数字17桁・小数点がピリオドの表記で出力し、残差をゼロに置換しない。</summary>
    /// <param name="editor">現在の図面の出力先。</param>
    /// <param name="label">量・単位・計算段階を区別する名前。</param>
    /// <param name="value">表示する値。</param>
    private static void WriteScalar(Editor editor, string label, double value) =>
        editor.WriteMessage($"\n  {label}: {value.ToString("G17", CultureInfo.InvariantCulture)}");

    /// <summary>ベクトルの3成分を有効数字17桁で表示する。位置や単位法線とはラベルで区別する。</summary>
    /// <param name="editor">現在の図面の出力先。</param>
    /// <param name="label">方向・辺・外積・単位法線を区別する名前。</param>
    /// <param name="vector">表示するベクトル。</param>
    private static void WriteVector(Editor editor, string label, Vector3d vector) =>
        editor.WriteMessage(string.Format(CultureInfo.InvariantCulture,
            "\n  {0}: ({1:G17}, {2:G17}, {3:G17})", label, vector.X, vector.Y, vector.Z));
}

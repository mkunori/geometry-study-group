using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCadGeometrySamples;

/// <summary>第3回の交点・共有部分を数式とEntity.IntersectWithで読み比べる参考コード。公式資料との静的照合のみ。</summary>
/// <remarks>
/// Windows版AutoCAD 2026 Update 1.2以降／.NET 10想定。ビルド未検証・NETLOAD未実施・AutoCAD実機未検証。
/// 入力はWCS・作図単位。2DはXY平面、3Dは右手系。図形の追加・既存図形の編集・UCS変更・保存は行わない。
/// 数式による分類とAPIの返却点を別々に表示し、実機で同じ結果を得たとは扱わない。
/// </remarks>
public static class IntersectionCommands
{
    // 既存IntersectionMath.csと同じ固定入力向けの尺度。SDK内部の許容誤差とは別。
    private const double MinimumDirectionLength = 1e-12;
    private const double AngularTolerance = 1e-12;
    private const double DistanceTolerance = 1e-9;
    private const double ParameterTolerance = 1e-12;

    /// <summary>非平行な2本のLineで、線分内部・範囲外・端点を数学と4種類の延長指定で比較する。</summary>
    /// <remarks>引数なし。交点の期待値は順に(2,2,0)、延長時のみ(3,0,0)、端点(2,0,0)。図面変更なし・実機未検証。</remarks>
    [CommandMethod("GEO03_LINES")]
    public static void LinesAndSegments()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        editor.WriteMessage("\nGEO03_LINES: 数式による分類とAPI返却点を別々に表示します（実機未検証）。図面変更なし。");
        ShowLinePair(editor, "A：線分内部", new(0, 0, 0), new(4, 4, 0), new(0, 4, 0), new(4, 0, 0));
        ShowLinePair(editor, "C：第1線分の範囲外", new(0, 0, 0), new(2, 0, 0), new(3, -1, 0), new(3, 1, 0));
        ShowLinePair(editor, "C：端点接触", new(0, 0, 0), new(2, 0, 0), new(2, -1, 0), new(2, 1, 0));
    }

    /// <summary>平行・同一直線・重複区間を点集合へ押し込まず、数式で分類する。</summary>
    /// <remarks>引数なし。重複区間の期待値は(2,0,0)〜(4,0,0)。零長とほぼ平行も表示。これらではLineを作成しない。</remarks>
    [CommandMethod("GEO03_OVERLAP")]
    public static void ParallelAndOverlap()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        editor.WriteMessage("\nGEO03_OVERLAP: 数式による補助分類（実機未検証）。平行・一致・重複にIntersectWithは使いません。");
        ShowLinePair(editor, "B：平行な別直線", new(0, 0, 0), new(4, 0, 0), new(0, 2, 0), new(2, 2, 0));
        ShowLinePair(editor, "B：同一無限直線", new(0, 0, 0), new(4, 0, 0), new(1, 0, 0), new(3, 0, 0));
        ShowLinePair(editor, "C：重複区間", new(0, 0, 0), new(4, 0, 0), new(2, 0, 0), new(6, 0, 0));
        ShowLinePair(editor, "同一直線・離れた線分", new(0, 0, 0), new(4, 0, 0), new(5, 0, 0), new(7, 0, 0));
        ShowLinePair(editor, "同一直線・端点1点", new(0, 0, 0), new(4, 0, 0), new(4, 0, 0), new(6, 0, 0));
        ShowLinePair(editor, "零長の第1線分", new(2, 0, 0), new(2, 0, 0), new(0, 0, 0), new(4, 0, 0));
        ShowLinePair(editor, "ほぼ平行", new(0, 0, 0), new(1, 0, 0), new(0, 1, 0), new(1, 1 + 1e-13, 0));
    }

    /// <summary>三角形から得た無限平面と直線の交わり、および固定した2平面の交線を数式で表示する。</summary>
    /// <remarks>引数なし。三角形の内部で切り取らない。PlaneやLine3dの交差APIは呼ばない。実機未検証。</remarks>
    [CommandMethod("GEO03_PLANES")]
    public static void LinesAndPlanes()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        Editor editor = document.Editor;
        Point3d a = new(0, 0, 2), b = new(2, 0, 2), c = new(0, 3, 2);
        Vector3d normal = a.GetVectorTo(b).CrossProduct(a.GetVectorTo(c));
        Point3d p = new(1, 2, 0);
        Vector3d direction = new(2, 0, 4);
        editor.WriteMessage("\nGEO03_PLANES: Point3d・Vector3dで数式を計算します（実機未検証）。図面変更なし。");
        WriteVector(editor, "三角形ABCの非正規化法線", normal);
        ShowLinePlane(editor, "D：一点", p, direction, a, normal);
        ShowLinePlane(editor, "平行で離れる", p, Vector3d.XAxis, a, normal);
        ShowLinePlane(editor, "直線全体が平面内", new(1, 2, 2), Vector3d.XAxis, a, normal);
        ShowLinePlane(editor, "ゼロ方向", p, new(0, 0, 0), a, normal);
        ShowLinePlane(editor, "ゼロ法線", p, direction, a, new(0, 0, 0));
        ShowLinePlane(editor, "ほぼ平行", p, new(1, 0, 1e-13), a, normal);
        ShowTwoPlaneExample(editor);
    }

    /// <summary>XY平面の2線分を、無限直線の交点式・閉区間・同一直線の区間へ分けて説明する。</summary>
    /// <param name="editor">現在の図面の出力先。</param><param name="label">固定例の名前。</param>
    /// <param name="p">第1始点。</param><param name="endP">第1終点。方向はendP-pで、正規化しない。</param>
    /// <param name="q">第2始点。</param><param name="endQ">第2終点。</param>
    /// <remarks>良条件の非平行例だけ、最後に一時LineでAPIを呼ぶ。零長・平行・重複・判定保留では呼ばない。</remarks>
    private static void ShowLinePair(Editor editor, string label, Point3d p, Point3d endP, Point3d q, Point3d endQ)
    {
        editor.WriteMessage($"\n\n{label}");
        if (!IsFinite(p) || !IsFinite(endP) || !IsFinite(q) || !IsFinite(endQ))
        { editor.WriteMessage("\n  数式：未定義入力（非有限値）。"); return; }
        if (p.Z != 0 || endP.Z != 0 || q.Z != 0 || endQ.Z != 0)
        { editor.WriteMessage("\n  対象外：この2D例はWCSのZ=0に限定。"); return; }
        Vector3d r = p.GetVectorTo(endP), s = q.GetVectorTo(endQ), w = p.GetVectorTo(q);
        double nr = r.Length, ns = s.Length;
        if (!double.IsFinite(nr) || !double.IsFinite(ns))
        { editor.WriteMessage("\n  数式：判定保留（長さの桁あふれ）。"); return; }
        if (nr <= MinimumDirectionLength || ns <= MinimumDirectionLength)
        { editor.WriteMessage("\n  数式：未定義入力（零長または方向長の下限以下）。Lineは作りません。"); return; }
        double denominator = r.CrossProduct(s).Z;
        double numeratorT = w.CrossProduct(s).Z, numeratorU = w.CrossProduct(r).Z;
        double scale = nr * ns;
        if (!IsFinite(w) || !double.IsFinite(denominator) || !double.IsFinite(numeratorT) ||
            !double.IsFinite(numeratorU) || !double.IsFinite(scale) || scale <= 0)
        { editor.WriteMessage("\n  数式：判定保留（非有限の中間値）。"); return; }
        WriteScalar(editor, "cross2(r,s)（分母）", denominator);
        if (denominator == 0)
        {
            if (numeratorU == 0)
            {
                editor.WriteMessage("\n  数式：同一無限直線（無数の共有点）。有限線分は別途区間で分類。");
                ShowCollinearInterval(editor, p, r, q, endQ);
            }
            else editor.WriteMessage(Math.Abs(numeratorU) / nr <= DistanceTolerance
                ? "\n  数式：判定保留（別直線だが距離の許容誤差内）。"
                : "\n  数式：平行な別直線。直線・有限線分とも共有点なし。");
            return;
        }
        if (Math.Abs(denominator) / scale <= AngularTolerance)
        { editor.WriteMessage("\n  数式：判定保留（ほぼ平行）。分母を0とみなして一致・なしへ分類しません。"); return; }

        double t = numeratorT / denominator, u = numeratorU / denominator;
        if (!double.IsFinite(t) || !double.IsFinite(u))
        { editor.WriteMessage("\n  数式：判定保留（媒介変数が非有限）。"); return; }
        Point3d x = At(p, r, t), other = At(q, s, u);
        if (!IsFinite(x) || !IsFinite(other) ||
            !(x.GetVectorTo(other).Length <= DistanceTolerance))
        { editor.WriteMessage("\n  数式：判定保留（交点の非有限値または残差超過）。"); return; }
        WriteScalar(editor, "分子T", numeratorT);
        WriteScalar(editor, "分子U", numeratorU);
        WriteScalar(editor, "t", t);
        WriteScalar(editor, "u", u);
        WritePoint(editor, "数式による無限直線の唯一の交点", x);
        if (t < -ParameterTolerance || t > 1 + ParameterTolerance || u < -ParameterTolerance || u > 1 + ParameterTolerance)
            editor.WriteMessage("\n  数式：有限線分では共有点なし（範囲外）。");
        else if (!InUnitInterval(t) || !InUnitInterval(u))
        { editor.WriteMessage("\n  数式：判定保留（範囲外だが境界付近）。端点へ丸めません。"); return; }
        else editor.WriteMessage(t == 0 || t == 1 || u == 0 || u == 1
            ? "\n  数式：有限線分の端点で接触する1点。" : "\n  数式：有限線分の内部で交わる1点。");
        QueryTemporaryLines(editor, p, endP, q, endQ);
    }

    /// <summary>同一直線と分かった2線分を第1線分のt座標へ射影し、共有区間を分類する。</summary>
    /// <param name="editor">出力先。</param><param name="p">第1始点。</param><param name="r">非ゼロの第1方向。</param>
    /// <param name="q">第2始点。</param><param name="endQ">第2終点。</param>
    /// <remarks>ShowLinePairによる有限性・非零長・同一直線の確認後だけ呼ぶ。IntersectWithの戻り値には依存しない。</remarks>
    private static void ShowCollinearInterval(Editor editor, Point3d p, Vector3d r, Point3d q, Point3d endQ)
    {
        double squaredLength = r.DotProduct(r);
        if (!double.IsFinite(squaredLength) || squaredLength <= 0)
        { editor.WriteMessage("\n  有限線分：判定保留（射影の分母が数値範囲外）。"); return; }
        double t0 = p.GetVectorTo(q).DotProduct(r) / squaredLength;
        double t1 = p.GetVectorTo(endQ).DotProduct(r) / squaredLength;
        if (!double.IsFinite(t0) || !double.IsFinite(t1))
        { editor.WriteMessage("\n  有限線分：判定保留（区間の射影が非有限）。"); return; }
        double low = Math.Max(0, Math.Min(t0, t1)), high = Math.Min(1, Math.Max(t0, t1));
        WriteScalar(editor, "第2始点のt", t0);
        WriteScalar(editor, "第2終点のt", t1);
        if (low > high)
        {
            editor.WriteMessage(low - high <= ParameterTolerance
                ? "\n  有限線分：判定保留（区間が離れているが境界付近）。" : "\n  有限線分：共有点なし（同一直線でも区間は離れる）。");
            return;
        }
        Point3d start = At(p, r, low), end = At(p, r, high);
        if (!IsFinite(start) || !IsFinite(end))
        { editor.WriteMessage("\n  有限線分：判定保留（区間端が非有限）。"); return; }
        editor.WriteMessage(low == high ? "\n  有限線分：端点で接触する1点。" : "\n  有限線分：重複区間（無数の共有点）。");
        WritePoint(editor, "共有部分の始端", start);
        if (low != high) WritePoint(editor, "共有部分の終端", end);
    }

    /// <summary>DB未登録の一時Lineに対し、延長指定ごとに独立した点コレクションで返却点を表示する。</summary>
    /// <param name="editor">出力先。</param><param name="p">第1始点。</param><param name="endP">第1終点。</param>
    /// <param name="q">第2始点。</param><param name="endQ">第2終点。</param>
    /// <remarks>呼出し前に非零長・有限・非平行を確認する。図面DBへの登録やTransactionは行わず、すべてusingで破棄する。</remarks>
    private static void QueryTemporaryLines(Editor editor, Point3d p, Point3d endP, Point3d q, Point3d endQ)
    {
        using (Line first = new(p, endP))
        using (Line second = new(q, endQ))
        {
            foreach (Intersect mode in new[] { Intersect.OnBothOperands, Intersect.ExtendThis, Intersect.ExtendArgument, Intersect.ExtendBoth })
            {
                try
                {
                    using (Point3dCollection points = new())
                    {
                        // Planeを受け取らないIntPtr版。GSマーカー0はサブエンティティを指定しない。
                        // 出力は追記方式なので、各呼出しに新しいコレクションを用意する。
                        first.IntersectWith(second, mode, points, IntPtr.Zero, IntPtr.Zero);
                        editor.WriteMessage($"\n  API {mode}: 返却要素数={points.Count}（未検証。重複区間の分類ではありません）。");
                        if (points.Count == 0) editor.WriteMessage(" 点コレクションは空。");
                        if (points.Count > 1) editor.WriteMessage(" この非平行Line例では分類保留。複数の返却点をすべて表示し、順序・重複は保証しません。");
                        foreach (Point3d candidate in points)
                        {
                            WritePoint(editor, "API返却点", candidate);
                            if (!IsFinite(candidate)) { editor.WriteMessage(" 非有限値のため分類保留。"); continue; }
                            Vector3d r = p.GetVectorTo(endP), s = q.GetVectorTo(endQ);
                            double rr = r.DotProduct(r), ss = s.DotProduct(s);
                            if (!double.IsFinite(rr) || !double.IsFinite(ss) || rr <= 0 || ss <= 0)
                            { editor.WriteMessage("\n    射影の分母が数値範囲外のため分類保留。"); continue; }
                            double t = p.GetVectorTo(candidate).DotProduct(r) / rr;
                            double u = q.GetVectorTo(candidate).DotProduct(s) / ss;
                            WriteScalar(editor, "返却点を射影して求めたt", t);
                            WriteScalar(editor, "返却点を射影して求めたu", u);
                            if (!double.IsFinite(t) || !double.IsFinite(u))
                            { editor.WriteMessage("\n    媒介変数が非有限のため分類保留。"); continue; }
                            WriteScalar(editor, "第1直線への残差長", candidate.GetVectorTo(At(p, r, t)).Length);
                            WriteScalar(editor, "第2直線への残差長", candidate.GetVectorTo(At(q, s, u)).Length);
                            editor.WriteMessage($"\n    閉線分の範囲内: t={InUnitInterval(t)}, u={InUnitInterval(u)}。残差も別途確認してください。");
                        }
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception error)
                {
                    // 例外を「交点なし」と言い換えない。コードごとの例外条件は実機未確認。
                    editor.WriteMessage($"\n  API {mode}: 例外のため結果未取得。{error.Message}");
                }
            }
        }
    }

    /// <summary>無限直線と点・法線で表した無限平面を、分母・分子・残差で分類する。</summary>
    /// <param name="editor">出力先。</param><param name="label">固定例の名前。</param>
    /// <param name="p">直線上の点。</param><param name="d">直線方向。</param>
    /// <param name="a">平面上の点。</param><param name="n">非正規化でもよい平面法線。</param>
    /// <remarks>三角形の境界では切らない。ほぼ平行はなし・平面内へ丸めず、判定保留にする。</remarks>
    private static void ShowLinePlane(Editor editor, string label, Point3d p, Vector3d d, Point3d a, Vector3d n)
    {
        editor.WriteMessage($"\n\n{label}");
        if (!IsFinite(p) || !IsFinite(d) || !IsFinite(a) || !IsFinite(n))
        { editor.WriteMessage("\n  未定義入力（非有限値）。"); return; }
        if (n.X == 0 && n.Y == 0 && n.Z == 0)
        { editor.WriteMessage("\n  未定義入力（ゼロ法線では平面を定められない）。"); return; }
        double nd = d.Length, nn = n.Length;
        if (!double.IsFinite(nd) || !double.IsFinite(nn) || nn == 0)
        { editor.WriteMessage("\n  判定保留（長さの数値範囲外）。"); return; }
        if (nd <= MinimumDirectionLength)
        { editor.WriteMessage("\n  未定義入力（ゼロ方向または下限以下）。"); return; }
        double denominator = n.DotProduct(d), numerator = n.DotProduct(p.GetVectorTo(a)), scale = nn * nd;
        if (!double.IsFinite(denominator) || !double.IsFinite(numerator) || !double.IsFinite(scale) || scale <= 0)
        { editor.WriteMessage("\n  判定保留（非有限の中間値）。"); return; }
        WriteScalar(editor, "分母 n・d", denominator);
        WriteScalar(editor, "分子 n・(A-P)", numerator);
        if (denominator == 0)
        {
            editor.WriteMessage(numerator == 0 ? "\n  直線全体が平面内（無数の共有点）。"
                : Math.Abs(numerator) / nn <= DistanceTolerance ? "\n  判定保留（平行だが平面の距離許容誤差内）。" : "\n  平行で共有点なし。");
            return;
        }
        if (Math.Abs(denominator) / scale <= AngularTolerance)
        { editor.WriteMessage("\n  判定保留（ほぼ平行）。小さな分母で割りません。"); return; }
        double t = numerator / denominator;
        if (!double.IsFinite(t)) { editor.WriteMessage("\n  判定保留（媒介変数が非有限）。"); return; }
        Point3d point = At(p, d, t);
        double residual = n.DotProduct(a.GetVectorTo(point)) / nn;
        if (!IsFinite(point) || !(Math.Abs(residual) <= DistanceTolerance))
        { editor.WriteMessage("\n  判定保留（交点が非有限または平面の残差超過）。"); return; }
        editor.WriteMessage("\n  無限直線と無限平面の唯一の交点。");
        WriteScalar(editor, "t", t);
        WritePoint(editor, "交点P+td", point);
        WriteScalar(editor, "平面への距離残差", residual);
        editor.WriteMessage($"\n  P〜P+dの閉線分でも範囲内: {InUnitInterval(t)}（有限の三角形面との交差判定ではありません）。");
    }

    /// <summary>z=2とx=1だけを対象に、外積の方向と制約から選んだ通過点で無限交線を表示する。</summary>
    /// <param name="editor">出力先。</param>
    /// <remarks>汎用の2平面交差APIではない。位置は外積から求めず、x=1・z=2・y=0から選ぶ。</remarks>
    private static void ShowTwoPlaneExample(Editor editor)
    {
        Vector3d n1 = new(0, 0, 1), n2 = new(1, 0, 0);
        Vector3d direction = n1.CrossProduct(n2);
        Point3d through = new(1, 0, 2);
        editor.WriteMessage("\n\n発展：z=2とx=1の共有部分は無限直線(1,t,2)。以下はこの固定例だけの手計算対応。");
        WriteVector(editor, "交線方向n1×n2", direction);
        WritePoint(editor, "制約を満たす通過点", through);
        WritePoint(editor, "交線上の別の点（t=1）", At(through, direction, 1));
        WriteScalar(editor, "通過点のz-2", through.Z - 2);
        WriteScalar(editor, "通過点のx-1", through.X - 1);
        WriteScalar(editor, "n1と交線方向の内積", n1.DotProduct(direction));
        WriteScalar(editor, "n2と交線方向の内積", n2.DotProduct(direction));
    }

    /// <summary>媒介変数表示P+tdを成分で計算する。方向を正規化せず、tの意味を維持する。</summary>
    /// <param name="p">基準点。</param><param name="d">方向。</param><param name="t">無次元の媒介変数。</param>
    /// <returns>計算した点。有限性は呼出し側で検査する。</returns>
    private static Point3d At(Point3d p, Vector3d d, double t) => new(p.X + t * d.X, p.Y + t * d.Y, p.Z + t * d.Z);

    /// <summary>有限なtが両端を含む[0,1]にあるか調べる。範囲を許容誤差で拡張しない。</summary>
    /// <param name="t">終点までの変位に対する媒介変数。</param><returns>閉区間内ならtrue。</returns>
    private static bool InUnitInterval(double t) => double.IsFinite(t) && t >= 0 && t <= 1;

    /// <summary>位置の全成分が有限かを検査する。</summary>
    /// <param name="p">検査する点。</param><returns>NaN・Infinityを含まない場合はtrue。</returns>
    private static bool IsFinite(Point3d p) => double.IsFinite(p.X) && double.IsFinite(p.Y) && double.IsFinite(p.Z);

    /// <summary>方向または法線の全成分が有限かを検査する。</summary>
    /// <param name="v">検査する方向・法線。</param><returns>NaN・Infinityを含まない場合はtrue。</returns>
    private static bool IsFinite(Vector3d v) => double.IsFinite(v.X) && double.IsFinite(v.Y) && double.IsFinite(v.Z);

    /// <summary>微小な残差をゼロへ置換せず、スカラーを有効数字17桁で表示する。</summary>
    /// <param name="editor">出力先。</param><param name="label">量の名前。</param><param name="value">値。</param>
    private static void WriteScalar(Editor editor, string label, double value) =>
        editor.WriteMessage($"\n  {label}: {value.ToString("G17", CultureInfo.InvariantCulture)}");

    /// <summary>WCSの点を有効数字17桁・小数点がピリオドの表記で表示する。</summary>
    /// <param name="editor">出力先。</param><param name="label">点の役割。</param><param name="p">位置。</param>
    private static void WritePoint(Editor editor, string label, Point3d p) =>
        editor.WriteMessage(string.Format(CultureInfo.InvariantCulture, "\n  {0}: ({1:G17}, {2:G17}, {3:G17})", label, p.X, p.Y, p.Z));

    /// <summary>方向・非正規化法線を有効数字17桁で表示し、点とはラベルで区別する。</summary>
    /// <param name="editor">出力先。</param><param name="label">方向・法線の役割。</param><param name="v">方向・法線。</param>
    private static void WriteVector(Editor editor, string label, Vector3d v) =>
        editor.WriteMessage(string.Format(CultureInfo.InvariantCulture, "\n  {0}: ({1:G17}, {2:G17}, {3:G17})", label, v.X, v.Y, v.Z));
}

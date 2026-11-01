using static ModelingMath;

/// <summary>0始まりの順序付き頂点番号。BとCの交換で表裏が反転する。</summary>
/// <param name="A">基準頂点番号。</param><param name="B">第2頂点番号。</param><param name="C">第3頂点番号。</param>
internal readonly record struct Triangle(int A, int B, int C);

/// <summary>位置の配列と三角形接続の配列を別々に持つ教材用メッシュ。</summary>
/// <param name="Vertices">世界座標の共有頂点。</param><param name="Triangles">外向きの順序を持つ三角形。</param>
/// <remarks>配列はデバッガで調べやすいよう保持する。編集後は幾何・接続を再検証する。B-repではない。</remarks>
internal sealed record Mesh(V3[] Vertices, Triangle[] Triangles);

/// <summary>退化検査後の三角形の外積・単位法線・面積。</summary>
/// <param name="AreaVector">2辺の外積。長さは面積の2倍。</param><param name="UnitNormal">長さ1の法線。</param>
/// <param name="Area">長さの2乗単位の三角形面積。</param>
internal readonly record struct FaceGeometry(V3 AreaVector, V3 UnitNormal, double Area);

/// <summary>単純な反時計回りの凸四角形を、単位法線方向へ垂直押し出しする。</summary>
/// <remarks>非凸・穴あり・自己交差輪郭、任意のスイープやCADソリッドを生成するAPIではない。</remarks>
internal static class MeshBuilder
{
    /// <summary>始端4点、終端4点の順で共有頂点を作る。</summary>
    /// <param name="section">+T側から見て反時計回りの凸四角形。始点の重複保存は禁止。</param>
    /// <param name="closed">末尾と先頭の辺を閉じる意図を明示する。falseは拒否。</param>
    /// <param name="frame">始端の右手系直交単位フレーム。</param><param name="length">T方向の正の長さ。</param>
    /// <returns>番号iが始端、i+4が対応する終端となる8頂点。</returns>
    /// <remarks>無効・下限以下・非有限入力や非有限な生成結果はArgumentException。</remarks>
    internal static V3[] CreateVertices(IReadOnlyList<V2> section, bool closed, SectionFrame frame, double length)
    {
        ValidateFrame(frame);
        if (!closed || section.Count != 4) throw new ArgumentException("閉じた凸四角形の4点が必要です。", nameof(section));
        if (!double.IsFinite(length) || length <= MinimumLength) throw new ArgumentException("押し出し長が無効です。", nameof(length));
        _ = SignedArea(section); // 非有限な座標・面積計算を、向きの判定に使わない。
        for (int i = 0; i < 4; i++)
        {
            for (int j = i + 1; j < 4; j++)
            {
                double du = section[j].U - section[i].U, dv = section[j].V - section[i].V;
                double distance = Math.Sqrt(du * du + dv * dv);
                if (!double.IsFinite(distance) || distance <= MinimumLength)
                    throw new ArgumentException("重複点または下限以下の点間距離があります。", nameof(section));
            }
            V2 a = section[i], b = section[(i + 1) % 4], c = section[(i + 2) % 4];
            double turn = (b.U - a.U) * (c.V - b.V) - (b.V - a.V) * (c.U - b.U);
            // 4つの角で厳密に左へ曲がる輪郭に限定する。非凸・交差・逆順を補正しない。
            if (!double.IsFinite(turn) || turn <= MinimumArea2)
                throw new ArgumentException("非退化で反時計回りの凸四角形が必要です。", nameof(section));
        }
        V3[] vertices = new V3[8];
        for (int i = 0; i < 4; i++)
        {
            vertices[i] = Place(frame, section[i]);
            vertices[i + 4] = Add(vertices[i], Scale(frame.T, length));
            if (!Finite(vertices[i + 4])) throw new ArgumentException("終端の座標が非有限です。", nameof(length));
        }
        return vertices;
    }

    /// <summary>側面8枚と始終端4枚、合計12三角形のメッシュを作る。</summary>
    /// <param name="section">閉輪郭として扱う反時計回りの凸四角形。</param><param name="closed">trueが必要。</param>
    /// <param name="frame">始端の右手系フレーム。</param><param name="length">正の押し出し長。</param>
    /// <returns>共有8頂点と外向き12三角形。</returns>
    /// <remarks>生成後にも三角形の退化を検査する。世界座標での丸めによるつぶれも拒否する。</remarks>
    internal static Mesh ExtrudeConvexQuad(IReadOnlyList<V2> section, bool closed, SectionFrame frame, double length)
    {
        V3[] vertices = CreateVertices(section, closed, frame, length);
        List<Triangle> triangles = [];
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            // 輪郭の内部は左側、e×Tは右側。対角線の両側を同じ外向きで覆う。
            triangles.Add(new(i, j, j + 4));
            triangles.Add(new(i, j + 4, i + 4));
        }
        triangles.AddRange([new(0, 2, 1), new(0, 3, 2), new(4, 5, 6), new(4, 6, 7)]);
        Mesh mesh = new(vertices, triangles.ToArray());
        foreach (Triangle face in mesh.Triangles) _ = AnalyzeTriangle(mesh.Vertices, face);
        return mesh;
    }

    /// <summary>有限性・頂点番号・退化を調べてから単位法線と面積を求める。</summary>
    /// <param name="vertices">世界座標の頂点一覧。</param><param name="triangle">0始まりの順序付き番号。</param>
    /// <returns>外積、単位法線、三角形面積。</returns>
    /// <remarks>面積の2倍が下限以下、非有限な演算、範囲外番号等はArgumentException。失敗値を法線にしない。</remarks>
    internal static FaceGeometry AnalyzeTriangle(IReadOnlyList<V3> vertices, Triangle triangle)
    {
        var (a, b, c) = triangle;
        if (a < 0 || b < 0 || c < 0 || a >= vertices.Count || b >= vertices.Count || c >= vertices.Count ||
            a == b || b == c || c == a || !Finite(vertices[a]) || !Finite(vertices[b]) || !Finite(vertices[c]))
            throw new ArgumentException("三角形の頂点番号・座標が無効です。", nameof(triangle));
        V3 areaVector = Cross(Sub(vertices[b], vertices[a]), Sub(vertices[c], vertices[a]));
        double area2 = Length(areaVector);
        // 外積はまず面積の退化検査に使う。ゼロ・非有限の値を正規化しない。
        if (!Finite(areaVector) || !double.IsFinite(area2) || area2 <= MinimumArea2)
            throw new ArgumentException("三角形が退化または計算範囲外です。", nameof(triangle));
        return new(areaVector, Scale(areaVector, 1 / area2), area2 / 2);
    }

    /// <summary>各無向辺が2回、有向辺が逆向きで共有されているかだけを検査する。</summary>
    /// <param name="mesh">共有頂点方式のメッシュ。</param><returns>空でなく、全辺が対になればtrue。</returns>
    /// <remarks>自己交差・頂点周りの非多様体接続・外向き・幾何的な退化を保証する検査ではない。</remarks>
    internal static bool HasPairedOppositeEdges(Mesh mesh)
    {
        if (mesh.Triangles.Length == 0) return false;
        Dictionary<(int, int), (int Count, int Balance)> edges = [];
        foreach (var (a, b, c) in mesh.Triangles)
        {
            if (a < 0 || b < 0 || c < 0 || a >= mesh.Vertices.Length || b >= mesh.Vertices.Length || c >= mesh.Vertices.Length ||
                a == b || b == c || c == a) return false;
            foreach (var (from, to) in new[] { (a, b), (b, c), (c, a) })
            {
                var key = (Math.Min(from, to), Math.Max(from, to));
                var current = edges.GetValueOrDefault(key);
                edges[key] = (current.Count + 1, current.Balance + (from < to ? 1 : -1));
            }
        }
        return edges.Values.All(e => e.Count == 2 && e.Balance == 0);
    }

    /// <summary>基準点oからの符号付き四面体、(a-o)・((b-o)×(c-o))/6を足す。</summary>
    /// <param name="mesh">辺の向きが対となる、退化していない有向メッシュ。</param>
    /// <returns>外向きなら正、全体が内向きなら負となる符号付き体積。</returns>
    /// <remarks>教材では単純な凸直柱の検算に限る。自己交差等の有効ソリッド検査ではない。無効入力はArgumentException。</remarks>
    internal static double SignedVolume(Mesh mesh)
    {
        if (!HasPairedOppositeEdges(mesh)) throw new ArgumentException("辺が逆向きの対になっていません。", nameof(mesh));
        V3 origin = mesh.Vertices[0];
        double sum = 0;
        foreach (var face in mesh.Triangles)
        {
            _ = AnalyzeTriangle(mesh.Vertices, face);
            // 近くの基準点を使い、大きな共通平行移動による不要な相殺を減らす。
            V3 a = Sub(mesh.Vertices[face.A], origin), b = Sub(mesh.Vertices[face.B], origin), c = Sub(mesh.Vertices[face.C], origin);
            sum += Dot(a, Cross(b, c)) / 6;
        }
        if (!double.IsFinite(sum)) throw new ArgumentException("体積計算が非有限です。", nameof(mesh));
        return sum;
    }
}

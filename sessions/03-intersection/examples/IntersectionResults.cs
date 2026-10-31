global using V2 = (double X, double Y);
global using V3 = (double X, double Y, double Z);

/// <summary>共有部分と、入力・数値計算の問題を区別する分類。</summary>
internal enum IntersectionKind
{
    /// <summary>有効な図形に共有点がない。</summary>
    None,
    /// <summary>唯一の共有点。</summary>
    Point,
    /// <summary>無限直線どうしの一致、直線全体が平面内、または平面どうしの一致。</summary>
    Coincident,
    /// <summary>両端が異なる重複線分。</summary>
    OverlapSegment,
    /// <summary>非平行2平面の交線。通過点と方向を持つ。</summary>
    Line,
    /// <summary>ゼロ方向・ゼロ法線、または教材の方向長の下限以下。</summary>
    Degenerate,
    /// <summary>入力にNaNまたはInfinityが含まれる。</summary>
    InvalidInput,
    /// <summary>ほぼ平行、境界付近、非有限な中間値、残差超過などで判定を保留。</summary>
    Uncertain
}

/// <summary>2Dの共有部分。Kindで意味のあるフィールドを選ぶ。</summary>
/// <param name="Kind">交差の分類。</param>
/// <param name="Point">Pointの場合だけ共有点。それ以外はnull。</param>
/// <param name="T">Pointの場合の第1直線の媒介変数。それ以外はnull。</param>
/// <param name="U">Pointの場合の第2直線の媒介変数。それ以外はnull。</param>
/// <param name="OverlapStart">OverlapSegmentの場合だけ、第1線分のtが小さい側の端。</param>
/// <param name="OverlapEnd">OverlapSegmentの場合だけ、第1線分のtが大きい側の端。</param>
/// <remarks>一点でない状態を原点で代用しない。重複区間の端の順序はX座標順とは限らない。</remarks>
internal readonly record struct Intersection2(
    IntersectionKind Kind, V2? Point = null, double? T = null, double? U = null,
    V2? OverlapStart = null, V2? OverlapEnd = null);

/// <summary>3D直線と平面、または2平面の共有部分。</summary>
/// <param name="Kind">交差の分類。Coincidentの対象は呼び出した演算に従う。</param>
/// <param name="Point">Pointでは唯一の交点、Lineでは交線上の1点。それ以外はnull。</param>
/// <param name="T">Pointの場合だけ、入力直線の媒介変数。</param>
/// <param name="Direction">Lineの場合だけ交線方向。必ずしも単位長ではない。</param>
/// <remarks>Lineは無数の共有点を表す。有限面の境界による切り取りは扱わない。</remarks>
internal readonly record struct Intersection3(
    IntersectionKind Kind, V3? Point = null, double? T = null, V3? Direction = null);

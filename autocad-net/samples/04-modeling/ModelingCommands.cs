using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCadGeometrySamples;

/// <summary>第4回の長方形断面をPolyline・Region・Solid3dへ対応させる参考コード。公式資料との静的照合のみ。</summary>
/// <remarks>
/// Windows版AutoCAD 2026 Update 1.2以降／.NET 10想定。ビルド未検証・NETLOAD未実施・AutoCAD実機未検証。
/// ローカルXYで生成してから、X/Y/Z基底をN=+Y、B=+Z、T=+Xへ配置する。現在のUCSは使わない。
/// 長さは作図単位。既存図形を消去・編集せず、新規図形1つをモデル空間へ追加する。保存は行わない。
/// </remarks>
public static class ModelingCommands
{
    private const double ExtrusionLength = 5;

    /// <summary>幅2・高さ1の閉じた長方形Polylineを、WCSのX=0のYZ平面へ1つ追加する。</summary>
    /// <remarks>引数なし。数学上の断面積は2、法線は+X。再実行は重複追加。表示・DB登録とも実機未検証。</remarks>
    [CommandMethod("GEO04_PROFILE")]
    public static void AddProfile()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        try
        {
            using (Polyline profile = CreateLocalProfile())
            {
                profile.TransformBy(LocalToWorld());
                AppendToModelSpace(document, profile);
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        {
            document.Editor.WriteMessage($"\nGEO04_PROFILE: AutoCADの処理で例外が発生しました。図面状態を確認してください。{error.Message}");
            return;
        }
        document.Editor.WriteMessage("\nGEO04_PROFILE: X=0のYZ平面に閉じたPolylineを1つ追加しました。Y範囲[-1,1]、Z範囲[-0.5,0.5]は数学上の期待値（実機未検証）。");
    }

    /// <summary>閉じた断面から一時Regionを作り、長さ5の直柱Solid3dをモデル空間へ1つ追加する。</summary>
    /// <remarks>引数なし。WCSのX=0〜5、Y=-1〜1、Z=-0.5〜0.5、体積10が数学上の期待値。ビルド・実機未検証。</remarks>
    [CommandMethod("GEO04_EXTRUDE")]
    public static void AddExtrusion()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        CreateAndAddSolid(document, false);
    }

    /// <summary>発展例の最初の直線区間だけを対象に、同じ長方形RegionをスイープしたSolid3dを1つ追加する。</summary>
    /// <remarks>
    /// 引数なし。最終経路はWCSの(0,0,0)→(5,0,0)で、押し出しと同位置の直柱を意図する。
    /// (5,0,0)→(5,4,0)の曲がり角は生成しない。成功は保証せず、APIの結果・例外とも実機未検証。
    /// </remarks>
    [CommandMethod("GEO04_SWEEP")]
    public static void AddStraightSweep()
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null) return;
        CreateAndAddSolid(document, true);
    }

    /// <summary>ローカルXY平面に、反時計回りの4頂点とClosedで長方形輪郭を作る。</summary>
    /// <returns>DB未登録のPolyline。所有権は呼出し側へ渡し、呼出し側のusingで破棄する。</returns>
    /// <remarks>ECSの(u,v)を指定する。法線+Z・標高0なので、この生成段階ではECSとWCSのXYが一致する。</remarks>
    private static Polyline CreateLocalProfile()
    {
        Polyline profile = new();
        try
        {
            profile.Normal = Vector3d.ZAxis;
            profile.Elevation = 0;
            // bulgeと線幅は0。始点を5番目に重複保存せず、Closedが最後と最初を結ぶ。
            profile.AddVertexAt(0, new Point2d(-1, -0.5), 0, 0, 0);
            profile.AddVertexAt(1, new Point2d(1, -0.5), 0, 0, 0);
            profile.AddVertexAt(2, new Point2d(1, 0.5), 0, 0, 0);
            profile.AddVertexAt(3, new Point2d(-1, 0.5), 0, 0, 0);
            profile.Closed = true;
            return profile;
        }
        catch
        {
            // 呼出し側へ所有権を渡す前に設定が失敗した場合も、新規Polylineを残さない。
            profile.Dispose();
            throw;
        }
    }

    /// <summary>ローカルXY断面と+Z方向の直柱を、教材のN/B/Tフレームへ配置する行列を返す。</summary>
    /// <returns>標準フレームからC=原点、N=+Y、B=+Z、T=+Xへの、右手系の剛体変換。</returns>
    /// <remarks>列方式で(u,v,w)を(w,u,v)へ写す。反射や拡大縮小を含まず、体積と向きを保つ。</remarks>
    private static Matrix3d LocalToWorld() => Matrix3d.AlignCoordinateSystem(
        Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
        Point3d.Origin, Vector3d.YAxis, Vector3d.ZAxis, Vector3d.XAxis);

    /// <summary>固定断面のRegionからローカルSolid3dを生成し、配置後に図面へ登録する。</summary>
    /// <param name="document">通常の非Sessionコマンドで取得した現在図面。</param>
    /// <param name="sweep">trueは直線スイープ、falseは方向ベクトルによる押し出し。</param>
    /// <remarks>Solid3dの所有権はここで持ち、usingで閉じる。Commit後の例外まで変更なしとは断定しない。</remarks>
    private static void CreateAndAddSolid(Document document, bool sweep)
    {
        string command = sweep ? "GEO04_SWEEP" : "GEO04_EXTRUDE";
        try
        {
            using (Solid3d solid = new())
            {
                BuildLocalSolid(solid, sweep);
                solid.TransformBy(LocalToWorld());
                AppendToModelSpace(document, solid);
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception error)
        {
            document.Editor.WriteMessage($"\n{command}: AutoCADの生成・登録処理で例外が発生しました。図面状態を確認してください。{error.Message}");
            return;
        }
        catch (InvalidOperationException error)
        {
            document.Editor.WriteMessage($"\n{command}: 固定した長方形の生成条件と一致しないため中止しました。{error.Message}");
            return;
        }
        document.Editor.WriteMessage($"\n{command}: モデル空間へSolid3dを1つ追加しました。X=0〜5、Y=-1〜1、Z=-0.5〜0.5、断面積2・体積10は数学上の期待値（APIでの体積測定・実機確認は未実施）。");
    }

    /// <summary>一時Polyline→4本のLine→Region→Solid3dの順に、ローカルで形を作る。</summary>
    /// <param name="solid">呼出し側が所有する空のSolid3d。ここではDB登録・Disposeしない。</param>
    /// <param name="sweep">trueは+Zへの直線経路、falseは+Z方向ベクトルを使う。</param>
    /// <remarks>戻る前に全一時曲線・全生成Region・コレクション・オプション・経路を破棄する。</remarks>
    private static void BuildLocalSolid(Solid3d solid, bool sweep)
    {
        using (Polyline profile = CreateLocalProfile())
        using (DBObjectCollection curves = new())
        {
            try
            {
                // CreateFromCurvesのReferenceの許可型にLineが明記されているため、Polylineを直接渡さない。
                // Explodeは元の輪郭を消さず、DB未登録の生成物を追記する。部分的な生成後の例外もfinallyで片付ける。
                profile.Explode(curves);
                if (curves.Count != 4) throw new InvalidOperationException("輪郭の分解結果が4本ではありません。");
                foreach (DBObject curve in curves)
                    if (curve is not Line) throw new InvalidOperationException("直線辺以外が生成されました。");

                // 既存図形をForWriteで開いて入力しない。ここで渡すのはDB未登録の一時Lineだけ。
                using (DBObjectCollection regions = Region.CreateFromCurves(curves))
                {
                    try
                    {
                        if (regions.Count != 1 || regions[0] is not Region region)
                            throw new InvalidOperationException("単一の長方形Regionが生成されませんでした。");
                        using (SweepOptions options = CreateOptions(sweep))
                        {
                            if (sweep)
                            {
                                // ローカルXYに垂直な直線だけ。折れ線の角・ねじれ・姿勢補間は実装しない。
                                using (Line path = new(Point3d.Origin, new Point3d(0, 0, ExtrusionLength)))
                                    solid.CreateSweptSolid(region, path, options);
                            }
                            else
                            {
                                // directionVectorの向きが押し出し方向、長さが押し出し距離。
                                solid.CreateExtrudedSolid(region, new Vector3d(0, 0, ExtrusionLength), options);
                            }
                        }
                    }
                    finally
                    {
                        // Count異常・型違い・生成失敗でも、返された全要素を処理する。RegionはDBに追加しない。
                        DisposeContents(regions);
                    }
                }
            }
            finally
            {
                DisposeContents(curves);
            }
        }
    }

    /// <summary>テーパー・拡縮・ねじれを加えない局所的なオプションを作る。</summary>
    /// <param name="sweep">スイープでは公式の要求に従いAlignSweepEntityToPathを指定する。</param>
    /// <returns>呼出し側のusingで破棄するSweepOptions。Builderは通常のマネージクラス。</returns>
    /// <remarks>断面中心と直線経路の始点はどちらもローカル原点。曲がる経路の姿勢制御には一般化しない。</remarks>
    private static SweepOptions CreateOptions(bool sweep)
    {
        SweepOptionsBuilder builder = new()
        {
            Align = sweep ? SweepOptionsAlignOption.AlignSweepEntityToPath : SweepOptionsAlignOption.NoAlignment,
            BasePoint = Point3d.Origin,
            DraftAngle = 0,
            ScaleFactor = 1,
            TwistAngle = 0
        };
        return builder.ToSweepOptions();
    }

    /// <summary>DB未登録の生成物を、取り出してラッパーを作った要素も含めて個別に破棄する。</summary>
    /// <param name="objects">この例が所有し、DBには登録していないLineまたはRegionのコレクション。</param>
    /// <remarks>コレクション自身は呼出し側のusingで破棄する。DBへ移したEntityには使わない。</remarks>
    private static void DisposeContents(DBObjectCollection objects)
    {
        foreach (DBObject item in objects) item.Dispose();
    }

    /// <summary>呼出し側が所有する新規Entityを、現在図面のモデル空間へ登録して確定する。</summary>
    /// <param name="document">現在の通常コマンドの図面。</param>
    /// <param name="entity">配置と生成を済ませた新規PolylineまたはSolid3d。</param>
    /// <remarks>Transactionはここでusing管理し、Entityのラッパーは呼出し側のusingで閉じる。保存は行わない。</remarks>
    private static void AppendToModelSpace(Document document, Entity entity)
    {
        using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
        {
            BlockTable blocks = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
            BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            // #69と同じ順序。DB追加後に新規DBObjectをTransactionへ登録する。
            modelSpace.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
            transaction.Commit();
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.IO;
using MmdCameraPlugin.Vmd;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Effects;
using YukkuriMovieMaker.Settings;

namespace MmdCameraPlugin;

/// <summary>
/// MMD カメラモーション (VMD) を読み込み、YMM4 のカメラ行列として適用する映像エフェクト。
/// グループ制御に標準カメラの代わり（または後段）として挿して使う。
/// </summary>
[VideoEffect("MMDカメラ", ["描画"], ["MMD", "VMD", "カメラ", "camera"], IsAviUtlSupported = false)]
[PluginDetails(AuthorName = "Mitac")]
public class MmdCameraEffect : VideoEffectBase
{
    public override string Label => "MMDカメラ";

    [Display(GroupName = "VMD", Name = "カメラVMD", Description = "MMDのカメラモーション(.vmd)ファイル")]
    [FileSelector(FileGroupType.None, CustomFilterName = "VMDファイル", CustomFilterValue = "*.vmd")]
    public string VmdPath
    {
        get => vmdPath;
        set
        {
            if (Set(ref vmdPath, value ?? string.Empty))
                InvalidateMotionCache();
        }
    }
    string vmdPath = string.Empty;

    [Display(GroupName = "タイミング", Name = "オフセット", Description = "タイムライン時刻から換算したVMDフレームに加算するオフセット（VMDフレーム単位）")]
    [AnimationSlider("F0", "f", -10000, 10000)]
    public Animation FrameOffset { get; } = new Animation(0, -1000000, 1000000);

    [Display(GroupName = "タイミング", Name = "VMD FPS", Description = "VMD作成時のフレームレート（通常30）。タイムラインFPSと違う場合は時間が揃うよう換算する")]
    [AnimationSlider("F0", "fps", 1, 120)]
    public Animation VmdFps { get; } = new Animation(30, 1, 240);

    [Display(GroupName = "変換", Name = "スケール", Description = "MMD座標→YMM4座標の倍率。キャラの見た目サイズに合わせて調整")]
    [AnimationSlider("F2", "x", 0.1, 100)]
    public Animation Scale { get; } = new Animation(12.5, 0.001, 10000);

    [Display(GroupName = "変換", Name = "オフセットX", Description = "注視点の平行移動（YMM4座標）")]
    [AnimationSlider("F1", "px", -2000, 2000)]
    public Animation OffsetX { get; } = new Animation(0, YMM4Constants.VerySmallValue, YMM4Constants.VeryLargeValue);

    [Display(GroupName = "変換", Name = "オフセットY", Description = "注視点の平行移動（YMM4座標）")]
    [AnimationSlider("F1", "px", -2000, 2000)]
    public Animation OffsetY { get; } = new Animation(0, YMM4Constants.VerySmallValue, YMM4Constants.VeryLargeValue);

    [Display(GroupName = "変換", Name = "オフセットZ", Description = "注視点の平行移動（YMM4座標・UIのZ）")]
    [AnimationSlider("F1", "px", -2000, 2000)]
    public Animation OffsetZ { get; } = new Animation(0, YMM4Constants.VerySmallValue, YMM4Constants.VeryLargeValue);

    [Display(GroupName = "変換", Name = "左右反転", Description = "カメラモーションを左右反転する（注視点X・ヨー・ロールを反転）")]
    [ToggleSlider]
    public bool FlipHorizontal { get => flipHorizontal; set => Set(ref flipHorizontal, value); }
    bool flipHorizontal = true;

    [Display(GroupName = "カメラ", Name = "FOVを適用", Description = "VMDの画角をYMM4のパース（PerspectiveDistance）へ反映する")]
    [ToggleSlider]
    public bool ApplyFov { get => applyFov; set => Set(ref applyFov, value); }
    bool applyFov = true;

    [Display(GroupName = "カメラ", Name = "既存カメラに合成", Description = "ON=手前のカメラエフェクト結果へ掛け算 / OFF=VMDカメラで置き換え")]
    [ToggleSlider]
    public bool CompositeWithExisting { get => compositeWithExisting; set => Set(ref compositeWithExisting, value); }
    bool compositeWithExisting;

    string? cachedPath;
    DateTime cachedWriteTime;
    VmdCameraMotion? cachedMotion;
    string? cachedError;

    internal VmdCameraMotion? GetMotion(out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(vmdPath) || !File.Exists(vmdPath))
        {
            error = string.IsNullOrWhiteSpace(vmdPath) ? null : "VMDファイルが見つかりません。";
            return null;
        }

        var writeTime = File.GetLastWriteTimeUtc(vmdPath);
        if (cachedMotion is not null && cachedPath == vmdPath && cachedWriteTime == writeTime)
        {
            error = cachedError;
            return cachedMotion;
        }

        try
        {
            var keys = VmdCameraParser.Load(vmdPath);
            cachedMotion = new VmdCameraMotion(keys);
            cachedError = keys.Count == 0 ? "カメラキーフレームがありません。" : null;
        }
        catch (Exception ex)
        {
            cachedMotion = new VmdCameraMotion([]);
            cachedError = ex.Message;
        }

        cachedPath = vmdPath;
        cachedWriteTime = writeTime;
        error = cachedError;
        return cachedMotion;
    }

    void InvalidateMotionCache()
    {
        cachedPath = null;
        cachedMotion = null;
        cachedError = null;
    }

    public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];

    public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices) =>
        new MmdCameraProcessor(this);

    protected override IEnumerable<IAnimatable> GetAnimatables() =>
        [FrameOffset, VmdFps, Scale, OffsetX, OffsetY, OffsetZ];
}

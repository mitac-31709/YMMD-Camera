using System.Numerics;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace MmdCameraPlugin;

/// <summary>
/// VMD カメラポーズを YMM4 の Camera / PerspectiveDistance へ書き込む。
/// 行列の組み立ては標準の「注視点カメラ」(CameraLookAtEffect) と同じ流儀。
/// </summary>
internal sealed class MmdCameraProcessor : IVideoEffectProcessor
{
    readonly MmdCameraEffect item;
    ID2D1Image? input;

    public ID2D1Image Output => input ?? throw new NullReferenceException($"{nameof(input)} is null");

    public MmdCameraProcessor(MmdCameraEffect item)
    {
        this.item = item;
    }

    public void SetInput(ID2D1Image? input) => this.input = input;

    public void ClearInput() => input = null;

    public DrawDescription Update(EffectDescription effectDescription)
    {
        var drawDesc = effectDescription.DrawDescription;
        var motion = item.GetMotion(out _);
        if (motion is null || motion.IsEmpty)
            return drawDesc;

        int frame = effectDescription.ItemPosition.Frame;
        int length = effectDescription.ItemDuration.Frame;
        int fps = Math.Max(1, effectDescription.FPS);

        float start = (float)item.StartFrame.GetValue(frame, length, fps);
        float vmdFps = Math.Max(1f, (float)item.VmdFps.GetValue(frame, length, fps));
        float scale = Math.Max(1e-6f, (float)item.Scale.GetValue(frame, length, fps));
        float ox = (float)item.OffsetX.GetValue(frame, length, fps);
        float oy = (float)item.OffsetY.GetValue(frame, length, fps);
        float oz = (float)item.OffsetZ.GetValue(frame, length, fps);

        // アイテム先頭からの経過を VMD フレームへ写像
        float vmdFrame = start + frame * (vmdFps / fps);
        var pose = motion.Sample(vmdFrame);

        // MMD → YMM4: 位置はスケール、Z は標準カメラと同様に符号反転
        var target = new Vector3(
            pose.Target.X * scale + ox,
            pose.Target.Y * scale + oy,
            -(pose.Target.Z * scale + oz));

        // 注視点まわりの軌道カメラ（ロールは位置に含めず、後段で視線まわりに適用）
        // 列ベクトル式 Ry*Rx に対応する行ベクトル順 = Rx*Ry
        var rot =
            Matrix4x4.CreateRotationX(pose.Rotation.X) *
            Matrix4x4.CreateRotationY(pose.Rotation.Y);

        var offset = Vector3.Transform(new Vector3(0f, 0f, pose.Distance * scale), rot);
        // offset.Z も YMM4 内部座標へ（距離オフセットの Z 成分を反転）
        offset = new Vector3(offset.X, offset.Y, -offset.Z);
        var eye = target + offset;

        // 視線がほぼゼロのときはそのまま返す
        if ((target - eye).LengthSquared() < 1e-8f)
            return drawDesc;

        var baseCamera = item.CompositeWithExisting ? drawDesc.Camera : Matrix4x4.Identity;
        var camera =
            baseCamera *
            Matrix4x4.CreateTranslation(0f, 0f, -1000f) *
            Matrix4x4.CreateLookAt(eye, target, Vector3.UnitY) *
            Matrix4x4.CreateTranslation(0f, 0f, 1000f);

        // ロールは標準注視点カメラと同じく CreateLookAt 後の Z 回転で付与
        if (MathF.Abs(pose.Rotation.Z) > 1e-6f)
        {
            camera *= Matrix4x4.CreateRotationZ(pose.Rotation.Z, new Vector3(0f, 0f, 1000f));
        }

        if (!IsFinite(camera))
            return drawDesc;

        var result = drawDesc with { Camera = camera };

        if (!item.ApplyFov || !pose.Perspective)
            return result;

        float perspective = CalculatePerspectiveDistance(pose.FovDegrees, effectDescription.ScreenSize.Height);
        return result with { PerspectiveDistance = perspective };
    }

    /// <summary>YMM4 標準 CameraFovEffect と同じ換算。</summary>
    static float CalculatePerspectiveDistance(double fovDegrees, int screenHeight)
    {
        if (!double.IsFinite(fovDegrees))
            fovDegrees = 30;
        fovDegrees = Math.Clamp(fovDegrees, 0.0, 179.9);
        if (fovDegrees < 0.1)
            return float.PositiveInfinity;
        if (screenHeight <= 0)
            screenHeight = 1080;

        double d = screenHeight / 2.0 / Math.Tan(fovDegrees * Math.PI / 360.0);
        if (!double.IsFinite(d) || d <= 0 || d > float.MaxValue)
            return 1000f;
        return (float)d;
    }

    static bool IsFinite(Matrix4x4 m) =>
        float.IsFinite(m.M11) && float.IsFinite(m.M12) && float.IsFinite(m.M13) && float.IsFinite(m.M14) &&
        float.IsFinite(m.M21) && float.IsFinite(m.M22) && float.IsFinite(m.M23) && float.IsFinite(m.M24) &&
        float.IsFinite(m.M31) && float.IsFinite(m.M32) && float.IsFinite(m.M33) && float.IsFinite(m.M34) &&
        float.IsFinite(m.M41) && float.IsFinite(m.M42) && float.IsFinite(m.M43) && float.IsFinite(m.M44);

    public void Dispose()
    {
    }
}

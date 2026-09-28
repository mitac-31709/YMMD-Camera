using System.Numerics;

namespace MmdCameraPlugin.Vmd;

/// <summary>カメラキーフレーム列をサンプリングし、補間済みポーズを返す。</summary>
public sealed class VmdCameraMotion
{
    readonly VmdCameraKeyframe[] _keys;

    public VmdCameraMotion(IReadOnlyList<VmdCameraKeyframe> keyframes)
    {
        _keys = keyframes.OrderBy(k => k.Frame).ToArray();
    }

    public bool IsEmpty => _keys.Length == 0;
    public int KeyCount => _keys.Length;
    public int FirstFrame => _keys.Length == 0 ? 0 : _keys[0].Frame;
    public int LastFrame => _keys.Length == 0 ? 0 : _keys[^1].Frame;

    public CameraPose Sample(float frame)
    {
        if (_keys.Length == 0)
            return CameraPose.Default;

        if (frame <= _keys[0].Frame)
            return FromKey(_keys[0]);

        if (frame >= _keys[^1].Frame)
            return FromKey(_keys[^1]);

        int hi = FindUpperBound(frame);
        var a = _keys[hi - 1];
        var b = _keys[hi];

        float span = b.Frame - a.Frame;
        float t = span <= 0 ? 0f : (frame - a.Frame) / span;

        // 補間曲線は「次キー」側の Interpolation を使う（MMD 仕様）
        float tx = Channel(b.Interpolation, 0, t);
        float ty = Channel(b.Interpolation, 1, t);
        float tz = Channel(b.Interpolation, 2, t);
        float tr = Channel(b.Interpolation, 3, t);
        float td = Channel(b.Interpolation, 4, t);
        float tf = Channel(b.Interpolation, 5, t);

        return new CameraPose
        {
            Target = new Vector3(
                Lerp(a.PositionX, b.PositionX, tx),
                Lerp(a.PositionY, b.PositionY, ty),
                Lerp(a.PositionZ, b.PositionZ, tz)),
            Distance = Lerp(a.Distance, b.Distance, td),
            Rotation = new Vector3(
                Lerp(a.RotationX, b.RotationX, tr),
                Lerp(a.RotationY, b.RotationY, tr),
                Lerp(a.RotationZ, b.RotationZ, tr)),
            FovDegrees = Lerp(a.FovDegrees, b.FovDegrees, tf),
            Perspective = t < 0.5f ? a.Perspective : b.Perspective,
        };
    }

    int FindUpperBound(float frame)
    {
        int lo = 1;
        int hi = _keys.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (_keys[mid].Frame <= frame)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

    static float Channel(byte[] interp, int channel, float t)
    {
        int o = channel * 4;
        return VmdBezier.Evaluate(interp[o], interp[o + 1], interp[o + 2], interp[o + 3], t);
    }

    static CameraPose FromKey(VmdCameraKeyframe k) => new()
    {
        Target = new Vector3(k.PositionX, k.PositionY, k.PositionZ),
        Distance = k.Distance,
        Rotation = new Vector3(k.RotationX, k.RotationY, k.RotationZ),
        FovDegrees = k.FovDegrees,
        Perspective = k.Perspective,
    };

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    public readonly struct CameraPose
    {
        public Vector3 Target { get; init; }
        public float Distance { get; init; }
        public Vector3 Rotation { get; init; }
        public float FovDegrees { get; init; }
        public bool Perspective { get; init; }

        public static CameraPose Default => new()
        {
            Target = Vector3.Zero,
            Distance = -45f,
            Rotation = Vector3.Zero,
            FovDegrees = 30f,
            Perspective = true,
        };
    }
}

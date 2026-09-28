namespace MmdCameraPlugin.Vmd;

/// <summary>VMD カメラキーフレーム1本分。</summary>
public sealed class VmdCameraKeyframe
{
    public required int Frame { get; init; }
    public required float Distance { get; init; }
    public required float PositionX { get; init; }
    public required float PositionY { get; init; }
    public required float PositionZ { get; init; }
    public required float RotationX { get; init; }
    public required float RotationY { get; init; }
    public required float RotationZ { get; init; }
    /// <summary>補間: X,Y,Z,R,Dist,FOV 各4バイト (x1,x2,y1,y2) = 24バイト。</summary>
    public required byte[] Interpolation { get; init; }
    public required uint FovDegrees { get; init; }
    public required bool Perspective { get; init; }
}

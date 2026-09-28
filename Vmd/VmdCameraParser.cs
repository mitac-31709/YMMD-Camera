using System.IO;
using System.Text;

namespace MmdCameraPlugin.Vmd;

/// <summary>VMD からカメラキーフレームだけを読み取る。</summary>
public static class VmdCameraParser
{
    const int BoneFrameSize = 111;
    const int MorphFrameSize = 23;
    const int CameraFrameSize = 61;

    public static IReadOnlyList<VmdCameraKeyframe> Load(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        // header (30) + model name (20)
        if (fs.Length < 50)
            throw new InvalidDataException("VMDファイルが短すぎます。");

        var header = Encoding.ASCII.GetString(br.ReadBytes(30)).TrimEnd('\0');
        if (!header.StartsWith("Vocaloid Motion Data", StringComparison.Ordinal))
            throw new InvalidDataException($"VMDヘッダが不正です: {header}");

        _ = br.ReadBytes(20); // model name (Shift-JIS)

        // bone frames
        if (fs.Position + 4 > fs.Length)
            return [];
        uint boneCount = br.ReadUInt32();
        long boneBytes = (long)boneCount * BoneFrameSize;
        if (fs.Position + boneBytes > fs.Length)
            throw new InvalidDataException("ボーンキーフレームがファイル末尾を超えています。");
        fs.Position += boneBytes;

        // morph frames
        if (fs.Position + 4 > fs.Length)
            return [];
        uint morphCount = br.ReadUInt32();
        long morphBytes = (long)morphCount * MorphFrameSize;
        if (fs.Position + morphBytes > fs.Length)
            throw new InvalidDataException("モーフキーフレームがファイル末尾を超えています。");
        fs.Position += morphBytes;

        // camera frames
        if (fs.Position + 4 > fs.Length)
            return [];
        uint cameraCount = br.ReadUInt32();
        long cameraBytes = (long)cameraCount * CameraFrameSize;
        if (fs.Position + cameraBytes > fs.Length)
            throw new InvalidDataException("カメラキーフレームがファイル末尾を超えています。");

        var frames = new List<VmdCameraKeyframe>((int)Math.Min(cameraCount, int.MaxValue));
        for (uint i = 0; i < cameraCount; i++)
        {
            int frame = br.ReadInt32();
            float distance = br.ReadSingle();
            float px = br.ReadSingle();
            float py = br.ReadSingle();
            float pz = br.ReadSingle();
            float rx = br.ReadSingle();
            float ry = br.ReadSingle();
            float rz = br.ReadSingle();
            byte[] interp = br.ReadBytes(24);
            uint fov = br.ReadUInt32();
            bool perspective = br.ReadByte() != 0;

            frames.Add(new VmdCameraKeyframe
            {
                Frame = frame,
                Distance = distance,
                PositionX = px,
                PositionY = py,
                PositionZ = pz,
                RotationX = rx,
                RotationY = ry,
                RotationZ = rz,
                Interpolation = interp,
                FovDegrees = fov,
                Perspective = perspective,
            });
        }

        frames.Sort((a, b) => a.Frame.CompareTo(b.Frame));
        return frames;
    }
}

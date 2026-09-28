namespace MmdCameraPlugin.Vmd;

/// <summary>MMD カメラ用ベジェ補間（VMD の 0〜127 制御点）。</summary>
internal static class VmdBezier
{
    /// <summary>
    /// キー間の正規化時間 t∈[0,1] に対し、補間曲線上の出力値（0〜1）を返す。
    /// </summary>
    public static float Evaluate(byte x1, byte x2, byte y1, byte y2, float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        // 線形（MMD 既定 20,107,20,107）に近い場合は高速パス
        if (x1 == 20 && x2 == 107 && y1 == 20 && y2 == 107)
            return t;

        float ax = x1 / 127f;
        float bx = x2 / 127f;
        float ay = y1 / 127f;
        float by = y2 / 127f;

        // ベジェの X(s)=t となる s を二分法で求める
        float s = t;
        for (int i = 0; i < 16; i++)
        {
            float x = Bezier1D(ax, bx, s);
            float dx = Bezier1DDerivative(ax, bx, s);
            if (MathF.Abs(dx) < 1e-6f)
                break;
            s -= (x - t) / dx;
            s = Math.Clamp(s, 0f, 1f);
        }

        return Bezier1D(ay, by, s);
    }

    static float Bezier1D(float p1, float p2, float s)
    {
        // P0=0, P3=1 の三次ベジェ
        float u = 1f - s;
        return 3f * u * u * s * p1 + 3f * u * s * s * p2 + s * s * s;
    }

    static float Bezier1DDerivative(float p1, float p2, float s)
    {
        float u = 1f - s;
        return 3f * u * u * p1 + 6f * u * s * (p2 - p1) + 3f * s * s * (1f - p2);
    }
}

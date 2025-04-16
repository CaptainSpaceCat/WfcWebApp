namespace WfcWebApp.Utils
{
    public readonly record struct Color(byte R, byte G, byte B, byte A)
    {
        // Return this color's hex code
        public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";

        public static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');

            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            byte a = (hex.Length >= 8) ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;

            return new Color(r, g, b, a);
        }

        public static Color Gray = FromHex("#888888");
        public static Color LightGray = FromHex("#cccccc");
        public static Color DarkGray = FromHex("#222222");

        public static Color Rainbow(float t) {
            t = Math.Clamp(t, 0f, 1f);
            float h = t * 360f; // hue in degrees
            float s = 1f, v = 1f;

            // HSV to RGB conversion
            int hi = (int)(h / 60f) % 6;
            float f = h / 60f - hi;
            float p = v * (1f - s);
            float q = v * (1f - f * s);
            float r = v * (1f - (1f - f) * s);

            (float rf, float gf, float bf) = hi switch {
                0 => (v, r, p),
                1 => (q, v, p),
                2 => (p, v, r),
                3 => (p, q, v),
                4 => (r, p, v),
                5 => (v, p, q),
                _ => (0f, 0f, 0f),
            };

            return new Color(
                (byte)(rf * 255),
                (byte)(gf * 255),
                (byte)(bf * 255),
                255
            );
        }

    }

}
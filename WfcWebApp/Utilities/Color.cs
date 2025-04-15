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

    }

}
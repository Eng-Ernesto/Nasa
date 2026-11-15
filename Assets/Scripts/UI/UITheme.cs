using UnityEngine;

namespace LunarEyes.UI
{
    /// <summary>Tokens do design system Lunar Mission (documento 13), para a UI do jogo.</summary>
    public static class UITheme
    {
        public static readonly Color DeepSpace = Hex(0x08111B);
        public static readonly Color SpaceNavy = Hex(0x101D2C);
        public static readonly Color PanelNavy = Hex(0x162638);
        public static readonly Color ElevatedSlate = Hex(0x203448);
        public static readonly Color LunarWhite = Hex(0xF4F6F3);
        public static readonly Color MistGrey = Hex(0xAAB8C5);
        public static readonly Color SignalCyan = Hex(0x36C5D8);
        public static readonly Color CyanText = Hex(0x7FDDEA);
        public static readonly Color EarthTeal = Hex(0x397C70);
        public static readonly Color AlertAmber = Hex(0xE5A34A);
        public static readonly Color AmberText = Hex(0xF2C27E);
        public static readonly Color BorderSlate = Hex(0x2B4054);
        public static readonly Color OnCyan = Hex(0x06202A);

        public static Color Hex(int rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}

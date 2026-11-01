using System.Globalization;
using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Converts a CSV cell into a <see cref="Color"/>. Accepts HTML-style hex (<c>#FF8800</c>,
    /// <c>#FF8800CC</c>, with or without the leading <c>#</c>) and comma-separated channel notation
    /// (<c>R,G,B</c> or <c>R,G,B,A</c>). Channel values are either 0..1 floats or 0..255 integers;
    /// integer notation is auto-detected and normalized.
    /// </summary>
    [CreateAssetMenu(menuName = "GDF/Config Import/Converter/Color", fileName = "ColorConverter", order = 0)]
    public sealed class ColorConverter : ConfigValueConverter<Color>
    {
        /// <inheritdoc />
        protected override bool TryConvertTyped(string raw, out Color result, out string error)
        {
            result = default;
            error = null;

            if (string.IsNullOrEmpty(raw))
            {
                error = "Color cell is empty.";
                return false;
            }

            string value = raw.Trim();

            // Hex notation, with or without the leading '#'.
            string hex = value.StartsWith("#") ? value.Substring(1) : value;
            if (IsHex(hex) && (hex.Length == 6 || hex.Length == 8))
            {
                return TryParseHex(hex, out result, out error);
            }

            // Channel notation.
            if (value.IndexOf(',') >= 0)
            {
                return TryParseChannels(value, out result, out error);
            }

            error = $"Value '{raw}' is neither a hex color nor a comma-separated channel triple/quad.";
            return false;
        }

        private static bool TryParseHex(string hex, out Color result, out string error)
        {
            result = default;
            error = null;

            byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte a = hex.Length == 8
                ? byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : (byte)255;

            result = new Color32(r, g, b, a);
            return true;
        }

        private static bool TryParseChannels(string value, out Color result, out string error)
        {
            result = default;
            error = null;

            string[] parts = value.Split(',');
            if (parts.Length != 3 && parts.Length != 4)
            {
                error = $"Expected 3 or 4 channels in '{value}'.";
                return false;
            }

            float[] channels = new float[4];
            channels[3] = 1f;
            bool anyAbove1 = false;

            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float channel))
                {
                    error = $"Channel #{i} '{parts[i]}' is not a number.";
                    return false;
                }

                channels[i] = channel;
                if (channel > 1f)
                {
                    anyAbove1 = true;
                }
            }

            // Any component above 1 implies 0..255 notation; normalize.
            if (anyAbove1)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    channels[i] /= 255f;
                }
            }

            result = new Color(channels[0], channels[1], channels[2], channels[3]);
            return true;
        }

        private static bool IsHex(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

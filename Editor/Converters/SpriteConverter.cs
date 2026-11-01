using UnityEditor;
using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Resolves a sprite by name through <see cref="AssetDatabase.FindAssets"/>. Matches on asset
    /// name (not file path); if multiple sprites share the name the first match is returned and a
    /// warning is logged. Intended for authoring workflows where a designer types the sprite name in
    /// the spreadsheet.
    /// </summary>
    [CreateAssetMenu(menuName = "GDF/Config Import/Converter/Sprite", fileName = "SpriteConverter", order = 1)]
    public sealed class SpriteConverter : ConfigValueConverter<Sprite>
    {
        /// <inheritdoc />
        protected override bool TryConvertTyped(string raw, out Sprite result, out string error)
        {
            result = null;
            error = null;

            if (string.IsNullOrEmpty(raw))
            {
                error = "Sprite cell is empty.";
                return false;
            }

            string name = raw.Trim();
            string[] guids = AssetDatabase.FindAssets($"{name} t:Sprite");
            if (guids == null || guids.Length == 0)
            {
                error = $"No Sprite asset with name '{name}' found via AssetDatabase.";
                return false;
            }

            Sprite firstMatch = null;
            int exactMatches = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Sprite candidate = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.name == name)
                {
                    exactMatches++;
                    if (firstMatch == null)
                    {
                        firstMatch = candidate;
                    }
                }
            }

            if (firstMatch == null)
            {
                error = $"Found sprites for '{name}' via AssetDatabase, but none match the name exactly.";
                return false;
            }

            if (exactMatches > 1)
            {
                Debug.LogWarning($"[SpriteConverter] Multiple sprites share the name '{name}' — using the first match.");
            }

            result = firstMatch;
            return true;
        }
    }
}

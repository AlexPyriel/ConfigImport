using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Converts a CSV cell into a generic <see cref="AssetReference"/>. The cell is interpreted as an
    /// Addressables address: the converter looks the address up in the active Addressables settings
    /// and builds a reference pointing at the matched entry's GUID. Addresses that do not exist in
    /// the project emit a conversion error.
    /// </summary>
    [CreateAssetMenu(menuName = "GDF/Config Import/Converter/Asset Reference", fileName = "AssetReferenceConverter", order = 2)]
    public sealed class AssetReferenceConverter : ConfigValueConverter<AssetReference>
    {
        /// <inheritdoc />
        protected override bool TryConvertTyped(string raw, out AssetReference result, out string error)
        {
            result = null;
            error = null;

            if (string.IsNullOrEmpty(raw))
            {
                error = "AssetReference cell is empty.";
                return false;
            }

            string address = raw.Trim();
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                error = "Addressables settings are not initialized in this project. Open Window -> Asset Management -> Addressables -> Groups.";
                return false;
            }

            AddressableAssetEntry entry = FindEntryByAddress(settings, address);
            if (entry == null)
            {
                error = $"Addressable address '{address}' was not found in the project settings.";
                return false;
            }

            result = new AssetReference(entry.guid);
            return true;
        }

        private static AddressableAssetEntry FindEntryByAddress(AddressableAssetSettings settings, string address)
        {
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry != null && entry.address == address)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }
    }
}

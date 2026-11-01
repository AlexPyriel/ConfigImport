using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Converts a delimited string (default separator <c>;</c>) into a <see cref="List{T}"/> of primitives.
    /// Supports <c>int</c>, <c>long</c>, <c>float</c>, <c>double</c>, <c>bool</c>, <c>string</c> and enums.
    /// Element type is inferred from the field type at import time.
    /// </summary>
    [CreateAssetMenu(menuName = "GDF/Config Import/Converter/List", fileName = "ListConverter", order = 3)]
    public sealed class ListConverter : ConfigValueConverter
    {
        [SerializeField]
        [Tooltip("Delimiter between elements inside the CSV cell. Defaults to ';'. The outer CSV delimiter stays ','.")]
        private string _delimiter = ";";

        [SerializeField]
        [Tooltip("Trim whitespace around each element after splitting.")]
        private bool _trimElements = true;

        /// <inheritdoc />
        public override Type TargetType => typeof(IList);

        /// <inheritdoc />
        public override bool TryConvert(Type fieldType, string raw, out object result, out string error)
        {
            result = null;
            error = null;

            if (fieldType == null || !fieldType.IsGenericType || fieldType.GetGenericTypeDefinition() != typeof(List<>))
            {
                error = $"ListConverter expects List<T>, got {fieldType?.FullName ?? "<null>"}.";
                return false;
            }

            Type elementType = fieldType.GetGenericArguments()[0];
            IList list = (IList)Activator.CreateInstance(fieldType);

            if (string.IsNullOrEmpty(raw))
            {
                result = list;
                return true;
            }

            string delimiter = string.IsNullOrEmpty(_delimiter) ? ";" : _delimiter;
            string[] parts = raw.Split(new[] { delimiter }, StringSplitOptions.None);

            for (int i = 0; i < parts.Length; i++)
            {
                string token = _trimElements ? parts[i].Trim() : parts[i];
                if (token.Length == 0)
                {
                    continue;
                }

                if (!TryParsePrimitive(elementType, token, out object element))
                {
                    error = $"Element '{token}' at index {i} is not a valid {elementType.Name}.";
                    return false;
                }

                list.Add(element);
            }

            result = list;
            return true;
        }

        private static bool TryParsePrimitive(Type type, string raw, out object result)
        {
            if (type == typeof(string))
            {
                result = raw;
                return true;
            }

            if (type == typeof(int) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
            {
                result = i;
                return true;
            }

            if (type == typeof(long) && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
            {
                result = l;
                return true;
            }

            if (type == typeof(float) && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
            {
                result = f;
                return true;
            }

            if (type == typeof(double) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                result = d;
                return true;
            }

            if (type == typeof(bool))
            {
                string t = raw.Trim();
                if (string.Equals(t, "true", StringComparison.OrdinalIgnoreCase) || t == "1") { result = true; return true; }
                if (string.Equals(t, "false", StringComparison.OrdinalIgnoreCase) || t == "0") { result = false; return true; }
                result = null;
                return false;
            }

            if (type.IsEnum)
            {
                try { result = Enum.Parse(type, raw, true); return true; }
                catch { result = null; return false; }
            }

            result = null;
            return false;
        }
    }
}

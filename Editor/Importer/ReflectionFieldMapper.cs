using System;
using System.Collections.Generic;
using System.Reflection;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Reflection utility that matches CSV columns onto the public and private instance fields of a
    /// config type by name, applies registered converters for complex field types, and writes the
    /// parsed values via <see cref="FieldInfo.SetValue"/>. Primitive fields (`int`, `float`, `bool`,
    /// `string`, `enum`) are converted inline; anything else is routed through <see cref="IConfigValueConverter"/>.
    /// </summary>
    public static class ReflectionFieldMapper
    {
        private const BindingFlags FIELD_BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Parses the key value from <paramref name="rawKey"/> into the type of the target repository key.
        /// Primitive keys and enum keys are handled inline.
        /// </summary>
        /// <param name="keyType">Type of the repository key.</param>
        /// <param name="rawKey">Raw CSV cell holding the key.</param>
        /// <param name="result">Boxed parsed key on success.</param>
        /// <param name="error">Human-readable error on failure.</param>
        /// <returns><see langword="true"/> on success.</returns>
        public static bool TryParseKey(Type keyType, string rawKey, out object result, out string error)
        {
            if (keyType == null)
            {
                result = null;
                error = "Key type is null.";
                return false;
            }

            if (string.IsNullOrEmpty(rawKey))
            {
                result = null;
                error = "Key cell is empty.";
                return false;
            }

            if (TryParsePrimitive(keyType, rawKey, out result, out error))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Builds a map of column index -> target field for a given config type and header row.
        /// Columns without a matching field are ignored silently; this is on purpose so a spreadsheet
        /// can carry extra authoring columns the game does not consume.
        /// </summary>
        /// <param name="configType">Type of the config wrapper.</param>
        /// <param name="header">Header row from the CSV (column names).</param>
        /// <param name="keyColumnName">Name of the key column; it is excluded from the field map because the key is passed to <c>TrySet</c> separately.</param>
        /// <returns>Pairs of column index and matched field.</returns>
        public static List<(int ColumnIndex, FieldInfo Field)> BuildFieldMap(Type configType, List<string> header, string keyColumnName)
        {
            List<(int, FieldInfo)> map = new List<(int, FieldInfo)>();
            if (configType == null || header == null)
            {
                return map;
            }

            FieldInfo[] fields = configType.GetFields(FIELD_BINDING_FLAGS);
            for (int col = 0; col < header.Count; col++)
            {
                string columnName = (header[col] ?? string.Empty).Trim();
                if (columnName.Length == 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(keyColumnName) && string.Equals(columnName, keyColumnName, StringComparison.Ordinal))
                {
                    continue;
                }

                for (int f = 0; f < fields.Length; f++)
                {
                    if (string.Equals(fields[f].Name, columnName, StringComparison.Ordinal))
                    {
                        map.Add((col, fields[f]));
                        break;
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Writes the given row into <paramref name="target"/> using <paramref name="fieldMap"/> and
        /// the supplied converters. Primitive and enum fields are converted inline; everything else
        /// looks up a converter whose <see cref="IConfigValueConverter.TargetType"/> is assignable
        /// from the field type.
        /// </summary>
        /// <param name="target">Config instance being populated.</param>
        /// <param name="row">Row cells aligned with the header width.</param>
        /// <param name="fieldMap">Column -> field pairs produced by <see cref="BuildFieldMap"/>.</param>
        /// <param name="converters">Available value converters. May be <see langword="null"/>.</param>
        /// <param name="errors">List that accumulates per-cell error messages. Not nullable.</param>
        /// <returns><see langword="true"/> when the row was written without any field-level error.</returns>
        public static bool PopulateFields(
            object target,
            List<string> row,
            List<(int ColumnIndex, FieldInfo Field)> fieldMap,
            List<IConfigValueConverter> converters,
            List<string> errors)
        {
            bool clean = true;

            for (int i = 0; i < fieldMap.Count; i++)
            {
                int col = fieldMap[i].ColumnIndex;
                FieldInfo field = fieldMap[i].Field;
                if (col >= row.Count)
                {
                    continue;
                }

                string raw = row[col];

                if (TryParsePrimitive(field.FieldType, raw, out object parsed, out string primitiveError))
                {
                    field.SetValue(target, parsed);
                    continue;
                }

                // Primitive attempt failed (empty allowed, otherwise look up converter).
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                IConfigValueConverter converter = PickConverter(converters, field.FieldType);
                if (converter == null)
                {
                    errors.Add($"No converter registered for field '{field.Name}' of type {field.FieldType.Name} (value='{raw}'). {primitiveError}");
                    clean = false;
                    continue;
                }

                if (converter.TryConvert(field.FieldType, raw, out object converted, out string convError))
                {
                    field.SetValue(target, converted);
                    continue;
                }

                errors.Add($"Converter {converter.GetType().Name} failed for field '{field.Name}' (value='{raw}'): {convError}");
                clean = false;
            }

            return clean;
        }

        private static IConfigValueConverter PickConverter(List<IConfigValueConverter> converters, Type fieldType)
        {
            if (converters == null)
            {
                return null;
            }

            // Pass 1: converter TargetType exactly equals field type (strict match).
            for (int i = 0; i < converters.Count; i++)
            {
                IConfigValueConverter candidate = converters[i];
                if (candidate != null && candidate.TargetType == fieldType)
                {
                    return candidate;
                }
            }

            // Pass 2: field is assignable to converter TargetType (e.g. subclass of a base).
            for (int i = 0; i < converters.Count; i++)
            {
                IConfigValueConverter candidate = converters[i];
                if (candidate != null && candidate.TargetType != null && candidate.TargetType.IsAssignableFrom(fieldType))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool TryParsePrimitive(Type type, string raw, out object result, out string error)
        {
            error = null;

            if (type == typeof(string))
            {
                result = raw ?? string.Empty;
                return true;
            }

            if (string.IsNullOrEmpty(raw))
            {
                // Keep the field at its default; this is a successful no-op.
                result = type.IsValueType ? Activator.CreateInstance(type) : null;
                return true;
            }

            if (type == typeof(int))
            {
                if (int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int value))
                {
                    result = value;
                    return true;
                }

                result = 0;
                error = $"Value '{raw}' is not a valid int.";
                return false;
            }

            if (type == typeof(long))
            {
                if (long.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long value))
                {
                    result = value;
                    return true;
                }

                result = 0L;
                error = $"Value '{raw}' is not a valid long.";
                return false;
            }

            if (type == typeof(float))
            {
                if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                {
                    result = value;
                    return true;
                }

                result = 0f;
                error = $"Value '{raw}' is not a valid float.";
                return false;
            }

            if (type == typeof(double))
            {
                if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value))
                {
                    result = value;
                    return true;
                }

                result = 0d;
                error = $"Value '{raw}' is not a valid double.";
                return false;
            }

            if (type == typeof(bool))
            {
                string trimmed = raw.Trim();
                if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) || trimmed == "1")
                {
                    result = true;
                    return true;
                }

                if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) || trimmed == "0")
                {
                    result = false;
                    return true;
                }

                result = false;
                error = $"Value '{raw}' is not a valid bool.";
                return false;
            }

            if (type.IsEnum)
            {
                try
                {
                    result = Enum.Parse(type, raw, ignoreCase: true);
                    return true;
                }
                catch (Exception ex)
                {
                    result = Activator.CreateInstance(type);
                    error = $"Value '{raw}' is not a member of enum {type.Name}: {ex.Message}";
                    return false;
                }
            }

            result = null;
            error = $"Type {type.Name} is not a primitive and requires a converter.";
            return false;
        }
    }
}

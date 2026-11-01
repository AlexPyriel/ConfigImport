using System;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Converts a raw CSV string into a strongly-typed value for a field of a config wrapper.
    /// Importers resolve converters by matching <see cref="TargetType"/> against a field's declared type.
    /// Implementations are typically <see cref="UnityEngine.ScriptableObject"/>s so designers can author them in-project.
    /// </summary>
    public interface IConfigValueConverter
    {
        /// <summary>
        /// Field type this converter claims. The importer picks the first converter whose target type
        /// is assignable from the declared field type. For generic container converters
        /// (e.g. <c>List&lt;T&gt;</c>) this is typically the open base (<see cref="System.Collections.IList"/>);
        /// the concrete element type is read from the <c>fieldType</c> argument passed to <see cref="TryConvert"/>.
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// Parses the raw CSV cell into a value matching <paramref name="fieldType"/>.
        /// </summary>
        /// <param name="fieldType">Declared type of the destination field. Allows generic converters (e.g. list) to resolve element types from closed generics.</param>
        /// <param name="raw">Raw cell contents (already un-quoted by the CSV parser; may be empty).</param>
        /// <param name="result">Parsed value on success; implementation-defined default on failure.</param>
        /// <param name="error">Human-readable error on failure; <see langword="null"/> on success.</param>
        /// <returns><see langword="true"/> when the raw value was successfully converted.</returns>
        bool TryConvert(Type fieldType, string raw, out object result, out string error);
    }
}

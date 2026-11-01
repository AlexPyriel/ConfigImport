using System;
using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Base <see cref="ScriptableObject"/> for a value converter. Concrete converters inherit from
    /// <see cref="ConfigValueConverter{T}"/> for a fixed field type, or override this class directly
    /// when they need the field type at runtime (e.g. <c>List&lt;T&gt;</c>).
    /// </summary>
    public abstract class ConfigValueConverter : ScriptableObject, IConfigValueConverter
    {
        /// <inheritdoc />
        public abstract Type TargetType { get; }

        /// <inheritdoc />
        public abstract bool TryConvert(Type fieldType, string raw, out object result, out string error);
    }

    /// <summary>
    /// Typed base for a value converter. Subclasses implement <see cref="TryConvertTyped"/> and the
    /// base takes care of exposing it through the non-generic <see cref="IConfigValueConverter"/> contract.
    /// The <paramref name="fieldType"/> argument is accepted for interface compatibility but ignored.
    /// </summary>
    /// <typeparam name="T">Field type produced by the converter.</typeparam>
    public abstract class ConfigValueConverter<T> : ConfigValueConverter
    {
        /// <inheritdoc />
        public override Type TargetType => typeof(T);

        /// <inheritdoc />
        public override bool TryConvert(Type fieldType, string raw, out object result, out string error)
        {
            if (TryConvertTyped(raw, out T typed, out error))
            {
                result = typed;
                return true;
            }

            result = default(T);
            return false;
        }

        /// <summary>Strongly-typed conversion contract implemented by concrete converters.</summary>
        /// <param name="raw">Raw CSV cell.</param>
        /// <param name="result">Parsed value on success.</param>
        /// <param name="error">Human-readable error on failure.</param>
        /// <returns><see langword="true"/> on success.</returns>
        protected abstract bool TryConvertTyped(string raw, out T result, out string error);
    }
}

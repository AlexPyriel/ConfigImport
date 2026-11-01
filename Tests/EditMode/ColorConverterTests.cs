using NUnit.Framework;
using UnityEngine;

namespace GDF.ConfigImport.Tests
{
    public sealed class ColorConverterTests
    {
        private ColorConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = ScriptableObject.CreateInstance<ColorConverter>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_converter);
        }

        [Test]
        public void Hex_WithHash_ParsesRgb()
        {
            Assert.IsTrue(_converter.TryConvert(typeof(Color), "#FF8000", out object result, out _));
            Color c = (Color)result;
            Assert.AreEqual(1f, c.r, 1e-3);
            Assert.AreEqual(128f / 255f, c.g, 1e-3);
            Assert.AreEqual(0f, c.b, 1e-3);
            Assert.AreEqual(1f, c.a, 1e-3);
        }

        [Test]
        public void Hex_WithAlpha_ParsesRgba()
        {
            Assert.IsTrue(_converter.TryConvert(typeof(Color), "FF8000AA", out object result, out _));
            Color c = (Color)result;
            Assert.AreEqual(170f / 255f, c.a, 1e-3);
        }

        [Test]
        public void Channels_FloatTriple_UsesNormalizedRange()
        {
            Assert.IsTrue(_converter.TryConvert(typeof(Color), "0.5, 0.5, 1", out object result, out _));
            Color c = (Color)result;
            Assert.AreEqual(0.5f, c.r, 1e-3);
            Assert.AreEqual(1f, c.b, 1e-3);
            Assert.AreEqual(1f, c.a, 1e-3);
        }

        [Test]
        public void Channels_IntegerTriple_NormalizedFrom255()
        {
            Assert.IsTrue(_converter.TryConvert(typeof(Color), "255,128,0", out object result, out _));
            Color c = (Color)result;
            Assert.AreEqual(1f, c.r, 1e-3);
            Assert.AreEqual(128f / 255f, c.g, 1e-3);
        }

        [Test]
        public void Empty_Fails()
        {
            Assert.IsFalse(_converter.TryConvert(typeof(Color), string.Empty, out _, out string error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        [Test]
        public void Garbage_Fails()
        {
            Assert.IsFalse(_converter.TryConvert(typeof(Color), "not-a-color", out _, out string error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }
    }
}

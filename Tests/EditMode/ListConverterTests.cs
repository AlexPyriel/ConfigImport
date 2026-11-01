using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GDF.ConfigImport.Tests
{
    public sealed class ListConverterTests
    {
        private ListConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = ScriptableObject.CreateInstance<ListConverter>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_converter);
        }

        [Test]
        public void IntList_SemicolonSeparated_Parses()
        {
            Assert.IsTrue(_converter.TryConvert(typeof(List<int>), "1;2;3", out object result, out _));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, (List<int>)result);
        }

        [Test]
        public void StringList_Trims()
        {
            Assert.IsTrue(_converter.TryConvert(typeof(List<string>), " a ; b ;c", out object result, out _));
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, (List<string>)result);
        }

        [Test]
        public void FloatList_Invariant()
        {
            Assert.IsTrue(_converter.TryConvert(typeof(List<float>), "0.5;1.25;3.14", out object result, out _));
            CollectionAssert.AreEqual(new[] { 0.5f, 1.25f, 3.14f }, (List<float>)result);
        }

        [Test]
        public void BadElement_ReportsError()
        {
            Assert.IsFalse(_converter.TryConvert(typeof(List<int>), "1;two;3", out _, out string error));
            StringAssert.Contains("two", error);
        }

        [Test]
        public void NonListType_Fails()
        {
            Assert.IsFalse(_converter.TryConvert(typeof(int), "1;2", out _, out string error));
            StringAssert.Contains("List", error);
        }

        [Test]
        public void EmptyInput_ReturnsEmptyList()
        {
            Assert.IsTrue(_converter.TryConvert(typeof(List<int>), string.Empty, out object result, out _));
            Assert.AreEqual(0, ((List<int>)result).Count);
        }
    }
}

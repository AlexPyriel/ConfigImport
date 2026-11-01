using System.Collections.Generic;
using NUnit.Framework;

namespace GDF.ConfigImport.Tests
{
    /// <summary>
    /// Pure-logic tests over <see cref="ReflectionFieldMapper"/> primitive parsing, field map
    /// construction and population. Converters are exercised separately in their own suites.
    /// </summary>
    public sealed class ReflectionFieldMapperTests
    {
        private sealed class SampleConfig
        {
            public int Hp;
            public float Speed;
            public string Name;
            public bool IsBoss;
            public Mood State;
        }

        private enum Mood { Calm, Angry }

        [Test]
        public void TryParseKey_IntegerKey_ParsesInvariant()
        {
            Assert.IsTrue(ReflectionFieldMapper.TryParseKey(typeof(int), "42", out object key, out _));
            Assert.AreEqual(42, (int)key);
        }

        [Test]
        public void TryParseKey_EnumKey_ParsesIgnoreCase()
        {
            Assert.IsTrue(ReflectionFieldMapper.TryParseKey(typeof(Mood), "angry", out object key, out _));
            Assert.AreEqual(Mood.Angry, (Mood)key);
        }

        [Test]
        public void TryParseKey_EmptyRaw_Fails()
        {
            Assert.IsFalse(ReflectionFieldMapper.TryParseKey(typeof(int), string.Empty, out _, out string error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        [Test]
        public void BuildFieldMap_IgnoresKeyColumn_AndMatchesByName()
        {
            var header = new List<string> { "Id", "Hp", "Speed", "Name", "IsBoss", "State", "Extra" };
            var map = ReflectionFieldMapper.BuildFieldMap(typeof(SampleConfig), header, "Id");
            Assert.AreEqual(5, map.Count, "Only 5 matching fields (Hp, Speed, Name, IsBoss, State). Id excluded, Extra not on type.");
        }

        [Test]
        public void PopulateFields_SetsPrimitives_AndEnum()
        {
            var header = new List<string> { "Id", "Hp", "Speed", "Name", "IsBoss", "State" };
            var map = ReflectionFieldMapper.BuildFieldMap(typeof(SampleConfig), header, "Id");
            var row = new List<string> { "k", "100", "3.5", "pike", "true", "angry" };
            var target = new SampleConfig();
            var errors = new List<string>();

            bool clean = ReflectionFieldMapper.PopulateFields(target, row, map, converters: null, errors: errors);

            Assert.IsTrue(clean, "No errors expected: " + string.Join(" / ", errors));
            Assert.AreEqual(100, target.Hp);
            Assert.AreEqual(3.5f, target.Speed);
            Assert.AreEqual("pike", target.Name);
            Assert.IsTrue(target.IsBoss);
            Assert.AreEqual(Mood.Angry, target.State);
        }

        [Test]
        public void PopulateFields_BadNumber_RecordsError()
        {
            var header = new List<string> { "Id", "Hp" };
            var map = ReflectionFieldMapper.BuildFieldMap(typeof(SampleConfig), header, "Id");
            var row = new List<string> { "k", "nan-value" };
            var target = new SampleConfig();
            var errors = new List<string>();

            bool clean = ReflectionFieldMapper.PopulateFields(target, row, map, converters: null, errors: errors);

            Assert.IsFalse(clean);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("Hp", errors[0]);
        }
    }
}

using System.Collections.Generic;
using System.IO;
using ConfigRepository;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GDF.ConfigImport.Tests
{
    /// <summary>
    /// End-to-end test for the local-CSV path: wires a fixture <c>.csv</c> into
    /// <see cref="LocalCsvDataSource"/>, pulls through <see cref="ConfigImporter"/>, and asserts the
    /// resulting <see cref="ConfigRepository{TKey, TConfig}"/> content. Covers the import pipeline
    /// without touching the network.
    /// </summary>
    public sealed class LocalCsvImportTests
    {
        private const string FIXTURE_RELATIVE_PATH = "Packages/com.gdf.config-import/Tests/EditMode/Fixtures/items.csv";

        private ConfigImporter _importer;
        private LocalCsvDataSource _dataSource;
        private ItemsRepository _repository;

        [SetUp]
        public void SetUp()
        {
            TextAsset fixture = LoadFixture();
            Assume.That(fixture, Is.Not.Null, "Fixture CSV must exist at " + FIXTURE_RELATIVE_PATH);

            _dataSource = ScriptableObject.CreateInstance<LocalCsvDataSource>();
            SetSerializedObjectField(_dataSource, "_asset", fixture);

            _repository = ScriptableObject.CreateInstance<ItemsRepository>();

            _importer = ScriptableObject.CreateInstance<ConfigImporter>();
            SetSerializedObjectField(_importer, "_dataSourceAsset", _dataSource);

            SerializedObject so = new SerializedObject(_importer);
            SerializedProperty sheets = so.FindProperty("_sheets");
            sheets.arraySize = 1;
            SerializedProperty element = sheets.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("SheetName").stringValue = "items";
            element.FindPropertyRelative("Target").objectReferenceValue = _repository;
            element.FindPropertyRelative("KeyColumnName").stringValue = "Id";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_importer);
            Object.DestroyImmediate(_dataSource);
            Object.DestroyImmediate(_repository);
        }

        [Test]
        public void Reload_PopulatesRepositoryFromFixture()
        {
            ImportResult result = _importer.Reload();

            Assert.IsTrue(result.Success, "Errors: " + (result.Errors == null ? string.Empty : string.Join(" / ", result.Errors)));
            Assert.AreEqual(3, result.RowsImported);
            Assert.AreEqual(3, _repository.Count);

            Assert.IsTrue(_repository.TryGet("pike", out ItemConfig pike));
            Assert.AreEqual(120, pike.Hp);
            Assert.AreEqual(3.5f, pike.Speed);
            Assert.AreEqual("Pike", pike.Name);
            Assert.IsFalse(pike.IsBoss);

            Assert.IsTrue(_repository.TryGet("troll", out ItemConfig troll));
            Assert.IsTrue(troll.IsBoss);

            Assert.IsTrue(_repository.TryGet("glass,golem", out ItemConfig golem), "Quoted key with an embedded comma must survive the round trip.");
            Assert.AreEqual("Golem, armored", golem.Name);
        }

        [Test]
        public void Reload_ClearsPreviousContent()
        {
            _importer.Reload();
            int firstCount = _repository.Count;
            _importer.Reload();

            Assert.AreEqual(firstCount, _repository.Count, "Reload must replace content, not append.");
        }

        private static TextAsset LoadFixture()
        {
            TextAsset fixture = AssetDatabase.LoadAssetAtPath<TextAsset>(FIXTURE_RELATIVE_PATH);
            if (fixture != null)
            {
                return fixture;
            }

            // Fallback for projects where the package is checked out at a different path.
            string[] guids = AssetDatabase.FindAssets("items t:TextAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith("ConfigImport/Tests/EditMode/Fixtures/items.csv"))
                {
                    return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                }
            }

            return null;
        }

        private static void SetSerializedObjectField(ScriptableObject target, string fieldName, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            Assert.IsNotNull(prop, $"Serialized property '{fieldName}' not found on {target.GetType().Name}");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    /// <summary>Simple test config used by <see cref="ItemsRepository"/>.</summary>
    [System.Serializable]
    internal sealed class ItemConfig : Config<string>
    {
        public int Hp;
        public float Speed;
        public string Name;
        public bool IsBoss;
    }

    /// <summary>Concrete repository used by the end-to-end import test.</summary>
    internal sealed class ItemsRepository : ConfigRepository<string, ItemConfig>
    {
    }
}

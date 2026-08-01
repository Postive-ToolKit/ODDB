using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TeamODD.ODDB.Editors.CodeGen;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Async;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBAsyncLoadingTests
    {
        private const string AsyncTypeKey = "__oddb_test_async";

        private FakeAsyncLoader _loader;

        [SetUp]
        public void SetUp()
        {
            TypeRegistry.ResetCache();
            _loader = new FakeAsyncLoader();
            TeamODD.ODDB.Runtime.ODDB.RegisterAsyncLoader(_loader, replaceExisting: true);
        }

        [TearDown]
        public void TearDown()
        {
            TeamODD.ODDB.Runtime.ODDB.UnregisterAsyncLoader(_loader);
            TypeRegistry.ResetCache();
        }

        [Test]
        public async Task Import_AsyncType_PreservesKeyAndUsesRegisteredLoader()
        {
            var fields = new List<Field>
            {
                new("Asset", new FieldType(AsyncTypeKey, "test-param"))
            };
            var row = new Row(new ODDBID("row-1"), fields, new[] { "asset-key" });
            var entity = new AsyncEntity();

            entity.Import(fields, row);

            Assert.That(entity.AssetKey, Is.EqualTo("asset-key"));
            Assert.That(await entity.GetAssetAsync(), Is.EqualTo("loaded:asset-key"));
            Assert.That(_loader.LastKey, Is.EqualTo("asset-key"));

            entity.ReleaseAsset("loaded:asset-key");
            Assert.That(_loader.ReleasedAsset, Is.EqualTo("loaded:asset-key"));
        }

        [Test]
        public void CodeGen_AsyncType_EmitsGetAndReleaseWrappers()
        {
            var view = new View(new[]
            {
                new Field("Asset", new FieldType(AsyncTypeKey, "test-param"))
            })
            {
                ID = new ODDBID("async-view"),
                Name = "AsyncEntity"
            };
            var classNames = new Dictionary<string, string>
            {
                [view.ID.ToString()] = view.Name
            };
            var mapper = new TypeMapper(classNames);
            var writer = new ViewClassWriter(mapper, classNames, new EmptyViewLookup());

            string source = writer.Write(view, view.Name);

            StringAssert.Contains("private string _asset;", source);
            StringAssert.Contains("ODDB.GetAsync<string>(_asset, cancellationToken)", source);
            StringAssert.Contains("public Task<string> GetAssetAsync", source);
            StringAssert.Contains("public void ReleaseAsset(string asset)", source);
            StringAssert.DoesNotContain("public string Asset =>", source);
        }

        [ODDBType(
            AsyncTypeKey,
            ODDBLoadType.Async,
            targetType: typeof(string))]
        public sealed class AsyncTestSerializer : IDataSerializer
        {
            public string Serialize(object data, string param) => data?.ToString() ?? string.Empty;

            public object Deserialize(string serializedData, string param)
                => throw new AssertionException("Async data must not deserialize during entity import.");
        }

        private sealed class AsyncEntity : ODDBEntity
        {
            private string _asset;

            public string AssetKey => _asset;

            public Task<string> GetAssetAsync(CancellationToken cancellationToken = default)
                => TeamODD.ODDB.Runtime.ODDB.GetAsync<string>(_asset, cancellationToken);

            public void ReleaseAsset(string asset) => TeamODD.ODDB.Runtime.ODDB.Release(asset);
        }

        private sealed class FakeAsyncLoader : IAsyncLoader
        {
            public string LastKey { get; private set; }

            public object ReleasedAsset { get; private set; }

            public Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            {
                LastKey = key;
                object value = "loaded:" + key;
                return Task.FromResult((T)value);
            }

            public void Release<T>(T asset)
            {
                ReleasedAsset = asset;
            }
        }

        private sealed class EmptyViewLookup : IODatabaseView
        {
            public IView Find(string viewId) => null;
        }
    }
}

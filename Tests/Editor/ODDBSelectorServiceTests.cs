using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors;
using TeamODD.ODDB.Editors.PropertyDrawers;
using TeamODD.ODDB.Runtime.Entities;
using UnityEngine;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBSelectorServiceTests
    {
        private sealed class AllowedEntity : ODDBEntity { }
        private sealed class OtherEntity : ODDBEntity { }

        [Test]
        public void IsAllowedEntityType_RejectsEntityOutsideConfiguredTypes()
        {
            Assert.That(
                ODDBSelectorService.IsAllowedEntityType(
                    typeof(OtherEntity),
                    new[] { typeof(AllowedEntity) }),
                Is.False);
        }

        [Test]
        public void IsAllowedEntityType_AcceptsAssignableConfiguredType()
        {
            Assert.That(
                ODDBSelectorService.IsAllowedEntityType(
                    typeof(AllowedEntity),
                    new[] { typeof(ODDBEntity) }),
                Is.True);
        }

        [Test]
        public void McpServerVersion_MatchesPackageJson()
        {
            var packagePath = Path.Combine(Application.dataPath, "Plugins", "ODDB", "package.json");
            var packageVersion = JObject.Parse(File.ReadAllText(packagePath))["version"]?.ToString();

            Assert.That(ODDBEditorRuntime.EmbeddedPackageVersion, Is.EqualTo(packageVersion));
            Assert.That(ODDBEditorRuntime.ResolveServerVersion(), Is.EqualTo(packageVersion));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using TeamODD.ODDB.Editors.PropertyDrawers;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class EnumCellDrawerTests
    {
        [ODDBEnum]
        private enum DrawerTestEnum
        {
            Alpha,
            Beta
        }

        [Test]
        public void CreatePropertyGUI_DoesNotCommitDefaultWhenExistingSerializedValueIsInvalid()
        {
            var cell = new Cell("NotAValue", new FieldType("enum", nameof(DrawerTestEnum)));
            var committed = new List<string>();

            _ = new EnumCellDrawer().CreatePropertyGUI(
                cell,
                "enum",
                nameof(DrawerTestEnum),
                committed.Add);

            Assert.That(committed, Is.Empty);
        }

        [Test]
        public void CreatePropertyGUI_CommitsDefaultWhenSerializedValueIsEmpty()
        {
            var cell = new Cell(string.Empty, new FieldType("enum", nameof(DrawerTestEnum)));
            var committed = new List<string>();

            _ = new EnumCellDrawer().CreatePropertyGUI(
                cell,
                "enum",
                nameof(DrawerTestEnum),
                committed.Add);

            Assert.That(committed, Is.EqualTo(new[] { "Alpha" }));
        }
    }
}

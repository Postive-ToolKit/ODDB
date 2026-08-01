using System.Collections.Generic;
using NUnit.Framework;
using TeamODD.ODDB.Editors.PropertyDrawers;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;
using UnityEngine.UIElements;

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

        [Test]
        public void ReusableStringDrawer_RebindsControlWithoutCommitting()
        {
            var drawer = new StringCellDrawer();
            var committed = new List<string>();
            var field = (TextField)drawer.CreateReusablePropertyGUI("string", string.Empty, committed.Add);

            drawer.BindPropertyGUI(
                field,
                new Cell("first", new FieldType("string", string.Empty)),
                "string",
                string.Empty);
            drawer.BindPropertyGUI(
                field,
                new Cell("second", new FieldType("string", string.Empty)),
                "string",
                string.Empty);

            Assert.That(field.value, Is.EqualTo("second"));
            Assert.That(committed, Is.Empty);
        }

        [Test]
        public void ReusableEnumDrawer_PreservesDefaultCommitWhileBinding()
        {
            var drawer = new EnumCellDrawer();
            var committed = new List<string>();
            var field = drawer.CreateReusablePropertyGUI("enum", nameof(DrawerTestEnum), committed.Add);

            drawer.BindPropertyGUI(
                field,
                new Cell(string.Empty, new FieldType("enum", nameof(DrawerTestEnum))),
                "enum",
                nameof(DrawerTestEnum));

            Assert.That(committed, Is.EqualTo(new[] { "Alpha" }));
        }

        [Test]
        public void CustomDrawer_DirectCallPreservesDelegatedElementType()
        {
            var cell = new Cell("value", new FieldType("custom", "string"));

            var element = new CustomCellDrawer().CreatePropertyGUI(
                cell,
                "custom",
                "string",
                _ => { });

            Assert.That(element, Is.TypeOf<TextField>());
        }

        [TestCase("string")]
        [TestCase("int")]
        [TestCase("float")]
        [TestCase("bool")]
        [TestCase("enum")]
        [TestCase("resource")]
        [TestCase("view")]
        [TestCase("custom")]
        public void BuiltInCellDrawer_SupportsVirtualizedReuse(string typeKey)
        {
            Assert.That(CellDrawerRegistry.Get(typeKey), Is.InstanceOf<IODDBReusableCellDrawer>());
        }
    }
}

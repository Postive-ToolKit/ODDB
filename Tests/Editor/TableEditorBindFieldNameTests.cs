using NUnit.Framework;
using TeamODD.ODDB.Editors.UI;
using TeamODD.ODDB.Runtime.Entities;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class TableEditorBindFieldNameTests
    {
        [Test]
        public void GetBindTypeFieldName_SkipsODDBEntityInfrastructureFields()
        {
            Assert.That(TableEditor.GetBindTypeFieldName(typeof(BoundItemData), 0), Is.EqualTo("Name"));
            Assert.That(TableEditor.GetBindTypeFieldName(typeof(BoundItemData), 1), Is.EqualTo("Desc"));
        }

        private sealed class BoundItemData : ODDBEntity
        {
#pragma warning disable 0169
            private string _name;
            private string _desc;
#pragma warning restore 0169
        }
    }
}

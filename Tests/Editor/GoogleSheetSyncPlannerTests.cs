using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Utils.Sheets;
using TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class GoogleSheetSyncPlannerTests
    {
        [Test]
        public void RemovedManagedField_ProducesPhysicalColumnDelete()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "Keep", "Old" },
                new[] { "#TYPE", "ID", "string", "int" });
            var desired = Sheet(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            var deletion = plan.Operations.Single(operation => operation.Kind == GoogleSheetColumnOperationKind.Delete);
            Assert.That(deletion.FromIndex, Is.EqualTo(3));
            Assert.That(deletion.DisplayName, Is.EqualTo("Old"));
            Assert.That(plan.DeletedColumnNames, Is.EqualTo(new[] { "Old" }));
        }

        [Test]
        public void LegacyRemovedMarker_IsMovedOutThenPhysicallyDeleted()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "#REMOVED Old", "Keep" },
                new[] { "#TYPE", "ID", "", "string" });
            var desired = Sheet(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            Assert.That(plan.Operations.Any(operation => operation.Kind == GoogleSheetColumnOperationKind.Move), Is.True);
            Assert.That(plan.Operations.Any(operation =>
                operation.Kind == GoogleSheetColumnOperationKind.Delete
                && operation.DisplayName == "Old"), Is.True);
        }

        [Test]
        public void UnmanagedUserColumn_IsPreserved()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "Designer Notes", "Keep" },
                new[] { "#TYPE", "ID", "", "string" },
                new[] { "", "row", "do not delete", "value" });
            var desired = Sheet(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            Assert.That(plan.Operations.Any(operation =>
                operation.Kind == GoogleSheetColumnOperationKind.Delete
                && operation.DisplayName == "Designer Notes"), Is.False);
        }

        [Test]
        public void MatchingMetadata_IsIdempotent()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });
            current.ColumnKeys[0] = GoogleSheetConfig.SYSTEM_NAME_COLUMN_KEY;
            current.ColumnKeys[1] = GoogleSheetConfig.SYSTEM_ID_COLUMN_KEY;
            current.ColumnKeys[2] = GoogleSheetConfig.FIELD_COLUMN_PREFIX + "Keep";
            var desired = Sheet(
                new[] { "#NAME", "ID", "Keep" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            Assert.That(plan.Operations, Is.Empty);
            Assert.That(plan.MetadataWrites, Is.Empty);
        }

        [Test]
        public void RenamedField_IsInsertPlusDeleteWithoutStableFieldId()
        {
            var current = Snapshot(
                new[] { "#NAME", "ID", "OldName" },
                new[] { "#TYPE", "ID", "string" });
            var desired = Sheet(
                new[] { "#NAME", "ID", "NewName" },
                new[] { "#TYPE", "ID", "string" });

            var plan = GoogleSheetSyncPlanner.BuildColumnPlan(desired, current);

            Assert.That(plan.Operations.Any(operation => operation.Kind == GoogleSheetColumnOperationKind.Insert), Is.True);
            Assert.That(plan.Operations.Any(operation => operation.Kind == GoogleSheetColumnOperationKind.Delete), Is.True);
        }

        private static GoogleSheetSnapshot Snapshot(params string[][] rows)
        {
            return new GoogleSheetSnapshot
            {
                SheetId = 10,
                Title = "Items_item",
                ColumnCount = rows.Max(row => row.Length),
                Values = rows.Select(row => row.ToList()).ToList()
            };
        }

        private static SheetInfo Sheet(params string[][] rows)
        {
            return new SheetInfo("Items", "item")
            {
                Values = rows.Select(row => row.ToList()).ToList()
            };
        }
    }
}

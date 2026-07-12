using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TeamODD.ODDB.Runtime.Entities;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class ODDBEntityFieldMappingTests
    {
        [Test]
        public void GetFieldFields_SkipsODDBEntityInfrastructureFields()
        {
            var method = typeof(ODDBEntity).GetMethod(
                "GetFieldFields",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);

            var fields = (List<FieldInfo>)method.Invoke(null, new object[] { typeof(BoundEntity) });

            Assert.That(fields, Has.Count.EqualTo(2));
            Assert.That(fields.Select(field => field.Name), Is.EqualTo(new[] { "_name", "_amount" }));
            Assert.That(fields.All(field => field.DeclaringType != typeof(ODDBEntity)), Is.True);
        }

        private sealed class BoundEntity : ODDBEntity
        {
#pragma warning disable 0169
            private string _name;
            private int _amount;
#pragma warning restore 0169
        }
    }
}

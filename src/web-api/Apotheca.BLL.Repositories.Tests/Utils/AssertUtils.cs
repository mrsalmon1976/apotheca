using MongoDB.Bson;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.BLL.Repositories.Tests.Utils
{
    internal static class AssertUtils
    {
        public static void IsEqualsFilter(BsonDocument filterDefinition, string expectedPropertyName, string expectedPropertyValue)
        {
            Assert.Contains(expectedPropertyName, filterDefinition.Names.ToList(), $"FilterDefinition does not contain expected property {expectedPropertyName}");

            string actualPropertyValue = filterDefinition["_id"].AsString;
            Assert.AreEqual(expectedPropertyValue, actualPropertyValue, $"'{actualPropertyValue} does not match expected property value '{expectedPropertyValue}'");
        }
    }
}

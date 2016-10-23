using Json4Uwp;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Data.Json;

namespace TestJson
{
    public static class KeyValuePair
    {
        public static KeyValuePair<K, V> Create<K, V>(K key, V value)
        {
            return new KeyValuePair<K, V>(key, value);
        }
    }

    class City
    {
        public string Name { get; set; }
        public int Population { get; set; }
    }

    class Country
    {
        public string Name { get; set; }
        public City Capital { get; set; }
    }

    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestParseBasic()
        {
            Assert.AreEqual(false, JSON.Parse<bool>("false"));
            Assert.AreEqual(true, JSON.Parse<bool>("true"));
            Assert.AreEqual(1234, JSON.Parse<int>("1234"));
            Assert.AreEqual(1234, JSON.Parse<int>("1234.5"));
            Assert.AreEqual(1234.5f, JSON.Parse<float>("1234.5"));
            Assert.AreEqual(1234.5, JSON.Parse<double>("1234.5"));
            Assert.AreEqual(@"Jay's son name is ""Jayson""", JSON.Parse<string>(@"""Jay's son name is \""Jayson\"""""));
            Assert.AreEqual(null, JSON.Parse<DateTime?>("null"));
            Assert.AreEqual(1234, JSON.Parse<int?>("1234"));
            Assert.IsNull(JSON.Parse<object>("null"));
            Assert.IsNotNull(JSON.Parse<IJsonValue>("null"));
            Assert.AreEqual(JsonValueType.Null, JSON.Parse<IJsonValue>("null").ValueType);
        }

        [TestMethod]
        public void TestParseObject()
        {
            {
                var biggest = JSON.Parse<City>(@"{""name"":""Shanghai"", ""population"":24256800}");
                Assert.AreEqual("Shanghai", biggest.Name);
                Assert.AreEqual(24256800, biggest.Population);
            }
            {
                var secondAndThird = JSON.Parse<City[]>(@"[{""name"":""Karachi"", ""population"":23500000}, {""Name"":""Beijing"", ""Population"":21516000}]");
                Assert.AreEqual(2, secondAndThird.Length);
                Assert.AreEqual("Karachi", secondAndThird[0].Name);
                Assert.AreEqual(23500000, secondAndThird[0].Population);
                Assert.AreEqual("Beijing", secondAndThird[1].Name);
                Assert.AreEqual(21516000, secondAndThird[1].Population);
            }
            {
                var secondAndThird = JSON.Parse<List<City>>(@"[{""Name"":""Karachi"", ""Population"":23500000}, {""name"":""Beijing"", ""population"":21516000}]");
                Assert.AreEqual(2, secondAndThird.Count);
                Assert.AreEqual("Karachi", secondAndThird[0].Name);
                Assert.AreEqual(23500000, secondAndThird[0].Population);
                Assert.AreEqual("Beijing", secondAndThird[1].Name);
                Assert.AreEqual(21516000, secondAndThird[1].Population);
            }
            {
                var country = JSON.Parse<Country>(@"{""capital"":{""population"":8673713, ""name"":""London""}, ""name"":""United Kingdom""}");
                Assert.AreEqual("United Kingdom", country.Name);
                Assert.AreEqual("London", country.Capital.Name);
                Assert.AreEqual(8673713, country.Capital.Population);
            }
            {
                var countryAndCaptital = JSON.Parse<Dictionary<string, City>>(@"{""United Kingdom"":{""population"":8673713, ""name"":""London""}}");
                Assert.AreEqual(1, countryAndCaptital.Count);
                Assert.AreEqual("London", countryAndCaptital["United Kingdom"].Name);
                Assert.AreEqual(8673713, countryAndCaptital["United Kingdom"].Population);
            }
        }

        [TestMethod]
        public void TestParseNonGenericCollections()
        {
            CollectionAssert.AreEqual(
                new[] { 1, 2, 3, 4 },
                JSON.Parse<int[]>("[1,2,3,4]"));

            CollectionAssert.AreEqual(
                new[] { "a", "b", "c" },
                JSON.Parse<string[]>(@"[""a"",""b"",""c""]"));

            CollectionAssert.AreEqual(new List<KeyValuePair<string, int>> {
                    KeyValuePair.Create("a", 1),
                    KeyValuePair.Create("b", 2),
                    KeyValuePair.Create("c", 3)},
                JSON.Parse<Dictionary<string, int>>(
                @"{""a"":1,""b"":2,""c"":3}").OrderBy(p => p.Key).ToList());
        }

        [TestMethod]
        public void TestParseGenericCollections()
        {
            CollectionAssert.AreEqual(new List<KeyValuePair<string, int>> {
                    KeyValuePair.Create("a", 1),
                    KeyValuePair.Create("b", 2),
                    KeyValuePair.Create("c", 3)},
                JSON.Parse<Dictionary<string, int>>(
                @"{""a"":1,""b"":2,""c"":3}").OrderBy(p => p.Key).ToList());
        }
    }
}

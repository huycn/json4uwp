using Json4Uwp;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestJson
{
    public static class KeyValuePair
    {
        public static KeyValuePair<K, V> Create<K, V>(K key, V value)
        {
            return new KeyValuePair<K, V>(key, value);
        }
    }

    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestParseBasic()
        {
            Assert.AreEqual(null, JSON.Parse<object>("null"));
            Assert.AreEqual(false, JSON.Parse<bool>("false"));
            Assert.AreEqual(true, JSON.Parse<bool>("true"));
            Assert.AreEqual(1234, JSON.Parse<int>("1234"));
            Assert.AreEqual(1234, JSON.Parse<int>("1234.5"));
            Assert.AreEqual(1234.5f, JSON.Parse<float>("1234.5"));
            Assert.AreEqual(1234.5, JSON.Parse<double>("1234.5"));
            Assert.AreEqual(@"Jay's son name is ""Jayson""", JSON.Parse<string>(@"""Jay's son name is \""Jayson\"""""));
            Assert.AreEqual(null, JSON.Parse<DateTime?>("null"));
            Assert.AreEqual(1234, JSON.Parse<int?>("1234"));
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

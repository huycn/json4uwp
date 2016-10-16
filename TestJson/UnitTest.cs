using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Json4Uwp;
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
        public void TestBasicParsing()
        {
            Assert.AreEqual(JSON.Parse<object>("null"), null);
            Assert.AreEqual(JSON.Parse<bool>("false"), false);
            Assert.AreEqual(JSON.Parse<bool>("true"), true);
            Assert.AreEqual(JSON.Parse<int>("1234"), 1234);
            Assert.AreEqual(JSON.Parse<int>("1234.5"), 1234);
            Assert.AreEqual(JSON.Parse<float>("1234.5"), 1234.5f);
            Assert.AreEqual(JSON.Parse<double>("1234.5"), 1234.5);
            Assert.AreEqual(JSON.Parse<string>(@"""Jay's son name is \""Jayson\"""""), @"Jay's son name is ""Jayson""");
            Assert.AreEqual(JSON.Parse<DateTime?>("null"), null);
            Assert.AreEqual(JSON.Parse<int?>("1234"), 1234);

            CollectionAssert.AreEqual(JSON.Parse<int[]>(
                "[1,2,3,4]"),
                new[] { 1, 2, 3, 4 });

            CollectionAssert.AreEqual(JSON.Parse<string[]>(
                @"[""a"",""b"",""c""]"),
                new[] { "a", "b", "c" });

            CollectionAssert.AreEqual(JSON.Parse<Dictionary<string, int>>(
                @"{""a"":1,""b"":2,""c"":3}").OrderBy(p => p.Key).ToList(),
                new List<KeyValuePair<string, int>> {
                    KeyValuePair.Create("a", 1),
                    KeyValuePair.Create("b", 2),
                    KeyValuePair.Create("c", 3)});
        }
    }
}

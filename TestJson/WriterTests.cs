using Json4Uwp;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Data.Json;

namespace TestJson
{
    class Person
    {
        [JSONKey("name")]
        public string FirstAndLastName { get; set; }

        public int Age { get; set; }
    }

    [TestClass]
    public class WriterTests
    {
        [TestMethod]
        public void TestWriteBasic()
        {
            Assert.AreEqual("null", JSON.Stringify(null));
            Assert.AreEqual("true", JSON.Stringify(true));
            Assert.AreEqual("true", JSON.Stringify((bool?)true));
            Assert.AreEqual("false", JSON.Stringify(false));
            Assert.AreEqual("false", JSON.Stringify((bool?)false));
            Assert.AreEqual("1234", JSON.Stringify(1234));
            Assert.AreEqual("1234.5", JSON.Stringify(1234.5));
            Assert.AreEqual("1234.5", JSON.Stringify((double?)1234.5));
            Assert.AreEqual("\"2016-10-16T14:08:04.677Z\"", JSON.Stringify(DateTimeOffset.Parse("2016-10-16T14:08:04.677Z")));
            Assert.AreEqual(@"""Jay's son name is \""Jayson\""""", JSON.Stringify(@"Jay's son name is ""Jayson"""));
            Assert.AreEqual(@"""\\ \"" ' \b \f \n \r \t \u001a   " + "\u4f60\u597d\"", JSON.Stringify("\\ \" ' \b \f \n \r \t \x1a \x7f \x9f \u4f60\u597d"));
            Assert.AreEqual("\"Hello\"", JSON.Stringify((object)"Hello"));
            Assert.AreEqual("{\"method\":\"getBirthdayDate\",\"params\":{\"user\":\"bob\",\"format\":{}}}", JSON.Stringify(new { method = "getBirthdayDate", @params = new { user = "bob", format = new { }  } }));
            Assert.AreEqual("{\"name\":\"Alice\",\"Age\":23}", JSON.Stringify(new Person { FirstAndLastName = "Alice", Age = 23 }));
            Assert.AreEqual("{\"name\":\"Alice\",\"age\":23}", JSON.Stringify(new Person { FirstAndLastName = "Alice", Age = 23 }, StringifyOptions.LowerCamelCase));
        }

        [TestMethod]
        public void TestWriteDataJson()
        {
            Assert.AreEqual("null", JSON.Stringify(JsonValue.CreateNullValue()));
            Assert.AreEqual("true", JSON.Stringify(JsonValue.CreateBooleanValue(true)));
            Assert.AreEqual("1234", JSON.Stringify(JsonValue.CreateNumberValue(1234)));
            Assert.AreEqual("1234.5", JSON.Stringify(JsonValue.CreateNumberValue(1234.5)));
            Assert.AreEqual("\"Hello\"", JSON.Stringify(JsonValue.CreateStringValue("Hello")));

            Assert.AreEqual("[1,false,\"xyz\",4.5,{},[]]", JSON.Stringify(new JsonArray {
                JsonValue.CreateNumberValue(1),
                JsonValue.CreateBooleanValue(false),
                JsonValue.CreateStringValue("xyz"),
                JsonValue.CreateNumberValue(4.5),
                new JsonObject(),
                new JsonArray(),
            }));

            Assert.AreEqual(@"{""aNumber"":123,""aBool"":true,""aString"":""abc"",""aArray"":[1,2,3],""aObject"":{""key"":""value"",""null"":null}}",
                JSON.Stringify(new JsonObject {
                    { "aNumber", JsonValue.CreateNumberValue(123) },
                    { "aBool", JsonValue.CreateBooleanValue(true) },
                    { "aString", JsonValue.CreateStringValue("abc") },
                    { "aArray", new JsonArray { JsonValue.CreateNumberValue(1), JsonValue.CreateNumberValue(2), JsonValue.CreateNumberValue(3)} },
                    { "aObject", new JsonObject { { "key", JsonValue.CreateStringValue("value") }, { "null", JsonValue.CreateNullValue() } } },
                }));
        }

        [TestMethod]
        public void TestWriteNonGenericCollections()
        {
            Assert.AreEqual("[1,2,3]", JSON.Stringify(new[] { 1, 2, 3 }));
            Assert.AreEqual(@"[""a"",null,""c""]", JSON.Stringify(new[] { "a", null, "c" }));
            Assert.AreEqual("[1,false,\"xyz\",4.5,{},[]]", JSON.Stringify(new object[] { 1, false, "xyz", 4.5f, new object(), new object[0] }));
            Assert.AreEqual(@"[1,2,3]", JSON.Stringify(new ArrayList { 1, 2, 3 }));
            Assert.AreEqual(@"[""a"",""b"",""c""]", JSON.Stringify(new ArrayList { "a", "b", "c" }));
            Assert.AreEqual(@"[""a"",2,null]", JSON.Stringify(new ArrayList { "a", 2, null }));
            Assert.AreEqual(@"{""a"":1}", JSON.Stringify(new Hashtable { { "a", 1 } }));
            Assert.AreEqual(@"{""a"":null}", JSON.Stringify(new Hashtable { { "a", null } }));
        }

        class CustomDict : IDictionary<string, double>
        {
            Dictionary<string, double> dicts = new Dictionary<string, double>();
            public double this[string key] { get { return dicts[key]; } set { dicts[key] = value; } }
            public int Count { get { return dicts.Count; } }
            public bool IsReadOnly { get { return false; } }
            public ICollection<string> Keys { get { return dicts.Keys; } }
            public ICollection<double> Values { get { return dicts.Values; } }
            public void Add(KeyValuePair<string, double> item) { dicts.Add(item.Key, item.Value); }
            public void Add(string key, double value) { dicts.Add(key, value); }
            public void Clear() { dicts.Clear(); }
            public bool Contains(KeyValuePair<string, double> item) { return dicts.Contains(item); }
            public bool ContainsKey(string key) { return dicts.ContainsKey(key); }
            public void CopyTo(KeyValuePair<string, double>[] array, int arrayIndex) { ((IDictionary<string, double>)dicts).CopyTo(array, arrayIndex); }
            public IEnumerator<KeyValuePair<string, double>> GetEnumerator() { return dicts.GetEnumerator(); }
            public bool Remove(KeyValuePair<string, double> item) { return ((IDictionary<string, double>)dicts).Remove(item); }
            public bool Remove(string key) { return dicts.Remove(key); }
            public bool TryGetValue(string key, out double value) { return dicts.TryGetValue(key, out value); }
            IEnumerator IEnumerable.GetEnumerator() { return dicts.GetEnumerator(); }
        }

        [TestMethod]
        public void TestWriteGenericCollections()
        {
            Assert.AreEqual("[1,2,3]", JSON.Stringify(new List<int> { 1, 2, 3 }));
            Assert.AreEqual(@"[""a"",null,""c""]", JSON.Stringify(new List<string> { "a", null, "c" }));
            Assert.AreEqual(@"{""a"":1}", JSON.Stringify(new Dictionary<string, int> { { "a", 1 } }));
            Assert.AreEqual(@"{""a"":null}", JSON.Stringify(new Dictionary<string, int?> { { "a", null } }));
            Assert.AreEqual(@"{""a"":""abc""}", JSON.Stringify(new Dictionary<string, string> { { "a", "abc" } }));
            Assert.AreEqual(@"{""a"":1}", JSON.Stringify(new CustomDict { { "a", 1 } }));
        }

        class Directory
        {
            public string Name { get; set; }
            public Directory Next { get; set; }
            public Directory[] Children { get; set; }
        }

        [TestMethod]
        public void TestWriteRecursive()
        {
            Assert.AreEqual(@"{""Name"":""abc"",""Next"":null,""Children"":[{""Name"":""child"",""Next"":null,""Children"":[]}]}",
                JSON.Stringify(new Directory { Name = "abc", Children = new[] { new Directory { Name = "child", Children = new Directory[0] } } }));
        }
    }
}

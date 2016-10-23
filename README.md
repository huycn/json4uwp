# json4uwp
A minimal JSON parser for UWP

# Goals
- As small as possible (single source file) for easy to integrate
- Easy to use
- As fast as possible without compromise these two goals above

# Usage
## Parsing
To parse JSON string, you use generic method JSON.Parse() providing the expected type.
``` C#
class City {
    public string Name { get; set; }
    public int Population { get; set; }
}
...
var cities = JSON.Parse<City[]>(@"[
  {""name"":""Shanghai"",
   ""population"":24256800},
  {""name"":""Karachi"",
   ""population"":23500000},
  {""bame"":""Beijing"",
   ""population"":21516000}
]");
```

The parser is only a wrapper around `Windows.Data.Json` parser. So:
``` C#
object obj = JSON.Parse<object>("[1,2,3]");
// obj is an object of type Windows.Data.Json.JsonArray
```

Note however that:
``` C#
object obj = JSON.Parse<object>("null");
// obj == null

IJsonValue obj2 = JSON.Parse<Windows.Data.Json.IJsonValue>("null");
// obj2 != null && obj2.ValueType == Windows.Data.Json.JsonValueType.Null
```

## Serialization
Conversion to string is done with JSON.Stringify(). It will convert into JSON anything you throw at it, including anonymous types:
```
string json = JSON.Stringify(new object { name = "Alice", age = 23 });
// json is: {"name":"Alice","age":23}
```
By default, the property name written as is, but you can pass `StringifyOptions.LowerCamelCase` to the method to convert to lower camel case, commonly used in Javascript. To define a different name for a given property, you need to decorate it with `JsonKey` attribute, e.g.:
```
class MyClass {
  [JsonKey("nameInJs")]
  public string NameInCSharpCode {...}
}
```

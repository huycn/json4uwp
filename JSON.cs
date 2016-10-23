/*
Copyright (c) 2016 Huy Cuong Nguyen

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Windows.Data.Json;

namespace Json4Uwp
{
    [AttributeUsage(AttributeTargets.Property)]
    public class JSONKeyAttribute : Attribute
    {
        public JSONKeyAttribute(string name) { KeyName = name; }
        public string KeyName { get; set; }
    }

    [Flags]
    public enum StringifyOptions
    {
        None            = 0,
        LowerCamelCase  = 1,    // Convert the first letter of property name to lower case
    }

    public static class JSON
    {
        public static string Stringify(object value, StringifyOptions options = StringifyOptions.None)
        {
            if (value != null)
            {
                var buffer = new StringBuilder();
                GetSerializer(value.GetType())(buffer, value, options);
                return buffer.ToString();
            }
            return "null";
        }

        public static T Parse<T>(string json)
        {
            int index = 0;
            char nonSpace = '\0';
            foreach (var c in json)
            {
                if (!Char.IsWhiteSpace(c))
                {
                    nonSpace = c;
                    break;
                }
                ++index;
            }
            switch (nonSpace)
            {
                case '{':
                    return (T)CachedConverter<T>.Instance(JsonObject.Parse(json));
                case '[':
                    return (T)CachedConverter<T>.Instance(JsonArray.Parse(json));
                default:
                    return (T)CachedConverter<T>.Instance(JsonValue.Parse(json));
            }
        }

        public static bool TryParse<T>(string json, out T output)
        {
            output = default(T);
            try
            {
                output = Parse<T>(json);
                return true;
            }
            catch (Exception e)
            {
                Debug.Fail(e.Message);
            }
            return false;
        }

        #region Private Implementation: Serializer
        const string DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffZ";

        delegate void Serializer(StringBuilder output, object value, StringifyOptions options);

        static Serializer GetSerializer(Type type)
        {
            return (Serializer) typeof(CachedSerializer<>).MakeGenericType(type)
                    .GetField("Instance", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        }

        static class CachedSerializer<T>
        {
            public static readonly Serializer Instance = GetSerializerForType(typeof(T));

            private static Serializer GetSerializerForType(Type type)
            {
                if (type == typeof(string))
                {
                    return WriteString;
                }
                var typeInfo = type.GetTypeInfo();
                if (!typeInfo.IsClass && !typeInfo.IsGenericType)
                {
                    if (type == typeof(bool))
                    {
                        return WriteBoolean;
                    }
                    if (typeInfo.IsPrimitive)
                    {
                        return WritePrimitive;
                    }
                    if (type == typeof(DateTimeOffset))
                    {
                        return WriteDateTimeOffset;
                    }
                    if (type == typeof(DateTime))
                    {
                        return WriteDateTime;
                    }
                }
                if (typeInfo.IsArray)
                {
                    return CreateArraySerializer(typeInfo);
                }
                if (typeof(IJsonValue).IsAssignableFrom(type))
                {
                    return WriteJsonValue;
                }
                // Dictionary detection should go before array
                if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    return CreateMapSerializer(typeInfo);
                }
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    var dictType = typeInfo.ImplementedInterfaces
                        .Select(t => t.GetTypeInfo())
                        .FirstOrDefault(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                    if (dictType != null)
                    {
                        var pairType = dictType.ImplementedInterfaces
                            .Select(t => t.GetTypeInfo())
                            .First(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                            .GenericTypeArguments[0].GetTypeInfo();
                        if (pairType.IsGenericType && pairType.GenericTypeArguments.Length >= 2)
                        {
                            return CreateGenericMapSerializer(pairType);
                        }
                    }
                    return CreateArraySerializer(typeInfo);
                }
                if (!typeInfo.IsAbstract && type != typeof(object))
                {
                    if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return CreateNullableSerializer(typeInfo.GenericTypeArguments[0]);
                    }
                    return CreateObjectSerializer(type, typeInfo.IsSealed);
                }
                return WriteDynamicObject;
            }
        }

        static void WriteString(StringBuilder output, object value, StringifyOptions options)
        {
            if (value == null)
                output.Append("null");
            else
                DoWriteString(output, (string)value);
        }

        static void DoWriteString(StringBuilder output, string str)
        {
            output.EnsureCapacity(output.Length + str.Length + 2);
            output.Append('"');
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"': output.Append("\\\""); break;
                    case '\\': output.Append("\\\\"); break;
                    case '\b': output.Append("\\b"); break;
                    case '\f': output.Append("\\f"); break;
                    case '\n': output.Append("\\n"); break;
                    case '\r': output.Append("\\r"); break;
                    case '\t': output.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            output.Append("\\u00").Append(((int)c).ToString("x2"));
                        else if (c < 0x7f || c >= 0xa0)
                            output.Append(c);
                        break;
                }
            }
            output.Append('"');
        }

        static void WriteJsonValue(StringBuilder output, object value, StringifyOptions options)
        {
            output.Append(((IJsonValue)value).Stringify());
        }

        static void WriteBoolean(StringBuilder output, object value, StringifyOptions options)
        {
            output.Append((bool)value ? "true" : "false");
        }
        static void WritePrimitive(StringBuilder output, object value, StringifyOptions options)
        {
            output.Append(value);
        }
        static void WriteDateTimeOffset(StringBuilder output, object value, StringifyOptions options)
        {
            output.Append('"')
                .Append(((DateTimeOffset)value).ToUniversalTime().ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture))
                .Append('"');
        }
        static void WriteDateTime(StringBuilder output, object value, StringifyOptions options)
        {
            output.Append(((DateTime)value).ToUniversalTime().ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture));
        }

        static Serializer CreateArraySerializer(TypeInfo typeInfo)
        {
            Type itemType;
            if (typeInfo.HasElementType)
                itemType = typeInfo.GetElementType();
            else if (typeInfo.IsGenericType && typeInfo.GenericTypeArguments.Length > 0)
                itemType = typeInfo.GenericTypeArguments[0];
            else
                itemType = null;

            if (itemType != null && itemType != typeof(object) && !itemType.GetTypeInfo().IsAbstract)
            {
                var itemSerializer = GetSerializer(itemType);
                return new ArrayWriteHelper((output, value, options) =>
                {
                    if (itemSerializer == null) // in case of recursion
                        itemSerializer = GetSerializer(itemType);
                    foreach (var item in (IEnumerable)value)
                    {
                        itemSerializer(output, item, options);
                        output.Append(',');
                    }
                }).Serializer;
            }
            return new ArrayWriteHelper((output, value, options) =>
            {
                foreach (var item in (IEnumerable)value)
                {
                    if (item == null)
                        output.Append("null");
                    else
                        GetSerializer(item.GetType())(output, item, options);
                    output.Append(',');
                }
            }).Serializer;
        }

        class ArrayWriteHelper
        {
            Serializer WriteContent;
            public ArrayWriteHelper(Serializer contentWriter) { WriteContent = contentWriter; }

            public void Serializer(StringBuilder output, object value, StringifyOptions options)
            {
                if (value == null)
                {
                    output.Append("null");
                }
                else
                {
                    output.Append('[');
                    int oldLength = output.Length;
                    WriteContent(output, value, options);
                    int newLength = output.Length;
                    if (oldLength != newLength)
                        output[newLength - 1] = ']';
                    else
                        output.Append(']');
                }
            }
        }

        static Serializer CreateMapSerializer(TypeInfo typeInfo)
        {
            if (typeInfo.IsGenericType && typeInfo.GenericTypeArguments.Length >= 2)
            {
                var valueType = typeInfo.GenericTypeArguments[1];
                if (valueType != typeof(object) && !valueType.GetTypeInfo().IsAbstract)
                {
                    var valueSerializer = GetSerializer(valueType);
                    return new MapWriteHelper((output, value, options) =>
                    {
                        if (valueSerializer == null) // in case of recursion
                            valueSerializer = GetSerializer(valueType);
                        foreach (DictionaryEntry item in (IDictionary)value)
                        {
                            DoWriteString(output, item.Key.ToString());
                            output.Append(':');
                            valueSerializer(output, item.Value, options);
                            output.Append(',');
                        }
                    }).Serializer;
                }
            }
            return new MapWriteHelper((output, value, options) =>
            {
                foreach (DictionaryEntry item in (IDictionary)value)
                {
                    DoWriteString(output, item.Key.ToString());
                    output.Append(':');
                    if (item.Value == null)
                        output.Append("null");
                    else
                        GetSerializer(item.Value.GetType())(output, item.Value, options);
                    output.Append(',');
                }
            }).Serializer;
        }

        static Serializer CreateGenericMapSerializer(TypeInfo pairType)
        {
            var keyProperty = pairType.GetDeclaredProperty("Key");
            var valueProperty = pairType.GetDeclaredProperty("Value");
            var valueType = pairType.GenericTypeArguments[1];
            var valueSerializer = GetSerializer(valueType);
            return new MapWriteHelper((output, value, options) =>
            {
                if (valueSerializer == null) // in case of recursion
                    valueSerializer = GetSerializer(valueType);
                foreach (var item in (IEnumerable)value)
                {
                    DoWriteString(output, keyProperty.GetValue(item).ToString());
                    output.Append(':');
                    valueSerializer(output, valueProperty.GetValue(item), options);
                    output.Append(',');
                }
            }).Serializer;
        }

        class MapWriteHelper
        {
            Serializer WriteContent;
            public MapWriteHelper(Serializer contentWriter) { WriteContent = contentWriter; }

            public void Serializer(StringBuilder output, object value, StringifyOptions options)
            {
                if (value == null)
                {
                    output.Append("null");
                }
                else
                {
                    output.Append('{');
                    int oldLength = output.Length;
                    WriteContent(output, value, options);
                    int newLength = output.Length;
                    if (oldLength != newLength)
                        output[newLength - 1] = '}';
                    else
                        output.Append('}');
                }
            }
        }

        static Serializer CreateNullableSerializer(Type innerType)
        {
            var innerSer = GetSerializer(innerType);
            return (output, value, options) =>
            {
                if (value == null)
                {
                    output.Append("null");
                }
                else
                {
                    if (innerSer == null) // in case of recursion
                        innerSer = GetSerializer(innerType);
                    innerSer(output, value, options);
                }
            };
        }

        static Serializer CreateObjectSerializer(Type type, bool sealedType)
        {
            var props = GetPropertySerializers(type);
            return (output, value, options) => {
                if (value == null)
                {
                    output.Append("null");
                }
                else
                {
                    if (sealedType || value.GetType() == type)
                    {
                        DoWriteObject(output, value, props, options);
                    }
                    else
                    {
                        DoWriteObject(output, value, GetPropertySerializers(value.GetType()), options);
                    }
                }
            };
        }

        static void DoWriteObject(StringBuilder output, object obj, PropertySerializer[] props, StringifyOptions options)
        {
            if (props.Length > 0)
            {
                output.Append('{');
                foreach (var entry in props)
                {
                    DoWriteString(output, (options & StringifyOptions.LowerCamelCase) != 0 ? ToLowerCamelCase(entry.Key) : entry.Key);
                    output.Append(':');
                    entry.WriteValue(output, obj, options);
                    output.Append(',');
                }
                output[output.Length - 1] = '}';
            }
            else
            {
                output.Append("{}");
            }
        }

        static string ToLowerCamelCase(string name)
        {
            if (name.Length > 0 && Char.IsUpper(name[0]))
            {
                return Char.ToLower(name[0]) + name.Substring(1);
            }
            return name;
        }

        static void WriteDynamicObject(StringBuilder output, object value, StringifyOptions options)
        {
            if (value == null)
            {
                output.Append("null");
            }
            else
            {
                var realType = value.GetType();
                if (realType != typeof(object))
                    GetSerializer(realType)(output, value, options);
                else
                    output.Append("{}");
            }
        }

        class PropertySerializer
        {
            public string Key;
            public Serializer WriteValue;
        }
        static PropertySerializer[] GetPropertySerializers(Type type)
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var result = new PropertySerializer[props.Length];
            for (int i = 0, length = props.Length; i < length; ++i)
            {
                var prop = props[i];
                var attr = prop.GetCustomAttribute<JSONKeyAttribute>(false);
                var key = attr != null ? attr.KeyName : prop.Name;
                var writer = GetSerializer(prop.PropertyType);
                if (writer != null) 
                {
                    result[i] = new PropertySerializer
                    {
                        Key = key,
                        WriteValue = (output, value, options) => writer(output, prop.GetValue(value), options)
                    };
                }
                else // happens when the type definition has recursion
                {
                    var propSer = result[i] = new PropertySerializer();
                    propSer.Key = key;
                    propSer.WriteValue = (output, value, options) =>
                    {
                        var newWriter = GetSerializer(prop.PropertyType);
                        newWriter(output, prop.GetValue(value), options);
                        propSer.WriteValue = (output2, value2, options2) => newWriter(output2, prop.GetValue(value2), options2);
                    };
                }
            }
            return result;
        }

        #endregion

        #region Private Implementation - Deserializer

        delegate object JConverter(IJsonValue input);

        static JConverter GetConverter(Type type)
        {
            return (JConverter) typeof(CachedConverter<>).MakeGenericType(type)
                .GetField("Instance", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        }

        static class CachedConverter<T>
        {
            public static readonly JConverter Instance = GetConverterForType(typeof(T));

            private static JConverter GetConverterForType(Type type)
            {
                if (type == typeof(string))
                {
                    return ConvertToString;
                }
                var typeInfo = type.GetTypeInfo();
                if (!typeInfo.IsClass && !typeInfo.IsGenericType)
                {
                    if (typeInfo.IsPrimitive)
                    {
                        return primitiveConverters[type];
                    }
                    if (type == typeof(DateTimeOffset))
                    {
                        return ConvertToDateTimeOffset;
                    }
                    if (type == typeof(DateTime))
                    {
                        return ConvertToDateTime;
                    }
                }
                if (type.IsArray)
                {
                    return CreateArrayConverter(type);
                }
                if (typeof(IJsonValue).IsAssignableFrom(type))
                {
                    return NoConvertion;
                }
                if (typeof(IDictionary).IsAssignableFrom(type))    // this has to go BEFORE IEnumerable
                {
                    return CreateMapConverter(type, typeInfo);
                }
                var dictType = typeInfo.ImplementedInterfaces
                    .Select(t => t.GetTypeInfo())
                    .FirstOrDefault(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                if (dictType != null)
                {
                    var colType = dictType.ImplementedInterfaces.First(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
                    return CreateGenericMapConverter(type, colType);
                }
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    var colType = typeInfo.ImplementedInterfaces.First(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
                    if (colType != null)
                    {
                        return CreateListConverter(type, colType);
                    }
                }
                else if (!typeInfo.IsAbstract && type != typeof(object))
                {
                    if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return CreateNullableConverter(typeInfo.GenericTypeArguments[0]);
                    }
                    return CreateObjectConverter(type);
                }
                return ToObject;
            }
        }

        static readonly Dictionary<Type, JConverter> primitiveConverters = new Dictionary<Type, JConverter>
        {
            { typeof(bool),   (value) => value.GetBoolean() },
            { typeof(char),   (value) => (char)value.GetNumber() },
            { typeof(sbyte),  (value) => (sbyte)value.GetNumber() },
            { typeof(byte),   (value) => (byte)value.GetNumber() },
            { typeof(short),  (value) => (short)value.GetNumber() },
            { typeof(ushort), (value) => (ushort)value.GetNumber() },
            { typeof(int),    (value) => (int)value.GetNumber() },
            { typeof(uint),   (value) => (uint)value.GetNumber() },
            { typeof(long),   (value) => (long)value.GetNumber() },
            { typeof(ulong),  (value) => (ulong)value.GetNumber() },
            { typeof(float),  (value) => (float)value.GetNumber() },
            { typeof(double), (value) => value.GetNumber() },
        };


        static object ConvertToString(IJsonValue input)
        {
            return input.ValueType == JsonValueType.Null ? null : input.GetString();
        }
        static object ConvertToDateTimeOffset(IJsonValue input)
        {
            return input.ValueType == JsonValueType.Null ? DateTimeOffset.MinValue : DateTimeOffset.Parse(input.GetString());
        }
        static object ConvertToDateTime(IJsonValue input)
        {
            return input.ValueType == JsonValueType.Null ? DateTime.MinValue : DateTime.Parse(input.GetString());
        }

        static JConverter CreateArrayConverter(Type arrType)
        {
            var itemType = arrType.GetElementType();
            var itemConverter = GetConverter(itemType);
            return (value) =>
            {
                if (value.ValueType == JsonValueType.Null)
                    return null;
                if (itemConverter == null) // in case of recursion
                    itemConverter = GetConverter(itemType);
                var jarr = value.GetArray();
                int length = jarr.Count;
                var array = Array.CreateInstance(itemType, length);
                for (int i = 0; i < length; ++i)
                    array.SetValue(itemConverter(jarr[i]), i);
                return array;
            };
        }

        static JConverter CreateListConverter(Type listType, Type colType)
        {
            var itemType = colType.GenericTypeArguments[0];
            var addMethod = colType.GetMethod("Add", new[] { itemType });
            var itemConverter = GetConverter(itemType);
            return (value) =>
            {
                if (value.ValueType == JsonValueType.Null)
                    return null;
                if (itemConverter == null) // in case of recursion
                    itemConverter = GetConverter(itemType);
                var jarr = value.GetArray();
                int length = jarr.Count;
                var list = Activator.CreateInstance(listType);
                foreach (var item in jarr)
                    addMethod.Invoke(list, new[] { itemConverter(item) });
                return list;
            };
        }

        static JConverter CreateMapConverter(Type mapType, TypeInfo mapTypeInfo)
        {
            if (mapTypeInfo.IsGenericType && mapTypeInfo.GenericTypeArguments.Length >= 2)
            {
                var valueType = mapTypeInfo.GenericTypeArguments[1];
                var valueConverter = GetConverter(valueType);
                return (value) =>
                {
                    if (value.ValueType == JsonValueType.Null)
                        return null;
                    if (valueConverter == null) // in case of recursion
                        valueConverter = GetConverter(valueType);
                    var jobject = value.GetObject();
                    IDictionary map = (IDictionary)Activator.CreateInstance(mapType);
                    foreach (var item in jobject)
                        map.Add(item.Key, valueConverter(item.Value));
                    return map;
                };
            }
            else
            {
                return (value) =>
                {
                    if (value.ValueType == JsonValueType.Null)
                        return null;
                    var jobject = value.GetObject();
                    IDictionary map = (IDictionary)Activator.CreateInstance(mapType);
                    foreach (var item in jobject)
                        map.Add(item.Key, item.Value);
                    return map;
                };
            }
        }

        static JConverter CreateGenericMapConverter(Type mapType, Type colType)
        {
            var pairType = colType.GenericTypeArguments[0];
            var addMethod = colType.GetMethod("Add", new[] { pairType });
            var pairTypeInfo = pairType.GetTypeInfo();
            if (pairTypeInfo.IsGenericType && pairTypeInfo.GenericTypeArguments.Length >= 2)
            {
                var constor = pairType.GetConstructor(pairTypeInfo.GenericTypeArguments);
                var valueType = pairTypeInfo.GenericTypeArguments[1];
                var valueConverter = GetConverter(valueType);
                return (value) =>
                {
                    if (value.ValueType == JsonValueType.Null)
                        return null;
                    if (valueConverter == null) // in case of recursion
                        valueConverter = GetConverter(valueType);
                    var jobject = value.GetObject();
                    var map = Activator.CreateInstance(mapType);
                    foreach (var item in jobject)
                    {
                        var pair = constor.Invoke(new object[] { item.Key, valueConverter(item.Value) });
                        addMethod.Invoke(map, new[] { pair });
                    }
                    return map;
                };
            }
            else
            {
                var keyProperty = pairTypeInfo.GetDeclaredProperty("Key");
                var valueProperty = pairTypeInfo.GetDeclaredProperty("Value");
                return (value) =>
                {
                    if (value.ValueType == JsonValueType.Null)
                        return null;
                    var jobject = value.GetObject();
                    var map = Activator.CreateInstance(mapType);
                    foreach (var item in jobject)
                    {
                        var pair = Activator.CreateInstance(pairType);
                        keyProperty.SetValue(pair, item.Key);
                        valueProperty.SetValue(pair, item.Value);
                        addMethod.Invoke(map, new[] { pair });
                    }
                    return map;
                };
            }
        }

        static JConverter CreateNullableConverter(Type innerType)
        {
            var conv = GetConverter(innerType);
            return (value) =>
            {
                if (value.ValueType == JsonValueType.Null)
                    return null;
                if (conv == null) // in case of recursion
                    conv = GetConverter(innerType);
                return conv(value);
            };
        }

        static JConverter CreateObjectConverter(Type type)
        {
            var props = GetPropertyConverters(type);
            return (value) =>
            {
                if (value.ValueType == JsonValueType.Null)
                    return null;
                var jobj = value.GetObject();
                var obj = Activator.CreateInstance(type);
                IJsonValue propVal;
                foreach (var prop in props)
                {
                    if (jobj.TryGetValue(prop.Key, out propVal))
                        prop.SetProperty(propVal, obj);
                    else if (prop.TryLower && jobj.TryGetValue(Char.ToLower(prop.Key[0]) + prop.Key.Substring(1), out propVal))
                        prop.SetProperty(propVal, obj);
                }
                return obj;
            };
        }

        static object NoConvertion(IJsonValue input)
        {
            return input;
        }

        static object ToObject(IJsonValue input)
        {
            return input.ValueType == JsonValueType.Null ? null : input;
        }

        delegate void PropertySetter(IJsonValue value, object obj);
        class PropertyConverter
        {
            public string Key;
            public bool TryLower;
            public PropertySetter SetProperty;
        }
        static PropertyConverter[] GetPropertyConverters(Type type)
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var result = new PropertyConverter[props.Length];
            for (int i = 0, length = props.Length; i < length; ++i)
            {
                var prop = props[i];
                var attr = prop.GetCustomAttribute<JSONKeyAttribute>(false);
                var key = attr != null ? attr.KeyName : prop.Name;
                var conv = GetConverter(prop.PropertyType);
                if (conv != null)
                {
                    result[i] = new PropertyConverter
                    {
                        Key = key,
                        TryLower = attr == null && Char.IsUpper(key[0]),
                        SetProperty = (value, obj) => prop.SetValue(obj, conv(value))
                    };
                }
                else // happens when the type definition has recursion
                {
                    var propConv = result[i] = new PropertyConverter();
                    propConv.Key = key;
                    propConv.TryLower = attr == null && Char.IsUpper(key[0]);
                    propConv.SetProperty = (value, obj) =>
                    {
                        var newConv = GetConverter(prop.PropertyType);
                        prop.SetValue(obj, newConv(value));
                        propConv.SetProperty = (value2, obj2) => prop.SetValue(obj2, newConv(value2));
                    };
                }
            }
            return result;
        }

        #endregion
    }
}

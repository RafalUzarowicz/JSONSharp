using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CSharpTesting;

static internal class JSONFunctions
{
    private static Type[] JSONABLE_TYPES = {
        typeof(int), typeof(string), typeof(bool), typeof(float),
    };

    private static Type[] JSONABLE_ARRAY_TYPES = 
    {
        typeof(List<int>), typeof(List<string>), typeof(List<bool>), typeof(List<float>),
        typeof(HashSet<int>), typeof(HashSet<string>), typeof(HashSet<bool>), typeof(HashSet<float>),
        typeof(int[]), typeof(string[]), typeof(bool[]), typeof(float[]),
    };

    private static Type[] JSONABLE_DICTIONARY_TYPES = 
    {
        typeof(Dictionary<string, int>), typeof(Dictionary<string, string>), typeof(Dictionary<string, bool>), typeof(Dictionary<string, float>),
    };

    private static List<MethodInfo> JSONOBJECT_ADD_METHODS = typeof(JSONObject).GetMethods().ToList().FindAll(x => x.Name.Contains("Add") && x.GetParameters().Length == 2);
    private static List<MethodInfo> JSONARRAY_ADD_METHODS = typeof(JSONArray).GetMethods().ToList().FindAll(x => x.Name.Contains("Add") && x.GetParameters().Length == 1);

    public static JSONObject ConvertToJsonObject(object objectToConvert)
    {
        JSONObject jsonObject = null;
        
        if (objectToConvert?.GetType().GetCustomAttribute<JSONable>() != null)
        {
            jsonObject = new JSONObject();
            
            var fields = objectToConvert.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).ToList().FindAll(x => x.GetCustomAttribute<JSONField>() != null);
            foreach (var fieldInfo in fields)
            {
                string fieldInfoName = fieldInfo.Name;
                if (JSONABLE_TYPES.Contains(fieldInfo.FieldType))
                {
                    var method = JSONOBJECT_ADD_METHODS.FirstOrDefault(x =>
                        x.GetParameters()[0].ParameterType == typeof(string) &&
                        x.GetParameters()[1].ParameterType == fieldInfo.FieldType);
                    method?.Invoke(jsonObject, new object?[]{fieldInfoName, fieldInfo.GetValue(objectToConvert)});
                }
                else if (JSONABLE_ARRAY_TYPES.Contains(fieldInfo.FieldType) || fieldInfo.FieldType.IsArray || fieldInfo.FieldType.GetGenericArguments().Length == 1 && (JSONABLE_TYPES.Contains(fieldInfo.FieldType.GetGenericArguments()[0]) || fieldInfo.FieldType.GetGenericArguments()[0].GetCustomAttribute<JSONable>() != null))
                {
                    JSONArray tmpJsonArray = new JSONArray();
                    
                    IEnumerable enumerable = fieldInfo.GetValue(objectToConvert) as IEnumerable;
                    if (enumerable == null)
                        continue;

                    MethodInfo method = null;

                    if (fieldInfo.FieldType.IsArray)
                    {
                        method = JSONARRAY_ADD_METHODS.FirstOrDefault(x =>
                            x.GetParameters()[0].ParameterType == fieldInfo.FieldType.GetElementType());
                    }
                    else
                    {
                        method = JSONARRAY_ADD_METHODS.FirstOrDefault(x =>
                            x.GetParameters()[0].ParameterType == fieldInfo.FieldType.GetGenericArguments()[0]);
                    }

                    JSONObject tmpInnerJsonObject = null;
                    bool convertToJsonObjects = false;
                    if (method == null)
                    {
                        IEnumerator en = enumerable.GetEnumerator();
                        en.MoveNext();
                        tmpInnerJsonObject = ConvertToJsonObject(en.Current);
                        if (tmpInnerJsonObject != null)
                        {
                            method = JSONARRAY_ADD_METHODS.FirstOrDefault(x =>
                                x.GetParameters()[0].ParameterType == typeof(JSONValue));
                            convertToJsonObjects = true;
                        }
                    }
                    
                    foreach (object item in enumerable.OfType<object>())
                    {
                        if (convertToJsonObjects)
                        {
                            method?.Invoke(tmpJsonArray, new object?[]{ConvertToJsonObject(item)});
                        }
                        else
                        {
                            method?.Invoke(tmpJsonArray, new object?[] {item});
                        }
                    }

                    jsonObject.Add(fieldInfoName, tmpJsonArray);
                }
                else if (JSONABLE_DICTIONARY_TYPES.Contains(fieldInfo.FieldType) || (fieldInfo.GetValue(objectToConvert) as IDictionary) != null)
                {
                    JSONObject tmpJsonObject = new JSONObject();
                    
                    IDictionary dict = fieldInfo.GetValue(objectToConvert) as IDictionary;
                    if (dict == null)
                        break;
                    
                    ICollection dictKeys = dict.Keys;
                    ICollection dictValues = dict.Values;
                    
                    var method = JSONOBJECT_ADD_METHODS.FirstOrDefault(x =>
                        x.GetParameters()[0].ParameterType == typeof(string) &&
                        x.GetParameters()[1].ParameterType == fieldInfo.FieldType.GetGenericArguments()[1]);

                    bool convertToJsonObjects = false;
                    JSONObject tmpInnerJsonObject;
                    
                    if (method == null && dictValues.Count > 0)
                    {
                        IEnumerator en = dictValues.GetEnumerator();
                        en.MoveNext();
                        tmpInnerJsonObject = ConvertToJsonObject(en.Current);
                        if (tmpInnerJsonObject != null)
                        {
                            method = JSONOBJECT_ADD_METHODS.FirstOrDefault(x =>
                                x.GetParameters()[0].ParameterType == typeof(string) &&
                                x.GetParameters()[1].ParameterType == typeof(JSONValue));
                            convertToJsonObjects = true;
                        }
                    }
                    
                    foreach (object item in dictKeys)
                    {
                        if (convertToJsonObjects)
                        {
                            method?.Invoke(tmpJsonObject, new object?[] {item, ConvertToJsonObject(dict[item])});
                        }
                        else
                        {
                            method?.Invoke(tmpJsonObject, new object?[] {item, dict[item]});
                        }
                    }

                    jsonObject.Add(fieldInfoName, tmpJsonObject);
                }
                else if (fieldInfo.FieldType.GetCustomAttribute<JSONable>() != null)
                {
                    var method = JSONOBJECT_ADD_METHODS.FirstOrDefault(x =>
                        x.GetParameters()[0].ParameterType == typeof(string) &&
                        x.GetParameters()[1].ParameterType == typeof(JSONObject).BaseType);
                    JSONObject tmpJsonObject = ConvertToJsonObject(fieldInfo.GetValue(objectToConvert));
                    method?.Invoke(jsonObject, new object?[]{fieldInfoName, tmpJsonObject});
                }
            }
        }
        return jsonObject;
    }

    public static void FillObjectFromJson<T>(T objectToFill, JSONObject jsonObject)
    {
        FillObjectFromJson(objectToFill as object, jsonObject);
    }

    private static void FillObjectFromJson(object objectToFill, JSONObject jsonObject)
    {
        var fields = objectToFill.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).ToList().FindAll(x => x.GetCustomAttribute<JSONField>() != null);
        foreach (var fieldInfo in fields)
        {
            string fieldInfoName = fieldInfo.Name;
            if(!jsonObject.Contains(fieldInfoName)) continue;
            
            if (JSONABLE_TYPES.Contains(fieldInfo.FieldType))
            {
                var value = jsonObject.GetValue(fieldInfoName).GetType().GetField("_content", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(jsonObject.GetValue(fieldInfoName));
                fieldInfo.SetValue(objectToFill, value);
            }
            else if (JSONABLE_ARRAY_TYPES.Contains(fieldInfo.FieldType) /*|| fieldInfo.FieldType.IsArray || fieldInfo.FieldType.GetGenericArguments().Length == 1 && (JSONABLE_TYPES.Contains(fieldInfo.FieldType.GetGenericArguments()[0]) || fieldInfo.FieldType.GetGenericArguments()[0].GetCustomAttribute<JSONable>() != null)*/)
            {
                // TODO fix reading from lists with jsonable types.
                var tmpArray = jsonObject.GetValue(fieldInfoName);
                
                if(tmpArray.GetType() != typeof(JSONArray)) continue;
                
                JSONArray jsonArray = tmpArray as JSONArray;

                IEnumerable enumerable = fieldInfo.GetValue(objectToFill) as IEnumerable;
                if (enumerable == null || jsonArray == null)
                    continue;

                Type elementType = fieldInfo.FieldType.IsArray
                    ? fieldInfo.FieldType.GetElementType()
                    : fieldInfo.FieldType.GetGenericArguments()[0];
                
                for (int i = 0; i < jsonArray.Length; ++i)
                {
                    var content = jsonArray[i].GetType().GetField("_content", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(jsonArray[i]);
                    var contentType = content?.GetType();
                    if (contentType != elementType 
                        && elementType.GetCustomAttribute<JSONable>() == null 
                        && !(jsonArray[i].GetType()
                            .GetField("_content", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(jsonArray[i]) is IDictionary))
                    {
                        goto EndOfOuterLoop;
                    }
                }

                if (fieldInfo.FieldType.IsArray)
                {
                    var arrayType = fieldInfo.FieldType.GetElementType();

                    Array array = Array.CreateInstance(arrayType, jsonArray.Length);

                    for (int i = 0; i < array.Length; ++i)
                    {
                        var content = jsonArray[i].GetType().GetField("_content", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                            ?.GetValue(jsonArray[i]);
                        array.SetValue(content, i);
                    }
                    
                    fieldInfo.SetValue(objectToFill, array);
                }
                else
                {
                    var listType = typeof(List<>);
                    var constructedListType = listType.MakeGenericType(fieldInfo.FieldType.GetGenericArguments()[0]);

                    IList instance = Activator.CreateInstance(constructedListType) as IList;
                    
                    foreach (var value in jsonArray)
                    {
                        var content = value.GetType().GetField("_content", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                            ?.GetValue(value);
                        instance.Add(content);
                    }
                    
                    fieldInfo.SetValue(objectToFill, instance);
                }
            }
            else if (JSONABLE_DICTIONARY_TYPES.Contains(fieldInfo.FieldType) || (fieldInfo.GetValue(objectToFill) as IDictionary) != null)
            {
                // TODO: filling from dictionary
                // TODO reading from dictionaries with jsonable types as values.
                // JSONObject tmpJsonObject = new JSONObject();
                //
                // IDictionary  dict = fieldInfo.GetValue(objectToConvert) as IDictionary;
                // if (dict == null)
                //     break;
                //
                // ICollection dictKeys = dict.Keys;
                //
                // var method = JSONOBJECT_ADD_METHODS.FirstOrDefault(x =>
                //     x.GetParameters()[0].ParameterType == typeof(string) &&
                //     x.GetParameters()[1].ParameterType == fieldInfo.FieldType.GetGenericArguments()[1]);
                //
                // foreach (object item in dictKeys)
                // {
                //     method?.Invoke(tmpJsonObject, new object?[]{item, dict[item]});
                // }
                //
                // jsonObject.Add(fieldInfoName, tmpJsonObject);
            }
            else if (fieldInfo.FieldType.GetCustomAttribute<JSONable>() != null)
            {
                // TODO: filling from not regular type.
                // var method = JSONOBJECT_ADD_METHODS.FirstOrDefault(x =>
                //     x.GetParameters()[0].ParameterType == typeof(string) &&
                //     x.GetParameters()[1].ParameterType == typeof(JSONObject).BaseType);
                // JSONObject tmpJsonObject = ConvertToJsonObject(fieldInfo.GetValue(objectToConvert));
                // method?.Invoke(jsonObject, new object?[]{fieldInfoName, tmpJsonObject});
            }
            EndOfOuterLoop:;
        }
    }
}
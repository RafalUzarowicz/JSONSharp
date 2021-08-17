using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CSharpTesting;

public class ObjectDumper
{
    private int _level;
    private readonly int _indentSize;
    private readonly StringBuilder _stringBuilder;
    private readonly List<int> _hashListOfFoundElements;

    private ObjectDumper(int indentSize)
    {
        _indentSize = indentSize;
        _stringBuilder = new StringBuilder();
        _hashListOfFoundElements = new List<int>();
    }

    public static string Dump(object element)
    {
        return Dump(element, 2);
    }

    public static string Dump(object element, int indentSize)
    {
        var instance = new ObjectDumper(indentSize);
        return instance.DumpElement(element);
    }

    private string DumpElement(object element)
    {
        if (element == null || element is ValueType || element is string)
        {
            Write(FormatValue(element));
        }
        else
        {
            var objectType = element.GetType();
            if (!typeof(IEnumerable).IsAssignableFrom(objectType))
            {
                Write("{{{0}}}", objectType.FullName);
                _hashListOfFoundElements.Add(element.GetHashCode());
                _level++;
            }

            var enumerableElement = element as IEnumerable;
            if (enumerableElement != null)
            {
                foreach (object item in enumerableElement)
                {
                    if (item is IEnumerable && !(item is string))
                    {
                        _level++;
                        DumpElement(item);
                        _level--;
                    }
                    else
                    {
                        if (!AlreadyTouched(item))
                            DumpElement(item);
                        else
                            Write("{{{0}}} <-- bidirectional reference found", item.GetType().FullName);
                    }
                }
            }
            else
            {
                MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                foreach (var memberInfo in members)
                {
                    var fieldInfo = memberInfo as FieldInfo;
                    var propertyInfo = memberInfo as PropertyInfo;

                    if (fieldInfo == null && propertyInfo == null)
                        continue;

                    var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                    object value = fieldInfo != null
                                       ? fieldInfo.GetValue(element)
                                       : propertyInfo.GetValue(element, null);

                    if (type.IsValueType || type == typeof(string))
                    {
                        Write("{0}: {1}", memberInfo.Name, FormatValue(value));
                    }
                    else
                    {
                        var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                        Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

                        var alreadyTouched = !isEnumerable && AlreadyTouched(value);
                        _level++;
                        if (!alreadyTouched)
                            DumpElement(value);
                        else
                            Write("{{{0}}} <-- bidirectional reference found", value.GetType().FullName);
                        _level--;
                    }
                }
            }

            if (!typeof(IEnumerable).IsAssignableFrom(objectType))
            {
                _level--;
            }
        }

        return _stringBuilder.ToString();
    }

    private bool AlreadyTouched(object value)
    {
        if (value == null)
            return false;

        var hash = value.GetHashCode();
        for (var i = 0; i < _hashListOfFoundElements.Count; i++)
        {
            if (_hashListOfFoundElements[i] == hash)
                return true;
        }
        return false;
    }

    private void Write(string value, params object[] args)
    {
        var space = new string(' ', _level * _indentSize);

        if (args != null)
            value = string.Format(value, args);

        _stringBuilder.AppendLine(space + value);
    }

    private string FormatValue(object o)
    {
        if (o == null)
            return ("null");

        if (o is DateTime)
            return (((DateTime)o).ToShortDateString());

        if (o is string)
            return string.Format("\"{0}\"", o);

        if (o is char && (char)o == '\0') 
            return string.Empty; 

        if (o is ValueType)
            return (o.ToString());

        if (o is IEnumerable)
            return ("...");

        return ("{ }");
    }
}

/// <summary>
/// Struct representing resource used in game.
/// </summary>
[Serializable][JSONable]
public struct ROOT_Resource
{
    public string name;
    public HashSet<string> tags;
    public int value;

    public ROOT_Resource(string name, int value)
    {
        this.name = name;
        this.value = value;
        tags = new HashSet<string>();
    }
        
    public ROOT_Resource(string name, int value, HashSet<string> tags)
    {
        this.name = name;
        this.value = value;
        this.tags = tags ?? new HashSet<string>();
    }
    
    public static ROOT_Resource operator+(ROOT_Resource first, ROOT_Resource second)
    {
        ROOT_Resource resource = new ROOT_Resource();
        if (first.name.Equals(second.name))
        {
            resource.name = first.name;
            resource.tags = first.tags;
            foreach (var tag in second.tags)
            {
                resource.tags.Add(tag);
            }

            resource.value = first.value + second.value;
        }
        return resource;
    }
}

/// <summary>
/// Class that can be used as a resource container. It has basic adding, removing and takes care of tags system.
/// </summary>
public class ROOT_Resources
{
    private Dictionary<string, ROOT_Resource> _resources;
    private Dictionary<string, HashSet<ROOT_Resource>> _tagsSets;

    /// <summary>
    /// Default constructor. It creates empty collections of resources and tags.
    /// </summary>
    public ROOT_Resources()
    {
        _resources = new Dictionary<string, ROOT_Resource>();
        _tagsSets = new Dictionary<string, HashSet<ROOT_Resource>>();
    }

    /// <summary>
    /// Constructor that can read resources from JSONObject. JSONObject's content must be compatible with Resource type.
    /// </summary>
    /// <param name="resourcesJson">Source for collection.</param>
    public ROOT_Resources(JSONObject resourcesJson)
    {
        foreach (var potentialResource in resourcesJson)
        {
            JSONObject resourceObject = potentialResource as JSONObject;
            if (resourceObject != null && resourceObject.Contains("name") && resourceObject.Contains("tags") && resourceObject.Contains("value"))
            {
                JSONArray tagsArray = resourceObject.GetValue("tags") as JSONArray;
                if (tagsArray != null)
                {
                    ROOT_Resource resource = new ROOT_Resource();
                    resource.name = (resourceObject.GetValue("name") as JSONString)?.Content;
                    var intPtr = (resourceObject.GetValue("value") as JSONInt)?.Content;
                    if (intPtr != null)
                        resource.value = (int) intPtr;
                    foreach (var tag in tagsArray)
                    {
                        JSONString jsonTag = tag as JSONString;
                        if (jsonTag != null)
                        {
                            resource.tags.Add(jsonTag.Content);
                        }
                    }
                    AddResource(resource, true);
                }
            }
        }
    }
    
    /// <summary>
    /// Returns list of resources that use given tag.
    /// </summary>
    /// <param name="tag">Tag used to find resources.</param>
    /// <returns>Found resource or null if this tag is not used.</returns>
    public List<ROOT_Resource> GetFromTag(string tag)
    {
        if (_tagsSets.TryGetValue(tag, out var resources))
        {
            return resources.ToList();
        }

        return null;
    }

    /// <summary>
    /// Get specific resource by it's name.
    /// </summary>
    /// <param name="name">Resource name used while searching.</param>
    /// <returns>Found resource or null if there is no resource related to given name.</returns>
    public ROOT_Resource? GetFromName(string name)
    {
        if (_resources.TryGetValue(name, out var resource))
        {
            return resource;
        }

        return null;
    }

    /// <summary>
    /// Add resource without tags.
    /// </summary>
    /// <param name="name">Name of the new resource.</param>
    /// <param name="overwriteIfExists">Overwrite existing resource.</param>
    /// <param name="initialValue">Resource's initial value.</param>
    public void AddResource(string name, bool overwriteIfExists, int initialValue = 0)
    {
        AddResource(name, initialValue, null, overwriteIfExists);
    }

    /// <summary>
    /// Add given resource.
    /// </summary>
    /// <param name="resource">Resource to add.</param>
    /// <param name="overwriteIfExists">Overwrite existing resource.</param>
    public void AddResource(ROOT_Resource resource, bool overwriteIfExists = false)
    {
        AddResource(resource.name, resource.value, resource.tags, overwriteIfExists);
    }

    /// <summary>
    /// Add resource without tags.
    /// </summary>
    /// <param name="name">Name of the new resource.</param>
    /// <param name="tags">Tags that will be used for new resource.</param>
    /// <param name="initialValue">New resource's initial value.</param>
    public void AddResource(string name, HashSet<string> tags, int initialValue = 0)
    {
        AddResource(name, initialValue, tags, false);
    }

    /// <summary>
    /// Add resource to collection.
    /// </summary>
    /// <param name="name">Name of the new resource.</param>
    /// <param name="initialValue">New resource's initial value.</param>
    /// <param name="tags">Tags that will be used for new resource.</param>
    /// <param name="overwriteIfExists">Overwrite existing resource.</param>
    public void AddResource(string name, int initialValue = 0, HashSet<string> tags = null,
        bool overwriteIfExists = false)
    {
        if (_resources.TryGetValue(name, out var resource) && overwriteIfExists)
        {
            resource.value = initialValue;
        }
        else
        {
            _resources[name] = new ROOT_Resource
            {
                name = name,
                value = initialValue,
                tags = tags ?? new HashSet<string>()
            };

            foreach (var tag in _resources[name].tags)
            {
                if (!_tagsSets.ContainsKey(tag))
                {
                    _tagsSets[tag] = new HashSet<ROOT_Resource>();
                }

                _tagsSets[tag].Add(_resources[name]);
            }
        }
    }

    /// <summary>
    /// Remove resource from collection.
    /// </summary>
    /// <param name="name">Name of resource that will be removed.</param>
    public void RemoveResource(string name)
    {
        if (_resources.ContainsKey(name))
        {
            foreach (var tag in _resources[name].tags)
            {
                if (_tagsSets[tag].Count == 1)
                {
                    _tagsSets.Remove(tag);
                }
                else
                {
                    _tagsSets[tag].Remove(_resources[name]);
                }
            }

            _resources.Remove(name);
        }
    }

    /// <summary>
    /// Add specific tag to all giver resources.
    /// </summary>
    /// <param name="tag">Name of the new tag.</param>
    /// <param name="resources">Set of resources that need this tag to be added.</param>
    public void AddTagToResources(string tag, HashSet<string> resources)
    {
        foreach (var resource in resources)
        {
            if (_resources.ContainsKey(resource))
            {
                AddTagToResource(tag, resource);
            }
        }
    }

    /// <summary>
    /// Remove tag from collection.
    /// </summary>
    /// <param name="tag">Tag to remove.</param>
    public void RemoveTag(string tag)
    {
        if (_tagsSets.ContainsKey(tag))
        {
            foreach (var resource in _tagsSets[tag])
            {
                resource.tags.Remove(tag);
            }

            _tagsSets.Remove(tag);
        }
    }

    /// <summary>
    /// Add specific tag to specific resource.
    /// </summary>
    /// <param name="tag">New tag's name.</param>
    /// <param name="name">Resource's name.</param>
    public void AddTagToResource(string tag, string name)
    {
        if (_resources.ContainsKey(name))
        {
            var resource = _resources[name];
            resource.tags.Add(tag);
            _resources[name] = resource;

            if (!_tagsSets.ContainsKey(tag))
            {
                _tagsSets[tag] = new HashSet<ROOT_Resource>();
            }

            _tagsSets[tag].Add(_resources[name]);
        }
    }

    /// <summary>
    /// Remove specific tag from specific resource.
    /// </summary>
    /// <param name="tag">Name of the tag that will be removed.</param>
    /// <param name="name">Resource's name.</param>
    public void RemoveTagFromResource(string name, string tag)
    {
        if (_resources.ContainsKey(name))
        {
            var resource = _resources[name];
            resource.tags.Remove(tag);
            _resources[name] = resource;

            if (_tagsSets[tag].Count == 1)
            {
                _tagsSets.Remove(tag);
            }
            else
            {
                _tagsSets[tag].Remove(_resources[name]);
            }
        }
    }

    /// <summary>
    /// Add value to specific resource.
    /// </summary>
    /// <param name="name">Resource's name.</param>
    /// <param name="value">Value to add.</param>
    /// <param name="createIfNotExists">Create resource if it doesn't exist.</param>
    public void AddToResource(string name, int value, bool createIfNotExists = false)
    {
        if (_resources.ContainsKey(name))
        {
            var resource = _resources[name];
            resource.value += value;
            _resources[name] = resource;
        }
        else if (createIfNotExists)
        {
            _resources[name] = new ROOT_Resource()
            {
                name = name,
                value = value,
                tags = new HashSet<string>()
            };
        }
    }

    /// <summary>
    /// Subtract value from specific resource.
    /// </summary>
    /// <param name="name">Resource's name.</param>
    /// <param name="value">Value to subtract.</param>
    /// <param name="createIfNotExists">Create resource if it doesn't exist.</param>
    public void SubFromResource(string name, int value, bool createIfNotExists = false)
    {
        AddToResource(name, -value, createIfNotExists);
    }
}


/// <summary>
/// Class managing all resources.
/// </summary>
public class ROOT_ResourcesManager
{
    private ROOT_Resources _resources;
}

[JSONable]
public class InnerTest
{
    [JSONField] public bool z = true;
}

[JSONable]
public class Test
{
    [JSONField] public int x = 2;
    [JSONField] public float y = 3.0f;
    [JSONField] public string text = "tak";
    // [JSONField] public InnerTest InnerTest = new InnerTest();
    // [JSONField] private int test = 0;

    // [JSONField] public List<int> intList = new List<int>() {2, 4, 5, 7};
    [JSONField] public int[] intArray = new []{2, 4, 5, 7};
    // [JSONField] public Dictionary<string, int> IntDictionary = new Dictionary<string, int>() {["dwa"] = 2, ["cztery"] = 4};
}

public class Program
{
    [JSONable]
    public interface ILevelData
    {
        
    }

    [JSONable]
    public class ROOT_Slot
    {
        [JSONField] public Dictionary<string, LevelData> levelsDatas = new Dictionary<string, LevelData>();
        [JSONField] public List<TM_LevelData> TmLevelDatas = new List<TM_LevelData>();
    }

    [JSONable]
    public class LevelData
    {
        [JSONField] public ROOT_LevelData RootLevelData;
        [JSONField] public ILevelData MechanicLevelData;
    }

    [JSONable]
    public class ROOT_LevelData
    {
        [JSONField] public string name;
        [JSONField] public int difficulty;
    }

    [JSONable]
    public class TM_LevelData : ILevelData
    {
        [JSONField] public int customers;
    }
    
    
    public static void Main()
    {
        ROOT_Slot slot = new ROOT_Slot();

        slot.levelsDatas["jeden"] = new LevelData()
        {
            RootLevelData = new ROOT_LevelData()
            {
                name = "jeden",
                difficulty = 1
            },
            MechanicLevelData = new TM_LevelData()
            {
                customers = 10
            }
        };
        
        slot.levelsDatas["dwa"] = new LevelData()
        {
            RootLevelData = new ROOT_LevelData()
            {
                name = "dwa",
                difficulty = 2
            },
            MechanicLevelData = new TM_LevelData()
            {
                customers = 20
            }
        };
        
        slot.levelsDatas["trzy"] = new LevelData()
        {
            RootLevelData = new ROOT_LevelData()
            {
                name = "trzy",
                difficulty = 3
            },
            MechanicLevelData = new TM_LevelData()
            {
                customers = 30
            }
        };
        
        slot.TmLevelDatas.Add(new TM_LevelData(){customers = 11});
        slot.TmLevelDatas.Add(new TM_LevelData(){customers = 13});
        slot.TmLevelDatas.Add(new TM_LevelData(){customers = 17});


        JSONObject jsonObject = JSONFunctions.ConvertToJsonObject(slot);


        ROOT_Slot slot2 = new ROOT_Slot();

        JSONFunctions.FillObjectFromJson(slot2, jsonObject);
        
        
        
        Test test2 = new Test();
        // test.intArray = new[] {3, 3, 21, 41, 42};
        //
        // JSONObject jsonObject = ConvertToJsonObject(test);
        //
        // Test test2 = new Test();
        //
        // test2.x = -1;
        // test2.y = 37.5f;
        // test2.text = "xddd";
        //
        // test2 = FillObjectFromJson(test2, jsonObject);
        //
        // Console.WriteLine("xd");
        //     string test = "{\"tak\":2, \"nie\":true}";
        //     string test2 = @"{
        //     ""array \"""": [
        //     {
        //         ""_id"": ""61162798ce940bfb96693a65"",
        //         ""index"": 0,
        //         ""guid"": ""7103a95c-54b1-41a2-acc9-05fc483ce8ba"",
        //         ""isActive"": true,
        //         ""balance"": ""$2,857.21"",
        //         ""picture"": ""http://placehold.it/32x32"",
        //         ""age"": 30,
        //         ""eyeColor"": ""blue"",
        //         ""name"": ""Virginia Glenn"",
        //         ""gender"": ""female"",
        //         ""company"": ""FITCORE"",
        //         ""email"": ""virginiaglenn@fitcore.com"",
        //         ""phone"": ""+1 (893) 532-2871"",
        //         ""address"": ""932 Chauncey Street, Wawona, Nebraska, 5191"",
        //         ""about"": ""Esse nostrud reprehenderit veniam sit culpa proident irure elit consequat. Magna aute aliqua elit adipisicing incididunt mollit ipsum laboris tempor commodo veniam anim. Irure ex commodo elit et incididunt cillum exercitation est qui dolor.\r\n"",
        //         ""registered"": ""2014-07-30T09:38:35 -02:00"",
        //         ""latitude"": -39.655422,
        //         ""longitude"": -49.351272,
        //         ""tags"": [
        //         ""pariatur"",
        //         ""ex"",
        //         ""excepteur"",
        //         ""id"",
        //         ""minim"",
        //         ""exercitation"",
        //         ""eu""
        //             ],
        //         ""friends"": [
        //         {
        //             ""id"": 0,
        //             ""name"": ""Rochelle Arnold""
        //         },
        //         {
        //             ""id"": 1,
        //             ""name"": ""Sykes Mckinney""
        //         },
        //         {
        //             ""id"": 2,
        //             ""name"": ""Queen Raymond""
        //         }
        //         ],
        //         ""greeting"": ""Hello, Virginia Glenn! \"" You have 4 unread messages."",
        //         ""favoriteFruit"": ""strawberry""
        //     }
        //     ]
        // }";

        // JSONSource jsonSource = new JSONSource(test2);

        // for (int j = 0; j<test.Length; ++j)
        // {
        //     Console.WriteLine(Convert.ToChar(jsonSource.Get()));
        // }
        // var properties = typeof(Resource).GetFields();
        // foreach (var propertyInfo in properties)
        // {
        //     string propertyName = propertyInfo.Name;
        //     
        //     object[] attribute = propertyInfo.GetCustomAttributes(typeof(JSONField), true);
        //     // Just in case you have a property without this annotation
        //     if (attribute.Length > 0)
        //     {
        //         JSONField myAttribute = (JSONField)attribute[0];
        //         Console.WriteLine(propertyName);
        //     }
        //
        //     var properties2 = propertyInfo.FieldType.GetCustomAttributes(typeof(JSONable));
        //     if (properties2.ToList().Count > 0)
        //     {
        //         JSONable myAttribute = (JSONable)properties2.ToList()[0];
        //         Console.WriteLine(propertyName);
        //     }
        // }

        // JSONScanner jsonScanner = new JSONScanner(jsonSource);
        //
        // // while (jsonScanner.Peek().Type != JSONTokenType.EOF)
        // // {
        // //     Console.WriteLine(jsonScanner.Get());
        // // }
        //
        // JSONParser jsonParser = new JSONParser(test2);
        //
        // JSONObject jsonObject = jsonParser.Parse();

        // JsonSource jsonSource = new JsonSource(test);
        //
        // // while (jsonSource.peek() != JSONSource.EOF)
        // // {
        // //     Console.WriteLine(Convert.ToChar(jsonSource.get()));
        // // }
        //
        // JSONObject jsonObject = new JSONObject();
        // jsonObject.Add("tak", new JSONFloat(2.0f));
        // jsonObject.Add("nie", new JSONInt(7));
        // jsonObject.Add("trzy", new JSONString("chyba"));
        //
        // JSONArray jsonArray = new JSONArray();
        // jsonArray.Add(jsonObject);
        // jsonArray.Add(new JSONInt(24));
        //
        // JSONObject jsonObject2 = new JSONObject();
        // jsonObject2.Add("testy", jsonArray);
        //
        // foreach (var value in jsonObject)
        // {
        //     Console.WriteLine(value.Serialize());
        // }
        //
        //
        // Console.WriteLine(jsonObject.Serialize());
        // Console.WriteLine(jsonObject2.Serialize());


        // Resources resources = new Resources();
        //
        // resources.AddResource("test1", 12, new HashSet<string>(){"tag1", "tag2"});
        // resources.AddResource("test2", 12, new HashSet<string>(){"tag2"});
        // resources.AddResource("test3", 12, new HashSet<string>(){"tag1"});
        // resources.AddResource("test4", 12, null);
        //
        // resources.AddToResource("test2", 5);
        // resources.AddTagToResource("test3", "tag3");
        //
        // // resources.RemoveTag("tag1");
        //
        // ROOT_Resource resource = new ROOT_Resource()
        // {
        //     name = "jeden",
        //     tags = new HashSet<string>() {"tag1", "tag2"},
        //     value = 10
        // };
        //
        // JSONObject jsonObject = ConvertToJsonObject(resource);
        //
        // ROOT_Resource resource1 = new ROOT_Resource();
        //

    }
}
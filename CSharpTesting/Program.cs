using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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

public class JSONField : Attribute
{
    
}

public class JSONable : Attribute
{
    
}

public interface IJSONable
{
    public JSONObject ToJSON();
}

/// <summary>
/// Struct representing resource used in game.
/// </summary>
[Serializable][JSONable]
public struct ROOT_Resource
{
    [JSONField] public string name;
    [JSONField] public HashSet<string> tags;
    [JSONField] public int value;
    
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
    /// Constructor that can read resources from JSONObject. JSONObject's contents must be compatible with Resource type.
    /// </summary>
    /// <param name="resourcesJson">Source for collection.</param>
    public ROOT_Resources(JSONObject resourcesJson)
    {
        // TODO: implement
    }
    
    /// <summary>
    /// Returns list of resources that use specific tag.
    /// </summary>
    /// <param name="tag">Tag used to find resources.</param>
    /// <returns></returns>
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
    /// <returns>Found resource or null.</returns>
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
    public void AddResourceWithOverwriteFlag(string name, bool overwriteIfExists = false, int initialValue = 0)
    {
        AddResource(name, initialValue, null, overwriteIfExists);
    }

    /// <summary>
    /// Add resource without tags.
    /// </summary>
    /// <param name="name">Name of the new resource.</param>
    /// <param name="tags">Tags that will be used for new resource.</param>
    /// <param name="initialValue">New resource's initial value.</param>
    public void AddResourceWithTags(string name, HashSet<string> tags = null, int initialValue = 0)
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
    public void AddResource(string name, int initialValue = 0, HashSet<string> tags = null, bool overwriteIfExists = false)
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
        else if(createIfNotExists)
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

public interface ISerializable
{
    public string Serialize();
    public void Deserialize(string text);
}

public enum JSONTokenType
{
    EOF,
    BracesLeft,
    BracesRight,
    BracketsLeft,
    BracketsRight,
    Comma,
    Colon,
    String,
    Int,
    Float,
    True,
    False,
    Null
}

public class JSONToken
{
    public static Dictionary<int, JSONTokenType> JSON_SINGLE_CHARS = new()
    {
        ['{'] = JSONTokenType.BracesLeft,
        ['}'] = JSONTokenType.BracesRight,
        ['['] = JSONTokenType.BracketsLeft,
        [']'] = JSONTokenType.BracketsRight,
        [','] = JSONTokenType.Comma,
        [':'] = JSONTokenType.Colon
    };
    
    public JSONTokenType Type;
    public string Value;

    public JSONToken(JSONTokenType type)
    {
        Value = "";
        Type = type;
    }

    public JSONToken(string value, JSONTokenType type)
    {
        Value = value;
        Type = type;
    }

    public override string ToString()
    {
        return Type + (string.IsNullOrEmpty(Value) ? "" : " - "+Value);
    }
}

public class JSONSource
{
    private readonly string _text;
    private int _currentIndex;
    private int _currentChar;

    public const int EOF = -1;

    public JSONSource(string text)
    {
        _text = text;
        _currentIndex = 0;
        _currentChar = _text.Length > 0 ? _text[0] : EOF;
    }


    public int Peek()
    {
        return _currentChar;
    }

    public int Get()
    {
        var tmpChar = _currentChar;
        Next();
        return tmpChar;
    }

    public void Next()
    {
        if (++_currentIndex < _text.Length) {
            _currentChar = _text[_currentIndex];
        } else {
            _currentIndex = _text.Length;
            _currentChar = EOF;
        }
    }
}

public class JSONScanner
{
    private readonly JSONSource _source;
    private JSONToken _token;
    private StringBuilder _stringBuilder;
    public JSONScanner(JSONSource source)
    {
        _stringBuilder = new StringBuilder();
        _source = source;
        Next();
        _token ??= new JSONToken(JSONTokenType.EOF);
    }
    
    public JSONToken Get()
    {
        JSONToken token = _token;
        Next();
        return token;
    }

    public JSONToken Peek()
    {
        return _token;
    }

    public void Next()
    {
        if (TryEof())
        {
            return;
        }
        
        IgnoreWhiteSpaces();

        if (TryKeyWords())
        {
            return;
        }
        if (TrySingleCharacter())
        {
            return;
        } 
        if (TryString())
        {
            return;
        } 
        if (TryNumber())
        {
            return;
        }

        throw new Exception("Unknown symbol from source: " + _source.Peek());
    }
    
    private void IgnoreWhiteSpaces(){
        while (char.IsWhiteSpace(Convert.ToChar(_source.Peek())))
        {
            _source.Next();
        }
    }

    private bool TryEof()
    {
        if (_source.Peek() == JSONSource.EOF)
        {
            _token = new JSONToken(JSONTokenType.EOF);
            return true;
        }
        return false;
    }

    private static Dictionary<string, JSONTokenType> JSON_KEYWORDS = new ()
    {
        ["true"] = JSONTokenType.True, 
        ["false"] = JSONTokenType.False, 
        ["null"] = JSONTokenType.Null
    };
    
    private bool TryKeyWords()
    {
        string keyword = JSON_KEYWORDS.Keys.FirstOrDefault(x => x.First() == _source.Peek());
        if (!string.IsNullOrEmpty(keyword))
        {
            for (int i = 0; i < keyword.Length - 1; ++i)
            {
                if (IsNextEOF || _source.Get() != keyword[i])
                {
                    throw new Exception("Error writing \"true\" keyword.");
                }
            }

            if (_source.Get() == keyword.Last())
            {
                _token = new JSONToken(keyword, JSON_KEYWORDS[keyword]);
                return true;
            }
            
            throw new Exception("Wrong keyword.");
        }
        return false;
    }
    
    private bool TrySingleCharacter()
    {
        if (JSONToken.JSON_SINGLE_CHARS.Keys.Contains(_source.Peek()))
        {
            _token = new JSONToken(JSONToken.JSON_SINGLE_CHARS[_source.Get()]);
            return true;
        }
        return false;
    }

    private static Dictionary<string, char> JSON_ESCAPE_KEYWORDS = new()
    {
        ["\\\\"] = '\\',
        ["\\n"] = '\n',
        ["\\r"] = '\r',
        ["\\t"] = '\t',
        ["\\\""] = '\"',
        ["\\f"] = '\f',
        ["\\b"] = '\b',
    };

    private bool TryString()
    {
        if (_source.Peek() == '\"')
        {
            _source.Next();
            _token = new JSONToken(JSONTokenType.String);
            _stringBuilder.Clear();
            int prevChar = -1;
            int currChar = _source.Peek();
            
            while (currChar != '\"')
            {
                if (IsNextEOF)
                {
                    throw new Exception("End of source while creating string Token.");
                }
                prevChar = _source.Get();
                currChar = _source.Peek();

                var doubleChar =  "" + Convert.ToChar(prevChar) + Convert.ToChar(currChar);

                if (JSON_ESCAPE_KEYWORDS.ContainsKey(doubleChar))
                {
                    _stringBuilder.Append(JSON_ESCAPE_KEYWORDS[doubleChar]);
                    _source.Next();
                    currChar = _source.Peek();
                }
                else
                {
                    _stringBuilder.Append(Convert.ToChar(prevChar));
                }
                
            }
            
            _token.Value = _stringBuilder.ToString();
            _source.Next();
            return true;
        }
        return false;
    }

    private bool TryNumber()
    {
        if (_source.Peek() == '-' || IsNextDigit)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(Convert.ToChar(_source.Get()));
            if(_stringBuilder[^1] == '-' && !IsNextDigit)
            {
                throw new Exception("Wrong number format.");
            }

            while (IsNextDigit && !IsNextExponentCharacter && !IsNextFractionCharacter)
            {
                _stringBuilder.Append(Convert.ToChar(_source.Get()));
            }

            if (IsNextFractionCharacter)
            {
                _stringBuilder.Append(Convert.ToChar(_source.Get()));
                while (IsNextDigit && !IsNextExponentCharacter)
                {
                    _stringBuilder.Append(Convert.ToChar(_source.Get()));
                }
            }
            else if(!IsNextExponentCharacter)
            {
                _token = new JSONToken(_stringBuilder.ToString(), JSONTokenType.Int);
                return true;
            }

            if (IsNextExponentCharacter)
            {
                _stringBuilder.Append(Convert.ToChar(_source.Get()));
                if (IsNextDigit || IsNextSign)
                {
                    _stringBuilder.Append(Convert.ToChar(_source.Get()));
                }

                while (IsNextDigit)
                {
                    _stringBuilder.Append(Convert.ToChar(_source.Get()));
                }
            }
            _token = new JSONToken(_stringBuilder.ToString(), JSONTokenType.Float);
            return true;
        }
        return false;
    }

    private bool IsNextEOF => _source.Peek() == JSONSource.EOF;
    private bool IsNextDigit => _source.Peek() >= '0' && _source.Peek() <= '9';
    private bool IsNextFractionCharacter => _source.Peek() == '.';
    private bool IsNextExponentCharacter => _source.Peek() == 'E' || _source.Peek() == 'e';
    private bool IsNextSign => _source.Peek() == '-' || _source.Peek() == '+';
}

public class JSONParser
    {
        private JSONScanner _scanner;

        public JSONParser(JSONScanner scanner)
        {
            _scanner = scanner;
        }

        public JSONParser(JSONSource jsonSource) : this(new JSONScanner(jsonSource))
        {
            
        }

        public JSONParser(string jsonAsText) : this(new JSONScanner(new JSONSource(jsonAsText)))
        {
            
        }

        public JSONObject Parse()
        {
            return ParseJsonObject();
        }

        public JSONObject ParseJsonObject()
        {
            CheckCurrentToken(JSONTokenType.BracesLeft);
            return TryJsonObject();
        }

        private JSONObject TryJsonObject()
        {
            _scanner.Next();
            JSONObject jsonObject = new JSONObject();
            while (!CompareCurrentTokenType(JSONTokenType.BracesRight))
            {
                   CheckCurrentToken(JSONTokenType.String);
                   var name = _scanner.Get().Value;
                   CheckCurrentToken(JSONTokenType.Colon);
                   _scanner.Next();
                   JSONValue value = TryJsonValue();
                   jsonObject.Add(name, value);
                   if (CompareCurrentTokenType(JSONTokenType.Comma))
                   {
                       _scanner.Next();
                   }
            }
            _scanner.Next();
            return jsonObject;
        }

        private JSONValue TryJsonValue()
        {
            switch (_scanner.Peek().Type)
            {
                case JSONTokenType.BracesLeft:
                    return TryJsonObject();
                case JSONTokenType.Int :
                    return TryJsonInt();
                case JSONTokenType.Float:
                    return TryJsonFloat();
                case JSONTokenType.String:
                    return TryJsonString();
                case JSONTokenType.True:
                    return TryJsonTrue();
                case JSONTokenType.False:
                    return TryJsonFalse();
                case JSONTokenType.Null:
                    return TryJsonNull();
                case JSONTokenType.BracketsLeft:
                    return TryJsonArray();
            }
            throw new Exception("Wrong token while trying to parse value.");
        }

        public JSONArray ParseJsonArray()
        {
            CheckCurrentToken(JSONTokenType.BracketsLeft);
            return TryJsonArray();
        }
        
        private JSONArray TryJsonArray()
        {
            _scanner.Next();
            JSONArray jsonArray = new JSONArray();
            if (_scanner.Peek().Type != JSONTokenType.BracketsRight)
            {
                while (IsCurrentForValue)
                {
                    jsonArray.Add(TryJsonValue());

                    if (CompareCurrentTokenType(JSONTokenType.Comma))
                    {
                        _scanner.Next();
                    }
                }
                
                if (_scanner.Peek().Type != JSONTokenType.BracketsRight)
                {
                    throw new Exception("Error parsing array.");
                }
            }
            _scanner.Next();
            return jsonArray;
        }

        public JSONString ParseJsonString()
        {
            CheckCurrentToken(JSONTokenType.String);
            return TryJsonString();
        }
        
        private JSONString TryJsonString()
        {
            return new JSONString(_scanner.Get().Value);
        }
        
        public JSONInt ParseJsonInt()
        {
            CheckCurrentToken(JSONTokenType.Int);
            return TryJsonInt();
        }

        private JSONInt TryJsonInt()
        {
            return new JSONInt(int.Parse(_scanner.Get().Value));
        }
        
        public JSONFloat ParseJsonFloat()
        {
            CheckCurrentToken(JSONTokenType.Float);
            return TryJsonFloat();
        }

        private JSONFloat TryJsonFloat()
        {
            return new JSONFloat(float.Parse(_scanner.Get().Value));
        }
        
        public JSONBool ParseJsonTrue()
        {
            CheckCurrentToken(JSONTokenType.True);
            return TryJsonTrue();
        }

        private JSONBool TryJsonTrue()
        {
            _scanner.Next();
            return new JSONBool(true);
        }

        public JSONBool ParseJsonFalse()
        {
            CheckCurrentToken(JSONTokenType.False);
            return TryJsonFalse();
        }
        
        private JSONBool TryJsonFalse()
        {
            _scanner.Next();
            return new JSONBool(false);
        }
        
        public JSONNull ParseJsonNull()
        {
            CheckCurrentToken(JSONTokenType.Null);
            return TryJsonNull();
        }
        
        private JSONNull TryJsonNull()
        {
            _scanner.Next();
            return new JSONNull();
        }

        private bool IsCurrentForValue => _scanner.Peek().Type == JSONTokenType.String ||
                                       _scanner.Peek().Type == JSONTokenType.Float ||
                                       _scanner.Peek().Type == JSONTokenType.BracketsLeft ||
                                       _scanner.Peek().Type == JSONTokenType.BracesLeft ||
                                       _scanner.Peek().Type == JSONTokenType.Int ||
                                       _scanner.Peek().Type == JSONTokenType.False ||
                                       _scanner.Peek().Type == JSONTokenType.True ||
                                       _scanner.Peek().Type == JSONTokenType.Null;

        private void CheckCurrentToken(JSONTokenType type)
        {
            if (!CompareCurrentTokenType(type))
            {
                throw new Exception("Wrong token type.");
            }
        }

        private bool CompareCurrentTokenType(JSONTokenType type)
        {
            return _scanner.Peek().Type == type;
        }
    }

public class JSONObject : JSONValue, IEnumerable
{
    private Dictionary<string, JSONValue> _content;

    public JSONObject()
    {
        _content = new Dictionary<string, JSONValue>();
    }

    public static JSONObject operator +(JSONObject first, JSONObject second)
    {
        JSONObject jsonObject = new JSONObject();
        foreach (var jsonValue in first._content)
        {
            jsonObject._content[jsonValue.Key] = jsonValue.Value;
        }
        foreach (var jsonValue in second._content)
        {
            first._content[jsonValue.Key] = jsonValue.Value;
        }
        return first;
    }
    
    public Dictionary<string, JSONValue>.ValueCollection.Enumerator GetEnumerator()
    {
        return _content.Values.GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Contains(string name)
    {
        return _content.ContainsKey(name);
    }

    public JSONValue GetValue(string name)
    {
        if (_content.ContainsKey(name))
        {
            return _content[name];
        }
        return null;
    }

    public void Add(string name, JSONValue value)
    {
        _content[name] = value;
    }

    public void Add(string name, int value)
    {
        Add(name, new JSONInt(value));
    }
    
    public void Add(string name, float value)
    {
        Add(name, new JSONFloat(value));
    }
    
    public void Add(string name, string value)
    {
        Add(name, new JSONString(value));
    }
    
    public void Add(string name, bool value)
    {
        Add(name, new JSONBool(value));
    }

    public void Add(string name)
    {
        Add(name, new JSONNull());
    }
    
    public void AddRange(Dictionary<string, JSONValue> values)
    {
        values.ToList().ForEach(x =>
        {
                _content[x.Key] = x.Value;
        });
    }
    
    public override string Serialize()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("{");
        var contentTmp = _content.ToList();
        for (int i = 0; i < contentTmp.Count - 1; ++i)
        {
            stringBuilder.Append(contentTmp[i].Key);
            stringBuilder.Append(":");
            stringBuilder.Append(contentTmp[i].Value.Serialize());
            stringBuilder.Append(",");
        }
        if (contentTmp.Count > 0)
        {
            stringBuilder.Append(contentTmp.Last().Key);
            stringBuilder.Append(":");
            stringBuilder.Append(contentTmp.Last().Value.Serialize());
        }

        stringBuilder.Append("}");
        return stringBuilder.ToString();
    }

    public override void Deserialize(string text)
    {
        JSONParser jsonParser = new JSONParser(text);

        try
        {
            JSONObject jsonObject = jsonParser.ParseJsonObject();
            _content = jsonObject._content;
        }
        catch (Exception e)
        {
            _content = new Dictionary<string, JSONValue>();
        }
    }
}

public class JSONArray : JSONValue, IEnumerable
{
    private List<JSONValue> _content;

    public JSONArray()
    {
        _content = new List<JSONValue>();
    }
    
    public IEnumerator<JSONValue> GetEnumerator()
    {
        return _content.GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Length => _content.Count;
    
    public JSONValue this[int key]
    {
        get => _content[key];
        set => _content[key] = value;
    }

    public void Add(JSONValue value)
    {
        _content.Add(value);
    }

    public void AddRange(List<JSONValue> values)
    {
        _content.AddRange(values);
    }
    
    public void AddRange(List<int> values)
    {
        AddRange(values
            .Select(x => (JSONValue)new JSONInt(x))
            .ToList());
    }
    
    public void AddRange(List<float> values)
    {
        AddRange(values
            .Select(x => (JSONValue)new JSONFloat(x))
            .ToList());
    }
    
    public void AddRange(List<string> values)
    {
        AddRange(values
            .Select(x => (JSONValue)new JSONString(x))
            .ToList());
    }
    
    public void AddRange(List<bool> values)
    {
        
        AddRange(values
            .Select(x => (JSONValue)new JSONBool(x))
            .ToList());
    }
    
    public void Add(int value)
    {
        Add(new JSONInt(value));
    }
    
    public void Add(float value)
    {
        Add(new JSONFloat(value));
    }
    
    public void Add(string value)
    {
        Add(new JSONString(value));
    }
    
    public void Add(bool value)
    {
        Add(new JSONBool(value));
    }

    public void Add()
    {
        Add(new JSONNull());
    }
    
    public override string Serialize()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("[");
        for (int i = 0; i < _content.Count - 1; ++i)
        {
            stringBuilder.Append(_content[i].Serialize());
            stringBuilder.Append(",");
        }
        if (_content.Count > 0)
        {
            stringBuilder.Append(_content.Last().Serialize());;
        }

        stringBuilder.Append("]");
        return stringBuilder.ToString();
    }

    public override void Deserialize(string text)
    {
        JSONParser jsonParser = new JSONParser(text);

        try
        {
            JSONArray jsonObject = jsonParser.ParseJsonArray();
            _content = jsonObject._content;
        }
        catch (Exception e)
        {
            
        }
    }
}

public class JSONString : JSONValue
{
    private string _content;

    public JSONString(string value)
    {
        _content = value;
    }
    public override string Serialize()
    {
        return "\"" + Regex.Escape(_content) + "\"";
    }

    public override void Deserialize(string text)
    {
        throw new NotImplementedException();
    }
}

public class JSONInt : JSONValue
{
    private int _content;

    public JSONInt(int value)
    {
        _content = value;
    }
    public override string Serialize()
    {
        return _content.ToString(CultureInfo.InvariantCulture);
    }

    public override void Deserialize(string text)
    {
        _content = int.Parse(text);
    }
}

public class JSONFloat : JSONValue
{
    private float _content;

    public JSONFloat(float value)
    {
        _content = value;
    }

    public float GetValue()
    {
        return _content;
    }
    public override string Serialize()
    {
        return _content.ToString(CultureInfo.InvariantCulture);
    }

    public override void Deserialize(string text)
    {
        _content = float.Parse(text);
    }
}

public class JSONBool : JSONValue
{
    private bool _content = false;

    public bool Content => _content;

    public JSONBool(bool value = false)
    {
        _content = value;
    }

    public override string Serialize()
    {
        return _content.ToString();
    }

    public override void Deserialize(string text)
    {
        if (text.Equals(true.ToString()))
        {
            _content = true;
        }
        else if (text.Equals(false.ToString()))
        {
            _content = false;
        }
        else
        {
            throw new Exception("Wrong deserialization text.");
        }
    }
}

public class JSONNull : JSONValue
{
    public const string Value = "null";
    public override string Serialize()
    {
        return Value;
    }

    public override void Deserialize(string text)
    {
        if (text != Value)
        {
            throw new Exception("Wrong deserialization text.");
        }
    }
}

public abstract class JSONValue : ISerializable
{
    public abstract string Serialize();
    public abstract void Deserialize(string text);
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
    [JSONField] public int[] intArray= new []{2, 4, 5, 7};
    // [JSONField] public Dictionary<string, int> IntDictionary = new Dictionary<string, int>() {["dwa"] = 2, ["cztery"] = 4};
}

public class Program
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
                            method?.Invoke(tmpJsonArray, new object?[]{item});
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


        JSONObject jsonObject = ConvertToJsonObject(slot);


        ROOT_Slot slot2 = new ROOT_Slot();

        FillObjectFromJson(slot2, jsonObject);
        
        
        
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
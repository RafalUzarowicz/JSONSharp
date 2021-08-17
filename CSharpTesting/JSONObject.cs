using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
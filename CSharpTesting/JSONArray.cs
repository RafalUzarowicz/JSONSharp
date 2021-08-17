using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
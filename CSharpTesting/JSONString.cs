using System;
using System.Text.RegularExpressions;

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
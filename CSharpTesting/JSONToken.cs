using System.Collections.Generic;

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
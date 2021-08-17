using System;

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
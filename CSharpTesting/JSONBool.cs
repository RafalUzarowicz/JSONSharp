using System;

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
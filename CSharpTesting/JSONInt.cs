using System.Globalization;

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
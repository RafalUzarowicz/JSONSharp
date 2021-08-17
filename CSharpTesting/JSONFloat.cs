using System.Globalization;

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
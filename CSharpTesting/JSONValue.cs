public abstract class JSONValue : ISerializable
{
    public abstract string Serialize();
    public abstract void Deserialize(string text);
}
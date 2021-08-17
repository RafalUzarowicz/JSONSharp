public interface ISerializable
{
    public string Serialize();
    public void Deserialize(string text);
}
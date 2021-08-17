public interface IJSONable
{
    public JSONObject ToJSON();
    public string ToJSONText();

    public void FromJSON(JSONObject jsonObject);

    public void FromJSONText(string jsonText);
}
using System.Text.Json.Serialization;

public class Button
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
    [JsonPropertyName("displayText")]
    public string DisplayText { get; set; }
    [JsonPropertyName("value")]
    public string Value { get; set; }
}
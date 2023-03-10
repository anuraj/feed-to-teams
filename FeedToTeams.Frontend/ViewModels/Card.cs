using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Card
{
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("Text")]
    public string Text { get; set; }
    [JsonPropertyName("buttons")]
    public List<Button> Buttons { get; set; }
}

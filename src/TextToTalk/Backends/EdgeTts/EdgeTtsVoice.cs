using System.Text.Json.Serialization;

namespace TextToTalk.Backends.EdgeTts;

/// <summary>Edge 语音服务返回的语音信息。</summary>
public sealed class EdgeTtsVoice
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ShortName")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("Gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("Locale")]
    public string Locale { get; set; } = string.Empty;

    [JsonPropertyName("FriendlyName")]
    public string FriendlyName { get; set; } = string.Empty;
}

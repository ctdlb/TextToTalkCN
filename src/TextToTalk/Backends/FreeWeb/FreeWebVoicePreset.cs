using System.Text.Json.Serialization;

namespace TextToTalk.Backends.FreeWeb;

public class FreeWebVoicePreset : VoicePreset
{
    /// <summary>服务提供方：Baidu 或 Google。</summary>
    public string Provider { get; set; } = "Baidu";

    /// <summary>语言代码，见 FreeWebTtsClient.Languages。</summary>
    public string Language { get; set; } = "zh";

    /// <summary>语速，1-9（仅百度生效，5 为正常）。</summary>
    public int Speed { get; set; } = 5;

    public float Volume { get; set; } = 1.0f;

    public override bool TrySetDefaultValues()
    {
        Provider = "Baidu";
        Language = "zh";
        Speed = 5;
        Volume = 1.0f;
        EnabledBackend = TTSBackend.FreeWeb;
        return true;
    }
}

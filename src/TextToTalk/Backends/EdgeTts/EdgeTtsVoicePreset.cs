using System.Text.Json.Serialization;

namespace TextToTalk.Backends.EdgeTts;

public class EdgeTtsVoicePreset : VoicePreset
{
    /// <summary>Edge 语音短名，如 zh-CN-XiaoxiaoNeural。</summary>
    [JsonPropertyName("EdgeTtsVoiceName")]
    public string? VoiceName { get; set; }

    /// <summary>语速百分比，-50 ~ +50，0 为原速。</summary>
    public int Rate { get; set; }

    /// <summary>音量倍数，0.0 ~ 1.0。</summary>
    public float Volume { get; set; } = 1.0f;

    public override bool TrySetDefaultValues()
    {
        VoiceName = "zh-CN-XiaoxiaoNeural";
        Rate = 0;
        Volume = 1.0f;
        EnabledBackend = TTSBackend.EdgeTts;
        return true;
    }
}

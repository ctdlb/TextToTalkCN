namespace TextToTalk.Backends.System
{
    public class SystemSoundQueueItem : SoundQueueItem
    {
        public required VoicePreset Preset { get; set; }

        public required string Text { get; set; }
    }
}
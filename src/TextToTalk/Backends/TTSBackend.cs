using System;
using TextToTalk.Backends.Kokoro;
using TextToTalk.Resources;

namespace TextToTalk.Backends
{
    public enum TTSBackend : long
    {
        System,
        Websocket,
        AmazonPolly,
        Uberduck,
        Azure,
        ElevenLabs,
        OpenAi,
        GoogleCloud,
        Kokoro,
        Megaphone,
        FishAudio,
    }

    public static class TTSBackendExtensions
    {
        public static string GetFormattedName(this TTSBackend backend, PluginConfiguration? config = null)
        {
            return backend switch
            {
                TTSBackend.System => Strings.BackendNameSystem,
                TTSBackend.Websocket => Strings.BackendNameWebsocket,
                TTSBackend.AmazonPolly => Strings.BackendNameAmazonPolly,
                TTSBackend.Uberduck => Strings.BackendNameUberduck,
                TTSBackend.Azure => Strings.BackendNameAzure,
                TTSBackend.ElevenLabs => Strings.BackendNameElevenLabs,
                TTSBackend.OpenAi => Strings.BackendNameOpenAi,
                TTSBackend.GoogleCloud => Strings.BackendNameGoogleCloud,
                TTSBackend.Kokoro when config != null && KokoroBackend.IsModelFileDownloaded(config) => Strings.BackendNameKokoro,
                TTSBackend.Kokoro => Strings.BackendNameKokoroDownloadRequired,
                TTSBackend.Megaphone => Strings.BackendNameMegaphone,
                TTSBackend.FishAudio => Strings.BackendNameFishAudio,
                _ => throw new ArgumentOutOfRangeException(nameof(backend)),
            };
        }

        public static int GetDisplayOrder(this TTSBackend backend) => backend switch
        {
            TTSBackend.System => 0,
            TTSBackend.Megaphone => 1,
            TTSBackend.Websocket => 2,
            TTSBackend.AmazonPolly => 3,
            TTSBackend.Azure => 4,
            TTSBackend.GoogleCloud => 5,
            TTSBackend.ElevenLabs => 6,
            TTSBackend.OpenAi => 7,
            TTSBackend.FishAudio => 8,
            TTSBackend.Kokoro => 9,
            TTSBackend.Uberduck => 10,
            _ => int.MaxValue,
        };

        public static bool IsRelayBackend(this TTSBackend backend) =>
            backend is TTSBackend.Websocket or TTSBackend.Megaphone;

        public static bool AreLexiconsEnabled(this TTSBackend backend)
        {
            return backend switch
            {
                TTSBackend.System => true,
                TTSBackend.Websocket => false,
                TTSBackend.AmazonPolly => true,
                TTSBackend.Uberduck => false,
                TTSBackend.Azure => true,
                TTSBackend.ElevenLabs => false,
                TTSBackend.OpenAi => false,
                TTSBackend.GoogleCloud => false,
                TTSBackend.Kokoro => false,
                TTSBackend.Megaphone => false,
                TTSBackend.FishAudio => false,
                _ => throw new ArgumentOutOfRangeException(nameof(backend)),
            };
        }
    }
}

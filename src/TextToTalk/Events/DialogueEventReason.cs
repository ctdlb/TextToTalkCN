namespace TextToTalk.Events;

public enum DialogueEventReason
{
    TextReceived,
    AddonShown,
    DialogueContextStarted,
    DialogueContextEnded,
    TerritoryChanged,
    LoggedOut,
    PluginStopped,
}

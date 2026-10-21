using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;

namespace TextToTalk.CommandModules;

public class CommandModule(ICommandManager commandManager) : IDisposable
{
    private readonly List<string> commandNames = [];

    protected void AddCommand(string name, IReadOnlyCommandInfo.HandlerDelegate method, string help)
    {
        var info = new CommandInfo(method)
        {
            HelpMessage = help,
            ShowInHelp = true,
        };

        // 注册失败（命令名已被其他插件占用）时不要记进 commandNames，
        // 否则 Dispose 里的 RemoveHandler 会按名字删掉别人的命令。
        if (commandManager.AddHandler(name, info))
        {
            this.commandNames.Add(name);
        }
    }

    protected void RemoveCommand(string name)
    {
        commandManager.RemoveHandler(name);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var commandName in this.commandNames)
            {
                RemoveCommand(commandName);
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
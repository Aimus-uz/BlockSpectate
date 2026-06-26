using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;

namespace BlockSpectate;

public class BlockSpectate : BasePlugin
{
    public override string ModuleName => "BlockSpectate";
    public override string ModuleVersion => "1.7.0";
    public override string ModuleAuthor => "ZAADROT.UZ";
    public override string ModuleDescription => "Players who join spectator mid-game skip next round";

    private bool _isWarmup = true;
    private HashSet<ulong> _skipNextRound = new();

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Pre);
    }

    private HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
    {
        _isWarmup = false;
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (_isWarmup) return HookResult.Continue;

        Server.NextFrame(() =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid || player.IsBot) continue;
                if (!_skipNextRound.Contains(player.SteamID)) continue;

                if (player.TeamNum != (int)CsTeam.Spectator)
                    player.SwitchTeam(CsTeam.Spectator);

                player.PrintToChat($" \x01[\x04ZAADROT\x01] \x07Вы пропускаете этот раунд!");
            }

            _skipNextRound.Clear();
        });

        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        if (_isWarmup) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;
        if (@event.Team != (int)CsTeam.Spectator) return HookResult.Continue;
        if (AdminManager.PlayerHasPermissions(player, "@css/generic")) return HookResult.Continue;

        _skipNextRound.Add(player.SteamID);
        player.PrintToChat($" \x01[\x04ZAADROT\x01] \x07Вы пропустите следующий раунд!");

        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (_isWarmup) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;
        if (!_skipNextRound.Contains(player.SteamID)) return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (player == null || !player.IsValid) return;
            player.SwitchTeam(CsTeam.Spectator);
            player.PrintToChat($" \x01[\x04ZAADROT\x01] \x07Вы пропускаете этот раунд!");
        });

        return HookResult.Continue;
    }
}

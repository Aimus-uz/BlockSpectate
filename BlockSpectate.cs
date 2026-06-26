using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;

namespace BlockSpectate;

public class BlockSpectate : BasePlugin
{
    public override string ModuleName => "BlockSpectate";
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "ZAADROT.UZ";
    public override string ModuleDescription => "Players who join spectator mid-game skip current and next round respawn";

    private bool _isWarmup = true;

    // Players who went to spectator during live game - they must wait 1 extra round
    private HashSet<ulong> _spectatedThisRound = new();
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

        // Players who were in spectator last round now skip this round
        foreach (var steamId in _spectatedThisRound)
            _skipNextRound.Add(steamId);

        _spectatedThisRound.Clear();
        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        if (_isWarmup) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

        // Player going TO spectator
        if (@event.Team == (int)CsTeam.Spectator)
        {
            // Admins can spectate freely
            if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
                return HookResult.Continue;

            // Mark this player - they went to spec during live game
            _spectatedThisRound.Add(player.SteamID);
            player.PrintToChat($" \x01[\x04ZAADROT\x01] \x07Вы пропустите следующий раунд!");
        }

        // Player leaving spectator - remove from skip list after 1 round delay handled in spawn
        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (_isWarmup) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

        if (_skipNextRound.Contains(player.SteamID))
        {
            _skipNextRound.Remove(player.SteamID);

            // Kill them immediately so they can't play this round
            Server.NextFrame(() =>
            {
                if (player == null || !player.IsValid) return;
                var pawn = player.PlayerPawn?.Value;
                if (pawn != null && pawn.IsValid)
                {
                    pawn.CommitSuicide(false, true);
                    player.PrintToChat($" \x01[\x04ZAADROT\x01] \x07Вы пропустили раунд за выход в spectator!");
                }
            });
        }

        return HookResult.Continue;
    }
}

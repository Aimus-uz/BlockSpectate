using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

namespace BlockSpectate;

public class BlockSpectate : BasePlugin
{
    public override string ModuleName => "BlockSpectate";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ZAADROT.UZ";
    public override string ModuleDescription => "Blocks spectator during live game, admins are allowed";

    private bool _isWarmup = true;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
    }

    private HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
    {
        _isWarmup = false;
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (_isWarmup) return HookResult.Continue;

        // Check all spectators at round start and move non-admins out
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot || player.IsHLTV) continue;
            if (player.TeamNum != (int)CsTeam.Spectator) continue;

            if (!AdminManager.PlayerHasPermissions(player, "@css/generic"))
            {
                player.ChangeTeam(CsTeam.None);
                player.PrintToChat($" \x01[\x04ZAADROT\x01] \x07Spectator недоступен во время игры!");
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        if (_isWarmup) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

        // Check if player is trying to join spectator
        if (@event.Team != (int)CsTeam.Spectator) return HookResult.Continue;

        // Allow admins
        if (AdminManager.PlayerHasPermissions(player, "@css/generic"))
            return HookResult.Continue;

        // Block non-admins
        info.DontBroadcast = true;

        // Send back to unassigned / force team select
        Server.NextFrame(() =>
        {
            if (player.IsValid)
            {
                player.ChangeTeam(CsTeam.None);
                player.PrintToChat($" \x01[\x04ZAADROT\x01] \x07Spectator недоступен во время игры!");
            }
        });

        return HookResult.Stop;
    }
}

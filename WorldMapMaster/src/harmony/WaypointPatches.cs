using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using WorldMapMaster.src.Map;

namespace WorldMapMaster.src.harmony;

[HarmonyPatch]
public static class WaypointPatches
{


    [HarmonyPatch(typeof(WaypointMapComponent), "OnMouseMove")]
    [HarmonyPostfix]
    public static void TrackWayPointOnMouseMove(bool ___mouseOver, int ___waypointIndex) => WorldMapMasterModSystem.TrackedWaypointIndex = ___mouseOver ? ___waypointIndex : -1;


    [HarmonyPatch(typeof(GuiDialogAddWayPoint), "TryOpen")]
    [HarmonyPrefix]
    public static void DefaultWaypointPositionToPlayerPosition(GuiDialogAddWayPoint __instance, ICoreClientAPI ___capi) => __instance.WorldPos ??= ___capi.World.Player.Entity.Pos.XYZ;

    [HarmonyPatch(typeof(WorldMapManager), "RegisterDefaultMapLayers")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ReplaceWaypointMapLayer(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions).Start();
        matcher.MatchStartForward(
            CodeMatch.Calls(AccessTools.Method(typeof(WorldMapManager), "RegisterMapLayer", [typeof(string), typeof(double)], [typeof(WaypointMapLayer)]))
        );

        matcher.Operand = AccessTools.Method(typeof(WorldMapManager), "RegisterMapLayer", [typeof(string), typeof(double)], [typeof(CustomWaypointMapLayer)]);

        return matcher.InstructionEnumeration();
    }
}

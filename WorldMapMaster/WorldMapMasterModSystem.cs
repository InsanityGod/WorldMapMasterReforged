using HarmonyLib;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WorldMapMaster;

public class WorldMapMasterModSystem : ModSystem
{

    private Harmony harmony;

    public static int TrackedWaypointIndex { get; internal set; } = -1;
    
    private GuiDialogAddWayPoint addWaypointDialog;

    private ICoreAPI api;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        this.api = api;
        
        if (!Harmony.HasAnyPatches(Mod.Info.ModID))
        {
            Mod.Logger.Event("Harmony Patching");
            harmony = new Harmony(Mod.Info.ModID);
            try
            {
                harmony.PatchAllUncategorized();
            }
            catch (Exception e)
            {
                Mod.Logger.Error(e);
            }
        }

    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        RegisterKeybinds(api.Input);
    }

    public void RegisterKeybinds(IInputAPI input)
    {
        input.RegisterHotKey("waypointDelete", "Remove hovered Waypoint", GlKeys.Delete, HotkeyType.HelpAndOverlays);
        input.SetHotKeyHandler("waypointDelete", DeleteWaypoint);
        
        input.RegisterHotKey("waypointAdd", "Add a Waypoint at current position", GlKeys.PageDown);
        input.SetHotKeyHandler("waypointAdd", AddNewWaypoint);

        input.RegisterHotKey("waypointQuickAdd", "Add a Waypoint at current position with default title", GlKeys.KeypadPlus);
        input.SetHotKeyHandler("waypointQuickAdd", QuickAddWaypoint);
    }

    private bool DeleteWaypoint(KeyCombination keyCombination)
    {
        if (api is not ICoreClientAPI capi || TrackedWaypointIndex <= -1) return true;

        capi.SendChatMessage(string.Format("/waypoint remove {0}", TrackedWaypointIndex));
        TrackedWaypointIndex = -1;

        return true;
    }

    private bool AddNewWaypoint(KeyCombination keyCombination)
    {
        if(api is not ICoreClientAPI capi) return true;
        
        if (addWaypointDialog is not null)
        {
            addWaypointDialog.TryClose();
            addWaypointDialog.Dispose();
        }
        var maplayers = capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers;

        var wml = maplayers.OfType<WaypointMapLayer>().Single();
        addWaypointDialog = new GuiDialogAddWayPoint(capi, wml);
        addWaypointDialog.TryOpen();

        return true;
    }

    private bool QuickAddWaypoint(KeyCombination keyCombination)
    {
        if(api is not ICoreClientAPI capi) return true;
        
        Vec3d curPos = capi.World.Player.Entity.Pos.XYZ;
        Vec3d hrPos = curPos.Clone().Sub(capi.World.DefaultSpawnPosition.AsBlockPos);

        capi.SendChatMessage(
            $"/waypoint addati {"circle"} ={curPos.XInt.ToString(GlobalConstants.DefaultCultureInfo)} ={curPos.YInt.ToString(GlobalConstants.DefaultCultureInfo)} ={curPos.ZInt.ToString(GlobalConstants.DefaultCultureInfo)} {"false"} {"white"} {hrPos.XInt}, {hrPos.YInt}, {hrPos.ZInt}"
        );

        return true;
    }

    public override void Dispose()
    {
        base.Dispose();
        harmony?.UnpatchAll(harmony.Id);
    }
}

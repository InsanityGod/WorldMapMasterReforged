using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WorldMapMaster;

public partial class WorldMapMasterReforgedModSystem : ModSystem
{
    public static int TrackedWaypointIndex { get; internal set; } = -1;
    
    private GuiDialogAddWayPoint? addWaypointDialog;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        AutoSetup(api);
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        AutoAssetsLoaded(api);
    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        RegisterKeybinds(api.Input);
    }

    public void RegisterKeybinds(IInputAPI input)
    {
        input.RegisterHotKey("waypointDelete", Lang.Get("worldmapmasterreforged:hotkey-waypoint-delete"), GlKeys.Delete, HotkeyType.HelpAndOverlays);
        input.SetHotKeyHandler("waypointDelete", DeleteWaypoint);
        
        input.RegisterHotKey("waypointAdd", Lang.Get("worldmapmasterreforged:hotkey-waypoint-add"), GlKeys.PageDown);
        input.SetHotKeyHandler("waypointAdd", AddNewWaypoint);

        input.RegisterHotKey("waypointQuickAdd", Lang.Get("worldmapmasterreforged:hotkey-waypoint-quickadd"), GlKeys.KeypadPlus);
        input.SetHotKeyHandler("waypointQuickAdd", QuickAddWaypoint);

        input.RegisterHotKey("waypointQuickAddTarget", Lang.Get("worldmapmasterreforged:hotkey-waypoint-quickaddtarget"), GlKeys.Unknown);
        input.SetHotKeyHandler("waypointQuickAddTarget", QuickAddTargetWaypoint);
    }

    private bool DeleteWaypoint(KeyCombination keyCombination)
    {
        if (_api is not ICoreClientAPI capi || TrackedWaypointIndex <= -1) return true;

        capi.SendChatMessage(string.Format("/waypoint remove {0}", TrackedWaypointIndex));
        TrackedWaypointIndex = -1;

        return true;
    }

    private bool AddNewWaypoint(KeyCombination keyCombination)
    {
        if(_api is not ICoreClientAPI capi) return true;
        
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
        if(_api is not ICoreClientAPI capi) return true;
        
        Vec3d curPos = capi.World.Player.Entity.Pos.XYZ;
        Vec3d hrPos = curPos.Clone().Sub(capi.World.DefaultSpawnPosition.AsBlockPos);

        capi.SendChatMessage(
            $"/waypoint addati {"circle"} ={curPos.XInt.ToString(GlobalConstants.DefaultCultureInfo)} ={curPos.YInt.ToString(GlobalConstants.DefaultCultureInfo)} ={curPos.ZInt.ToString(GlobalConstants.DefaultCultureInfo)} false white {hrPos.XInt}, {hrPos.YInt}, {hrPos.ZInt}"
        );

        return true;
    }

    private bool QuickAddTargetWaypoint(KeyCombination keyCombination)
    {
        if(_api is not ICoreClientAPI capi) return true;
        Vec3d? curPos = null;
        string? title = null;
        if(capi.World.Player.Entity.EntitySelection?.Entity is Entity entity)
        {
            curPos = entity.Pos.XYZ;
            title = entity.GetName();
        }
        else if(capi.World.Player.Entity.BlockSelection?.Position is BlockPos pos)
        {
            curPos = pos.ToVec3d();
            title = capi.World.BlockAccessor.GetBlock(pos).GetPlacedBlockName(capi.World, pos);
        }
        if(curPos is null) return true;

        capi.SendChatMessage(
            $"/waypoint addati {"circle"} ={curPos.XInt.ToString(GlobalConstants.DefaultCultureInfo)} ={curPos.YInt.ToString(GlobalConstants.DefaultCultureInfo)} ={curPos.ZInt.ToString(GlobalConstants.DefaultCultureInfo)} false white {title}"
        );

        return true;
    }

    public override void Dispose()
    {
        base.Dispose();
        AutoDispose();
    }
}

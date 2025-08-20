using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using WorldMapMaster.src.UI;

namespace WorldMapMaster.src.Map;

public class CustomWaypointMapLayer : WaypointMapLayer
{
    public const string ComposerKey = "worldmap-layer-waypoints";
    
    private readonly ICoreClientAPI capi;
    
    public string SearchText { get; private set; } = string.Empty;

    public string OrderBy { get; private set; } = "timeasc";
    
    public static readonly string[] ValidOrderByValues = new string[] { "timeasc", "timedesc", "distanceasc", "distancedesc", "titleasc", "titledesc" };

    private List<WaypointListItem> SortedWaypointItems = new();

    private GuiDialogWorldMap _guiDialogWorldMap;
    
    private GuiComposer _compo;

    private readonly ILogger logger;

    public CustomWaypointMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        capi = api as ICoreClientAPI;
        logger = api.ModLoader.GetModSystem<WorldMapMasterModSystem>().Mod.Logger;
    }

    public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        WorldMapMasterModSystem.TrackedWaypointIndex = -1;
        base.OnMouseMoveClient(args, mapElem, hoverText);
    }

    private void OnFocusChange(bool focus)
    {
        if(focus && IsDirty)
        {
           Update();
        }
    }

    public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
    {
        _guiDialogWorldMap = guiDialogWorldMap ?? _guiDialogWorldMap;
        _compo = compo ?? _compo;
        
        UpdateSorting();
        _compo.OnFocusChanged = OnFocusChange;
        ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog
            .WithFixedPosition(
                (_compo.Bounds.renderX + _compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale + 10,
                _compo.Bounds.renderY / RuntimeEnv.GUIScale + 120
            )
            .WithAlignment(EnumDialogArea.None);

        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        _guiDialogWorldMap.Composers[ComposerKey] = capi.Gui
            .CreateCompo(ComposerKey, dlgBounds)
            .AddShadedDialogBG(bgBounds, false)
            .AddDialogTitleBar(Lang.Get("Your waypoints:"), () => _guiDialogWorldMap.Composers[ComposerKey].Enabled = false)
            .BeginChildElements(bgBounds)
            .AddDropDown(
                SortedWaypointItems.Select(o => o.Id).ToArray(),
                SortedWaypointItems.Select(o => o.Title).ToArray(),
                0,
                OnSelectedWaypointChanged,
                ElementBounds.Fixed(0, 75, 300, 35),
                "wplist"
            )
            .AddAutoclearingText(
                ElementBounds.Fixed(0, 30, 120, 35),
                OnSearchTextChanged,
                null,
                "qs"
            )
            .AddDropDown(
                ValidOrderByValues,
                ValidOrderByValues.Select(value => Lang.Get(value)).ToArray(),
                0,
                OnOrderByChanged,
                ElementBounds.Fixed(125, 30, 125, 35),
                "orderlist"
            )
            .EndChildElements()
            .Compose();

        var searchElement = _guiDialogWorldMap.Composers[ComposerKey].GetElement("qs") as GuiElementTextInput;
        searchElement.SetValue(SearchText);
        searchElement.SetPlaceHolderText(Lang.Get("Search..."));

        var orderList = _guiDialogWorldMap.Composers[ComposerKey].GetElement("orderlist") as GuiElementDropDown;
        orderList.SetSelectedValue(OrderBy);
        
        _guiDialogWorldMap.Composers[ComposerKey].Enabled = false;
    }

    public bool IsDirty { get; private set; }

    public void MarkDirty()
    {
        IsDirty = true;
        if(_guiDialogWorldMap is null || !_guiDialogWorldMap.IsOpened() || !_guiDialogWorldMap.Composers[ComposerKey].Enabled) return;
        Update();
    }

    /// <summary>
    /// Updates sorting and GUI, should only be called if <see cref="IsDirty"/>
    /// </summary>
    public void Update()
    {
        UpdateSorting();
        UpdateGUI();
        
        IsDirty = false;
    }

    private void UpdateGUI()
    {
        if(_guiDialogWorldMap.Composers[ComposerKey]?.GetElement("wplist") is not GuiElementDropDown dropdown) return;
        
        
        var newValues = SortedWaypointItems.Select(o => o.Id).ToArray();
        var newNames = SortedWaypointItems.Select(o => o.Title).ToArray();
        
        if(newValues.SequenceEqual(dropdown.listMenu.Values) && newNames.SequenceEqual(dropdown.listMenu.Names)) return;
        dropdown.SetList(newValues, newNames);
    }

    public void UpdateSorting()
    {
        SortedWaypointItems.Clear();
        EntityPos playerPosition = capi.World.Player.Entity.Pos;

        for (int i = 0; i < ownWaypoints.Count; i++)
        {
            Waypoint waypoint = ownWaypoints[i];

            if (waypoint.Title.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase))
            {
                //HACK: work around for vanilla issue where server returns an empty Guid for story locations and death points (can be solved by restarting the server)
                if (waypoint.Guid is null)
                {
                    waypoint.Guid = Guid.NewGuid().ToString();
                    logger.Warning($"Fixed Waypoint GUID for '{waypoint.Title}' (vanilla issue)");
                }

                float distance = (float)Math.Sqrt(Math.Pow(playerPosition.X - waypoint.Position.X, 2) + Math.Pow(playerPosition.Z - waypoint.Position.Z, 2));
                SortedWaypointItems.Add(new WaypointListItem
                {
                    Id = waypoint.Guid,
                    Title = $"{waypoint.Title} - {distance:F2}m",
                    Distance = distance,
                    Index = i
                });
            }
        }

        SortedWaypointItems = OrderBy switch
        {
            "timeasc" => SortedWaypointItems.OrderBy(o => o.Index).ToList(),
            "timedesc" => SortedWaypointItems.OrderByDescending(o => o.Index).ToList(),
            "distanceasc" => SortedWaypointItems.OrderBy(o => o.Distance).ToList(),
            "distancedesc" => SortedWaypointItems.OrderByDescending(o => o.Distance).ToList(),
            "titleasc" => SortedWaypointItems.OrderBy(o => o.Title).ToList(),
            "titledesc" => SortedWaypointItems.OrderByDescending(o => o.Title).ToList(),
        };

        //Add an empty start/unselect element
        SortedWaypointItems.Insert(0, new WaypointListItem { Id = "placeholder", Title = "- - -", Distance = 0, Index = -1 });
    }

    private void OnSearchTextChanged(string text)
    {
        SearchText = text;
        MarkDirty();
    }

    private void OnOrderByChanged(string orderBy, bool selected)
    {
        OrderBy = orderBy;
        MarkDirty();
    }


    private void OnSelectedWaypointChanged(string waypointId, bool selected)
    {
        if (string.IsNullOrEmpty(waypointId) || waypointId == "placeholder")  return;

        var selectedWaypoint = ownWaypoints.Find(waypoint => waypoint.Guid == waypointId);
        if(selectedWaypoint is null)
        {
            logger.Error("Could not find waypoint by identifier: {Identifier}", waypointId);
            return;
        }

        var mapElem = _compo.GetElement("mapElem") as GuiElementMap;
        mapElem.CenterMapTo(selectedWaypoint.Position.AsBlockPos);
    }

    public override void OnDataFromServer(byte[] data)
    {
        base.OnDataFromServer(data);

        //TODO Couldn't figure out why we mark dirty here, but ideally we should avoid calling dirty here as it is called a lot when interacting with the map
        MarkDirty();
    }
}
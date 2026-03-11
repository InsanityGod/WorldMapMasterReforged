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

public class CustomWaypointMapLayer(ICoreAPI api, IWorldMapManager mapSink) : WaypointMapLayer(api, mapSink)
{
    public const string ComposerKey = "worldmap-layer-waypoints";
    
    public static readonly string[] ValidOrderByValues = ["timeasc", "timedesc", "distanceasc", "distancedesc", "titleasc", "titledesc"];

    private readonly ICoreClientAPI capi = (ICoreClientAPI)api;

    private readonly ILogger logger = api.ModLoader.GetModSystem<WorldMapMasterModSystem>().Mod.Logger;
    
    public string SearchText { get; private set; } = string.Empty;

    public string OrderBy { get; private set; } = "timeasc";

    private readonly List<WaypointListItem> SortedWaypointItems = [];

    private GuiDialogWorldMap? _guiDialogWorldMap;
    
    private GuiComposer? _compo;

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
        if(_compo is null || _guiDialogWorldMap is null) return;

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
            .AddDialogTitleBar(Lang.Get("worldmapmasterreforged:waypoints-title"), () => _guiDialogWorldMap.Composers[ComposerKey].Enabled = false)
            .BeginChildElements(bgBounds)
            .AddDropDown(
                ["placeholder", .. SortedWaypointItems.Select(o => o.Id)],
                ["- - -", .. SortedWaypointItems.Select(o => o.Title)],
                0,
                OnSelectedWaypointChanged,
                ElementBounds.Fixed(0, 75, 300, 35),
                "wplist"
            )
            .AddAutoclearingText(
                ElementBounds.Fixed(0, 30, 120, 35),
                OnSearchTextChanged,
                null!,
                "qs"
            )
            .AddDropDown(
                ValidOrderByValues,
                [.. ValidOrderByValues.Select(value => Lang.Get($"worldmapmasterreforged:{value}"))],
                0,
                OnOrderByChanged,
                ElementBounds.Fixed(125, 30, 125, 35),
                "orderlist"
            )
            .EndChildElements()
            .Compose();

        if (_guiDialogWorldMap.Composers[ComposerKey].GetElement("qs") is GuiElementTextInput searchElement)
        {
            searchElement.SetValue(SearchText);
            searchElement.SetPlaceHolderText(Lang.Get("Search..."));
        }

        if (_guiDialogWorldMap.Composers[ComposerKey].GetElement("orderlist") is GuiElementDropDown orderList)
        {
            orderList.SetSelectedValue(OrderBy);
        }
        
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
        if(_guiDialogWorldMap is null || _guiDialogWorldMap.Composers[ComposerKey]?.GetElement("wplist") is not GuiElementDropDown dropdown) return;
        
        string[] newValues = ["placeholder", .. SortedWaypointItems.Select(o => o.Id)];
        string[] newNames = ["- - -", .. SortedWaypointItems.Select(o => o.Title)];
        
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

            if (!waypoint.Title.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)) continue;

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

        SortedWaypointItems.Sort(OrderBy switch
        {
            "timeasc"      => static (a, b) => a.Index.CompareTo(b.Index),
            "timedesc"     => static (a, b) => b.Index.CompareTo(a.Index),
            "distanceasc"  => static (a, b) => a.Distance.CompareTo(b.Distance),
            "distancedesc" => static (a, b) => b.Distance.CompareTo(a.Distance),
            "titleasc"     => static (a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase),
            "titledesc"    => static (a, b) => string.Compare(b.Title, a.Title, StringComparison.OrdinalIgnoreCase),
            _ => throw new NotSupportedException($"Unsopported OrderBy was detected: '{OrderBy}'")
        });

        //Add an empty start/unselect element
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
        if (_compo is null || string.IsNullOrEmpty(waypointId) || waypointId == "placeholder") return;

        var selectedWaypoint = ownWaypoints.Find(waypoint => waypoint.Guid == waypointId);
        if (selectedWaypoint is null)
        {
            logger.Error("Could not find waypoint by identifier: {0}", waypointId);
            return;
        }

        if (_compo.GetElement("mapElem") is not GuiElementMap mapElem) return;

        mapElem.CenterMapTo(selectedWaypoint.Position.AsBlockPos);
    }

    public override void OnDataFromServer(byte[] data)
    {
        base.OnDataFromServer(data);

        MarkDirty();
    }
}
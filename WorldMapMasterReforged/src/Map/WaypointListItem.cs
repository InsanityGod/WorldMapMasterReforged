using Vintagestory.GameContent;

namespace WorldMapMaster.src.Map;

public class WaypointListItem
{
    /// <summary>
    /// The internal identifier of the waypoint <see cref="Waypoint.Guid"/> or "placeholder" if a placeholder 
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The title used for display, consisting of <see cref="Waypoint.Title"/> and <see cref="Distance"/>
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// The position of the waypoint in <see cref="WaypointMapLayer.ownWaypoints"/> or -1 if placeholder
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Last known distance between player and waypoint
    /// </summary>
    public float Distance { get; set; }
}
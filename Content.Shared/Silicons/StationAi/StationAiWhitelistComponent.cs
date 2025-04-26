using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Indicates an entity that has <see cref="StationAiHeldComponent"/> can interact with this.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStationAiSystem))]
public sealed partial class StationAiWhitelistComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Can this still be interacted with even when it's being jammed by a signal jammer, etc.?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BypassesJamming = true;
}

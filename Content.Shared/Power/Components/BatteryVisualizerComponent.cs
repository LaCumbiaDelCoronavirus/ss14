using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Power.Visualizers;

/// <summary>
/// Component for visualising a power-holding entity that can either be: discharging, charging, and full.
/// Requires the entity to have BatteryComponent and it's other related components.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class BatteryVisualizerComponent : Component
{
    /// <summary>
    /// The prefix used for the RSI states of the sprite layers indicating the charge level of the entity.
    /// Suffixed with the current charge level, going from 1 to <see cref="ChargeLevelCount"/>.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ChargeOverlayPrefix = null;

    /// <summary>
    /// Number of distinct charge levels this has. Not used for anything if <see cref="ChargeOverlayPrefix"/>
    /// is null.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int ChargeLevelCount = 6;

    /// <summary>
    /// Last charge level this had.
    /// Not replicated to the client; server-only.
    /// </summary>
    [DataField(serverOnly: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public int LastChargeLevel = -1;

    /// <summary>
    /// Next time that this has its appearance updated.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextVisualsUpdate;

    /// <summary>
    /// Dictionary used to determine the input-indicator overlay and output-indicator overlay sprites for every chargestate.
    /// If a chargestate isn't specified here, both of its layers will be hidden.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<ChargeState, BatteryVisualizerOverlayStates> ChargeStateOverlays = new();
}

// todo: replace this with a tuple when datafields dont explode if you try to
[NetSerializable, Serializable]
[DataDefinition]
public partial record struct BatteryVisualizerOverlayStates
{
    [DataField]
    public string? InputState = null;

    [DataField]
    public string? OutputState = null;
};

public enum BatteryVisuals : byte
{
    /// <summary>
    /// Corresponds to <see cref="Power.ChargeState"/>. 
    /// </summary>
    ChargeState,

    /// <summary>
    /// At most, at <see cref="BatteryVisualizerComponent.ChargeLevelCount"/>. 
    /// </summary>
    ChargeLevel,
}

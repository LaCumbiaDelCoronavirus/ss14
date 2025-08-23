using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Aircraft.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AircraftCabinComponent : Component
{
    [ViewVariables]
    public readonly string PilotSlotId = "aircraft-pilot-slot";

    /// <summary>
    /// The slot which the pilot is in.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot PilotSlot = default!;

    /// <summary>
    /// How long it takes to exit the aircraft.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ExitDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long it takes to enter the aircraft.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EntryDuration = TimeSpan.FromSeconds(2);
}

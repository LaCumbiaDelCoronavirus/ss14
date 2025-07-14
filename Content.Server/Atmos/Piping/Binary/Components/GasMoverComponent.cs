using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Server.Atmos.Components;

/// <summary>Base class for any atmos device that moves gas from one node to another.</summary>
[RegisterComponent]
public sealed partial class GasMoverComponent : Component
{
    [DataField("outlet")]
    public string OutletName = "outlet";

    public GasMoverNodeGroupType MoverType = GasMoverNodeGroupType.Binary;
}

/// <summary>Enum for how the nodes of an entity with a <see cref="GasMoverComponent"/> are structured, like whether the device is binary or trinary.</summary>
public enum GasMoverNodeGroupType : byte
{
    Binary,
    Trinary
}

/// <summary>Event raised on an atmos device to check how much gas a device wants to input into a pipenet, and the device's output threshold.</summary>
[ByRefEvent]
public record struct ResolveGasMoverEvent(float OutputThreshold, bool Handled = false);

/// <summary>Event raised on an atmos device for it to move gas.</summary>
[ByRefEvent]
public record struct ProcessGasMoverEvent(GasMixture MovedGas, float OutputThreshold, bool Handled = false);

/// <summary>Event raised on an atmos device to find the pipenets that the device depends on.</summary>
[ByRefEvent]
public record struct GetDependantPipeNetsEvent(HashSet<IPipeNet> DependantPipeNets);

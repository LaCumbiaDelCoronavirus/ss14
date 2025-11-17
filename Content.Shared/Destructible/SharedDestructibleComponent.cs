using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Destructible;

/// <summary>
///     When attached to an <see cref="EntityUid"/>, allows it to take damage
///     and triggers thresholds when reached.
/// </summary>
[NetworkedComponent, AutoGenerateComponentState]
public abstract partial class SharedDestructibleComponent : Component
{
    /// <summary>
    /// The amount of damage this entity can have before it gets broken/destroyed.
    /// </summary>
    /// <remarks>
    /// This assumes that this entity has some sort of destruction or breakage behavior triggered by a
    /// total-damage threshold. Otherwise, this will default to <see cref="FixedPoint2.MaxValue"/>.
    /// This is also only determined server-side, but
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [Access(typeof(SharedDestructibleSystem), Other = AccessPermissions.ReadExecute)]
    public FixedPoint2 DestructionThreshold = FixedPoint2.MaxValue;
}

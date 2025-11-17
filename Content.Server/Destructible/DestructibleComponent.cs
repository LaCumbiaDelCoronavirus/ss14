using Content.Server.Destructible.Thresholds;
using Content.Shared.Destructible;

namespace Content.Server.Destructible;

/// <inheritdoc cref="SharedDestructibleComponent"/>
[RegisterComponent]
public sealed partial class DestructibleComponent : SharedDestructibleComponent
{
    /// <summary>
    /// A list of damage thresholds for the entity;
    /// includes their triggers and resultant behaviors
    /// </summary>
    [DataField]
    public List<DamageThreshold> Thresholds = new();

    /// <summary>
    /// Specifies whether the entity has passed a damage threshold that causes it to break
    /// </summary>
    [DataField]
    public bool IsBroken = false;
}

using Content.Shared.Destructible.Thresholds;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Server.Destructible
{
    /// <summary>
    ///     When attached to an <see cref="EntityUid"/>, allows it to take damage
    ///     and triggers thresholds when reached.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class DestructibleComponent : Component
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

        /// <summary>
        /// The amount of damage before this entity gets broken/destroyed.
        /// </summary>
        /// <remarks>
        /// This assumes that this entity has some sort of destruction or breakage behavior triggered by a
        /// total-damage threshold. Otherwise, this will default to <see cref="FixedPoint2.MaxValue"/>.
        /// This is also only determined server-side, but
        /// </remarks>
        [AutoNetworkedField]
        public FixedPoint2 DestructionThreshold = FixedPoint2.MaxValue;
    }
}

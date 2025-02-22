namespace Content.Server.Ghost.Components
{
    /// <summary>
    /// Component to track players that are in the afterlife
    /// </summary>
    [RegisterComponent]
    public sealed partial class GhostAfterlifePlayerComponent : Component
    {
        /// <summary>
        /// UID of the entity that originally belonged to this player
        /// </summary>
        [DataField]
        public EntityUid? OriginalEntityUid;
    }
}

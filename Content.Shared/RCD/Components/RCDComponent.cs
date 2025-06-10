using Content.Shared.RCD.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Shared.RCD.Components;

/// <summary>
/// Main component for the RCD, which can optionally use <see cref="LimitedChargesComponent"/>,
///     which allows it to use charges and be refilled.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRCDSystem))]
public sealed partial class RCDComponent : Component
{
    #region Fields - Prototypes

    /// <summary>
    /// List of <see cref="RCDPrototype"/>s that the device has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<RCDPrototype>> AvailablePrototypes { get; set; } = new();

    /// <summary>
    /// The <see cref="ProtoId"/> of the currently selected <see cref="RCDPrototype"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<RCDPrototype> SelectedProtoId { get; set; } = "Invalid";

    /// <summary>
    /// The <see cref="ProtoId"/> of the RCD prototype used for deconstructing any tile that <i>is not</i> space. (e.g., steel tiles, but not lattices.)
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<RCDPrototype> NonSpaceTileDeconstructionProtoId { get; set; } = "DeconstructTile";

    /// <summary>
    /// The <see cref="ProtoId"/> of the RCD prototype used for deconstructing any tile that <i>is</i> space (e.g., lattices, but not hull plating.)
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<RCDPrototype> SpaceTileDeconstructionProtoId { get; set; } = "DeconstructLattice";

    #endregion
    #region Fields - Sounds

    /// <summary>
    /// Sound that plays when a RCD operation successfully completes
    /// </summary>
    [DataField]
    public SoundSpecifier SuccessSound { get; set; } = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");


    #endregion
    #region Fields - Physics

    /// <summary>
    /// The direction constructed entities will face upon spawning
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction ConstructionDirection
    {
        get => _constructionDirection;
        set
        {
            _constructionDirection = value;
            ConstructionTransform = new Transform(new(), _constructionDirection.ToAngle());
        }
    }

    private Direction _constructionDirection = Direction.South;

    /// <summary>
    /// Returns a rotated transform based on the specified ConstructionDirection
    /// </summary>
    /// <remarks>
    /// Contains no position data
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public Transform ConstructionTransform { get; private set; }


    #endregion
}

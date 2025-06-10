using Content.Shared.Physics;
using Content.Shared.Random.Rules.TileRules;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.RCD;

/// <summary>
/// Contains the parameters for an RCD operation, such as a construction (separate for entities and tiles)
///     and deconstruction.
/// </summary>
[Prototype("rcd")]
public sealed partial class RCDPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    #region Fields - UI

    /// <summary>
    /// The RCD mode associated with the operation.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public RcdMode Mode { get; private set; } = RcdMode.Invalid;

    /// <summary>
    /// The form of the operation; whether the operation is on a tile or an object.
    /// </summary>
    [DataField("form", required: true), ViewVariables(VVAccess.ReadOnly)]
    public RcdOperationForm OperationForm { get; private set; } = RcdOperationForm.Invalid;

    /// <summary>
    /// The name associated with the prototype
    /// </summary>
    [DataField("name"), ViewVariables(VVAccess.ReadOnly)]
    public string SetName { get; private set; } = "Unknown";

    /// <summary>
    /// The name of the radial container that this prototype will be listed under on the RCD menu
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string Category { get; private set; } = "Undefined";

    /// <summary>
    /// <see cref="SpriteSpecifier"/> for this prototypes menu icon
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SpriteSpecifier? Sprite { get; private set; }

    #endregion
    #region Fields - Operations

    /// <summary>
    /// Number of charges consumed when the operation is completed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int Cost { get; private set; } = 1;

    /// <summary>
    /// The length of the operation
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Delay { get; private set; } = 1f;

    #endregion
    #region Fields - Prototypes

    /// <summary>
    /// The visual effect that plays during this operation
    /// </summary>
    [DataField("fx"), ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId? Effect { get; private set; }

    /// <summary>
    /// The entity prototype that will be constructed (mode dependent)
    /// </summary>
    [DataField("prototype"), ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId? ConstructedPrototype { get; private set; }

    #endregion
    #region Fields - Misc.

    /// <summary>
    /// A list of <see cref="TileRule"/>s that govern where the entity prototype can be constructed
    /// </summary>
    [DataField("rules"), ViewVariables(VVAccess.ReadOnly)]
    public HashSet<TileRule> ConstructionRules { get; private set; } = new();

    #endregion
    #region Fields - Physics

    /// <summary>
    /// The collision mask used for determining whether the entity prototype will fit into the target tile.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public CollisionGroup CollisionMask { get; private set; } = CollisionGroup.None;

    /// <summary>
    /// Specifies a set of custom collision bounds for determining whether the entity prototype will fit into the target tile
    /// </summary>
    /// <remarks>
    /// Should be set assuming that the entity faces south.
    /// Make sure that Rotation is set to RcdRotation.User if the entity is to be rotated by the user
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Box2? CollisionBounds
    {
        get => _collisionBounds;

        private set
        {
            _collisionBounds = value;

            if (_collisionBounds != null)
            {
                var poly = new PolygonShape();
                poly.SetAsBox(_collisionBounds.Value);

                CollisionPolygon = poly;
            }
        }
    }

    private Box2? _collisionBounds;

    /// <summary>
    /// The optional polygon shape associated with the prototype CollisionBounds (if set)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public PolygonShape? CollisionPolygon { get; private set; }

    /// <summary>
    /// Governs how the local rotation of the constructed entity will be set
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public RcdRotation Rotation { get; private set; } = RcdRotation.User;

    #endregion
}

/// <summary>
/// Base class for RCD operations.
/// </summary>
public interface IRcdOperation
{
    /// <summary>
    /// The coordinates at which this operation takes place at.
    ///     In the case of a <see cref="ObjectRcdConstructionOperation"/>,
    ///     this is also where the constructed object is spawned.
    /// </summary>
    public EntityCoordinates Position { get; }

    /// <summary>The cost, in RCD charges, of this operation.</summary>
    public int Cost { get; }

    /// <summary>How long this operation will take.</summary>
    public float Delay { get; }

    /// <summary>The visual effect that will play during this operation.</summary>
    public EntProtoId? Effect { get; }
}

/// <summary>An RCD operation that creates an entity of a given <see cref="EntProtoId"/> at a given position, facing a given direction.</summary>
public readonly record struct ObjectRcdConstructionOperation(EntityCoordinates Position, int Cost, float Delay, EntProtoId? Effect,
    Direction Direction, EntProtoId ConstructedProtoId) : IRcdOperation;

/// <summary>An RCD operation that manipulates (, possibly destroying,) a tile at a given tile position.</summary>
public readonly record struct TileRcdOperation(EntityCoordinates Position, int Cost, float Delay, EntProtoId? Effect,
    Vector2i TilePosition) : IRcdOperation;

/// <summary>An RCD operation that constructs a tile, being of the given <see cref="TileRef"/>, at a given tile position.</summary>
public readonly record struct TileRcdConstructionOperation(EntityCoordinates Position, int Cost, float Delay, EntProtoId? Effect,
    TileRef Tile, Vector2i TilePosition) : IRcdOperation;



#region Enums
public enum RcdMode : byte
{
    Invalid,
    Deconstruct,
    Construct,
}

public enum RcdOperationForm : byte
{
    Invalid,
    Object,
    Tile,
}
public enum RcdRotation : byte
{
    Fixed,      // The entity has a local rotation of zero
    Camera,     // The rotation of the entity matches the local player camera
    User,       // The entity can be rotated by the local player prior to placement
}
#endregion

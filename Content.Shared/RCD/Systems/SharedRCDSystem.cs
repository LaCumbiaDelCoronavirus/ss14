using System.Runtime.CompilerServices;
using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Random.Rules.TileRules;
using Content.Shared.RCD.Components;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Shared.RCD.Systems;

/// <summary>
/// System for handling RCD interactions, and mainly interfacing with
///     <see cref="TileRulesSystem"/> to validate those interactions.
/// </summary>
public abstract partial class SharedRCDSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly FloorTileSystem _floorTileSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedChargesSystem _chargesSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly TileRulesSystem _tileRulesSystem = default!;

    /// <summary>Delay for construction when replacing a tile.</summary>
    private const int InstantConstructionDelay = 0;
    private static readonly EntProtoId InstantConstructionFx = "EffectRCDConstruct0";

    // Tiles could just get their own RCDPrototype
    /// <summary>The <see cref="RCDPrototype"/> used when deconstructing non-space tiles.</summary>
    private static readonly ProtoId<RCDPrototype> DeconstructTileProto = "DeconstructTile";
    /// <summary>The <see cref="RCDPrototype"/> used when deconstructing space tiles. (e.g., lattices)</summary>
    private static readonly ProtoId<RCDPrototype> DeconstructLatticeProto = "DeconstructLattice";


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RCDComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RCDComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnExamine(Entity<RCDComponent> rcd, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var (rcdUid, rcdComponent) = rcd;
        if (!_prototypeManager.TryIndex(rcdComponent.SelectedProtoId, out var selectedPrototype))
            return;
    }


    #region RCD Validation

    /// <summary>
    /// If a <see cref="LimitedChargesComponent"/> is present in the RCD, checks if it has
    ///     a number of charges at least at the specified <paramref name="minimumCharge"/>,
    ///     as long as it is not 0.
    /// </summary>
    /// <returns>
    /// True, unless a <see cref="LimitedChargesComponent"/> is present, in which case
    ///     it is used to see if it has enough charge.
    /// </returns>
    [Pure]
    public bool CanRcdWork(Entity<RCDComponent, LimitedChargesComponent?> rcd, float minimumCharges = 0)
    {
        var (uid, _, chargesComponent) = rcd;

        if (chargesComponent == null && !TryComp(uid, out chargesComponent))
            return true;

        var charges = _chargesSystem.GetCurrentCharges((uid, chargesComponent, null));
        if (charges < minimumCharges || charges == 0)
            return false;

        return true;
    }

    #endregion
    #region Helper functions

    ///<inheritdoc cref="TileRulesSystem.IsTrue(EntityUid, TileRulesPrototype, TileRef, Vector2i, HashSet{EntityUid}?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsTileValidForPlacement(EntityUid tileParentUid, TileRef tile, Vector2i position, TileRulesPrototype rulesPrototype, HashSet<EntityUid>? intersectingEntites = null)
        => _tileRulesSystem.IsTrue(tileParentUid, rulesPrototype, tile, position, intersectingEntites);

    private bool DoesCustomBoundsIntersectWithFixture(PolygonShape boundingPolygon, Transform boundingTransform, EntityUid fixtureOwner, Fixture fixture)
    {
        var entXformComp = Transform(fixtureOwner);
        var entXform = new Transform(new(), entXformComp.LocalRotation);

        return boundingPolygon.ComputeAABB(boundingTransform, 0).Intersects(fixture.Shape.ComputeAABB(entXform, 0));
    }

    #endregion
}

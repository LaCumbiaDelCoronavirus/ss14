using System.Diagnostics.CodeAnalysis;
using Content.Shared.Charges.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.RCD.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.RCD.Systems;

public abstract partial class SharedRCDSystem
{
    /// <summary>
    /// If the RCD can work, returns true on server and false on client.
    ///     Otherwise, if the RCD can not work, returns false and shows
    ///     a clientside popup displaying why no work can be done.
    /// </summary>
    // This will always eventually just return false on client.
    public virtual bool TryRcdAfterInteract(Entity<RCDComponent> rcd, RCDPrototype selectedRcdPrototype, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return false;

        return CheckRcdCanWorkAndPopup(rcd, args.User, selectedRcdPrototype.Cost);
    }

    private void OnAfterInteract(Entity<RCDComponent> rcd, ref AfterInteractEvent args)
    {
        var (rcdUid, rcdComponent) = rcd;

        if (!_prototypeManager.TryIndex(rcdComponent.SelectedProtoId, out var selectedRcdPrototype))
            return;

        var user = args.User;
        var location = args.ClickLocation;

        if (!location.IsValid(EntityManager))
            return;

        if (_transformSystem.GetGrid(location) is not { } gridUid)
            return;

        // If we can't do any work, show a popup. Then if we're on client, just don't go past this point -- we can't predict entities.
        if (!TryRcdAfterInteract(rcd, selectedRcdPrototype, ref args))
            return;

        //var operation = new ObjectRcdConstructionOperation(location, selectedRcdPrototype.Cost, selectedRcdPrototype.Delay,
        //    selectedRcdPrototype.Effect, Direction.South, selectedRcdPrototype.ConstructedPrototype);
        // ObjectRcdConstructionOperation(EntityCoordinates Position, int Cost, float Delay, EntProtoId? Effect,
        // Direction Direction, EntProtoId ConstructedProtoId)

        ////FFFFUCCKK
        if (selectedRcdPrototype.OperationForm == RcdOperationForm.Tile)
        {
            if (!TryComp<MapGridComponent>(gridUid, out var mapGridComponent))
            {
                _popupSystem.PopupClient(Loc.GetString("rcd-component-no-valid-grid"), rcdUid, user);
                return;
            }

            if (!TryGetTileOperation((gridUid, mapGridComponent), location, rcdComponent, selectedRcdPrototype.Mode, out var operation))
                return;


        }
        else
        {
            return;
        }
    }


    private bool TryGetTileOperation(Entity<MapGridComponent> mapGrid, EntityCoordinates location, RCDComponent rcdComponent, RcdMode mode, [NotNullWhen(true)] out IRcdOperation? operation)
    {


        // `GetTileRef(Entity<MapGridComponent>, ...)` should just have a nullable comp that gets resolved. But whatever.
        var tileRefAtLocation = _mapSystem.GetTileRef(mapGrid, location);
        var tilePosition = _mapSystem.TileIndicesFor(mapGrid, location);

        operation = null;
        if (mode == RcdMode.Deconstruct)
        {
            var tileOperationProtoId = tileRefAtLocation.IsSpace() ?
                rcdComponent.SpaceTileDeconstructionProtoId : rcdComponent.NonSpaceTileDeconstructionProtoId;

            if (!_prototypeManager.TryIndex(tileOperationProtoId, out var tileDeconstructionPrototype))
                return false;

            operation = new TileRcdOperation(location, tileDeconstructionPrototype.Cost, tileDeconstructionPrototype.Delay,
                tileDeconstructionPrototype.Effect, tilePosition);

            return true;
        }
        else // assume they're making a tile
        {
            if (tileRefAtLocation.Tile.IsEmpty)
            {

            }
        }

        return false;
    }


    /// <summary>
    /// Checks whether an object (non-tile) operation can be done purely based on the state of the user and operation-related data.
    ///     Optionally, can show popups for why an operation can not be done.
    /// </summary>
    private bool CheckObjectOperationValidity(EntityUid rcdUid, RCDPrototype rcdPrototype, EntityUid user, EntityCoordinates targetPosition, bool popup = false)
    {
        if (!_interactionSystem.InRangeUnobstructed(user, targetPosition, popup: popup))
        {
            if (popup)
                _popupSystem.PopupClient(Loc.GetString(""), rcdUid, user);

            return false;
        }

        return true;
    }

    /// <summary>Spawns an RCD construction/deconstruction effect, and starts the doafter for the operation.</summary>
    private void StartOperation(IRcdOperation operation)
    {

    }


    /// <summary>
    /// If the RCD doesn't have a <see cref="LimitedChargesComponent"/>, returns true.
    ///     Otherwise, returns true if the RCD has as much as or more than the number
    ///     of charges specified in <paramref name="minimumCharges"/>. If it doesn't,
    ///     returns false with a clientside popup displaying why.
    /// </summary>
    /// <returns>True if the RCD can work.</returns>
    private bool CheckRcdCanWorkAndPopup(Entity<RCDComponent, LimitedChargesComponent?> rcd, EntityUid? user, float minimumCharges = 0)
    {
        var (uid, _, chargesComponent) = rcd;

        if (chargesComponent == null && !TryComp(uid, out chargesComponent))
            return true;

        var charges = _chargesSystem.GetCurrentCharges((uid, chargesComponent, null));
        if (charges < minimumCharges)
        {
            _popupSystem.PopupClient(Loc.GetString("rcd-component-insufficient-ammo-message"), uid, user);
            return false;
        }
        else if (charges == 0)
        {
            _popupSystem.PopupClient(Loc.GetString("rcd-component-no-ammo-message"), uid, user);
            return false;
        }

        return true;
    }
}

using Content.Shared.Aircraft.Components;
using Content.Shared.Aircraft.Events;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared.Aircraft.Systems;

public abstract partial class SharedAircraftSystem : EntitySystem
{
    public void InitializePiloting()
    {
        SubscribeLocalEvent<AircraftCabinComponent, ComponentStartup>(OnCabinStartup);

        // Inserting pilot
        SubscribeLocalEvent<AircraftCabinComponent, DragDropTargetEvent>(OnCabinDragDrop);
        SubscribeLocalEvent<AircraftCabinComponent, CanDropTargetEvent>(OnCabinCanDragDrop);

        SubscribeLocalEvent<AircraftCabinComponent, AircraftEntryDoAfterEvent>(OnAircraftEntryDoAfterFinish);

        // Ejecting pilot
        SubscribeLocalEvent<AircraftCabinComponent, GetVerbsEvent<AlternativeVerb>>(OnAircraftAltInteract);
        SubscribeLocalEvent<AircraftCabinComponent, AircraftExitDoAfterEvent>(OnAircraftExitDoAfterFinish);
    }

    public bool IsUserPilotOfAircraft(EntityUid entity, AircraftCabinComponent cabinComponent)
        => entity == cabinComponent.PilotSlot?.ContainedEntity;

    public bool IsAircraftPiloted(AircraftCabinComponent cabinComponent)
        => cabinComponent.PilotSlot?.ContainedEntity != null;

    private void OnCabinStartup(Entity<AircraftCabinComponent> aircraft, ref ComponentStartup args)
    {
        aircraft.Comp.PilotSlot = _containerSystem.EnsureContainer<ContainerSlot>(aircraft.Owner, aircraft.Comp.PilotSlotId);
    }

    private void SetupUser(EntityUid aircraftUid, EntityUid pilotUid)
    {

    }

    private void UnsetupUser(EntityUid aircraftUid, EntityUid pilotUid)
    {

    }

    /// <summary>
    /// Inserts a user (pilot) into an aircraft, without validation.
    /// </summary>
    /// <returns>Whether the user was successfully inserted.</returns>
    public bool InsertUser(Entity<AircraftCabinComponent> aircraft, EntityUid userUid)
    {
        var (aircraftUid, aircraftCabinComponent) = aircraft;

        SetupUser(aircraftUid, userUid);
        return _containerSystem.Insert(userUid, aircraftCabinComponent.PilotSlot);
    }

    /// <summary>
    /// Tries to insert a user (pilot) into an aircraft, after validating whether one can do so.
    /// </summary>
    /// <returns>Whether the user was successfully inserted.</returns>
    public bool TryInsertUser(Entity<AircraftCabinComponent> aircraft, EntityUid userUid)
    {
        if (IsAircraftPiloted(aircraft.Comp))
            return false;

        return InsertUser(aircraft, userUid);
    }

    /// <summary>
    /// Removes a user (pilot) from an aircraft, without validation.
    /// </summary>
    /// <returns>Whether the user was successfully ejected.</returns>
    public bool RemoveUser(Entity<AircraftCabinComponent> aircraft, EntityUid userUid)
    {
        var (aircraftUid, aircraftCabinComponent) = aircraft;

        UnsetupUser(aircraftUid, userUid);
        return _containerSystem.Remove(userUid, aircraftCabinComponent.PilotSlot);
    }

    /// <summary>
    /// Removes a user (pilot) from an aircraft, after validating whether one can do so.
    /// </summary>
    /// <returns>Whether the user was successfully ejected.</returns>
    public bool TryRemoveUser(Entity<AircraftCabinComponent> aircraft, EntityUid userUid)
    {
        if (!IsAircraftPiloted(aircraft.Comp))
            return false;

        return RemoveUser(aircraft, userUid);
    }


    private void OnAircraftExitDoAfterFinish(Entity<AircraftCabinComponent> aircraft, ref AircraftExitDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;
        TryRemoveUser(aircraft, args.Args.User);

        args.Handled = true;

        var selfEnterMessage = Loc.GetString("aircraft-exit-finish-self", ("ent", aircraft.Owner));
        _popupSystem.PopupClient(selfEnterMessage, user, user, PopupType.Small);

        var othersEnterMessage = Loc.GetString("aircraft-exit-finish-others", ("user", Identity.Entity(user, EntityManager)), ("ent", aircraft.Owner));
        _popupSystem.PopupPredicted(othersEnterMessage, user, null, PopupType.Small);
    }

    private void OnAircraftAltInteract(Entity<AircraftCabinComponent> aircraft, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanComplexInteract || !args.CanAccess)
            return;

        var user = args.User;
        var (aircraftUid, aircraftCabinComponent) = aircraft;

        // Only the pilot can do anything. That includes getting out.
        if (!IsUserPilotOfAircraft(user, aircraftCabinComponent))
            return;

        var exitVerb = new AlternativeVerb()
        {
            Text = Loc.GetString("aircraft-verb-exit"),
            Act = () =>
            {
                var doAfterEventArgs = new DoAfterArgs(EntityManager,
                    user,
                    aircraftCabinComponent.ExitDuration,
                    new AircraftExitDoAfterEvent(),
                    aircraftUid,
                    target: aircraftUid)
                {
                    BreakOnHandChange = true,
                };

                _popupSystem.PopupClient(Loc.GetString("aircraft-exit-start", ("ent", aircraft.Owner)), user, user, PopupType.Small);
                _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
            }
        };

        args.Verbs.Add(exitVerb);
    }

    private void OnAircraftEntryDoAfterFinish(Entity<AircraftCabinComponent> aircraft, ref AircraftEntryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Used is not { } entityEntering)
            return;

        TryInsertUser(aircraft, entityEntering);

        args.Handled = true;

        var selfEnterMessage = Loc.GetString("aircraft-enter-success-self", ("ent", aircraft.Owner));
        _popupSystem.PopupClient(selfEnterMessage, entityEntering, entityEntering, PopupType.Small);

        var othersEnterMessage = Loc.GetString("aircraft-enter-success-others", ("user", Identity.Entity(entityEntering, EntityManager)), ("ent", aircraft.Owner));
        _popupSystem.PopupPredicted(othersEnterMessage, entityEntering, null, PopupType.SmallCaution);
        Log.Debug("Entering!");
    }

    private void OnCabinDragDrop(Entity<AircraftCabinComponent> aircraft, ref DragDropTargetEvent ev)
    {
        if (ev.Handled)
            return;

        var (aircraftUid, aircraftCabinComponent) = aircraft;
        var user = ev.User;
        var dragged = ev.Dragged;

        if (IsAircraftPiloted(aircraftCabinComponent))
        {
            _popupSystem.PopupClient(Loc.GetString("aircraft-enter-already-occupied"), ev.User, PopupType.SmallCaution);
            return;
        }

        // If the drag-dropper is making something other than themself enter the aircraft,
        if (user != dragged)
        {
            var victimArg = ("victim", Identity.Entity(dragged, EntityManager));
            var selfForceMessage = Loc.GetString("aircraft-force-enter-self", victimArg, ("ent", aircraft.Owner));
            _popupSystem.PopupClient(selfForceMessage, user, user, PopupType.SmallCaution);

            var othersForceMessage = Loc.GetString("aircraft-force-enter-others", ("suspect", Identity.Entity(user, EntityManager)), victimArg, ("ent", aircraft.Owner));
            _popupSystem.PopupPredicted(othersForceMessage, user, null, PopupType.MediumCaution);
        }

        ev.Handled = true;
        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            user,
            aircraftCabinComponent.EntryDuration,
            new AircraftEntryDoAfterEvent(),
            aircraftUid,
            target: aircraftUid,
            used: dragged)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnCabinCanDragDrop(Entity<AircraftCabinComponent> aircraft, ref CanDropTargetEvent ev)
    {
        ev.Handled = true;
        ev.CanDrop = true;
    }
}

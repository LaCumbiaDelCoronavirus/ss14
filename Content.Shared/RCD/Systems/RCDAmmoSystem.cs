using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.RCD.Components;
using Robust.Shared.Timing;

namespace Content.Shared.RCD.Systems;

public sealed class RCDAmmoSystem : EntitySystem
{
    [Dependency] private readonly SharedChargesSystem _sharedChargesSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RCDAmmoComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RCDAmmoComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnExamine(Entity<RCDAmmoComponent> rcdAmmo, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString("rcd-ammo-component-on-examine", ("charges", rcdAmmo.Comp.Charges)));
    }

    private void OnAfterInteract(Entity<RCDAmmoComponent> rcdAmmo, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !_timing.IsFirstTimePredicted)
            return;

        if (args.Target is not { Valid: true } rcdUid ||
            !HasComp<RCDComponent>(rcdUid) ||
            !TryComp<LimitedChargesComponent>(rcdUid, out var rcdChargesComponent))
            return;

        var (ammoUid, rcdAmmoComponent) = rcdAmmo;

        var currentRCDCharges = _sharedChargesSystem.GetCurrentCharges((rcdUid, rcdChargesComponent));
        var user = args.User;

        args.Handled = true;

        var addedCharges = Math.Min(rcdChargesComponent.MaxCharges - currentRCDCharges, rcdAmmoComponent.Charges);

        if (addedCharges <= 0)
        {
            _popupSystem.PopupClient(Loc.GetString("rcd-ammo-component-after-interact-full"), rcdUid, user);
            return;
        }

        _popupSystem.PopupClient(Loc.GetString("rcd-ammo-component-after-interact-refilled"), rcdUid, user);
        _sharedChargesSystem.AddCharges(rcdUid, addedCharges);

        rcdAmmoComponent.Charges -= addedCharges;

        // Prevent having useless ammo with 0 charges. If it still has any use, dirty it.
        if (rcdAmmoComponent.Charges <= 0)
            QueueDel(ammoUid);
        else
            Dirty(ammoUid, rcdAmmoComponent);
    }
}

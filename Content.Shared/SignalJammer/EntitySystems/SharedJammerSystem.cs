using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Content.Shared.Radio.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.SignalJammer.Components;
using Content.Shared.Silicons.StationAi;

namespace Content.Shared.SignalJammer.EntitySystems;

public abstract class SharedJammerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDeviceNetworkJammerSystem _jammer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    // this LocId is set as such incase someone wants it to be different
    static LocId _aiActionJammedMessage = "ai-device-not-responding";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignalJammerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<SignalJammerComponent, ExaminedEvent>(OnExamine);

        // AI events
        // literally anything
        SubscribeLocalEvent<StationAiWhitelistComponent, StationAiActionAttemptEvent>(OnAIActionAttempt);
    }

    private void OnGetVerb(Entity<SignalJammerComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        byte index = 0;
        foreach (var setting in entity.Comp.Settings)
        {
            // This is because Act wont work with index.
            // Needs it to be saved in the loop.
            var currIndex = index;
            var verb = new Verb
            {
                Priority = currIndex,
                Category = VerbCategory.PowerLevel,
                Disabled = entity.Comp.SelectedPowerLevel == currIndex,
                Act = () =>
                {
                    entity.Comp.SelectedPowerLevel = currIndex;
                    Dirty(entity);

                    // If the jammer is off, this won't do anything which is fine.
                    // The range should be updated when it turns on again!
                    _jammer.TrySetRange(entity.Owner, GetCurrentRange(entity));

                    Popup.PopupClient(Loc.GetString(setting.Message), user, user);
                },
                Text = Loc.GetString(setting.Name),
            };
            args.Verbs.Add(verb);
            index++;
        }
    }

    private void OnExamine(Entity<SignalJammerComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            var powerIndicator = HasComp<ActiveSignalJammerComponent>(ent)
                ? Loc.GetString("signal-jammer-component-examine-on-state")
                : Loc.GetString("signal-jammer-component-examine-off-state");
            args.PushMarkup(powerIndicator);

            var powerLevel = Loc.GetString(ent.Comp.Settings[ent.Comp.SelectedPowerLevel].Name);
            var switchIndicator = Loc.GetString("signal-jammer-component-switch-setting", ("powerLevel", powerLevel));
            args.PushMarkup(switchIndicator);
        }
    }

    public float GetCurrentWattage(Entity<SignalJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Wattage;
    }

    public float GetCurrentRange(Entity<SignalJammerComponent> jammer)
    {
        return jammer.Comp.Settings[jammer.Comp.SelectedPowerLevel].Range;
    }

    protected void ChangeLEDState(Entity<AppearanceComponent?> ent, bool isLEDOn)
    {
        _appearance.SetData(ent, SignalJammerVisuals.LEDOn, isLEDOn, ent.Comp);
    }

    protected void ChangeChargeLevel(Entity<AppearanceComponent?> ent, SignalJammerChargeLevel chargeLevel)
    {
        _appearance.SetData(ent, SignalJammerVisuals.ChargeLevel, chargeLevel, ent.Comp);
    }

    protected bool ShouldCancelSend(EntityUid sourceUid)
    {
        var source = Transform(sourceUid).Coordinates;
        var query = EntityQueryEnumerator<ActiveSignalJammerComponent, SignalJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var jam, out var transform))
        {
            if (_transform.InRange(source, transform.Coordinates, GetCurrentRange((uid, jam))))
            {
                return true;
            }
        }

        return false;
    }

    private void OnAIActionAttempt(Entity<StationAiWhitelistComponent> entity, ref StationAiActionAttemptEvent args)
    {
        if (entity.Comp.BypassesJamming)
            return;

        if (ShouldCancelSend(entity.Owner))
        {
            args.Cancelled = true;
            args.CancellationText = _aiActionJammedMessage;
        }
    }
}

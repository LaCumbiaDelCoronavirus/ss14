using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Interaction;
using Content.Shared.PowerCell.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Radio.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.SignalJammer.Components;
using Content.Shared.SignalJammer.EntitySystems;
using Content.Shared.Silicons.StationAi;
using System.Reflection.Metadata;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : SharedJammerSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDeviceNetworkJammerSystem _jammer = default!;

    static LocId _aiActionJammedMessage = "";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignalJammerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActiveSignalJammerComponent, PowerCellChangedEvent>(OnPowerCellChanged);

        // Radio events
        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);

        // AI events
        // literally anything
        SubscribeLocalEvent<Component, StationAiActionAttemptEvent>(OnAIActionAttempt);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveSignalJammerComponent, SignalJammerComponent>();

        while (query.MoveNext(out var uid, out var _, out var jam))
        {

            if (_powerCell.TryGetBatteryFromSlot(uid, out var batteryUid, out var battery))
            {
                if (!_battery.TryUseCharge(batteryUid.Value, GetCurrentWattage((uid, jam)) * frameTime, battery))
                {
                    ChangeLEDState(uid, false);

                    RemComp<ActiveSignalJammerComponent>(uid);
                    RemComp<DeviceNetworkJammerComponent>(uid);
                    RemComp<DeviceNetworkJammerComponent>(uid);
                }
                else
                {
                    var percentCharged = battery.CurrentCharge / battery.MaxCharge;
                    var chargeLevel = percentCharged switch
                    {
                        > 0.50f => SignalJammerChargeLevel.High,
                        < 0.15f => SignalJammerChargeLevel.Low,
                        _ => SignalJammerChargeLevel.Medium,
                    };
                    ChangeChargeLevel(uid, chargeLevel);
                }

            }

        }
    }

    private void OnActivate(Entity<SignalJammerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        var activated = !HasComp<ActiveSignalJammerComponent>(ent) &&
            _powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) &&
            battery.CurrentCharge > GetCurrentWattage(ent);
        if (activated)
        {
            ChangeLEDState(ent.Owner, true);
            EnsureComp<ActiveSignalJammerComponent>(ent);
            EnsureComp<DeviceNetworkJammerComponent>(ent, out var jammingComp);
            _jammer.SetRange((ent, jammingComp), GetCurrentRange(ent));
            _jammer.AddJammableNetwork((ent, jammingComp), DeviceNetworkComponent.DeviceNetIdDefaults.Wireless.ToString());
        }
        else
        {
            ChangeLEDState(ent.Owner, false);
            RemCompDeferred<ActiveSignalJammerComponent>(ent);
            RemCompDeferred<DeviceNetworkJammerComponent>(ent);
        }
        var state = Loc.GetString(activated ? "signal-jammer-component-on-state" : "signal-jammer-component-off-state");
        var message = Loc.GetString("signal-jammer-component-on-use", ("state", state));
        Popup.PopupEntity(message, args.User, args.User);
        args.Handled = true;
    }

    private void OnPowerCellChanged(Entity<ActiveSignalJammerComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected)
        {
            ChangeLEDState(ent.Owner, false);
            RemCompDeferred<ActiveSignalJammerComponent>(ent);
        }
    }

    private void OnAIActionAttempt(Entity<Component> ent, ref StationAiActionAttemptEvent args)
    {
        if (ShouldCancelSend(ent.Owner))
        {
            args.Cancelled = true;
            args.CancellationText = _aiActionJammedMessage;
        }
    }
    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (ShouldCancelSend(args.RadioSource))
        {
            args.Cancelled = true;
        }
    }

    private bool ShouldCancelSend(EntityUid sourceUid)
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
}

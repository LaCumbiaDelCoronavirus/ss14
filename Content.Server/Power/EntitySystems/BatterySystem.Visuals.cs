using Content.Server.Power.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Visualizers;
using Content.Shared.Rounding;

namespace Content.Server.Power.EntitySystems;

public sealed partial class BatterySystem : SharedBatterySystem
{
    private EntityQuery<PowerNetworkBatteryComponent> _netBatteryQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;

    public void InitializeVisuals()
    {
        _netBatteryQuery = GetEntityQuery<PowerNetworkBatteryComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();

        SubscribeLocalEvent<BatteryVisualizerComponent, MapInitEvent>(OnNetBatteryMapInit);
        SubscribeLocalEvent<BatteryVisualizerComponent, ChargeChangedEvent>(OnNetBatteryChargeChanged);
    }

    private void UpdateNetBatteryVisuals(in Entity<BatteryVisualizerComponent> visualizerEntity, PowerNetworkBatteryComponent netBatteryComponent, AppearanceComponent appearanceComponent)
    {
        _appearanceSystem.SetData(
            visualizerEntity,
            BatteryVisuals.ChargeState,
            CalculateNetBatteryComponentChargeState(netBatteryComponent),
            appearanceComponent
        );

        var battery = netBatteryComponent.NetworkBattery;
        var currentChargeLevel = ContentHelpers.RoundToLevels(battery.CurrentStorage, battery.Capacity, visualizerEntity.Comp.ChargeLevelCount);

        ref var lastChargeLevel = ref visualizerEntity.Comp.LastChargeLevel;
        if (currentChargeLevel != lastChargeLevel)
        {
            lastChargeLevel = currentChargeLevel;
            _appearanceSystem.SetData(
                visualizerEntity,
                BatteryVisuals.ChargeLevel,
                currentChargeLevel,
                appearanceComponent
            );
        }
    }

    private void OnNetBatteryMapInit(Entity<BatteryVisualizerComponent> visualizerEntity, ref MapInitEvent args)
    {
        if (!_netBatteryQuery.TryGetComponent(visualizerEntity, out var netBatteryComponent) /* don't waste time if we arent going to visualize anything */ ||
            !_appearanceQuery.TryGetComponent(visualizerEntity, out var appearanceComponent))
            return;

        UpdateNetBatteryVisuals(visualizerEntity, netBatteryComponent, appearanceComponent);
    }

    private void OnNetBatteryChargeChanged(Entity<BatteryVisualizerComponent> visualizerEntity, ref ChargeChangedEvent args)
    {
        if (!_netBatteryQuery.TryGetComponent(visualizerEntity, out var netBatteryComponent) /* don't waste time if we arent going to visualize anything */ ||
            !_appearanceQuery.TryGetComponent(visualizerEntity, out var appearanceComponent))
            return;

        UpdateNetBatteryVisuals(visualizerEntity, netBatteryComponent, appearanceComponent);
    }
}

using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Audio;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

[UsedImplicitly]
public sealed class GasPressurePumpSystem : SharedGasPressurePumpSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPressurePumpComponent, ResolveGasMoverEvent>(OnPumpMoverResolve);
        SubscribeLocalEvent<GasPressurePumpComponent, ProcessGasMoverEvent>(OnPumpUpdated);
        SubscribeLocalEvent<GasPressurePumpComponent, GetDependantPipeNetsEvent>(OnPumpGetDependantNets);
    }

    private bool CanPumpWork(Entity<GasPressurePumpComponent> ent)
        => ent.Comp.Enabled && _power.IsPowered(ent);

    /// <summary>Gets the amount of gas that the pump would move into it's outlet, without actually moving it. Returns true if any gas is actually transferred.</summary>
    private bool TryComputeMovedGas(Entity<GasPressurePumpComponent> ent, PipeNode inlet, PipeNode outlet, out GasMixture movedGas)
    {
        var inletAir = inlet.Air;
        var outletAir = outlet.Air;

        var outputStartingPressure = outletAir.Pressure;
        if (outputStartingPressure >= ent.Comp.TargetPressure
            || inletAir.TotalMoles <= 0
            || inletAir.Temperature <= 0)
        {
            movedGas = new GasMixture(outletAir.Volume) { Temperature = outletAir.Temperature };
            return false;
        }

        var pressureDelta = ent.Comp.TargetPressure - outputStartingPressure;
        var transferMoles = (pressureDelta * outletAir.Volume) / (inlet.Air.Temperature * Atmospherics.R);

        movedGas = inlet.Air.GetRatio(transferMoles / inlet.Air.TotalMoles);
        return true;
    }

    private void OnPumpMoverResolve(Entity<GasPressurePumpComponent> ent, ref ResolveGasMoverEvent args)
    {
        if (!CanPumpWork(ent) || !_nodeContainer.TryGetNodes(ent.Owner, ent.Comp.InletName, ent.Comp.OutletName, out PipeNode? _, out PipeNode? _))
            return;

        args.OutputThreshold = ent.Comp.TargetPressure;
        args.Handled = true;
    }

    private void OnPumpUpdated(Entity<GasPressurePumpComponent> ent, ref ProcessGasMoverEvent args)
    {
        if (!CanPumpWork(ent) || !_nodeContainer.TryGetNodes(ent.Owner, ent.Comp.InletName, ent.Comp.OutletName, out PipeNode? inlet, out PipeNode? outlet))
            return;

        if (!TryComputeMovedGas(ent, inlet, outlet, out var removed))
        {
            _ambientSoundSystem.SetAmbience(ent, false);
            return; // No need to pump gas if target has been reached.
        }

        inlet.Air.Remove(removed);
        _atmosphereSystem.Merge(outlet.Air, removed);
        _ambientSoundSystem.SetAmbience(ent, removed.TotalMoles > 0f);
    }

    private void OnPumpGetDependantNets(Entity<GasPressurePumpComponent> ent, ref GetDependantPipeNetsEvent args)
    {
        // Typecasting it is sus but there's not really a better way.
        if (!_nodeContainer.TryGetNode(ent.Owner, ent.Comp.InletName, out PipeNode? outlet)
            || outlet.NodeGroup is not IPipeNet outletNet)
            return;

        args.DependantPipeNets.Add(outletNet);
    }
}

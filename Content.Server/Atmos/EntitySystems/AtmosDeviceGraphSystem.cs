using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Components;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;


//public sealed class AtmosDeviceGraphSystem : EntitySystem
public partial class AtmosphereSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;

    private HashSet<IPipeNet> _resolvedPipeNets = new();
    private HashSet<IPipeNet> _pipeNetsQueuedForResolution = new();

    // Gas-mover device resolution:
    // 1. Get the desired gas outflow of the device
    // 2. If we have no input pipenets, count it as resolved
    // 3. Resolve all input pipenets that aren't queued for resolution
    // 4. If any input pipenet is queued in resolution, mark us as looping, and recursively A-mark pipenets that depend on us
    // 5. turn A-marked pipenets, which output into looping-marked pipenets, into looping-marked pipenets
    // 6. for all looping-marked pipenets, resolve their gases as pre-device-update gases (as opposed to post-update)
    // 7. compute gas outflow with input pipenets' gases as parameters
    // Returns whether any the output pipenet was queued in resolution; if we weren't resolved
    private bool GraphResolveDevice(GasPressurePumpComponent pumpComponent, PipeNode inlet, IPipeNet outputPipeNet)
    {
        if (inlet.ReachableNodes.Count != 0)
            return false;

        // If the input isn't a pipenet, mark it as resolved.
        // This is the pipenet that the outputPipenet depends on.
        if (inlet.NodeGroup is not IPipeNet inputPipeNet)
            return false;

        var deviceHasDependencyQueuedForResolution = !_pipeNetsQueuedForResolution.Contains(inputPipeNet) || GraphProcessPipeNet(outputPipeNet);

        Log.Debug("Device successfully pumping");
        _atmosphereSystem.PumpGasTo(inlet.Air, outputPipeNet.Air, pumpComponent.TargetPressure);

        inputPipeNet.IsLooping |= deviceHasDependencyQueuedForResolution;
        return deviceHasDependencyQueuedForResolution;
    }

    /// <summary>Adds a pipenet to be later resolved by <see cref="GraphResolvePipenets"/>.</summary>
    /// <returns>Whether the pipenet was not already queued for resolution.</returns>
    public bool GraphQueuePipenet(IPipeNet pipeNet)
        => _pipeNetsQueuedForResolution.Add(pipeNet);

    /// <summary>Processes a pipenet, maybe resolving it. Will process any unresolved dependency.</summary>
    private bool GraphProcessPipeNet(IPipeNet pipeNet)
    {
        // Has any processed pipenet already been found to be resolved?
        var foundAlreadyQueuedInResolution = false;
        GraphQueuePipenet(pipeNet);

        foreach (var node in pipeNet.Nodes)
        {
            var nodeOwner = node.Owner;

            // Temporary code; TODO: replace this later with generalised comp
            if (!TryComp<GasPressurePumpComponent>(nodeOwner, out var pumpComponent)
                || !_nodeContainerSystem.TryGetNodes(nodeOwner, pumpComponent.InletName, pumpComponent.OutletName, out PipeNode? inlet, out PipeNode? outlet))
                continue;

            // If the mover's outlet isn't an input of our pipenet, ignore it.
            if (outlet.NodeGroup != pipeNet)
            {
                Log.Debug("Continuing due to input status");
                continue;
            }

            foundAlreadyQueuedInResolution |= GraphResolveDevice(pumpComponent, inlet, pipeNet);
        }
        _pipeNetsQueuedForResolution.Remove(pipeNet);
        Log.Debug("Resolved Pipenet");

        return foundAlreadyQueuedInResolution;
    }

    public bool GraphProcessPipeNets(HashSet<IPipeNet> pipeNets)
    {
        var iteration = 0;
        foreach (var pipeNet in pipeNets)
        {
            GraphProcessPipeNet(pipeNet);
            _pipeNetsQueuedForResolution.Remove(pipeNet);

            if (iteration++ < LagCheckIterations)
                continue;

            iteration = 0;

            // Process the rest next time.
            if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                return false;
        }

        _resolvedPipeNets.Clear();
        _pipeNetsQueuedForResolution.Clear();


        return true;
    }
}

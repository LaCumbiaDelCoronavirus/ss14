using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Atmos.EntitySystems;

// This goes through the process of updating devices by going through pipenets and forming a graph of pipenets between GasMovers.
// Pipenets may depend on other pipenets. They will start from pipenets with no input GasMovers, therefore the pipenets that depend on the lowest number of pipenets will update the earliest.
// Pipenets that are in loops, or pipenets that depend on pipenets which are in loops, will be handled separately.

// First, all given pipenets for a single processing will be looped through.
// (D) means the action gets logged.
// For each pipenet,
// - Add the pipenet to the _pipeNetsQueuedInResolution set..
// - If the pipenet was already in there, then,
// -    - L-mark the pipenet, and also add it to HLD-queue (D)

// - Now regardless of whether the pipenet was already QIR...
// - Go through each node,
// -    - Ignore unless the entity has a GasMoverComponent
// -    - Ignore unless the entity is an input of the given pipenet;
//           aka, ignore if the gasmover does not move gas in to our pipenet
// -    - Get the output threshold of the entity

// - Sort every device by their output threshold, in ascending order
//      Therefore, all devices will probably have a chance to output before hitting their output threshold (pressure limit).
// - Process each device by that order

// Now, we have finished processing every pipenet.
// If the HLD-queue isn't empty, then until it isn't empty:
// - Pop the pipenet at front of the HLD-queue (D)
// - Go through each pipenet that depends on our pipenet, (aka, only if the pipenet has a device whose input pipenets includes our pipenet, this isn't directly recursive)
// -   - And add it to the HLD-queue (ofcourse, HLD-marking it too), therefore making this recursive (D)
// -   - However, if this pipenet was already HLD-marked, skip recursively going through it. (D, including the opposite)
//          This is because we know that every pipenet that is HLD-marked was recursively searched forwards.

// Finally, we print how long the processing took in milliseconds. Right now with the broken algo it's less than 0.2ms for the Dev map which has ~60 pipenets.
public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;

    private readonly HashSet<IPipeNet> _pipeNetsQueuedInResolution = new();

    // The two next hashsets exist because keeping a var for this on the pipenet itself is unwieldy.
    private readonly HashSet<IPipeNet> _loopingPipeNets = new();

    private readonly HashSet<IPipeNet> _pipeNetsWithLoopingDependency = new();
    private readonly Queue<IPipeNet> _loopingPipeNetDependencyQueue = new();

    /// <summary>Gets a list of pipenets that use the provided pipenet as an input (aka depend on our pipenet), but not *directly* recursing past that.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HashSet<IPipeNet> GetDirectlyDependantPipeNets(IPipeNet pipeNet)
    {
        HashSet<IPipeNet> dependants = new();
        foreach (var node in pipeNet.Nodes)
        {
            // If this entity isn't a gasmover, ignore it.
            var nodeOwner = node.Owner;
            if (!_gasMoverQuery.HasComponent(nodeOwner))
                continue;

            var pipeNetRelianceEvent = new GetDependantPipeNetsEvent(new() { pipeNet });
            RaiseLocalEvent(nodeOwner, ref pipeNetRelianceEvent);

            if (pipeNetRelianceEvent.DependantPipeNets.Count > 0)
            {
                Log.Debug($"Recursing from one pipenet to another, passthroughed device: {ToPrettyString(nodeOwner)}");

                // This will be looped back through in GraphProcessPipenets with the gradual dequeue of _pipeNetsWithLoopingDependency; therefore, it will recurse.
                dependants.UnionWith(pipeNetRelianceEvent.DependantPipeNets);
            }
        }

        return dependants;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueuePipeNetInResolution(IPipeNet pipeNet)
    {
        // X: Queued ourselves as in-resolution
        if (!_pipeNetsQueuedInResolution.Add(pipeNet)
            && !_loopingPipeNets.Contains(pipeNet))
        {
            _loopingPipeNets.Add(pipeNet);
            _loopingPipeNetDependencyQueue.Enqueue(pipeNet);
            Log.Debug("Marked pipenet as looping (L-marked)");
        }
    }

    public void GraphProcessPipeNets(HashSet<IPipeNet> pipeNets)
    {
        _simulationStopwatch.Restart();
        var processed = 0;

        foreach (var pipeNet in pipeNets)
        {

            QueuePipeNetInResolution(pipeNet);
            GraphProcessPipeNet(pipeNet);

            ++processed;
        }

        while (_loopingPipeNetDependencyQueue.Count > 0)
        {
            if (!_loopingPipeNetDependencyQueue.TryDequeue(out var front))
                break;

            foreach (var dependant in GetDirectlyDependantPipeNets(front))
            {
                if (!_pipeNetsWithLoopingDependency.Add(dependant))
                {
                    Log.Debug("Skipped depending pipenet, as it was already A-marked");
                    continue;
                }

                Log.Debug("Recursed pipenet that had looping dependancy (A-marked)");
                _loopingPipeNetDependencyQueue.Enqueue(dependant);
            }
        }

        _pipeNetsQueuedInResolution.Clear();
        _loopingPipeNets.Clear();
        _pipeNetsWithLoopingDependency.Clear();

        if (processed > 0)
            Log.Debug($"Took {_simulationStopwatch.Elapsed.TotalMilliseconds}ms to process {processed} pipenets.");
    }

    private bool GraphProcessPipeNet(IPipeNet pipeNet)
    {
        // Dictionary of a device that's an input of this pipenet and it's output threshold.
        Dictionary<Entity<GasMoverComponent>, float> inputMovers = new();

        // X: Resolved all input devices
        foreach (var node in pipeNet.Nodes)
        {
            // If this entity isn't a gasmover, ignore it.
            var nodeOwner = node.Owner;
            if (!_gasMoverQuery.TryGetComponent(nodeOwner, out var gasMoverComponent))
                continue;

            // Make sure the outlet node of this entity outputs into this pipenet; the entity is an input of this pipenet.
            // Yes, `outletNode` is `node`. It's clearer to use the former.
            // AKA, continue if this entity isn't an input of our pipenet.
            if (!_nodeContainerSystem.TryGetNode(nodeOwner, gasMoverComponent.OutletName, out PipeNode? outletNode)
                || node != outletNode)
                continue;

            // If the device (supposedly) didn't do anything, do nothing.
            Entity<GasMoverComponent> gasMover = (nodeOwner, gasMoverComponent);
            if (!GraphResolveDevice(gasMover, outletNode, pipeNet, out var resolveGasMoverEvent))
                continue;

            inputMovers[gasMover] = resolveGasMoverEvent.OutputThreshold;
        }

        // If there's no valid movers that are an input of our pipenet, then end early and mark us as resolved.
        if (inputMovers.Count == 0)
            return true;

        // Sort by the output threshold, in ascending order, so that each device will probably get a chance to pump gas.
        foreach (KeyValuePair<Entity<GasMoverComponent>, float> element in inputMovers.OrderBy(el => el.Value))
        {
            var moveGasEvent = new ProcessGasMoverEvent();
            RaiseLocalEvent(element.Key, ref moveGasEvent);
        }

        return false;
    }

    private bool GraphResolveDevice(Entity<GasMoverComponent> gasMover, PipeNode outlet, IPipeNet outputPipeNet, out ResolveGasMoverEvent resolveGasMoverEvent)
    {
        resolveGasMoverEvent = new ResolveGasMoverEvent();
        RaiseLocalEvent(gasMover, ref resolveGasMoverEvent);

        return resolveGasMoverEvent.Handled;
    }
}

using System.Threading;
using System.Threading.Tasks;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

public sealed partial class EquipOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private SharedHandsSystem _handsSystem = default!;

    [DataField("target", required: true)]
    public string Target = "Target";


    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _handsSystem = sysManager.GetEntitySystem<SharedHandsSystem>();
    }
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Target, out var target, _entityManager) ||
            !_entityManager.TryGetComponent<HandsComponent>(owner, out var handsComponent) ||
            !blackboard.TryGetValue<Hand?>(NPCBlackboard.ActiveHand, out var activeHand, _entityManager)/* || handsComponent.ActiveHand == null */)
            return (false, null);

        if (activeHand == null)
            return (false, null);

        // If CanPickupToHand failed but the item we need in our hand is already in our hand, call the plan valid anyway
        if (_handsSystem.CanPickupToHand(owner, target, activeHand, handsComp: handsComponent) ||
         (blackboard.TryGetValue<EntityUid>(NPCBlackboard.ActiveHandItem, out var activeHandItem, _entityManager) && activeHandItem == target))
            return (true, new Dictionary<string, object>()
            {
                {NPCBlackboard.ActiveHandFree, false},
                {NPCBlackboard.ActiveHandItem, target},
            });

        return (false, null);
    }
    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Target, out var target, _entityManager) || !_entityManager.TryGetComponent<HandsComponent>(owner, out var handsComponent))
        {
            return HTNOperatorStatus.Failed;
        }

        // if pickup will fail for whatever reason, but the item we want in our hand is already there, treat it as a success since our mission is technically complete :D
        if (_handsSystem.GetActiveItem(owner) == target)
            return HTNOperatorStatus.Finished;

        var activeHand = _handsSystem.GetActiveHand(owner);
        if (activeHand == null)
            return HTNOperatorStatus.Failed;

        // TODO: As elsewhere need some generic interaction cooldown system
        if (_handsSystem.TryPickup(owner, target, handsComp: handsComponent, handName: activeHand.Name))
            return HTNOperatorStatus.Finished;

        return HTNOperatorStatus.Failed;
    }
}

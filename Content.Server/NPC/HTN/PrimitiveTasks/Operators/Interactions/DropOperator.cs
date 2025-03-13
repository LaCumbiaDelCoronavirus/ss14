using System.Threading;
using System.Threading.Tasks;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.ActionBlocker;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

/// <summary>
/// Drops the active hand entity underneath us.
/// </summary>
public sealed partial class DropOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private SharedHandsSystem _handsSystem = default!;
    private ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _handsSystem = sysManager.GetEntitySystem<SharedHandsSystem>();
        _actionBlocker = sysManager.GetEntitySystem<ActionBlockerSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        // The point of this operator is to: make sure we don't have anything in our active hand when we finish it. So all these conditions would be fine.
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        // uhhhhh, should we really still count it as valid even if we dont have hands? whatever.
        if (!_entityManager.TryGetComponent<HandsComponent>(owner, out var handsComponent))
            return (true, null);

        if (!blackboard.TryGetValue<Hand?>(NPCBlackboard.ActiveHand, out var activeHand, _entityManager))
            return (true, null);

        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.ActiveHandItem, out var activeHandItem, _entityManager))
            return (true, null);

        if (activeHand == null)
            return (true, null);

        var effects = new Dictionary<string, object>()
        {
            {NPCBlackboard.ActiveHandFree, true},
            // VVV good enough i guess
            {NPCBlackboard.ActiveHandItem, ""},
        };

        if (activeHand.IsEmpty == true)
            return (true, effects);

        // TODO: Account for interaction cooldown here. I don't know if its actually necessary though.
        if (_actionBlocker.CanDrop(activeHandItem))
            return (true, effects);

        return (false, null);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (blackboard.TryGetValue<Hand?>(NPCBlackboard.ActiveHand, out var activeHand, _entityManager) && activeHand.IsEmpty == true)
            return HTNOperatorStatus.Finished;

        var owner = blackboard.GetValueOrDefault<EntityUid>(NPCBlackboard.Owner, _entityManager);
        // TODO: Need some sort of interaction cooldown probably.
        if (_handsSystem.TryDrop(owner))
        {
            return HTNOperatorStatus.Finished;
        }

        return HTNOperatorStatus.Failed;
    }
}

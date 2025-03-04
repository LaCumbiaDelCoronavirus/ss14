using System.Threading;
using System.Threading.Tasks;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;


/// <summary>
/// Swaps to any free hand.
/// </summary>
public sealed partial class SwapToFreeHandOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private HandsSystem _handsSystem = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _handsSystem = sysManager.GetEntitySystem<HandsSystem>();
    }
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<List<string>>(NPCBlackboard.FreeHands, out var freeHands, _entManager) ||
            !_entManager.TryGetComponent<HandsComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner), out var handsComponent))
        {
            return (false, null);
        }

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        foreach (var hand in freeHands)
        {
            // try to set active hand directly to one of our free hands. if we cant do that (reason e.x.: we are already using that hand as an active hand, which we can't then switch to), continue to next.
            // if none of that works we eventually just fail (see the last return in this function)
            if (!_handsSystem.TrySetActiveHand(owner, hand, handsComponent))
                continue;

            return (true, new Dictionary<string, object>()
            {
                {
                    NPCBlackboard.ActiveHand, handsComponent.Hands[hand]
                },
                {
                    NPCBlackboard.ActiveHandFree, true
                },
            });
        }

        return (false, null);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        // TODO: Need interaction cooldown
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<List<string>>(NPCBlackboard.FreeHands, out var freeHands, _entManager) ||
            !_entManager.TryGetComponent<HandsComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner), out var handsComponent))
        {
            return HTNOperatorStatus.Failed;
        }

        foreach (var hand in freeHands)
        {
            // yay. yippee.
            if (_handsSystem.TrySetActiveHand(owner, hand, handsComponent))
                return HTNOperatorStatus.Finished;
        }

        // FUCK!!!
        return HTNOperatorStatus.Failed;
    }
}

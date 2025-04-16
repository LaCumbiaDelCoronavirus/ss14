using System.Threading;
using System.Threading.Tasks;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Robust.Shared.Utility;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;


/// <summary>
/// Swaps to any free hand. Finishes if active hand was changed to another, fails otherwise.
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
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<List<string>>(NPCBlackboard.FreeHands, out var freeHands, _entManager) ||
            !_entManager.TryGetComponent<HandsComponent>(owner, out var handsComponent) || handsComponent == null)
        {
            return (false, null);
        }

        var activeHand = handsComponent.ActiveHand;

        // PLS NO
        if (activeHand == null)
            // FUCK
            return (false, null);

        foreach (var handKey in freeHands)
        {
            var hand = handsComponent.Hands[handKey];

            // if we cant do this (reason e.x.: we are already using that hand as an active hand, which we can't then switch to) then continue to next...
            // ... unless the hand is our active hand and also free
            if (hand == null)
                continue;

            var newFreeHandsList = freeHands.Clone();
            newFreeHandsList.Remove(handKey);
            newFreeHandsList.Add(activeHand.Name);

            return (true, new Dictionary<string, object>()
            {
                {NPCBlackboard.ActiveHand, hand},
                {NPCBlackboard.ActiveHandFree, true}, // ActiveHandEntity
                {NPCBlackboard.FreeHands, newFreeHandsList}, // bro
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

        blackboard.TryGetValue<Hand?>(NPCBlackboard.ActiveHand, out var activeHand, _entManager);

        foreach (var handKey in freeHands)
        {
            var hand = handsComponent.Hands[handKey];
            if (activeHand != null && hand == activeHand)
                return HTNOperatorStatus.Finished;
            // yay. yippee.
            if (_handsSystem.TrySetActiveHand(owner, handKey, handsComponent))
                return HTNOperatorStatus.Finished;
        }

        // FUCK!!!
        return HTNOperatorStatus.Failed;
    }
}

using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat.Ranged;

public sealed partial class GunEjectAmmoOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private SharedGunSystem _gunSystem = default!;
    private ItemSlotsSystem _itemSlotsSystem = default!;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    /// <summary>
    /// Key that contains the gun entity.
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gunSystem = sysManager.GetEntitySystem<SharedGunSystem>();
        _itemSlotsSystem = sysManager.GetEntitySystem<ItemSlotsSystem>();
    }
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        // Don't do anything if the gun isnt real
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var gun, _entityManager) || !_entityManager.TryGetComponent<GunComponent>(gun, out var gunComponent))
            return (false, null);

        // the 2 checks below are because we only support ejecting ammo from 1. guns with magazines, and 2. revolvers. Nothing else
        if ((_entityManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(gun, out var chamberMagazineAmmoProviderComponent) || _entityManager.TryGetComponent<MagazineAmmoProviderComponent>(gun, out var magazineAmmoProviderComponent))
         && _entityManager.TryGetComponent<ItemSlotsComponent>(gun, out var itemSlotsComponent))
        {
            return (true, null);
        }

        if (_entityManager.TryGetComponent<RevolverAmmoProviderComponent>(gun, out var revolverAmmoProviderComponent))
        {
            return (true, null);
        }

        return (false, null);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var gun, _entityManager) || !_entityManager.TryGetComponent<GunComponent>(gun, out var gunComponent))
            return HTNOperatorStatus.Failed;

        // NOTE: This probably won't work with stuff that has multiple different AmmoProvider components. Though it will be very stupid if something does that and it will probably be bugged by itself anyway.
        // eject magazine. we can just check for ItemSlotsComponent; we don't actually need to check for the chamber+mag & just mag ammo provider components
        if (_entityManager.TryGetComponent<ItemSlotsComponent>(gun, out var itemSlotsComponent))
        {
            var magazineContainerSlot = itemSlotsComponent.Slots[SharedGunSystem.MagazineSlot];
            // this doesn't actually NEED to fail (it should still end early, but not necessarily fail) however that would be the expected behaviour of this operator for someone who doesn't know this
            if (magazineContainerSlot == null)
                return HTNOperatorStatus.Failed;

            var wasSlotEmpty = magazineContainerSlot.HasItem;
            // TryEject will return false if there was no item in the slot but that's good enough for what we're doing here, so we don't fail the operator if TryEject fails AND the slot is empty.
            // wasSlotEmpty is defined before TryEject is called because TryEject empties the slot which would make it true if called after TryEject. duh. i absolutely don't need to explain that in depth (or at all) but whatever.
            // TLDR only fail if TryEject failed for a reason other than the slot being empty
            if (!_itemSlotsSystem.TryEject(gun, magazineContainerSlot, null, out var magazine) && wasSlotEmpty == false)
                return HTNOperatorStatus.Failed;
        }
        // empty revolver cylinder
        else if (_entityManager.TryGetComponent<RevolverAmmoProviderComponent>(gun, out var revolverAmmoProviderComponent))
        {
            _gunSystem.EmptyRevolver(gun, revolverAmmoProviderComponent);
        }

        return HTNOperatorStatus.Finished;
    }
}

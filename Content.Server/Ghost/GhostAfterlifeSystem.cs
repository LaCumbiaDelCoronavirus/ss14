using Robust.Server.GameObjects;
using Robust.Server.Containers;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Station.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Mind;
using Content.Server.Antag.Components;
using Content.Shared.Ghost;
using Content.Server.Ghost.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Robust.Shared.Player;
using Content.Shared.Mind;


namespace Content.Server.GhostAfterlife;

public sealed class GhostAfterlifeSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSpawningSystem _spawnSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<GhostAfterlifePlayerComponent, MindRemovedMessage>(OnPlayerGhosted);
        SubscribeNetworkEvent<GhostAfterlifeSpawnRequestEvent>(SpawnPlayer);
    }

    private static ResPath _afterlifeMapPath = new("Maps/Nonstations/ghost-afterlife.yml");
    private string _afterlifeJob = "Passenger";
    private void OnRoundStart(RoundStartingEvent ev)
    {
        var mapLoadOptions = DeserializationOptions.Default with {InitializeMaps = true, PauseMaps = false};
        if (_mapLoader.TryLoadMap(_afterlifeMapPath, out var map, out var grids, mapLoadOptions))
        {
            // no idea why this can't just use the 2nd overload for SetPaused that takes a MapComponent for the first param, rather than MapId. but whatever
            _map.SetPaused(map.Value.Comp.MapId, false);
        }
    }

    private void OnPlayerGhosted(EntityUid uid, GhostAfterlifePlayerComponent component, MindRemovedMessage args)
    {
        // return them to their body (aka their real body will be catatonic while theyre in the afterlife until they leave the afterlife. if it wasnt already.)
        // do that by giving them a new mind
        // also i dont know how to do this without all the nesting since i need that god forsaken '_entityManager.DeleteEntity(uid);' at the end of all this
        if (_entityManager.TryGetComponent(uid, out ActorComponent? actorComponent))
        {
            var player = actorComponent.PlayerSession;
            if (_mindSystem.TryGetMind(player, out var mindId, out var mind))
            {
                // delete old body
                _entityManager.DeleteEntity(uid);

                // forcefully returns them to their original body
                if (component.OriginalEntityUid != null)
                    _mindSystem.TransferTo(mindId, component.OriginalEntityUid, false, createGhost: false);
            }
        }
    }
    private void SpawnPlayer(GhostAfterlifeSpawnRequestEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;

        if (!_mindSystem.TryGetMind(player, out var mindId, out var mind))
            return;
        // check if theyre an observer (ghosts use visiting attached entities, not transferring between attached entities)
        if (!_entityManager.TryGetComponent<GhostComponent>(player.AttachedEntity, out var ghost))
            return;

        var query = EntityQueryEnumerator<ContainerSpawnPointComponent, ContainerManagerComponent, TransformComponent>();
        var possibleContainers = new List<Entity<ContainerSpawnPointComponent, ContainerManagerComponent, TransformComponent>>();

        while (query.MoveNext(out var uid, out var spawnPoint, out var container, out var xform))
        {
            if (spawnPoint.SpawnType == SpawnPointType.GhostAfterlife)
            {
                possibleContainers.Add((uid, spawnPoint, container, xform));
            }
        }

        if (possibleContainers.Count == 0)
            return;

        var playerData = player.ContentData();

        if (playerData == null)
            return;

        // get default coords for the player
        var mobProfile = _ticker.GetPlayerProfile(player);

        var baseCoords = possibleContainers[0].Comp3.Coordinates;
        var spawnedMobUid = _spawnSystem.SpawnPlayerMob(baseCoords, _afterlifeJob, mobProfile, null);

        // i took some of this from goobstation's ghost bar code since it's a good way to do it anyway
        _entityManager.EnsureComponent<AntagImmuneComponent>(spawnedMobUid);
        _entityManager.EnsureComponent<IsDeadICComponent>(spawnedMobUid);
        var afterlifePlayerComponent = _entityManager.EnsureComponent<GhostAfterlifePlayerComponent>(spawnedMobUid);
        afterlifePlayerComponent.OriginalEntityUid = mind.OwnedEntity;

        //mindId = _mindSystem.CreateMind(playerData.UserId, mobProfile.Name).Owner;
        _mindSystem.TransferTo(mindId, spawnedMobUid, true, createGhost: false);

        // your honour i did copy and paste this from ContainerSpawnPointSystem.cs
        _random.Shuffle(possibleContainers);
        foreach (var (uid, spawnPoint, manager, xform) in possibleContainers)
        {
            if (!_container.TryGetContainer(uid, spawnPoint.ContainerId, out var container, manager))
                continue;

            if (!_container.Insert(spawnedMobUid, container, containerXform: xform))
                continue;

            return;
        }
    }
}

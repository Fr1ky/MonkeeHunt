using System.Collections.Generic;
using PurrNet;
using PurrNet.Modules;
using UnityEngine;

// Server: when a player finishes loading this scene, spawns the body prefab for their team
// inside that team's SpawnArea and gives them ownership.
// A team with no prefab (e.g. Unassigned in the round) gets no body; that player spectates.
public class TeamSpawner : PurrMonoBehaviour {

    [SerializeField]
    private GameObject unassignedPrefab;

    [SerializeField]
    private GameObject hunterPrefab;

    [SerializeField]
    private GameObject monkeyPrefab;

    [SerializeField]
    private SpawnArea[] spawnAreas;

    [SerializeField]
    private MatchFlowSettings settings;

    private readonly List<Vector3> usedPoints = new();
    private readonly HashSet<PlayerID> spawnedPlayers = new();

    public override void Subscribe(NetworkManager manager, bool asServer) {
        if (!asServer || !manager.TryGetModule(out ScenePlayersModule scenePlayers, true)) {
            return;
        }
        scenePlayers.onPlayerLoadedScene += OnPlayerLoadedScene;

        // Players that already finished loading before we subscribed.
        if (manager.TryGetModule(out ScenesModule scenes, true)
            && scenes.TryGetSceneID(gameObject.scene, out var sceneID)
            && scenePlayers.TryGetPlayersInScene(sceneID, out var players)) {
            foreach (var player in players) {
                OnPlayerLoadedScene(player, sceneID, true);
            }
        }
    }

    public override void Unsubscribe(NetworkManager manager, bool asServer) {
        if (asServer && manager.TryGetModule(out ScenePlayersModule scenePlayers, true)) {
            scenePlayers.onPlayerLoadedScene -= OnPlayerLoadedScene;
        }
    }

    private void OnPlayerLoadedScene(PlayerID player, SceneID scene, bool asServer) {
        var networkManager = NetworkManager.main;
        if (!asServer || !networkManager || !networkManager.TryGetModule(out ScenesModule scenes, true)) {
            return;
        }
        if (!scenes.TryGetSceneID(gameObject.scene, out var sceneID) || sceneID != scene) {
            return;
        }
        if (!spawnedPlayers.Add(player)) {
            return;
        }

        var entry = MatchSession.GetOrAdd(player);
        var team = entry.Team;
        var prefab = team switch {
            Team.Hunters => hunterPrefab,
            Team.Monkeys => monkeyPrefab,
            _ => unassignedPrefab
        };
        var area = FindArea(team);
        if (!prefab || !area) {
            return;
        }

        var position = area.PickPoint(usedPoints);
        usedPoints.Add(position);
        var lookAtCentre = Vector3.ProjectOnPlane(-position, Vector3.up);
        var rotation = lookAtCentre.sqrMagnitude > 0.01f ? Quaternion.LookRotation(lookAtCentre) : Quaternion.identity;

        var body = UnityProxy.Instantiate(prefab, position, rotation, gameObject.scene);
        if (!body.TryGetComponent(out NetworkIdentity identity)) {
            return;
        }
        if (!identity.IsSpawned(true)) {
            networkManager.Spawn(body);
        }
        identity.GiveOwnership(player);

        if (body.TryGetComponent(out HunterLoadout loadout)) {
            loadout.SetItems(entry.Loadout);
        }

        if (body.TryGetComponent(out PlayerHealth health)) {
            health.Initialize(settings.HealthFor(team));
            var round = RoundController.Instance;
            if (round) {
                round.RegisterParticipant(team, health);
            }
        }
    }

    private SpawnArea FindArea(Team team) {
        foreach (var area in spawnAreas) {
            if (area && area.Team == team) {
                return area;
            }
        }
        return null;
    }
}

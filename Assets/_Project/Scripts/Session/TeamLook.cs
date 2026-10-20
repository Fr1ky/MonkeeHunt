using UnityEngine;

// Lobby hub body: swaps the placeholder material to match the team zone the player stands in.
public class TeamLook : MonoBehaviour {

    [SerializeField]
    private PlayerInfo playerInfo;

    [SerializeField]
    private Renderer bodyRenderer;

    [SerializeField]
    private Material unassignedMaterial;

    [SerializeField]
    private Material huntersMaterial;

    [SerializeField]
    private Material monkeysMaterial;

    private void OnEnable() {
        playerInfo.OnInfoChanged += Refresh;
        Refresh();
    }

    private void OnDisable() {
        playerInfo.OnInfoChanged -= Refresh;
    }

    private void Refresh() {
        bodyRenderer.sharedMaterial = playerInfo.Team switch {
            Team.Hunters => huntersMaterial,
            Team.Monkeys => monkeysMaterial,
            _ => unassignedMaterial
        };
    }
}

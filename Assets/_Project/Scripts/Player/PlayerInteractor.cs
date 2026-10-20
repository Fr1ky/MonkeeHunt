using UnityEngine;

// Local player only: raycasts from the camera centre and uses the first IInteractable hit on Interact.
public class PlayerInteractor : MonoBehaviour {

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private MouseAimController mouseAim;

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private float interactRange = 2.5f;

    [SerializeField]
    private LayerMask interactLayers = ~0;

    private void Update() {
        if (!input.InteractPressed) {
            return;
        }
        var ray = mouseAim.GetCameraRay();
        if (Physics.Raycast(ray, out var hit, interactRange, interactLayers, QueryTriggerInteraction.Ignore)) {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            interactable?.Interact(player);
        }
    }
}

using TMPro;
using UnityEngine;

// Local status line for the round (netted, riding a hunter's head).
public class StatusHud : MonoBehaviour {

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private string nettedMessage = "NETTED";

    [SerializeField]
    private string debugFreeHint = "  (F8 to free - debug)";

    [SerializeField]
    private string ridingMessage = "RIDING  ·  WASD steer  ·  Jump hop off";

    private void Update() {
        var local = PlayerNetworkController.Local;
        var inScene = local && local.gameObject.scene == gameObject.scene;
        var net = inScene ? local.GetComponent<NetTrap>() : null;
        var ride = inScene ? local.GetComponent<MonkeyRide>() : null;
        var netted = net && net.IsInNet;
        var riding = ride && ride.IsSeated;
        statusText.enabled = netted || riding;
        if (netted) {
            statusText.text = nettedMessage + (Application.isEditor || Debug.isDebugBuild ? debugFreeHint : "");
        } else if (riding) {
            statusText.text = ridingMessage;
        }
    }
}

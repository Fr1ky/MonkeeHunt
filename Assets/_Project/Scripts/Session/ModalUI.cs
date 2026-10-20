using UnityEngine;

// Tracks world-opened menus (host console, loadout station) so the Esc menu leaves them alone:
// Esc closes the open menu instead of opening the pause menu on the same frame.
public static class ModalUI {

    private static int openCount;

    public static bool IsOpen => openCount > 0;

    public static int LastClosedFrame { get; private set; } = -1;

    public static void NotifyOpened() {
        openCount++;
    }

    public static void NotifyClosed() {
        openCount = Mathf.Max(0, openCount - 1);
        LastClosedFrame = Time.frameCount;
    }

    public static bool BlocksEscMenu => IsOpen || LastClosedFrame == Time.frameCount;

    // Statics survive play mode restarts when domain reload is off.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
        openCount = 0;
        LastClosedFrame = -1;
    }
}

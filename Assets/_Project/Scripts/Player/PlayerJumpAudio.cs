using UnityEngine;

// Owner only: quiet jump and landing sounds for your own body (2D). Harder landings are louder.
public class PlayerJumpAudio : MonoBehaviour {

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerMovementController movement;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip jumpSound;

    [SerializeField]
    private AudioClip landSound;

    // Landings slower than this (m/s down) are silent; at loudLandSpeed they play at full volume.
    [SerializeField]
    private float minLandSpeed = 2f;

    [SerializeField]
    private float loudLandSpeed = 10f;

    private void OnEnable() {
        if (movement) {
            movement.OnJumped += Jumped;
            movement.OnLanded += Landed;
        }
    }

    private void OnDisable() {
        if (movement) {
            movement.OnJumped -= Jumped;
            movement.OnLanded -= Landed;
        }
    }

    private void Jumped() {
        Play(jumpSound, 1f);
    }

    private void Landed(float downSpeed) {
        if (downSpeed >= minLandSpeed) {
            Play(landSound, Mathf.Lerp(0.4f, 1f, Mathf.InverseLerp(minLandSpeed, loudLandSpeed, downSpeed)));
        }
    }

    private void Play(AudioClip clip, float volume) {
        if (player && player.isOwner && audioSource && clip) {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}

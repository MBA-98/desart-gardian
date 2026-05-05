using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class CreatureEnemyChase : MonoBehaviour
{
    [Header("Detect Player")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float aggroDistance = 10f;
    [SerializeField] private float loseChaseDistance = 22f;

    [Header("Combat")]
    [SerializeField] private float damagePerHit = 12f;
    [SerializeField] private float damageCooldownSeconds = 0.7f;
    [SerializeField] private bool damageOnlyWhileChasing = true;

    [Header("Movement While Chasing")]
    [SerializeField] private bool runWhileChasing = true;
    [SerializeField] private float lookHeightOffset = 0.65f;

    [Header("Random Chase Sounds")]
    [SerializeField] private AudioSource chaseAudio;
    [SerializeField] private AudioClip[] chaseSounds;
    [SerializeField] private float maxSoundDistance = 12f;
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float maxVolume = 0.8f;
    [SerializeField] private float soundIntervalMin = 2f;
    [SerializeField] private float soundIntervalMax = 5f;

    private bool _chasing;
    private Transform _playerRoot;
    private float _nextDamageTime;
    private float _nextPlayerResolveTime;
    private float _nextSoundTime;

    private void Awake()
    {
        if (chaseAudio == null)
            chaseAudio = GetComponent<AudioSource>();

        if (chaseAudio != null)
        {
            chaseAudio.playOnAwake = false;
            chaseAudio.loop = false;
            chaseAudio.volume = 0f;
            chaseAudio.spatialBlend = 1f;
        }
    }

    private void OnValidate()
    {
        aggroDistance = Mathf.Max(0.5f, aggroDistance);
        loseChaseDistance = Mathf.Max(loseChaseDistance, aggroDistance + 1f);
        maxSoundDistance = Mathf.Max(1f, maxSoundDistance);
        fadeSpeed = Mathf.Max(0.1f, fadeSpeed);
        maxVolume = Mathf.Clamp01(maxVolume);
        soundIntervalMin = Mathf.Max(0.2f, soundIntervalMin);
        soundIntervalMax = Mathf.Max(soundIntervalMax, soundIntervalMin);
    }

    private void Update()
    {
        ResolvePlayerTransform();
        TickChaseState();
        UpdateChaseSound();
    }

    private void TickChaseState()
    {
        // Make chase state independent from external movement callers.
        // This ensures audio (and any other chase logic) still works even if no one calls TryGetChaseInput().
        if (!TryGetPlayerPlanarOffset(out _, out float dist))
        {
            _chasing = false;
            return;
        }

        if (_chasing)
        {
            if (dist > loseChaseDistance)
                _chasing = false;
        }
        else
        {
            if (dist <= aggroDistance)
                _chasing = true;
        }
    }

    public bool TryGetChaseInput(out Vector2 axis, out Vector3 target, out bool run)
    {
        axis = Vector2.zero;
        target = transform.position + transform.forward * 6f + Vector3.up * lookHeightOffset;
        run = false;

        if (!isActiveAndEnabled)
            return false;

        if (!TryGetPlayerPlanarOffset(out var toPlayer, out float dist))
        {
            _chasing = false;
            return false;
        }

        // Keep behavior consistent with TickChaseState().
        if (_chasing)
        {
            if (dist > loseChaseDistance)
                _chasing = false;
        }
        else
        {
            if (dist <= aggroDistance)
                _chasing = true;
        }

        if (!_chasing)
            return false;

        var dir = dist > 0.08f ? toPlayer / dist : HorizontalForward(transform.forward);

        target = _playerRoot.position + Vector3.up * lookHeightOffset;

        float lateral = Vector3.Dot(transform.right, dir);
        axis = new Vector2(Mathf.Clamp(lateral, -0.45f, 0.45f), 0.92f);
        axis = Vector2.ClampMagnitude(axis, 1f);

        run = runWhileChasing;
        return true;
    }

    private void UpdateChaseSound()
    {
        if (chaseAudio == null)
            return;

        // If we can't play (no target / no clips / not chasing), just fade out smoothly.
        if (!_chasing || _playerRoot == null || chaseSounds == null || chaseSounds.Length == 0)
        {
            FadeSoundDown();
            return;
        }

        // Cheaper than Vector3.Distance (no sqrt).
        var a = transform.position;
        var b = _playerRoot.position;
        float distSq = (a - b).sqrMagnitude;
        float maxDist = Mathf.Max(0.01f, maxSoundDistance);
        float maxDistSq = maxDist * maxDist;

        float targetVolume = 0f;

        if (distSq <= maxDistSq)
        {
            float dist = Mathf.Sqrt(distSq);
            targetVolume = 1f - (dist / maxDist);
            targetVolume *= maxVolume;

            // Start a sound occasionally (no spamming/restarting).
            if (Time.time >= _nextSoundTime && !chaseAudio.isPlaying)
            {
                PlayRandomChaseSound();
                _nextSoundTime = Time.time + Random.Range(soundIntervalMin, soundIntervalMax);
            }
        }

        chaseAudio.volume = Mathf.MoveTowards(
            chaseAudio.volume,
            targetVolume,
            fadeSpeed * Time.deltaTime
        );

        if (distSq > maxDistSq && chaseAudio.volume <= 0.01f)
        {
            chaseAudio.volume = 0f;

            if (chaseAudio.isPlaying)
                chaseAudio.Stop();
        }
    }

    private void PlayRandomChaseSound()
    {
        if (chaseSounds == null || chaseSounds.Length == 0)
            return;

        if (chaseAudio == null)
            return;

        // Pick a valid clip (skip null entries).
        AudioClip clip = null;
        for (int i = 0; i < 6; i++)
        {
            var c = chaseSounds[Random.Range(0, chaseSounds.Length)];
            if (c != null) { clip = c; break; }
        }

        if (clip == null)
            return;

        // Don't restart the same clip while it's already playing.
        if (chaseAudio.isPlaying && chaseAudio.clip == clip)
            return;

        chaseAudio.clip = clip;
        chaseAudio.Play();
    }

    private void FadeSoundDown()
    {
        chaseAudio.volume = Mathf.MoveTowards(
            chaseAudio.volume,
            0f,
            fadeSpeed * Time.deltaTime
        );

        if (chaseAudio.volume <= 0.01f)
        {
            chaseAudio.volume = 0f;

            if (chaseAudio.isPlaying)
                chaseAudio.Stop();
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!isActiveAndEnabled || damagePerHit <= 0f)
            return;

        if (!hit.collider.CompareTag(playerTag))
            return;

        if (Time.time < _nextDamageTime)
            return;

        if (damageOnlyWhileChasing && !_chasing)
            return;

        if (!TryGetPlayerPlanarOffset(out _, out float dist))
            return;

        if (dist > loseChaseDistance + 2f)
            return;

        var health = hit.collider.GetComponent<PlayerHealth>() ?? hit.collider.GetComponentInParent<PlayerHealth>();

        if (health == null)
            health = PlayerHealth.FindOnPlayer();

        if (health == null || health.IsDead)
            return;

        health.TakeDamage(damagePerHit);
        _nextDamageTime = Time.time + Mathf.Max(0.05f, damageCooldownSeconds);
    }

    private bool TryGetPlayerPlanarOffset(out Vector3 planarToPlayer, out float distance)
    {
        planarToPlayer = default;
        distance = float.MaxValue;

        if (_playerRoot == null)
            ResolvePlayerTransform();

        if (_playerRoot == null)
            return false;

        var p = _playerRoot.position;
        var self = transform.position;

        planarToPlayer = new Vector3(p.x - self.x, 0f, p.z - self.z);
        distance = planarToPlayer.magnitude;

        return true;
    }

    private void ResolvePlayerTransform()
    {
        if (_playerRoot != null && _playerRoot.gameObject.activeInHierarchy)
            return;

        if (Time.time < _nextPlayerResolveTime)
            return;

        _nextPlayerResolveTime = Time.time + 0.35f;

        var go = GameObject.FindGameObjectWithTag(playerTag);
        _playerRoot = go != null ? go.transform : null;
    }

    private static Vector3 HorizontalForward(Vector3 dir)
    {
        var h = new Vector3(dir.x, 0f, dir.z);
        return h.sqrMagnitude > 0.0001f ? h.normalized : Vector3.forward;
    }
}
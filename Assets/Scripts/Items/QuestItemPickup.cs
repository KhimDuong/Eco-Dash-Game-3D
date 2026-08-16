using UnityEngine;

/// <summary>
/// Lá Thuốc (medicinal herb) — Ông Sáu's M8 quest collectible. Only pickable while
/// the quest is actually asking for herbs, so it can't be hoovered up early; three
/// of them take the quest to <see cref="QuestStage.HerbsReady"/>.
///
/// <para>3D port: trigger callback loses its `2D` suffix.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class QuestItemPickup : MonoBehaviour
{
    [SerializeField] AudioClip pickupSound;

    [Header("Bob animation")]
    [SerializeField] float bobSpeed = 2f;
    [SerializeField] float bobHeight = 0.1f;

    bool isCollected;
    Vector3 basePosition;

    void Start()
    {
        basePosition = transform.position;
        GetComponent<Collider>().isTrigger = true;

        // Already picked this specific herb (M9 save persistence), or already gathered
        // enough herbs in a previous visit — don't respawn it.
        if (SceneProgress.IsConsumed(gameObject) || QuestProgress.Stage >= QuestStage.HerbsReady)
            gameObject.SetActive(false);
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * bobSpeed + basePosition.x) * bobHeight;
        transform.position = basePosition + new Vector3(0f, y, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected || !other.CompareTag("Player")) return;

        // Only allow pickup if the quest is active and needing herbs.
        if (QuestProgress.Stage != QuestStage.HerbsInProgress) return;

        isCollected = true;
        // Was played at the *camera* rather than the herb, which is how the 2D build worked
        // around PlayClipAtPoint's 3D attenuation one call site at a time. Sfx handles the
        // distance itself, so this can say where the sound actually happens again.
        Sfx.Play(pickupSound, transform.position);

        SceneProgress.Consume(gameObject);
        QuestProgress.AddHerb();
        Destroy(gameObject);
    }
}

using UnityEngine;
using System.Collections;

public class Portal : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject portalVisual;       // The Procedural Portal Effect object
    [SerializeField] private Transform targetPoint;         // Destination position (empty GameObject)
    [SerializeField] private Portal destinationPortal;      // The portal at the destination (optional)

    [Header("Animation")]
    [SerializeField] private float scaleUpDuration = 1.5f;  // Time to grow from 0 to full size
    [SerializeField] private float scaleDownDuration = 1.5f;// Time to shrink from full size to 0
    [SerializeField] private float disappearDelay = 5f;     // Seconds to wait before shrinking

    private Vector3 originalScale;
    private bool playerTeleported = false;

    private void Awake()
    {
        if (portalVisual != null)
        {
            originalScale = portalVisual.transform.localScale;
            portalVisual.transform.localScale = Vector3.zero;
            portalVisual.SetActive(false);
        }
    }

    /// <summary>
    /// Called by the Boss's dieEvent to make the portal appear (scale up).
    /// Also activates the destination portal if assigned.
    /// </summary>
    public void ActivatePortal()
    {
        if (portalVisual != null)
        {
            portalVisual.SetActive(true);
            StartCoroutine(ScaleUp());
            Debug.Log("Portal is appearing!");
        }

        // Also activate the destination portal
        if (destinationPortal != null)
        {
            destinationPortal.ActivateAsDestination();
        }
    }

    /// <summary>
    /// Called internally to activate the destination portal (scale up only, no chain).
    /// </summary>
    public void ActivateAsDestination()
    {
        if (portalVisual != null)
        {
            portalVisual.SetActive(true);
            StartCoroutine(ScaleUp());
            Debug.Log("Destination portal is appearing!");
        }
    }

    private IEnumerator ScaleUp()
    {
        float elapsed = 0f;
        portalVisual.transform.localScale = Vector3.zero;

        while (elapsed < scaleUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleUpDuration);
            // Ease out for a nice effect
            float eased = 1f - (1f - t) * (1f - t);
            portalVisual.transform.localScale = originalScale * eased;
            yield return null;
        }

        portalVisual.transform.localScale = originalScale;
    }

    private IEnumerator ScaleDown()
    {
        float elapsed = 0f;
        Vector3 startScale = portalVisual.transform.localScale;

        while (elapsed < scaleDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleDownDuration);
            // Ease in for a nice shrink
            float eased = 1f - t * t;
            portalVisual.transform.localScale = startScale * eased;
            yield return null;
        }

        portalVisual.transform.localScale = Vector3.zero;
        portalVisual.SetActive(false);
    }

    /// <summary>
    /// Start the disappear sequence: wait, then shrink.
    /// </summary>
    public void StartDisappear()
    {
        StartCoroutine(DisappearAfterDelay());
    }

    private IEnumerator DisappearAfterDelay()
    {
        yield return new WaitForSeconds(disappearDelay);
        yield return StartCoroutine(ScaleDown());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playerTeleported && other.CompareTag("Player"))
        {
            if (targetPoint == null)
            {
                Debug.LogError("Target Point is not assigned in Portal script!");
                return;
            }

            playerTeleported = true;
            Debug.Log("Player entered portal. Teleporting to " + targetPoint.position);

            // Disable CharacterController so we can move the player directly
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            other.transform.position = targetPoint.position;

            if (cc != null) cc.enabled = true;

            // After teleport: both portals wait, then shrink and disappear
            StartDisappear();
            if (destinationPortal != null)
            {
                destinationPortal.StartDisappear();
            }
        }
    }
}

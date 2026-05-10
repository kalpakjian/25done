using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject portalVisual; // The Procedural Portal Effect object to show/hide
    [SerializeField] private Transform targetPoint;   // Drag the destination transform here (the target portal/spawn point)

    private void Awake()
    {
        // Ensure portal is hidden at start
        if (portalVisual != null)
        {
            portalVisual.SetActive(false);
        }
    }

    /// <summary>
    /// Called by the Boss's dieEvent to make the portal appear.
    /// </summary>
    public void ActivatePortal()
    {
        if (portalVisual != null)
        {
            portalVisual.SetActive(true);
            Debug.Log("Portal has appeared!");
        }
        else
        {
            Debug.LogError("Portal Visual is not assigned in Portal script!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only teleport the player
        if (other.CompareTag("Player"))
        {
            if (targetPoint == null)
            {
                Debug.LogError("Target Point is not assigned in Portal script!");
                return;
            }

            Debug.Log("Player entered portal. Teleporting to " + targetPoint.position);

            // Disable the CharacterController (if any) so we can move the player directly
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            other.transform.position = targetPoint.position;

            if (cc != null) cc.enabled = true;
        }
    }
}

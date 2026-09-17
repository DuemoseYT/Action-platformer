using UnityEngine;
using TMPro; // Optional, remove if not using TextMeshPro

public class RedButton : MonoBehaviour
{
    [Header("References")]
    public GameObject wallToMove;
    public GameObject interactionPrompt; // Optional UI prompt "Press E"

    [Header("Wall Settings")]
    public float moveSpeed = 2f;
    public float moveDistance = 3f;

    [Header("Button Visual")]
    public Color normalColor = Color.red;
    public Color pressedColor = new Color(0.5f, 0f, 0f); // Dark red

    private bool playerInRange = false;
    private bool activated = false;
    private SpriteRenderer sr;
    private Vector3 wallStartPos;
    private Vector3 wallTargetPos;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = normalColor;

        if (wallToMove != null)
        {
            wallStartPos = wallToMove.transform.position;
            wallTargetPos = wallStartPos + Vector3.up * moveDistance;
        }

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    void Update()
    {
        // Show/hide prompt
        if (interactionPrompt != null)
            interactionPrompt.SetActive(playerInRange && !activated);

        // Press E
        if (playerInRange && !activated && Input.GetKeyDown(KeyCode.E))
        {
            Activate();
        }

        // Move wall upward smoothly
        if (activated && wallToMove != null)
        {
            wallToMove.transform.position = Vector3.MoveTowards(
                wallToMove.transform.position,
                wallTargetPos,
                moveSpeed * Time.deltaTime
            );
        }
    }

    void Activate()
    {
        activated = true;
        if (sr != null) sr.color = pressedColor;
        Debug.Log("Button activated!");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }
}
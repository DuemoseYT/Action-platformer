using UnityEngine;

public class Respawn : MonoBehaviour
{
    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.position = originalPosition;
        }
    }
}
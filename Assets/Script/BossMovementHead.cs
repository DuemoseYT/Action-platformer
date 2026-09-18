using UnityEngine;

public class MoveBetweenPoints2D : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float waitTime = 0.5f; // pause at each point
    [SerializeField] private bool loop = true;

    private int currentIndex = 0;
    private float waitTimer = 0f;

    void Update()
    {
        if (points.Length == 0) return;

        // Pause at waypoint before moving on
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Transform target = points[currentIndex];
        transform.position = Vector2.MoveTowards(
            transform.position, target.position, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.01f)
        {
            waitTimer = waitTime;
            currentIndex++;

            if (currentIndex >= points.Length)
            {
                currentIndex = loop ? 0 : points.Length - 1;
            }
        }
    }
}
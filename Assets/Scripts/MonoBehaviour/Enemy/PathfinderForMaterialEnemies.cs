using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfinderForMaterialEnemies : MonoBehaviour
{
    public Vector2 TargetPosition { get; set; }
    public Vector2 boxColliderSize = new(1f, 1f);

    [SerializeField] private LayerMask obstacleLayerMask;

    public Queue<Vector2> FindPath(Vector2 startPos, Vector2 targetPos)
    {
        Queue<Vector2> path = new();

        // return if no obstacles in direct line
        var hit = Physics2D.Linecast(startPos, targetPos, obstacleLayerMask);
        if (hit.collider == null)
        {
            path.Enqueue(targetPos);
            return path;
        }

        // use a pathfinding algorithm here (like A* or Dijkstra) for complex scenarios
        return path;
    }
}

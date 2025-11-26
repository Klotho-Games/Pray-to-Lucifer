using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PathfinderForMaterialEnemies : MonoBehaviour
{
    public Vector2 TargetPosition;

    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private PolygonCollider2D polygonCollider;

    private BoxCollider2D coll;
    private float halfDiagonalLength;

    void Awake()
    {
        coll = GetComponent<BoxCollider2D>();
        halfDiagonalLength = Mathf.Sqrt(Mathf.Pow(coll.size.x * 0.5f, 2) + Mathf.Pow(coll.size.y * 0.5f, 2));
    }

    Vector2? FindRoadEndPosition()
    {
        Vector2 direction = TargetPosition - (Vector2)transform.position;
        float distance = direction.magnitude;
        direction.Normalize();

        polygonCollider.points = GetOverlapPolygonPoints(direction, distance);

        // Setup contact filter
        ContactFilter2D filter = new();
        filter.SetLayerMask(obstacleLayerMask);
        filter.useTriggers = false;

        // Check for overlaps and get the outside points
        List<Collider2D> results = new();
        int count = Physics2D.OverlapCollider(polygonCollider, filter, results);

        Stack<Vector2> gateEdgePoints = new();
        for (int i = 0; i < count; ++i)
        {
            if (results[i] != null && results[i] is EdgeCollider2D edgeColl)
            {
                gateEdgePoints.Push(edgeColl.points[0]);
                gateEdgePoints.Push(edgeColl.points[edgeColl.pointCount - 1]);
            }
        }

        if (gateEdgePoints.Count == 0)
        {
            // No obstacles detected, move directly to target
            return TargetPosition;
        }

        // Find the point that is furthest from the road direction, and the angle to it from the road direction
        Vector2 furthestPoint = Vector2.zero;
        float angleToFurthest = 0f;

        while (gateEdgePoints.Count > 0)
        {
            Vector2 point = gateEdgePoints.Pop(); // Pop each point
            Vector2 directionToPoint = point - (Vector2)transform.position;

            float angleToPoint = Vector2.SignedAngle(direction, directionToPoint);

            if (Mathf.Abs(angleToPoint) >= Mathf.Abs(angleToFurthest))
            {
                if (angleToPoint == -angleToFurthest)
                {
                    int rng = Random.Range(0, 2);
                    if (rng == 0)
                    {
                        angleToFurthest = angleToPoint;
                        furthestPoint = point;
                    }
                    //else Keep previous
                }
                else
                {
                    angleToFurthest = angleToPoint;
                    furthestPoint = point;
                }
            }
        }

        Vector2 nextRoadDirection;
        bool goAroundClockwise;

        if (angleToFurthest < 0f)
        {
            nextRoadDirection = furthestPoint - polygonCollider.points[0];
            goAroundClockwise = false;
        }
        else
        {
            nextRoadDirection = furthestPoint - polygonCollider.points[2];
            goAroundClockwise = true;
        }

        float totalAngle = Mathf.Abs(angleToFurthest);
        Vector2 pointToIgnore = furthestPoint;

        return NextFindRoadEndPosition(nextRoadDirection, goAroundClockwise, totalAngle, pointToIgnore);
    }

    Vector2? NextFindRoadEndPosition(Vector2 direction, bool goAroundClockwise, float totalAngle, Vector2 pointToIgnore)
    {
        float distance = direction.magnitude;
        direction.Normalize();

        // TODO!!!!!!!!!!!!!!!!!!!TODO!!!!!!!!!!!!!!!!!!!!!!!!!!!!!TODO!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        polygonCollider.points = GetOverlapPolygonPoints(direction, distance);

        return null;
    }

    private Vector2[] GetOverlapPolygonPoints(Vector2 direction, float distance)
    {
        Vector2[] overlapPolygonPoints = new Vector2[6];
        Vector2[] extremeCorners = GetExtremeCorners(direction);
        overlapPolygonPoints[0] = extremeCorners[0];
        overlapPolygonPoints[1] = (Vector2)transform.position + Vector2.one * halfDiagonalLength;
        overlapPolygonPoints[2] = extremeCorners[1];
        for (int i = 0; i < 3; ++i)
        {
            overlapPolygonPoints[5-i] = overlapPolygonPoints[i] + direction * distance;
        }
        return overlapPolygonPoints;
    }

    private Vector2[] GetExtremeCorners(Vector2 direction)
    {
        Vector2[] corners = new Vector2[2];

        if (direction.x >= 0 && direction.y >= 0) // Top Right
        {
            corners[0] = new Vector2(coll.bounds.max.x, coll.bounds.min.y); // Bottom Right
            corners[1] = new Vector2(coll.bounds.min.x, coll.bounds.max.y); // Top Left
        }
        else if (direction.x < 0 && direction.y >= 0) // Top Left
        {
            corners[0] = new Vector2(coll.bounds.min.x, coll.bounds.min.y); // Bottom Left
            corners[1] = new Vector2(coll.bounds.max.x, coll.bounds.max.y); // Top Right
        }
        else if (direction.x < 0 && direction.y < 0) // Bottom Left
        {
            corners[0] = new Vector2(coll.bounds.min.x, coll.bounds.max.y); // Top Left
            corners[1] = new Vector2(coll.bounds.max.x, coll.bounds.min.y); // Bottom Right
        }
        else // Bottom Right
        {
            corners[0] = new Vector2(coll.bounds.max.x, coll.bounds.max.y); // Top Right
            corners[1] = new Vector2(coll.bounds.min.x, coll.bounds.min.y); // Bottom Left
        }

        return corners;
    }
}

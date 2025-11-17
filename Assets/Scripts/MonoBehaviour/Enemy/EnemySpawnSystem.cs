using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class EnemySpawnSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [Header("Do not change")]
    [SerializeField] private List<GameObject> enemies = new();
    [SerializeField] private Camera cam;
    [SerializeField] private Transform playerTransform;

    private float spawnTimer = 0f;

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        GameObject enemyToSpawn = GetRandomEnemy();
        Vector3 spawnPosition = GetPointOutsideCameraView();
        EnergyMeleeAI AI = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity).GetComponent<EnergyMeleeAI>();
        AI.Initialize(playerTransform);
    }

    private Vector3 GetPointOutsideCameraView()
    {
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        camWidth += 2f; // Extra buffer
        camHeight += 2f; // Extra buffer

        float x, y;
        int side = Random.Range(0, 4); // 0: top, 1: bottom, 2: left, 3: right

        switch (side)
        {
            case 0: // Top
                x = Random.Range(cam.transform.position.x - camWidth / 2, cam.transform.position.x + camWidth / 2);
                y = cam.transform.position.y + camHeight / 2 + 1f;
                break;
            case 1: // Bottom
                x = Random.Range(cam.transform.position.x - camWidth / 2, cam.transform.position.x + camWidth / 2);
                y = cam.transform.position.y - camHeight / 2 - 1f;
                break;
            case 2: // Left
                x = cam.transform.position.x - camWidth / 2 - 1f;
                y = Random.Range(cam.transform.position.y - camHeight / 2, cam.transform.position.y + camHeight / 2);
                break;
            case 3: // Right
                x = cam.transform.position.x + camWidth / 2 + 1f;
                y = Random.Range(cam.transform.position.y - camHeight / 2, cam.transform.position.y + camHeight / 2);
                break;
            default:
                x = 0f;
                y = 0f;
                break;
        }

        return new Vector3(x, y, 0f);
    }

    private GameObject GetRandomEnemy()
    {
        if (enemies.Count == 0)
        {
            Debug.LogError("No enemies assigned to EnemySpawnSystem.");
            return null;
        }
        int randomIndex = Random.Range(0, enemies.Count);
        return enemies[randomIndex];
    }
}

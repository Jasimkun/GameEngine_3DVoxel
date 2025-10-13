using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;
    public float spawnRange = 5f;    // 반경

    // === 스폰 횟수 제한 설정 ===
    [Header("Spawn Limit")]
    public int maxEnemiesToSpawn = 10; // 총 스폰할 최대 적의 수
    private int spawnedCount = 0;      // 현재까지 스폰된 적의 수

    // === 💡 스폰 위치 검사 설정 (추가) ===
    public float spawnCheckDistance = 10f; // 땅을 체크할 최대 거리 (Y축 아래로)
    public float spawnHeightOffset = 0.5f; // 타일 표면에서 적이 떠있는 높이

    private float timer = 0f;


    void Update()
    {
        // 1. 최대 스폰 횟수에 도달하면 함수를 종료합니다.
        if (spawnedCount >= maxEnemiesToSpawn)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            // x, z는 랜덤
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRange, spawnRange),
                0,
                Random.Range(-spawnRange, spawnRange)
            );

            // 💡 스포너 위치 + 랜덤 오프셋을 스폰 시도 위치로 설정합니다.
            Vector3 attemptedSpawnPos = transform.position + randomOffset;

            // 💡 적 스폰 시도 함수를 호출합니다.
            TrySpawnEnemy(attemptedSpawnPos);

            timer = 0f;
        }
    }

    // 💡 새로운 함수: Raycast를 사용하여 유효한 타일 위에만 적을 스폰합니다.
    void TrySpawnEnemy(Vector3 attemptedPosition)
    {
        RaycastHit hit;

        // 1. 시도 위치에서 아래로 Raycast를 쏩니다.
        //    (시작 높이를 안전하게 하기 위해 Y축을 1m 정도 높여서 시작합니다.)
        Vector3 startRay = attemptedPosition + Vector3.up * 1f;

        if (Physics.Raycast(startRay, Vector3.down, out hit, spawnCheckDistance))
        {
            // 2. VoxelCollapse 스크립트가 붙은 타일을 찾았는지 확인합니다.
            VoxelCollapse tileScript = hit.collider.GetComponent<VoxelCollapse>();

            if (tileScript != null)
            {
                // 3. 유효한 타일이 확인되면 스폰합니다.

                // 스폰 높이를 타일 표면(hit.point.y) + 적의 오프셋으로 설정
                Vector3 finalSpawnPosition = new Vector3(attemptedPosition.x,
                                                        hit.point.y + spawnHeightOffset,
                                                        attemptedPosition.z);

                Instantiate(enemyPrefab, finalSpawnPosition, Quaternion.identity);

                // 4. 스폰 성공 시 카운터 증가
                spawnedCount++;
            }
            // 붕괴된 타일이거나 타일이 아닌 곳에 Raycast가 맞으면 스폰하지 않고 그냥 실패합니다.
        }
        // Raycast가 아무것도 맞추지 못하면 (붕괴된 영역이 넓으면) 스폰 실패.
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // Gizmos를 spawnRange만큼의 크기로 표시
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRange * 2, 0.1f, spawnRange * 2));
    }
}
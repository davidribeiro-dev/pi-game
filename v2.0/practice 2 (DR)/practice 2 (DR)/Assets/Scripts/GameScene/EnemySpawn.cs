using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;
    [SerializeField] GameObject[] _spawnPoints;
    [SerializeField] GameObject _enemy;
    [SerializeField] GameObject _bossEnemy; 

    float _spawnTimer = 2f;
    float _bossTimer = 20f;
    float _spawnRateIncrease = 10f;
    void Start()
    {
        StartCoroutine(SpawnNextEnemy());
        StartCoroutine(SpawnRateIncrease());
        StartCoroutine(SpawnBoss());
    }


    IEnumerator SpawnBoss()
    {
        yield return new WaitForSeconds(_bossTimer);
        int nextSpawnLocation = Random.Range(0, _spawnPoints.Length);

        Instantiate(_bossEnemy, _spawnPoints[nextSpawnLocation].transform.position, Quaternion.identity);

        if (!_gameManager._gameOver)
        {
            StartCoroutine(SpawnBoss());
        }
    }

    IEnumerator SpawnNextEnemy()
    {
        int nextSpawnLocation = Random.Range(0, _spawnPoints.Length);

        Instantiate(_enemy, _spawnPoints[nextSpawnLocation].transform.position, Quaternion.identity);

        yield return new WaitForSeconds(_spawnTimer);

        if (!_gameManager._gameOver)
        {
            StartCoroutine(SpawnNextEnemy());
        }
    }

    IEnumerator SpawnRateIncrease()
    {
        yield return new WaitForSeconds(_spawnRateIncrease);

        if (_spawnTimer >= 0.5f)
        {
            _spawnTimer -= 0.2f;
        }
        StartCoroutine(SpawnRateIncrease());
    }
}

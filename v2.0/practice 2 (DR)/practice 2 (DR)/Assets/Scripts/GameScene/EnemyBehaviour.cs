using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    GameManager _gameManager;
    GameObject _player;
    [SerializeField] ParticleSystem _playerExplosion;
    [SerializeField] ParticleSystem _enemyExplosion;

    float _enemyHealth = 100f;
    float _enemyMoveSpeed = 5.2f;
    Quaternion _targetRotation;
    bool _disableEnemy = false;
    Vector2 _moveDirection;

    // Start is called before the first frame update
    void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (!_gameManager._gameOver && !_disableEnemy)
        {
            MoveEnemy();
            RotateEnemy();
        }
    }

    void MoveEnemy()
    {
         transform.position = Vector2.MoveTowards(transform.position, _player.transform.position, _enemyMoveSpeed * Time.deltaTime);
    }
    void RotateEnemy()
    {
        _moveDirection = _player.transform.position - transform.position;
        _moveDirection.Normalize();

        _targetRotation = Quaternion.LookRotation(Vector3.forward, _moveDirection);

        if (transform.rotation != _targetRotation)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, 200 * Time.deltaTime);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Bullet")
        {
            StartCoroutine(BulletDamage());

            _enemyHealth -= 25f;

            if (_enemyHealth <= 0f)
            {
                Instantiate(_enemyExplosion, transform.position, Quaternion.identity);
                Destroy(gameObject);
                ScoreCounter.scoreNumber += 1;
            }

            Destroy(collision.gameObject);
        }

        if (collision.gameObject.tag == "Rocket")
        {
            StartCoroutine(RocketDamage());

            _enemyHealth -= 50f;

            if (_enemyHealth <= 0f)
            {
                Instantiate(_enemyExplosion, transform.position, Quaternion.identity);
                Destroy(gameObject);
                ScoreCounter.scoreNumber += 1;
            }

            Destroy(collision.gameObject);
        }

        else if (collision.gameObject.tag == "Player")
        {
            _gameManager._gameOver = true;
            Instantiate(_playerExplosion, transform.position, Quaternion.identity);
            collision.gameObject.SetActive(false);
        }

        if (collision.gameObject.tag == "BossEnemy")
        {
            Instantiate(_enemyExplosion, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    IEnumerator BulletDamage()
    {
        _disableEnemy = true;
        yield return new WaitForSeconds(0.5f);
        _disableEnemy = false;
    }

    IEnumerator RocketDamage()
    {
        _disableEnemy = true;
        yield return new WaitForSeconds(1f);
        _disableEnemy = false;
    }
}

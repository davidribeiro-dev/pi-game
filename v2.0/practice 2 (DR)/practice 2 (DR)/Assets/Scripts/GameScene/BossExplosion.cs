using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossExplosion : MonoBehaviour
{
    GameManager _gameManager;
    GameObject player;
    [SerializeField] ParticleSystem _playerExplosion;

    private void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        player = GameObject.FindGameObjectWithTag("Player");
    }
    private void OnParticleCollision(GameObject other)
    {
        if (other.tag == ("Player"))
        {
            _gameManager._gameOver = true;
            Instantiate(_playerExplosion, player.transform.position, Quaternion.identity);
            player.SetActive(false);
        }
    }
}

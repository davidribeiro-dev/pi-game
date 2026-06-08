using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] GameObject _gameOverUI;
    [SerializeField] GameManager _gameManager;

    void Update()
    {
        if (_gameManager._gameOver)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        _gameOverUI.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit successful.");
    }
}

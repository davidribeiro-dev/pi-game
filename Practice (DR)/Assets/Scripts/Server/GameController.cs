using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Authentication;
using Unity.VisualScripting;

public class GameController : MonoBehaviour
{
    [Header("References")]
    public PhotonView myPV;
    public GameObject tagger;
    public PlayerMovement playerMovement;

    public List<GameObject> PlayerList = new List<GameObject>();
    public GameObject playerPrefab;
    public float gameTime;
    public bool gameTimerEnd;
    public bool gameStarted;

    [Header("Spawn Range")]
    public float minX;
    public float maxX;
    public float minZ;
    public float maxZ;
    void Start()
    {
        Vector3 randomPosition = new Vector3(Random.Range(minX, maxX), 2, Random.Range(minZ, maxZ));
        PhotonNetwork.Instantiate(playerPrefab.name, randomPosition, Quaternion.identity);
        gameTimerEnd = false;
        myPV = GetComponent<PhotonView>();
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
    }

    private void FixedUpdate()
    {
        if ((PhotonNetwork.CountOfPlayers) >= 2 && gameTime != 0 && !gameTimerEnd)
        {
            gameTime -= Time.deltaTime;
        }

        if (gameTime <= 0)
        {
            gameTimerEnd = true;
        }

        if (gameTimerEnd && !gameStarted)
        {
            GameStart();
        }
    }
    private void Update()
    {
      
    }

    private void GameStart()
    {
        Debug.Log("Game has Started.");
        gameStarted = true;
        TagRandomPlayer();
    }

    void TagRandomPlayer()
    {
        int PlayerListRange = PlayerList.Count;
        System.Random Rand = new System.Random();
        int RandomPick = Rand.Next(0, PlayerListRange);
        tagger = PlayerList[RandomPick];
        Debug.Log(tagger);
    }
}


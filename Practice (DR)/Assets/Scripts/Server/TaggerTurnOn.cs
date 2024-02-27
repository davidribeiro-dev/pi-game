using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Unity.VisualScripting;

public class TaggerTurnOn : MonoBehaviour
{
    [Header("References")]
    public GameController gameController;
    public PlayerMovement playerMovement;
    public PhotonView localPlayer;

    public bool activated = false;

    private void Start()
    {
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        localPlayer = GameObject.FindGameObjectWithTag("Player").GetComponent<PhotonView>();
    }

    private void Update()
    {
       TurnOnTagger();
    }

    // GameTagger
    public void TurnOnTagger()
    {
        if ((gameController.gameStarted == true) && (activated == false))
        {
            activated = true;
            localPlayer.RPC("ActivateTagger", RpcTarget.All);
        }
    }

    [PunRPC]
    public void ActivateTagger()
    {
        Debug.Log("Tagger is" + gameController.tagger);
        gameController.tagger.gameObject.GetComponentInChildren<MeshRenderer>().material.color = Color.red;
    }
}

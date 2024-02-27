using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Unity.VisualScripting;

public class TagOthers : MonoBehaviour
{
    [Header("References")]
    public GameController controller;
    public PlayerMovement playerMovement;

    void Start()
    {
        controller = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
    }
    void Update()
    {
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.tag == "Player")
        {
            if (controller.tagger = playerMovement.gameObject)
            {
                controller.tagger = collision.collider.gameObject;
                collision.collider.gameObject.GetComponentInChildren<MeshRenderer>().material.color = Color.red;
                controller.myPV.RPC("collision.collider.gameObject.GetComponentInChildren<MeshRenderer>().material.color", RpcTarget.All);
                Debug.Log("You Tagged A Player");
            }
        }
    }   
}

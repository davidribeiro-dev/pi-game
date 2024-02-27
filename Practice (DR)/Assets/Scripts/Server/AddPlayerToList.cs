using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AddPlayerToList : MonoBehaviour
{
    public GameController controller;
    public GameObject self;
    void Start()
    {
        self = GameObject.FindGameObjectWithTag("Player");
        controller = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        controller.PlayerList.Add(self);
    }
}

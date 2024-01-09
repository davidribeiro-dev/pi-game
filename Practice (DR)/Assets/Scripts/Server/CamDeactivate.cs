using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CamDeactivate : MonoBehaviourPunCallbacks
{
    
    void Start()
    {
        if (!photonView.IsMine)
        {
            gameObject.SetActive(false);
        }
    }

}

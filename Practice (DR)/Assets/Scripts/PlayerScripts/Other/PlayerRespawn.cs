using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    private KeyCode escape = KeyCode.Escape;
    void Update()
    {
        if (Input.GetKeyDown(escape))
        {
            player.position = new Vector3(0f, 2.5f, 0f);
        }
    }
}

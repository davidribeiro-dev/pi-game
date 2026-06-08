using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TrapScript : MonoBehaviour
{
    [Header("References")]
    public Collider trapOneCollider;
    public Collider trapTwoCollider;
    private bool colliderOne;
    private bool colliderTwo;

    public float maxTrapTime;
    public float trapTimer;
    void Update()
    {
        if (trapTimer > 0)
            trapTimer -= Time.deltaTime;

        if (trapTimer <= 0)
        {
            trapTimer = maxTrapTime;

            if (trapTwoCollider.enabled)
            {
                TrapOneActivate();
            }
            else
            {
                TrapTwoActivate();
            }
        }
    }

    private void TrapOneActivate()
    {
        trapOneCollider.enabled = true;
        trapTwoCollider.enabled = false;
    }
    private void TrapTwoActivate()
    {
        trapOneCollider.enabled = false;
        trapTwoCollider.enabled = true;
    }
}

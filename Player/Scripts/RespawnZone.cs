using System;
using System.Collections;
using System.Collections.Generic;
using Foundry;
using UnityEngine;

public class RespawnZone : MonoBehaviour
{
    [Header("RespawnPoint")] 
    public Transform respawnPoint;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player))
            player.TeleportLook(respawnPoint.position, respawnPoint.forward, respawnPoint.up);
    }
}

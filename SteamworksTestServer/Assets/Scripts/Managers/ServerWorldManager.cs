using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerWorldManager : MonoBehaviour
{
    [Header("Settings")]
    public float Tick_HZ = 60f;

    float accumulatedTime = 0f;

    private void Awake()
    {
        
    }
}

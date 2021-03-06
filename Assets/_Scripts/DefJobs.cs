﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

public class DefJobs : MonoBehaviour
{
    public DefPlayer myDefPlayer;
    internal float zoneSize;
    internal bool isZone;
    internal bool isDeepDefender;
    [SerializeField] internal bool isRusher;
    private Color zoneColor;
    [HideInInspector] public Vector3 zoneCenter;
    public bool isPress;
    SphereCollider sphereCollider;
    //todo, these should be lists with assignable values in inspector
    [System.Serializable]
    public enum ZoneType
    {
        Man, Flat, Seam, DeepHalf, DeepThird, Curl, Hook, Spy, Rush
    }
    public ZoneType type;

    // Use this for initialization
    void Start()
    {
        ZoneTypeSwitch();
        zoneCenter = transform.position;
        sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
    }
   
    private void ZoneTypeSwitch()
    {
        //todo I would love to do some kind like fill in the box math here
        switch (type)
        {
            case ZoneType.Curl:
                zoneColor = Color.yellow;
                zoneSize = 5;
                isZone = true;
                break;
            case ZoneType.Hook:
                zoneColor = new Color(253, 5, 5);
                zoneSize = 5;
                isZone = true;
                break;
            case ZoneType.DeepHalf:
                zoneColor = Color.blue;
                zoneSize = 10;
                isZone = true;
                isDeepDefender = true;
                break;
            case ZoneType.DeepThird:
                zoneColor = Color.white;
                zoneSize = 9;
                isZone = true;
                isDeepDefender = true;
                break;
            case ZoneType.Flat:
                zoneColor = Color.cyan;
                zoneSize = 6;
                isZone = true;
                break;
            case ZoneType.Seam:
                zoneColor = Color.green;
                zoneSize = 6;
                isZone = true;
                break;
            case ZoneType.Rush:
                zoneColor = Color.red;
                zoneSize = 1;
                isZone = true;
                isRusher = true;
                break;
            case ZoneType.Spy:
                zoneColor = Color.gray;
                zoneSize = 1;
                break;
            case ZoneType.Man:
                zoneColor = Color.black;
                zoneSize = 1;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        sphereCollider.radius = zoneSize;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = zoneColor;
        Gizmos.DrawWireSphere(transform.position, zoneSize);
    }

    public void SetDefPlayer(DefPlayer defPlayer)
    {
        myDefPlayer = defPlayer;
    }

}


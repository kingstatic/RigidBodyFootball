﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    GameObject player;
	// Use this for initialization
	void Start () {
        player = GameObject.FindGameObjectWithTag("Player");
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = player.transform.position;
   
	}
    public void ResetPlayer()
    { 
        player = null;
        player = GameObject.FindGameObjectWithTag("Player");
    }
}
﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


public class Dline : DefPlayer
{

   
    // Start is called before the first frame update
    internal override void Start()
    {
        base.Start();
        rayColor = Color.red;
        gameManager.hikeTheBall += HikeTheBall;
    }


   

    // Update is called once per frame
    void Update()
    {
        if (!gameManager.isHiked) return;

        if (targetPlayer == null)
        {
            targetPlayer = GameObject.FindGameObjectWithTag("Player").transform;
        }
        SetTargetPlayer(targetPlayer);

        if (wasBlocked && !isBlocked)
            StartCoroutine("BlockCoolDown"); 
    }

    public override void FixedUpdate()
    {
       base.FixedUpdate();  
    }


    public void HikeTheBall(bool wasHiked)
    {
        anim.SetTrigger("HikeTrigger");
    }
    
   

   

}
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TE : OffPlayer
{
    // Start is called before the first frame update
    internal override void Start()
    {
        base.Start();
        AddClickCollider();
        rayColor = Color.green;
        gameManager.shedBlock += DefShedBlock;
    }

    // Update is called once per frame
    internal override void Update()
    {
        base.Update();
        if (!gameManager.isHiked)
        {
            return;
        }

        if (gameManager.WhoHasBall() == this) return;
        if (isCatching) return;
        if (gameManager.isRun)
        {
            if (gameManager.ballOwner == this)
            {
                transform.forward = Vector3.forward;
                navMeshAgent.enabled = false;
                return;
            }
            canBlock = true;
            BlockProtection();
        }

        if (gameManager.isPass)
        {
            if (isBlocker)
            {
                BlockProtection();
                return;
            }

            if (!isReciever) return;

            if (wasAtLastCut)
            {
                WatchQb();
                //Debug.Log("last cut");
                return;
            }


            if (IsEndOfRoute())
            {
                StopNavMeshAgent();
                Debug.Log("EndOfRoute Update");
                return;
            }
            else RunRoute();


        }
    }

    
}

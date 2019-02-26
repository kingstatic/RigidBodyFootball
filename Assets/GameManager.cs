﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public bool isRun = false;
    public bool isPass = false;
    public bool isHiked = false;
    public Object BallOwner;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(BallOwner != null)
        {

        }
    }
    public void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    //TODO add hike system
    public void Hike() 
    {
        isHiked = true;
        BallOwner = FindObjectOfType<QB>();
    }
    public void PassPlay()
    {
        Debug.Log("Pass Play");
        isPass = true;
        isRun = false;
    }
    public void RunPlay()
    {
        Debug.Log("Run Play");
        isRun = true;
        isPass = false;
    }
}
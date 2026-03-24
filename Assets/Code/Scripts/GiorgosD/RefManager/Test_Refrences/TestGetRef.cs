using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestGetRef : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(RefrenceManager.Instance.GamePlayRef.TestRef.GetPosition());
    }
}

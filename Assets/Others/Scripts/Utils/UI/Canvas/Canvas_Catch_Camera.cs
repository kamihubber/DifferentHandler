using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Canvas_Catch_Camera : MonoBehaviour
{
    private Canvas _canvas;

    private void Start()
    {
        _canvas = GetComponent<Canvas>();
        findCamera();
        Invoke("findCamera",.2f);
    }

    void findCamera()
    {
        if (_canvas.worldCamera == null)
            _canvas.worldCamera = Camera.main;
    }
}
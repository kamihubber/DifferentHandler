using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Ui_Animation : MonoBehaviour
{
    [Range(.01f, .5f)] public float pressure = .01f;
    [Range(.1f, 1)] public float duration = .5f;

    private Transform _transform;
    private Vector3 _scale = new Vector3(-1, -1, -1);

    private void Start()
    {
        _transform = transform;
    }

    public void OnDown()
    {
        if (_scale.x == -1)
            _scale = _transform.localScale;
        
        _transform.DOScale(_scale.x - pressure, duration);
    }

    public void OnUp()
    {
        //_transform.DOScale(_scale.x + pressure, duration);
        _transform.DOScale(_scale.x, duration);
    }
}
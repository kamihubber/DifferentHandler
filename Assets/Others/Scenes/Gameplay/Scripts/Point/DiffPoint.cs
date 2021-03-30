using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]

public class DiffPoint : MonoBehaviour
{
    public bool answered = false;
    private SpriteRenderer thisPoint;
    public SpriteRenderer anotherPoint;
    public float scaleUpAnimationSpeed = 1;
    private Vector3 _scale;

    private void Start()
    {
        _scale = transform.localScale;
        thisPoint = GetComponent<SpriteRenderer>();

#if UNITY_EDITOR
        if (!SceneManager.GetActiveScene().name.Equals("Gameplay"))
            scaleUpAnimationSpeed = 0;        
#endif
    }

    private void OnMouseDown()
    {
        //if(!GameplayManager.getInstance.playing) return;
        //if(StateChecker.getInstance.isAnyUiOpen) return;

        if (!thisPoint)
            thisPoint = transform.GetComponent<SpriteRenderer>();
        
        OnClick();
    }

    public void OnClick()
    {
        if (answered) return;

        answered = true;
        anotherPoint.GetComponent<DiffPoint>().answered = true;

        //play animation
        thisPoint.DOFade(.01f, 0);
        thisPoint.DOFade(1f, scaleUpAnimationSpeed);
        thisPoint.transform.DOScale(0, 0);
        thisPoint.transform.DOScale(_scale, scaleUpAnimationSpeed);
        
        anotherPoint.DOFade(.01f, 0);
        anotherPoint.DOFade(1f, scaleUpAnimationSpeed);
        anotherPoint.transform.DOScale(0, 0);
        anotherPoint.transform.DOScale(_scale, scaleUpAnimationSpeed);

        try
        {
            //GameplayManager.getInstance.addUserAnswer();
        }
        catch (Exception e)
        {
        }
        
        
        try
        {
            //GetComponent<AudioSource>().Play();
        }
        catch (Exception e)
        {
        }
    }


    #region ---

    //km    
    [HideInInspector]
    public bool Transparent = true;
    [HideInInspector]
    public Color color;

    [Header("Images")]
    public Sprite TransparentImage;
    public Sprite FillImage;


    private void Update()
    {
        if (Transparent)
        {
            //color.a = 0;
            gameObject.GetComponent<SpriteRenderer>().color = color;
            gameObject.GetComponent<SpriteRenderer>().sprite = TransparentImage;
        }
        else
        {
            //color.a = 1;
            gameObject.GetComponent<SpriteRenderer>().color = color;
            gameObject.GetComponent<SpriteRenderer>().sprite = FillImage;
        }
        
    }       

    //

    #endregion
}
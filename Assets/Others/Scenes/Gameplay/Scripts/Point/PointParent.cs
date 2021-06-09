using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using RTLTMPro;
using TMPro;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class PointParent : MonoBehaviour
{
    [HideInInspector] public int id = 0;
    public string type = "";
    [Header("Scene")] [Range(.1f, 3)] public float scaleX = 1f;
    [Range(.1f, 4)] public float scaleY = 1f;
    [Range(0, 359)] public int rotation = 0;


    public int Difficulty
    {
        get
        {
            return BaseDifficulty + ExtraDifficulty;
        }
        set
        {
            difficulty = value;
            BaseDifficulty = (difficulty > 10) ? 10 : difficulty;
            ExtraDifficulty = (difficulty > 10) ? (difficulty - 10) : 0;
        }
    }
    private int difficulty;    
    [Header("Properties")] [Range(1, 10)] public int BaseDifficulty = 1;
    [Range(0, 10)] public int ExtraDifficulty = 0;

    [Header("Debug")] public Color color = new Color(1, 1, 1, .5f);

    public bool showDifficulyLabel = false;
    public Transform difficultyCanvas;
    [HideInInspector] public List<DiffPoint> points = new List<DiffPoint>();


    [HideInInspector] public bool HideOtherPoints = true;
    [Header("Images")]
    public bool Transparent = true;
    public DiffPoint PointA;
    public DiffPoint PointB;

    private void OnDestroy()
    {
//#if Unity_Editor
        //call a method on pointtool(which holds me!) to set overall currentdifficulty label text.
        //since pointtool currently can not do anything in edit mode
        if (gameObject.GetComponentInParent<PointTool>() != null)
            gameObject.GetComponentInParent<PointTool>().set_difficulty_label(1);
//#endif
    }



    private void Update()
    {

        if (BaseDifficulty < 10) ExtraDifficulty = 0;
//#if Unity_Editor
        //call a method on pointtool(which holds me!) to set overall currentdifficulty label text.
        //since pointtool currently can not do anything in edit mode
        if (gameObject.GetComponentInParent<PointTool>() != null)
            gameObject.GetComponentInParent<PointTool>().set_difficulty_label();
//#endif

        id = Random.Range(1000, 9999);

        Vector3 scale = new Vector3(scaleX, scaleY, 1);
        points[0].transform.localScale = scale;
        points[1].transform.localScale = scale;

        Vector3 rotate = new Vector3(0, 0, rotation);
        points[0].transform.eulerAngles = rotate;
        points[1].transform.eulerAngles = rotate;

        //debug

        //km edit
        //set color
        points[0].color = color;
        points[1].color = color;

        //difficulty label        
        if (showDifficulyLabel)
        {
            difficultyCanvas.gameObject.SetActive(true);
            difficultyCanvas.transform.GetChild(0).GetComponent<RTLTextMeshPro>().text = Difficulty.ToString();

            float difficultyCanvasSize = scaleY;
            if (scaleX < scaleY) difficultyCanvasSize = scaleX;

            difficultyCanvas.transform.localScale = new Vector3(difficultyCanvasSize, difficultyCanvasSize);
        }
        else
        {
            difficultyCanvas.gameObject.SetActive(false);
        }

        //km
        if (Transparent)
        {
            PointA.Transparent = true;
            PointB.Transparent = true;
        }
        else
        {
            PointA.Transparent = false;
            PointB.Transparent = false;
        }
        //
        //

    }

    public void setPointsProperty(string pointAPosition, string pointAScale, string pointBPosition, string pointBScale)
    {
        if (points.Count < 1)
        {
            points.AddRange(gameObject.GetComponentsInChildren<DiffPoint>());

            points[0].anotherPoint = points[1].GetComponent<SpriteRenderer>();
            points[0].GetComponent<SpriteRenderer>().DOFade(0, 0);
            points[1].anotherPoint = points[0].GetComponent<SpriteRenderer>();
            points[1].GetComponent<SpriteRenderer>().DOFade(0, 0);
        }

        points[0].transform.position = StringToVector3(pointAPosition);
        //points[0].transform.localScale = StringToVector3(pointAScale);
        points[1].transform.position = StringToVector3(pointBPosition);
        //points[1].transform.localScale = StringToVector3(pointBScale);
    }


    public static Vector3 StringToVector3(string sVector)
    {
        sVector = sVector.Replace("\"", "");

        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

#region       

    void OnValidate()
    {       
        //Debug.Log("");
        if (gameObject.GetComponentInParent<PointTool>() != null)
            HideOtherPoints = gameObject.GetComponentInParent<PointTool>().HideOthers;
    }
    
//#if Unity_Editor
    void OnEnable()
    {        
        Selection.selectionChanged += EditorSelectionChanged;
    }

    void OnDisable()
    {
        Selection.selectionChanged -= EditorSelectionChanged;
    }

    

    private void EditorSelectionChanged()
    {
        //if we should hide ourself when not selected...this is set by my gameobject parent pointtool script 
        if (HideOtherPoints && Selection.activeGameObject != null)
        {
            if (Selection.activeGameObject == gameObject)
            {

                /*PointParent[] tests = FindObjectsOfType(typeof(PointParent)) as PointParent[];
                foreach (var t in tests)
                {
                    if (t.gameObject != gameObject) t.gameObject.SetActive(false);
                }*/
                PointA.gameObject.SetActive(true);
                PointB.gameObject.SetActive(true);
            }
            else
            {
                //if i am not selected and the selected one( neo! ;) ) has pointparent script so it is a point prefab(in fact i should check for the prefab type)
                if (Selection.activeGameObject.GetComponent<PointParent>() != null)
                {
                    PointA.gameObject.SetActive(false);
                    PointB.gameObject.SetActive(false);
                }
                //if the selected one is not a point then show us all
                else
                {
                    PointA.gameObject.SetActive(true);
                    PointB.gameObject.SetActive(true);
                }
            }
        }

        //Debug.Log("asdasd");
    }
    //
//#endif

#endregion
}
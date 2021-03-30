using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Boomlagoon.JSON;
using ICSharpCode.SharpZipLib.Zip;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using GolbaharApiClient;


public class DebugInfoPage : MonoBehaviour
{
    [Header("...")]
    public GridLayoutGroup InfoGridGroup;
    public Button CloseBtn;

    List<GameObject> GridTexts = new List<GameObject>();

    JSONObject info = new JSONObject();

    private async void Start()
    {       

        InfoGridGroup.cellSize = new Vector2(Screen.width / 8 - 5, Screen.height / 8 - 5);
    }

    private async void OnEnable()
    {
        try
        {
            //fetch from server
            LoadingManager.getInstance.show();

            //GolbaharSandBoxApiClient.ImageEdit golbahar_ImageEdit = new GolbaharSandBoxApiClient.ImageEdit();
            ImageEdit golbahar_ImageEdit = new ImageEdit();

            info = JSONObject.Parse((await golbahar_ImageEdit.GetImagesCountInfo(PlayerPrefs.GetString("Token"))).Value);

            //column headres
            for (var k = 0; k < 8; k++)
            {
                var textobj = new GameObject();
                var txtmp = textobj.AddComponent<RTLTextMeshPro>();
                txtmp.enableAutoSizing = true;
                txtmp.isRightToLeftText = true;
                txtmp.fontSizeMax = 28;

                if (k == 0) textobj.SetActive(false);
                else if (k == 1) txtmp.text = "همه تفاوت ها ";
                else txtmp.text = (k - 1) + 4 + "تفاوت";

                if (k == 0)
                    CloseBtn.transform.parent = InfoGridGroup.gameObject.GetComponent<RectTransform>();
                else
                {
                    txtmp.transform.parent = InfoGridGroup.gameObject.GetComponent<RectTransform>();
                    txtmp.transform.localScale = new Vector3(1, 1, 1);
                }

                GridTexts.Add(textobj);

            }

            var x = info["stateAll"];
            var y = x.Obj["AllPointsCount"];
            var w = info["stateAll"].Obj["AllPointsCount"];           

            //rows              
            //i , each state
            for (var i = -1; i < 7; i++)
            {
                //k , differences
                for (var j = -2; j < 6; j++)
                {
                    var textobj = new GameObject();
                    var txtmp = textobj.AddComponent<RTLTextMeshPro>();
                    txtmp.enableAutoSizing = true;
                    txtmp.isRightToLeftText = true;

                    if (j == -2)
                    {
                        if (i == -1) txtmp.text = "همه ی حالت ها";
                        else txtmp.text = "State " + i;
                    }
                    else
                    {
                        txtmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
                        if (i == -1)
                        {
                            if (j == -1)
                                txtmp.text = info["stateAll"].Obj["AllPointsCount"].ToString();
                            //dont need 0 , 1 , 2...4 pointscount,so start from 5 pointscount(5 offset)
                            else
                                txtmp.text = info["stateAll"].Obj["PointsCount" + (j + 5)].ToString();
                        }
                        else
                        {
                            if (j == -1)
                                txtmp.text = info["state" + i].Obj["AllPointsCount"].ToString();
                            //dont need 0 , 1 , 2...4 pointscount,so start from 5 pointscount(5 offset)
                            else
                                txtmp.text = info["state" + i].Obj["PointsCount" + (j + 5)].ToString();
                        }
                    }

                    txtmp.transform.parent = InfoGridGroup.gameObject.GetComponent<RectTransform>();
                    txtmp.transform.localScale = new Vector3(1, 1, 1);

                    GridTexts.Add(textobj);
                }
            }

            
        }
        catch(Exception ex)
        {

        }

        LoadingManager.getInstance.hide();

    }

    public void ShowInfoPage()
    {
        gameObject.SetActive(true);
    }

    public void HideInfoPage()
    {
        foreach (GameObject obj in GridTexts)
          Destroy(obj);
        gameObject.SetActive(false);
    }


}
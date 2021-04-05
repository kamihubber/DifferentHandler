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
using GolbaharSandBoxApiClient;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Assets.Others.Scenes.DifferentHandller
{
    //[ExecuteInEditMode]
    class EditManager : MonoBehaviour
    {

        //download zip file
        private string imageHostUrl = "https://find-differences.ir/5_diffrences";
        private string jsonZipUrl = "https://golsaar.ir/5_diffrences/jsons.zip";
        private string jsonDirectory = "";
        private string jsonZipPath = "";

        #region getInstance

        public static EditManager getInstance;

        private void OnEnable()
        {
            getInstance = this;
        }

        #endregion
        [Header("Points")] public GameObject circlePrefab;
        public GameObject quadPrefab;
        public List<Transform> pointsList = new List<Transform>();        
        public PointTool Points;
        [Header("UI")] public GameObject Loading;
        [Header("Json")] public string image_jsons_tring;

        List<string> jsonsId = new List<string>();
        List<JSONObject> jsons = new List<JSONObject>();

        ImageEdit sandbox_webapi_imagedit = new ImageEdit();
        

        private async  void Start()
        {
            jsonDirectory = Application.persistentDataPath + "/jsons";
            jsonZipPath = Application.persistentDataPath + "/jsons.zip";

            Loading.SetActive(true);

            if (Points.edit)
            {
                LoadingManager.getInstance.hide();

                if (string.IsNullOrEmpty(image_jsons_tring))
                {
                    addpoints(await getimagejson(Points.filename + Points.filetype.Replace("." + Points.filetype.Split('.')[Points.filetype.Split('.').Length - 1], "")));                    
                }
                else
                {
                    addpoints(JSONObject.Parse(image_jsons_tring));
                }
            }
            else LoadingManager.getInstance.hide();

        }        

        void prepare_for_refrsh()
        {            
            //show loading ui
            LoadingManager.getInstance.show();

            //clear points
            foreach (Transform point in pointsList)
            {
                try
                {
                    Destroy(point.gameObject);
                }
                catch (Exception e)
                {
                }
            }

            pointsList.Clear();

            
        }                        
        async Task<JSONObject> getimagejson(string imagename)
        {
            JSONObject result = new JSONObject();

            var get_image_result = await sandbox_webapi_imagedit.GetEditImageJson(imagename, PlayerPrefs.GetString("Token"));

            if (get_image_result.Status == "Success")
            {
                result = JSONObject.Parse(get_image_result.Value);
            }    
            else
            {
                Points.loadingUi.SetActive(true);
                Points.loadingUi.transform.Find("ResultMessage").GetComponent<RTLTMPro.RTLTextMeshPro>().text = "داده ای برای ویرایش وجود ندارد!";
            }
            
            return result;
        }
        
        void addpoints(JSONObject json)
        {                       
            Debug.Log(json);

            Points.addeditpoints(json,circlePrefab,quadPrefab);            
        }       
        
    }
}

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


namespace Assets.Others.Scenes.DifferentHandller
{
    //[ExecuteInEditMode]
    class EditManager : MonoBehaviour
    {

        //download zip file
        private string imageHostUrl = "https://golsaar.ir/5_diffrences";
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
        

        private void Start()
        {
            jsonDirectory = Application.persistentDataPath + "/jsons";
            jsonZipPath = Application.persistentDataPath + "/jsons.zip";

            Loading.SetActive(true);

            if (Points.edit)
            {
                if (Points.refreshJsons)
                    //download zip file
                    startDownloadZip();
                else
                {
                    LoadingManager.getInstance.hide();

                    if (string.IsNullOrEmpty(image_jsons_tring))
                    {
                        loadJsons();
                        //StartCoroutine(ExtractZipFile(System.IO.File.ReadAllBytes(jsonZipPath), jsonDirectory + "/"));
                        addpoints(getimagejson(Points.filename + Points.filetype.Replace("." + Points.filetype.Split('.')[Points.filetype.Split('.').Length - 1], "")));
                    }
                    else
                    {
                        addpoints(JSONObject.Parse(image_jsons_tring));
                    }                    

                }
            }
            else LoadingManager.getInstance.hide();

        }


        public void startDownloadZip()
        {
            StartCoroutine(downloadjsonZip());
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

        IEnumerator downloadjsonZip()
        {
            prepare_for_refrsh();

            using (UnityWebRequest www = UnityWebRequest.Get(jsonZipUrl))
            {
                yield return www.Send();
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    #region delete old files

                    //delete files from zip directory
                    if (Directory.Exists(jsonDirectory))
                    {
                        string[] files = System.IO.Directory.GetFiles(jsonDirectory);
                        foreach (string file in files)
                            try
                            {
                                if (File.Exists(file))
                                    System.IO.File.Delete(file);
                            }
                            catch (Exception e)
                            {
                            }
                    }

                    //delete directory
                    try
                    {
                        if (Directory.Exists(jsonDirectory))
                            System.IO.Directory.Delete(jsonDirectory);
                    }
                    catch (Exception e)
                    {
                    }

                    //delete zip file
                    try
                    {
                        if (File.Exists(jsonZipPath))
                            System.IO.File.Delete(jsonZipPath);
                    }
                    catch (Exception e)
                    {
                    }

                    #endregion

                    yield return new WaitForSeconds(0.2f);

                    System.IO.File.WriteAllBytes(jsonZipPath, www.downloadHandler.data);

                    if (www.downloadHandler.isDone)
                    {
                        LoadingManager.getInstance.hide();

                        StartCoroutine(ExtractZipFile(System.IO.File.ReadAllBytes(jsonZipPath), jsonDirectory + "/"));
                    }
                }
            }
        }

        //extract downloaded file
        public IEnumerator ExtractZipFile(byte[] zipFileData, string targetDirectory, int bufferSize = 256 * 1024)
        {
            LoadingManager.getInstance.show();                        

            jsonsId.Clear();
            jsons.Clear();

            ICSharpCode.SharpZipLib.Zip.ZipConstants.DefaultCodePage = System.Text.Encoding.UTF8.CodePage;

            Directory.CreateDirectory(targetDirectory);

            using (MemoryStream fileStream = new MemoryStream())
            {
                fileStream.Write(zipFileData, 0, zipFileData.Length);
                fileStream.Flush();
                fileStream.Seek(0, SeekOrigin.Begin);

                ZipFile zipFile = new ZipFile(fileStream);

                foreach (ZipEntry entry in zipFile)
                {
                    string targetFile = Path.Combine(targetDirectory, entry.Name);

                    using (FileStream outputFile = File.Create(targetFile))
                    {
                        if (entry.Size > 0)
                        {
                            Stream zippedStream = zipFile.GetInputStream(entry);
                            byte[] dataBuffer = new byte[bufferSize];

                            int readBytes;
                            while ((readBytes = zippedStream.Read(dataBuffer, 0, bufferSize)) > 0)
                            {
                                outputFile.Write(dataBuffer, 0, readBytes);
                                outputFile.Flush();
                                outputFile.Dispose();
                                yield return null;
                            }

                            //process downloaded jsons
                            JSONObject imageJson = JSONObject.Parse(File.ReadAllText(jsonDirectory + "/" + entry.Name));

                            jsons.Add(imageJson);
                            jsonsId.Add(imageJson.GetString("id"));
                        }
                    }
                }
            }

            jsonsId.Sort();
            saveJsons();

            if (Points.edit) addpoints(getimagejson(Points.filename + Points.filetype.Replace("." + Points.filetype.Split('.')[Points.filetype.Split('.').Length - 1],"") ) );

            LoadingManager.getInstance.hide();

        }

        JSONObject getimagejson(string imagename)
        {
            var result = jsons.Find(jso => jso.GetString("id") == imagename);

            if (result != null) return result;
            
            return new JSONObject();
        }
        
        void addpoints(JSONObject json)
        {                       
            Debug.Log(json);

            Points.addeditpoints(json,circlePrefab,quadPrefab);
            
        }

        void saveJsons()
        {
            var i = 0;
            foreach (JSONObject jsno in jsons)
            {
                if (PlayerPrefs.HasKey("editJsn" + i)) PlayerPrefs.DeleteKey("editJsn" + i);
                PlayerPrefs.SetString("editJsn" + i,jsno.ToString());
                PlayerPrefs.SetInt("editJsnsCount",i + 1);
                PlayerPrefs.Save();
                i++;
            }
        }

        void loadJsons()
        {
            jsons.Clear();

            var count = PlayerPrefs.GetInt("editJsnsCount");

            var i = 0;
            for (i=0;i<count;i++)
            {
                jsons.Add(JSONObject.Parse( PlayerPrefs.GetString("editJsn" + i) ));
            }
        }

        
    }
}

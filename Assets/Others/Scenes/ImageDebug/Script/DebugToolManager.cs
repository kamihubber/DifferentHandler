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
using Assets.Helpers;
using GolbaharSandBoxApiClient;
using System.Threading.Tasks;
using static GolbaharSandBoxApiClient.ImageEdit;
using UnityEngine.SceneManagement;

public class DebugToolManager : MonoBehaviour
{
    //km
    //count of images in each page of imagelist,blanckimage included,only used with non-database method
    int EachPageImagesCount = 6;
    int CurrentState = 0;
    //0 = difficultty , 1 = name
    int SortType = 0;
    int CurrentDiffsCountRequested = -1;
    int CurrentPage = 1;

    //database
    //count of images should be fetched from database,blanckimage not included,only used with database method
    int EachPageImagesCount_DB = 5;
    ImageStateNames CurrentState_DB = ImageStateNames.Disabled;
    ImagesOrderType SortType_DB = 0;
    int CurrentDiffsCountRequested_DB = -1;
    int CurrentPage_DB = 1;

    List<KeyValuePair<int, List<JSONObject>>> BrowsedPages = new List<KeyValuePair<int, List<JSONObject>>>();
    //

    List<string> jsonsId = new List<string>();
    List<JSONObject> jsons = new List<JSONObject>();

    public List<List<KeyValuePair<int, int>>> InfoPageData
    {
        get
        {
            infopagedata.Clear();

            //each state
            for (var i = 0; i <= 7; i++)
            {
                // differences list for one state
                var differenceslist = new List<KeyValuePair<int, int>>();

                for (var j = 0; j < 7; j++)
                {
                    var keyvalue = new KeyValuePair<int, int>();

                    //all states
                    if (i == 0)
                    {
                        //all images
                        if (j == 0)
                        {
                            keyvalue = new KeyValuePair<int, int>(-1, jsons.FindAll(jsnobj => jsnobj == jsnobj).Count);
                        }
                        //images with this much differences count
                        else keyvalue = new KeyValuePair<int, int>(j - 1 + 5, jsons.FindAll(jsnobj => (int)(jsnobj.GetNumber("points_count")) == j - 1 + 5).Count);

                    }
                    //this state
                    else
                    {
                        //all differences count
                        if (j == 0)
                        {
                            keyvalue = new KeyValuePair<int, int>(-1, jsons.FindAll(jsnobj => (int)(jsnobj.GetNumber("state")) == i).Count);
                        }
                        //images with this state and differences count
                        else keyvalue = new KeyValuePair<int, int>(j - 1 + 5, jsons.FindAll(jsnobj => (int)(jsnobj.GetNumber("state")) == i && (int)(jsnobj.GetNumber("points_count")) == j - 1 + 5).Count);
                    }



                    differenceslist.Add(keyvalue);
                }

                infopagedata.Add(differenceslist);
            }


            return infopagedata;
        }
    }

    private List<List<KeyValuePair<int, int>>> infopagedata = new List<List<KeyValuePair<int, int>>>();
    //
    #region getInstance

    public static DebugToolManager getInstance;

    private void OnEnable()
    {
        getInstance = this;
    }

    #endregion

    private async void Start()
    {
        jsonDirectory = Application.persistentDataPath + "/jsons";
        jsonZipPath = Application.persistentDataPath + "/jsons.zip";
        SinglejsonPath = Application.persistentDataPath + "/images_array_json.json";

        LoadingManager.getInstance.show();

        //download zip file
        if (database)
        {
            //initial load from database

            //fetch zero state images count
            var countresult = (await golbahar_imageedit.GetStateImagesCount(0, PlayerPrefs.GetString("Token")));

            if (countresult.Status == "Success")
              CurrentStateImagesCountText.text = countresult.Value;

            //then get images
            var jsonslist = await FetchImagesFromDB(CurrentState_DB, 1, EachPageImagesCount_DB, PlayerPrefs.GetString("Token"));
            await UpdateImageList(jsonslist);
        }
        else if (SingleJson) startDownloadSingleJson();        
        else startDownloadZip(); 


        //km , set the size of imagelist settings panel same as imgelistitems
        //SettingsPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width / 3, Screen.height / 2);
        SettingsPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width / 3);
        SettingsPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height / 2);

    }

    [Header("UI")] public GameObject canvasNoMoreImages;
    public GameObject btnLoadImages;
    public RTLTextMeshPro txtImageId;
    public RTLTextMeshPro txtImageDifficulty;

    //download zip file
    private string imageHostUrl = "https://find-differences.ir/5_diffrences";
    private string jsonZipUrl = "https://golsaar.ir/5_diffrences/jsons.zip";
    private string jsonDirectory = "";
    private string jsonZipPath = "";
    public List<string> imageJsonList = new List<string>();

    [Header("Server")]
    public bool SingleJson;
    public bool database;

    private string SingleJsonUrl = "https://find-differences.ir/5_diffrences/images_array_json.json";
    private string SinglejsonPath = "";

    //GolbaharSandBoxApiClient.ImageEdit golbahar_imageedit = new GolbaharSandBoxApiClient.ImageEdit();
    ImageEdit golbahar_imageedit = new ImageEdit();

    public void startDownloadZip()
    {
        StartCoroutine(downloadjsonZip());
    }

    public void startDownloadSingleJson()
    {
        StartCoroutine(downloadsinglejson());
    }
    
    void prepare_for_refrsh()
    {
        btnLoadImages.SetActive(false);
        showDetailsButton.SetActive(false);
        showHidePointsButton.SetActive(false);

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

        //destroy imag list content children
        for (int j = 0; j < imageListContent.transform.childCount; j++)
            Destroy(imageListContent.transform.GetChild(j).gameObject);
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

    IEnumerator downloadsinglejson()
    {
        prepare_for_refrsh();

        using (UnityWebRequest www = UnityWebRequest.Get(SingleJsonUrl))
        {
            yield return www.Send();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                #region delete old files                                

                //delete zip file
                try
                {
                    if (File.Exists(SinglejsonPath))
                        System.IO.File.Delete(SinglejsonPath);
                }
                catch (Exception e)
                {
                }

                #endregion

                yield return new WaitForSeconds(0.2f);

                System.IO.File.WriteAllBytes(SinglejsonPath, www.downloadHandler.data);

                if (www.downloadHandler.isDone)
                {
                    LoadingManager.getInstance.hide();

                    StartCoroutine(ExtractSingleJsonFile());
                }
            }
        }
    }

    //Database
    async Task<List<JSONObject>> FetchImagesFromDB(ImageStateNames state, int pagenumber, int pageimagecount, string token, int pointscount = -1, ImagesOrderType orderby = ImagesOrderType.Difficulty)
    {
        LoadingManager.getInstance.show();

        List<JSONObject> result = new List<JSONObject>();

        try
        {
            if ((int)state == -1) state = ImageStateNames.All;            

            var editimages_result = await golbahar_imageedit.GetEditImagesAsJson_Paged(state, pagenumber, pageimagecount, token, pointscount, orderby);

            var jsonsarray = JSONArray.Parse(editimages_result.Value);

            if ((jsonsarray != null) && (jsonsarray.Length > 0))
            {
                foreach (JSONValue jo in jsonsarray)
                    result.Add(jo.Obj);

                BrowsedPages.Add(new KeyValuePair<int, List<JSONObject>>(pagenumber, result));
            }
        }
        catch(Exception ex)
        {
        }        

        LoadingManager.getInstance.hide();

        return result;
    }    

    async Task UpdateImageList(List<JSONObject> imagesjsons)
    {
        try
        {            

            int index = 0;

            foreach (JSONObject json in imagesjsons)
            {
                //km mod
                if (index % 6 == 0)
                {
                    index++;
                    addBlankCell();
                }

                index++;

                addNewImage(imageHostUrl + json.GetString("image_url"), json);

            }

            //km , add as many  blank images as needed  ,in order to make last block a complete block that has as many images as EachPageImagesCount
            var loopcount = EachPageImagesCount - (index % EachPageImagesCount);
            for (var j = 0; j < loopcount; j++)
            {
                index++;
                addBlankCell();
            }

            var imagesCount = index;

            if (imagesCount > 0)
            {
                int imageHeight = (int)((imageListContent.rect.width / 3) -
                                         (((imageListContent.rect.width / 3) * 1.25f) - (imageListContent.rect.width / 3)));

                if (imagesCount % 3 == 0)
                    imageHeight = imageHeight * (imagesCount / 3);
                else
                    imageHeight = (imageHeight * (imagesCount / 3)) + imageHeight;

                //set image content list height
                int contentHeight = (int)imageHeight;

                //set images size in content list (set grid layout size)
                imageListContent.GetComponent<GridLayoutGroup>().cellSize =
                    new Vector2(Screen.width / 3, Screen.height / 2);
                //new Vector2(Screen.width / 3, Screen.width / 4);

                imageListContent.sizeDelta = new Vector2(0, contentHeight);
            }
            else
            {
                canvasNoMoreImages.SetActive(true);
            }
        }
        catch(Exception ex)
        {
        }        

        LoadingManager.getInstance.hide();
    }

    //


    //extract downloaded file
    public IEnumerator ExtractZipFile(byte[] zipFileData, string targetDirectory, int bufferSize = 256 * 1024)
    {
        LoadingManager.getInstance.show();

        showImageList();

        /*//update image canvas state
        //imageListCanvas.SetActive(true);

        //destroy imag list content children
        for (int j = 0; j < imageListContent.transform.childCount; j++)
            Destroy(imageListContent.transform.GetChild(j).gameObject);*/

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

        CreateImageList();


    }

    public IEnumerator ExtractSingleJsonFile()
    {
        LoadingManager.getInstance.show();

        showImageList();


        jsonsId.Clear();
        jsons.Clear();

        using (MemoryStream fileStream = new MemoryStream())
        {

            FileStream fs = new FileStream(SinglejsonPath, FileMode.Open);

            StreamReader sr = new StreamReader(fs);

            string jsonarraystring = sr.ReadToEnd();                       

            var mylist = JSONArray.Parse(jsonarraystring); 

            foreach (JSONValue jobject in mylist)
            {
                jsons.Add(jobject.Obj);
                jsonsId.Add(jobject.Obj.GetString("id"));
            }


        }

        jsonsId.Sort();

        CreateImageList();

        yield return null;

    }

    void CreateImageList()
    {
        imagesCount = 0;

        var current_state_images_count = 0;

        //sort by image difficulty if sorttype is zero,else use default sorting
        if (SortType == 0) jsons.Sort((JSONObject jsn1, JSONObject jsn2) => jsn1.GetNumber("image_difficulty").CompareTo(jsn2.GetNumber("image_difficulty")));
        else jsons.Sort((JSONObject jsn1, JSONObject jsn2) => jsn1.GetString("id").CompareTo(jsn2.GetString("id")));

        for (var i = 0; i < 1; i++)
        {
            //foreach (string id in jsonsId)
            //{
            foreach (JSONObject json in jsons)
            {
                //if (id.Equals(json.GetString("id")))
                {
                    if (CurrentDiffsCountRequested == -1)
                    {
                        if (((int)json.GetNumber("state") == CurrentState) || (CurrentState == -1))
                        {


                            //km mod
                            if (imagesCount % 6 == 0)
                            {
                                imagesCount++;
                                addBlankCell();
                                //if (imagesCount == 36) break;
                            }

                            imagesCount++;
                            current_state_images_count++;
                            addNewImage(imageHostUrl + json.GetString("image_url"), json);
                            //if (imagesCount == 36) break;
                            //
                        }

                    }
                    else
                    {
                        if ((((int)json.GetNumber("state") == CurrentState) || (CurrentState == -1)) && ((int)json.GetNumber("points_count") == CurrentDiffsCountRequested))
                        {


                            //km mod
                            if (imagesCount % 6 == 0)
                            {
                                imagesCount++;
                                addBlankCell();
                                //if (imagesCount == 36) break;
                            }

                            imagesCount++;
                            current_state_images_count++;
                            addNewImage(imageHostUrl + json.GetString("image_url"), json);
                            //if (imagesCount == 36) break;
                            //
                        }
                    }
                }
            }
            //if (imagesCount == 36) break;
            //}
        }

        //km , add as many  blank images as needed  ,in order to make last block a complete block made up of EachPageImagesCount images
        var loopcount = EachPageImagesCount - (imagesCount % EachPageImagesCount);
        for (var j = 0; j < loopcount; j++)
        {
            imagesCount++;
            addBlankCell();
        }

        //km ,set selected state images count text , state drop down text and reset page number input field
        CurrentStateImagesCountText.text = current_state_images_count.ToString();
        CurrentStateDB.captionText.text = CurrentState.ToString();
        CurrentPage = 1;
        PageNumberInput.text = "1";

        if (imagesCount > 0)
        {
            int imageHeight = (int)((imageListContent.rect.width / 3) -
                                     (((imageListContent.rect.width / 3) * 1.25f) - (imageListContent.rect.width / 3)));

            if (imagesCount % 3 == 0)
                imageHeight = imageHeight * (imagesCount / 3);
            else
                imageHeight = (imageHeight * (imagesCount / 3)) + imageHeight;

            //set image content list height
            int contentHeight = (int)imageHeight;

            //set images size in content list (set grid layout size)
            imageListContent.GetComponent<GridLayoutGroup>().cellSize =
                new Vector2(Screen.width / 3, Screen.height / 2);
            //new Vector2(Screen.width / 3, Screen.width / 4);

            imageListContent.sizeDelta = new Vector2(0, contentHeight);
        }
        else
        {
            canvasNoMoreImages.SetActive(true);
        }

        LoadingManager.getInstance.hide();
    }

    public void showImageList()
    {
        /*btnLoadImages.SetActive(false);
        showDetailsButton.SetActive(false);
        showHidePointsButton.SetActive(false);*/

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

        //update image canvas state
        imageListCanvas.GetComponent<Canvas>().enabled = true;
        //imageListCanvas.SetActive(true);
        //imageListCanvas.transform.localScale = new Vector3(1, 1, 1);

        //km
        //SettingsPanel.SetActive(true);
    }


    public async void SaveImageDetails()
    {
        var stateDDB = (GameObject.Find("UpdateImagesStateDB").GetComponent<TMP_Dropdown>());

        var statevalue = stateDDB.value;

        (GameObject.Find("ImageDetailesSaveBTN").GetComponent<Button>()).interactable = false;

        ImageStateNames state;

        if (statevalue == 0) state = ImageStateNames.All;
        else state = (ImageStateNames)(statevalue - 1);

        var result = await golbahar_imageedit.UpdateImageState(imageJson.GetString("id"),state,PlayerPrefs.GetString("Token"));

        (GameObject.Find("ImageDetailesSaveBTN").GetComponent<Button>()).interactable = true;

    }

    [Header("Images")] public int imagesCount = 0;
    public int downloadedImagesCount = 0;
    public SpriteRenderer imageRenderer;
    public JSONObject imageJson;
    public GameObject imageListItemPrefab;
    public GameObject imageListBlankItemPrefab;
    public GameObject imageListCanvas;
    public RectTransform imageListContent;
    public List<GameObject> imageList = new List<GameObject>();

    //km
    public void addBlankCell()
    {
        GameObject img = Instantiate(imageListBlankItemPrefab);
        img.transform.parent = imageListContent;
        img.transform.localScale = new Vector3(1, 1, 1);
    }
    //

    public void addNewImage(string imageUrl, JSONObject json)
    {
        GameObject img = Instantiate(imageListItemPrefab);
        img.transform.parent = imageListContent;
        img.transform.localScale = new Vector3(1, 1, 1);

        //km
        img.GetComponent<ImageListItemController>().Json = json;
        img.GetComponent<ImageListItemController>().ImageUrl = imageUrl;
        //

        /*StartCoroutine(downloadImage(imageUrl, sprite =>
        {
            try
            {
                img.GetComponent<Image>().sprite = sprite;
                img.GetComponent<Image>().color = Color.white;
                img.transform.GetChild(0).gameObject.SetActive(false);
                img.GetComponent<ImageListItemController>().imageJsonString = json.ToString();
                img.GetComponent<ImageListItemController>().idText.text = json.GetString("id");
                img.GetComponent<ImageListItemController>().imageText.text = json.GetString("image_url");

                downloadedImagesCount++;

                LoadingManager.getInstance.hide();
            }
            catch (Exception e)
            {
            }
        }));*/
    }

    IEnumerator downloadImage(string url, System.Action<Sprite> callback)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);

                callback(null);
            }
            else
            {
                if (www.isDone)
                {
                    var texture = DownloadHandlerTexture.GetContent(www);
                    var rect = new Rect(0, 0, 1400f, 1024f);
                    var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
                    callback(sprite);
                }
            }
        }
    }

    [Header("Points")] public GameObject circlePrefab;
    public GameObject quadPrefab;
    public List<Transform> pointsList = new List<Transform>();

    [Header("Image Details")] public GameObject detailsCanvas;
    public GameObject showDetailsButton;
    public GameObject showHidePointsButton;
    public bool isPointsVisible = true;
    public RTLTextMeshPro detailsText;

    public void showHidePoints()
    {
        isPointsVisible = !isPointsVisible;

        foreach (Transform p in pointsList)
        {
            p.gameObject.SetActive(isPointsVisible);
        }

        if (isPointsVisible)
        {
            showHidePointsButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Hide differences";
        }
        else
        {
            showHidePointsButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Show differences";
        }
    }

    public void updateImage(Sprite sprite, string json)
    {
        imageRenderer.sprite = sprite;
        imageJson = JSONObject.Parse(json);

        //set id and difficulty
        txtImageId.text = "Id : " + imageJson.GetString("id");
        txtImageDifficulty.text = "Difficulty : " + (int)imageJson.GetNumber("image_difficulty");

        Debug.Log(json);

        try
        {
            //generate points
            JSONArray points = imageJson.GetArray("points");
            for (int i = 0; i < points.Length; i++)
            {
                JSONObject point = points[i].Obj;
                string type = point.GetString("type");

                //create object
                PointParent pointObject = null;
                if (type.Equals("circle")) pointObject = Instantiate(circlePrefab).GetComponent<PointParent>();
                if (type.Equals("quad")) pointObject = Instantiate(quadPrefab).GetComponent<PointParent>();

                //km
                pointObject.Transparent = false;
                //

                //Debug.Log("json value : " + point.GetString("position"));
                Vector3 position = stringToVec(point.GetString("position"));
                //Debug.Log("str to vec : " + position.x);

                float x = position.x;
                float y = position.y;
                float z = position.z;

                Vector3 p = new Vector3();
                p.x = x;
                p.y = y;
                p.z = z;

                pointObject.transform.position = p;

                //Debug.Log("point pos  : " + pointObject.transform.position.x);
                //Debug.Log("point pos  : " + pointObject.transform.position);

                //left and right point
                JSONObject leftPoint = point.GetObject("point0");

                //todo: ding 1
                //pointObject.points[0].transform.position = stringToVec(leftPoint.GetString("position"));

                pointObject.difficultyCanvas.position = pointObject.points[0].transform.position;
                pointObject.scaleX = stringToVec(leftPoint.GetString("scale")).x;
                pointObject.scaleY = stringToVec(leftPoint.GetString("scale")).y;
                pointObject.rotation = (int)stringToVec(leftPoint.GetString("rotation")).z;

                JSONObject rightPoint = point.GetObject("point1");

                //todo: ding 2
                //pointObject.points[1].transform.position = stringToVec(rightPoint.GetString("position"));

                //set difficulty
                pointObject.showDifficulyLabel = true;
                pointObject.Difficulty = (int)point.GetNumber("difficulty");

                Color pointColor = Color.white;
                switch (Random.Range(1, 5))
                {
                    case 1:
                        pointColor = Color.green;
                        break;
                    case 2:
                        pointColor = Color.magenta;
                        break;
                    case 3:
                        pointColor = Color.red;
                        break;
                    case 4:
                        pointColor = Color.yellow;
                        break;
                }

                pointColor.a = .5f;

                pointObject.color = pointColor;

                pointsList.Add(pointObject.transform);
            }
        }
        catch (Exception e)
        {
        }

        //imageListCanvas.SetActive(false);        
        //imageListCanvas.transform.localScale = new Vector3(0,0,0);
        //var panelrenderer = imageListCanvas.transform.Find("Panel").GetComponent<CanvasRenderer>();
        //panelrenderer.SetAlpha(0);
        imageListCanvas.GetComponent<Canvas>().enabled = false;

        showDetailsButton.SetActive(true);
        showHidePointsButton.SetActive(true);
        btnLoadImages.SetActive(true);
    }

    public Vector3 stringToVec(string s)
    {
        //Debug.Log("------------------------------------------------------");
        string[] temp = s.Substring(1, s.Length - 2).Split(',');

        //Debug.Log("t  : " + temp[0]);

        float tf = float.Parse(temp[0]);

        //Debug.Log("tf : " + tf.ToString("F3"));

        Vector3 result = new Vector3(float.Parse(temp[0]), float.Parse(temp[1]), float.Parse(temp[2]));

        //result.Set(tf,0,0);

        //Debug.Log("r  : " + result);
        //Debug.Log("rx : " + result.x);
        //Debug.Log("rx : " + result.x.ToString("F3"));

        //Debug.Log(s + " > " + new Vector3(float.Parse(temp[0]), float.Parse(temp[1]), float.Parse(temp[2])));
        return result;
    }


    public void showImageDetails()
    {
        string details = "";
        details += "آی دی : " + imageJson.GetString("id") + "\n";
        details += "آدرس تصویر : " + imageJson.GetString("image_url") + "\n";
        details += "سختی تصویر : " + imageJson.GetNumber("image_difficulty") + "/100" + "\n";

        //tags
        string tags = "";

        if (database)
        {
            var tag_list_string_value = imageJson.GetValue("image_tags").ToString();
            if (!string.IsNullOrEmpty(tag_list_string_value))
            {
                tag_list_string_value = tag_list_string_value.ToString().Replace("[","").Replace("]","").Replace("\"","");
                var tagslist = tag_list_string_value.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in tagslist)
                    tags += s + "-";
            }            
        }
        else
        {
            JSONArray tagsArray = imageJson.GetArray("image_tags");
            for (int i = 0; i < tagsArray.Length; i++)
            {
                tags += tagsArray[i].Obj.GetString("tag");
                if (i <= tagsArray.Length)
                    tags += "-";
            }

            tags = tags.Substring(0, tags.Length - 1);            
        }

        details += "تگ : " + tags + "\n";

        DateTime dt = DateTime.Parse(imageJson.GetString("creation_datetime"));

        details += "زمان ساخت : " + dt.ToString("G") + "\n";
        details += "تعداد اختلافات : " + imageJson.GetNumber("points_count") + "\n";

        detailsText.text = details;


        detailsCanvas.SetActive(true);
    }

    public void hideImageDetails()
    {
        detailsText.text = "";

        detailsCanvas.SetActive(false);
    }

    //km//
    public void ImagesStateDB_OnValueChanged(int value)
    {
        //"All" is first item with index 0 , we set it to -1 , so that CurrentState begins from zero 
        CurrentState = value - 1;
        CurrentStateImagesCountText.text = "...";
        StartCoroutine(SetImageStateDBTextCoroutine());
        //CurrentStateDB.captionText.text = CurrentState.ToString();
        //startDownloadZip();
    }

    IEnumerator SetImageStateDBTextCoroutine()
    {
        yield return null;
        if (CurrentState == -1)
            CurrentStateDB.captionText.text = "All";
        else
            CurrentStateDB.captionText.text = CurrentState.ToString();
    }

    public void UpdateImagesStateDB_OnValueChanged(int value)
    {        
        StartCoroutine(UpdateImagesStateDBTextCoroutine());        
    }

    IEnumerator UpdateImagesStateDBTextCoroutine()
    {        
        yield return null;
        var _value = GameObject.Find("UpdateImagesStateDB").GetComponent<TMP_Dropdown>().value.ToString();

        if (_value == (0).ToString())
          GameObject.Find("UpdateImagesStateDB").GetComponent<TMP_Dropdown>().captionText.text = "All";
        else
           GameObject.Find("UpdateImagesStateDB").GetComponent<TMP_Dropdown>().captionText.text = (Convert.ToInt32(_value) - 1).ToString();
    }

    public void SortType_OnValueChanged(int value)
    {
        if (database)
        {
            SortType = value;
            StartCoroutine(SetSortTypeDBTextCoroutine());
        }
        else
        {
            //
            SortType = value;
            StartCoroutine(SetSortTypeDBTextCoroutine());
            //search offline
            prepare_for_refrsh();
            CreateImageList();
        }        
    }    

    IEnumerator SetSortTypeDBTextCoroutine()
    {
        yield return null;
        switch (SortType)
        {
            case 0:
                SortTypeDB.captionText.text = "سختی";
                break;

            case 1:
                SortTypeDB.captionText.text = "نام";
                break;
        }
    }

    public void DiffCountsInput_OnValueChanged(string value)
    {
        CurrentStateImagesCountText.text = "...";
        if ((value == null) || (value == ""))
            CurrentDiffsCountRequested = -1;
        else CurrentDiffsCountRequested = Convert.ToInt32(value);
    }

    public async void ReportsPageBTN_Clicked()
    {
        SceneManager.LoadSceneAsync("Reports");
    }

    public async void SearchBtn_Clicked()
    {
        if (database)
        {
            //database
            ////currentstate -1 means all
            if (CurrentState == -1)
                CurrentState_DB = ImageStateNames.All;
            else
                CurrentState_DB = (ImageStateNames)CurrentState;

            CurrentDiffsCountRequested_DB = CurrentDiffsCountRequested;
            CurrentPage = CurrentPage_DB = 1;
            PageNumberInput.text = "1";
            SortType_DB = (ImagesOrderType)SortType;

            BrowsedPages.Clear();

            LoadingManager.getInstance.show();

            prepare_for_refrsh();

            //fetch this state images count
            var countresult = (await golbahar_imageedit.GetStateImagesCount(CurrentState_DB, PlayerPrefs.GetString("Token")));

            if (countresult.Status == "Success")
                CurrentStateImagesCountText.text = countresult.Value;

            await UpdateImageList(await FetchImagesFromDB(CurrentState_DB, CurrentPage_DB, EachPageImagesCount_DB, PlayerPrefs.GetString("Token"), CurrentDiffsCountRequested_DB, SortType_DB));
            //
        }
        else
        {
            //search offline
            prepare_for_refrsh();
            CreateImageList();
        }        
    }


    #region settings

    [Header("Settings")]
    public TMP_InputField PageNumberInput;
    public GameObject SettingsPanel;
    public TMP_Text CurrentStateImagesCountText;
    public TMP_Dropdown CurrentStateDB;
    public TMP_Dropdown SortTypeDB;
    public async void ScrollBTNClicked()
    {        
        //get scroll rect
        var imagelistscrollrect = imageListCanvas.transform.Find("Panel").transform.Find("Scroll View").GetComponent<ScrollRect>();

        ////scroll to page
        float vertical_scroll_factor = 1;

        //page number , set current page number
        var pagenum = PageNumberInput.text;
        CurrentPage_DB = CurrentPage = Convert.ToInt32(pagenum);

        //database mode
        if (database)
        {
            LoadingManager.getInstance.show(); 

            var _browsedpage = BrowsedPages.Find(kv => kv.Key == CurrentPage_DB);

            prepare_for_refrsh();

            if (_browsedpage.Equals(default(KeyValuePair<int,List<JSONObject>>)))
                await UpdateImageList(await FetchImagesFromDB(CurrentState_DB, CurrentPage_DB, EachPageImagesCount_DB, PlayerPrefs.GetString("Token"), CurrentDiffsCountRequested_DB, SortType_DB));
            else await UpdateImageList(_browsedpage.Value);
        }
        //
        else
        {
            var pagescount = 1f;
            if (((float)imagesCount % (float)EachPageImagesCount) == 0) pagescount = ((float)imagesCount / (float)EachPageImagesCount);
            else pagescount = (float)Math.Ceiling(((float)imagesCount / (float)EachPageImagesCount));


            //calculaate how many 6(EachPageImagesCount) images parts we should pass and bring it between 0 and 1
            vertical_scroll_factor = 1 - ((float)(Convert.ToDouble(pagenum) - 1)) * (1 / (pagescount - 1));
            //vertical_scroll_factor = 0;

            Debug.Log("km - vertical_scroll_factor = " + vertical_scroll_factor);

            imagelistscrollrect.normalizedPosition = new Vector2(0, vertical_scroll_factor);
            ////
        }
    }

    public void NextPageBtn_Clicked()
    {
        PageNumberInput.text = (Convert.ToInt32(PageNumberInput.text) + 1).ToString();
        ScrollBTNClicked();
    }

    public void PreviousPageBtn_Clicked()
    {
        PageNumberInput.text = (Convert.ToInt32(PageNumberInput.text) - 1).ToString();
        ScrollBTNClicked();
    }

    public void PageInput_EditEdned()
    {
        ScrollBTNClicked();
    }

    #endregion

    //
}
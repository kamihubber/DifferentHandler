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
using GolbaharApiClient;
using System.Threading.Tasks;
using static GolbaharApiClient.ImageEdit;
using UnityEngine.SceneManagement;

public class ReportManager : MonoBehaviour
{
    [Header("Images")] public int reportsCount = 0;
    public int downloadedImagesCount = 0;
    public SpriteRenderer imageRenderer;
    public JSONObject imageJson;
    public GameObject imageListItemPrefab;
    public GameObject imageListBlankItemPrefab;
    public GameObject imageListCanvas;
    public RectTransform imageListContent;
    public List<GameObject> imageList = new List<GameObject>();

    [Header("Points")] public GameObject circlePrefab;
    public GameObject quadPrefab;
    public List<Transform> pointsList = new List<Transform>();

    [Header("Image Details")] public GameObject detailsCanvas;
    public GameObject showDetailsButton;
    public GameObject showHidePointsButton;
    public bool isPointsVisible = true;
    public RTLTextMeshPro detailsText;
    public RTLTextMeshPro ReportsCountText;
    public RTLTextMeshPro ReportsOptionText;

    #region settings

    [Header("Settings")]
    public TMP_InputField PageNumberInput;
    public GameObject SettingsPanel;
    public TMP_Text CurrentOptionReportsCountText;    
    public Dropdown CurrentReportOptionsDDB;
    public TMP_Dropdown SortTypeDDB;

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
    string CurrentReportOptionText_DB = "";
    int SortType_DB = 0;
    int CurrentDiffsCountRequested_DB = -1;
    int CurrentPage_DB = 1;

    List<KeyValuePair<int, List<JSONObject>>> BrowsedPages = new List<KeyValuePair<int, List<JSONObject>>>();    
    List<string> jsonsId = new List<string>();
    List<JSONObject> jsons = new List<JSONObject>();
    List<string> reportoptions;

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
    
    #region getInstance

    public static ReportManager getInstance;

    private void OnEnable()
    {
        getInstance = this;
    }

    #endregion

    private async void Start()
    {
        
        LoadingManager.getInstance.show();

        //add items to report options drodownbox,can be changed to fetch from server 
        reportoptions = new List<string>
        {
            "All",
            "کادر،درست روی تفاوت قرار نگرفته",
            "تفاوت هست ولی انتخاب نمیشه",
            "تفاوتی وجود نداره اما انتخاب میشه",
            "محتوای عکس بنظرم مناسب نیست"
        };
        CurrentReportOptionsDDB.options.Clear();
        foreach (string option in reportoptions)
        {
            CurrentReportOptionsDDB.options.Add(new Dropdown.OptionData( Fa.faConvert(option) ));            
        }
        CurrentReportOptionsDDB.value = 0;

        //download zip file
        if (database)
        {
            //initial load from database

            //fetch zero state images count
            //var countresult = (await golbahar_imageedit.GetStateImagesCount(0, PlayerPrefs.GetString("Token")));

            //if (countresult.Status == "Success")
            //CurrentStateImagesCountText.text = countresult.Value;

            //then get images
            CurrentReportOptionText_DB = CurrentReportOptionsDDB.options[0].text;//remove meeeee!!!
            var jsonslist = await FetchReportsFromDB(CurrentReportOptionText_DB, 1, EachPageImagesCount_DB, PlayerPrefs.GetString("Token"), SortType_DB);
            CurrentOptionReportsCountText.text = Fa.faConvert("تعداد کل     " + jsonslist[0].GetNumber("OverAllReportsCount"));
            await UpdateImageList(jsonslist);
        }       

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
    public bool database  =true;  
    
    ImageEdit golbahar_imageedit = new ImageEdit();    
    
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

    //Database
    async Task<List<JSONObject>> FetchReportsFromDB(string reportoptiontext, int pagenumber, int pageimagecount, string token,int sorttype = 0)
    {
        LoadingManager.getInstance.show();

        List<JSONObject> result = new List<JSONObject>();

        try
        {            

            var editimages_result = await golbahar_imageedit.GetReportsAsJson_Paged(reportoptiontext, pagenumber, pageimagecount, token, sorttype);

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

                addNewImage(imageHostUrl + json.GetString("ImageUrl"), json);

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
            Debug.Log(ex.Message);
        }        

        LoadingManager.getInstance.hide();
    }

    //        

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

        var result = await golbahar_imageedit.UpdateImageState(imageJson.GetString("ImageName"),state,PlayerPrefs.GetString("Token"));

        (GameObject.Find("ImageDetailesSaveBTN").GetComponent<Button>()).interactable = true;

    }    

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
        img.GetComponent<ReportListItemController>().Json = json;
        img.GetComponent<ReportListItemController>().ImageUrl = imageUrl;
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

    public void updateReport(Sprite sprite, string json)
    {
        imageRenderer.sprite = sprite;
        imageJson = JSONObject.Parse(json);

        //set id and difficulty
        txtImageId.text = "Id : " + imageJson.GetString("ImageName");
        ReportsCountText.text =  "تعداد گزارش : " + imageJson.GetNumber("ReportsCount");
        ReportsOptionText.text = "نوع گزارش : " + imageJson.GetString("ReportText");

        Debug.Log(json);

        try
        {
            //generate points
            JSONArray points = imageJson.GetArray("Points");
            for (int i = 0; i < points.Length; i++)
            {
                JSONObject point = points[i].Obj;
                string type = point.GetString("Type");

                //create object
                PointParent pointObject = null;
                if (type.Equals("circle")) pointObject = Instantiate(circlePrefab).GetComponent<PointParent>();
                if (type.Equals("quad")) pointObject = Instantiate(quadPrefab).GetComponent<PointParent>();

                //km
                pointObject.Transparent = false;
                //

                //Debug.Log("json value : " + point.GetString("position"));
                Vector3 position = stringToVec(point.GetString("Position"));
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
                string leftPointposition = point.GetString("Child0Position");
                string leftPointrotation = point.GetString("Child0Rotation");
                string leftPointscale = point.GetString("Child0Scale");

                //todo: ding 1
                //pointObject.points[0].transform.position = stringToVec(leftPoint.GetString("position"));

                pointObject.difficultyCanvas.position = pointObject.points[0].transform.position;
                pointObject.scaleX = stringToVec(leftPointscale).x;
                pointObject.scaleY = stringToVec(leftPointscale).y;
                pointObject.rotation = (int)stringToVec(leftPointrotation).z;

                //JSONObject rightPoint = point.GetObject("point1");

                //todo: ding 2
                //pointObject.points[1].transform.position = stringToVec(rightPoint.GetString("position"));

                //set difficulty
                pointObject.showDifficulyLabel = true;
                pointObject.Difficulty = (int)point.GetNumber("Difficulty");

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
            Debug.Log(e.Message);
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
        details += "آی دی : " + imageJson.GetString("ImageName") + "\n";
        details += "آدرس تصویر : " + imageJson.GetString("ImageUrl") + "\n";
        //details += "سختی تصویر : " + imageJson.GetNumber("ImageDifficulty") + "/100" + "\n";

        //tags
        //string tags = "";

        //if (database)
        //{
        //    var tag_list_string_value = imageJson.GetValue("image_tags").ToString();
        //    if (!string.IsNullOrEmpty(tag_list_string_value))
        //    {
        //        tag_list_string_value = tag_list_string_value.ToString().Replace("[","").Replace("]","").Replace("\"","");
        //        var tagslist = tag_list_string_value.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries);
        //        foreach (string s in tagslist)
        //            tags += s + "-";
        //    }            
        //}
        //else
        //{
        //    JSONArray tagsArray = imageJson.GetArray("image_tags");
        //    for (int i = 0; i < tagsArray.Length; i++)
        //    {
        //        tags += tagsArray[i].Obj.GetString("tag");
        //        if (i <= tagsArray.Length)
        //            tags += "-";
        //    }

        //    tags = tags.Substring(0, tags.Length - 1);            
        //}

        //details += "تگ : " + tags + "\n";

        //DateTime dt = DateTime.Parse(imageJson.GetString("creation_datetime"));

        //details += "زمان ساخت : " + dt.ToString("G") + "\n";
        //details += "تعداد اختلافات : " + imageJson.GetNumber("points_count") + "\n";

        detailsText.text = details;


        detailsCanvas.SetActive(true);
    }

    public void hideImageDetails()
    {
        detailsText.text = "";

        detailsCanvas.SetActive(false);
    }

    //km//
    public void ReportOptionsDDB_OnValueChanged(int value)
    {
        CurrentReportOptionText_DB = reportoptions[value];
        CurrentOptionReportsCountText.text = "...";
        StartCoroutine(SetReportOptionsDDBTextCoroutine());        
    }

    IEnumerator SetReportOptionsDDBTextCoroutine()
    {
        yield return null;
        CurrentReportOptionsDDB.captionText.text = Fa.faConvert(CurrentReportOptionText_DB);
        //if (CurrentState == -1)
        //    CurrentStateDB.captionText.text = "All";
        //else
        //    CurrentStateDB.captionText.text = CurrentState.ToString();
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
            SortType_DB = SortType = value;
            StartCoroutine(SetSortTypeDBTextCoroutine());
        }               
    }    

    IEnumerator SetSortTypeDBTextCoroutine()
    {
        yield return null;
        SortTypeDDB.captionText.text = SortTypeDDB.options[SortType].text;
    }

    public void DiffCountsInput_OnValueChanged(string value)
    {
        CurrentOptionReportsCountText.text = "...";
        if ((value == null) || (value == ""))
            CurrentDiffsCountRequested = -1;
        else CurrentDiffsCountRequested = Convert.ToInt32(value);
    }

    public async void HomeBtn_Clicked()
    {
        SceneManager.LoadSceneAsync("ImageDebug");
    }

    public async void SearchBtn_Clicked()
    {
        if (database)
        {
            //database                        

            CurrentDiffsCountRequested_DB = CurrentDiffsCountRequested;
            CurrentPage = CurrentPage_DB = 1;
            PageNumberInput.text = "1";            

            BrowsedPages.Clear();

            LoadingManager.getInstance.show();

            prepare_for_refrsh();

            //fetch this state images count
            //var countresult = (await golbahar_imageedit.GetStateImagesCount(CurrentState_DB, PlayerPrefs.GetString("Token")));

            //if (countresult.Status == "Success")
            //CurrentStateImagesCountText.text = countresult.Value;

            var reportjsons = await FetchReportsFromDB(CurrentReportOptionText_DB, CurrentPage_DB, EachPageImagesCount_DB, PlayerPrefs.GetString("Token"), SortType_DB);
            CurrentOptionReportsCountText.text = Fa.faConvert("تعداد کل     " + reportjsons[0].GetNumber("OverAllReportsCount") );
            await UpdateImageList(reportjsons);
            //
        }             
    }
    
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
            {
                var reportjsons = await FetchReportsFromDB(CurrentReportOptionText_DB, CurrentPage_DB, EachPageImagesCount_DB, PlayerPrefs.GetString("Token"), SortType_DB);                
                CurrentOptionReportsCountText.text = Fa.faConvert("تعداد کل     " + reportjsons[0].GetNumber("OverAllReportsCount"));
                await UpdateImageList(reportjsons);
            }                
            else await UpdateImageList(_browsedpage.Value);
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
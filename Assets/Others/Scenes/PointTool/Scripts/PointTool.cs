using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Boomlagoon.JSON;
using GolbaharSandBoxApiClient;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

public class PointTool : MonoBehaviour
{
    [Header("Image")] private string hostUrl = "https://find-differences.ir/5_diffrences";
    private string name = "";
    public string url = "/image/";    
    public string filename = "";
    public string filetype = ".2-min.png";
    public List<string> tags = new List<string>();
    [Header("Edit")]
    public bool edit = false;
    //public bool refreshJsons = false;
    [Header("Ui")] public SpriteRenderer image;
    public GameObject saveUi;
    public GameObject loadingUi;
    public GameObject CurrentModeLabel;


    private List<PointParent> Points = new List<PointParent>();
    private int jsonCount = 0;
    private string finalurl;

    ImageEdit WebApi_ImageEdit = new ImageEdit();

    private void Awake()
    {
        //km
        if (PlayerPrefs.HasKey("filetype"))
        {
            //filetype = previousfiletype = PlayerPrefs.GetString("filetype");
        }
        else
        {
            //filetype = ".2-min.png";
        }

        if (PlayerPrefs.HasKey("folderurl"))
        {
            //url = previousurl = PlayerPrefs.GetString("folderurl");
        }
        else
        {
            //url = "/image/";
        }

        if (image.sprite != null)
        {
            if (image.sprite.name != previousimagename)
            {
                filename = image.sprite.name;
                previousimagename = filename;
            }
        }

        finalurl = hostUrl + url + filename + filetype;
    }



    void Start()
    {                        

        checkImageUrl();

        //
    }



    public void checkImageUrl()
    {
        saveUi.SetActive(false);
        loadingUi.transform.Find("ResultMessage").GetComponent<RTLTMPro.RTLTextMeshPro>().text = "Loading image from url...";
        loadingUi.SetActive(true);

        image.sprite = null;
        StartCoroutine(loadImage(finalurl,filename, imageSprite =>
        {
            image.sprite = imageSprite;

            saveUi.SetActive(true);
            loadingUi.SetActive(false);
        }));
    }

    IEnumerator loadImage(string url,string spritename, System.Action<Sprite> callback)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                loadingUi.transform.Find("ResultMessage").GetComponent<RTLTMPro.RTLTextMeshPro>().text = "عکس موجود نیست یا مشکلی در برقراری ارتباط رخ داده! - برای بازگشت کلیک کنید";
            }
            else
            {
                if (www.isDone)
                {
                    var texture = DownloadHandlerTexture.GetContent(www);
                    var rect = new Rect(0, 0, 1400f, 1024f);
                    var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
                    sprite.name = filename;
                    callback(sprite);
                }
            }
        }
    }

    public void GenerateJson()
    {
#if UNITY_EDITOR

        if (finalurl.Length < 1)
        {
            Debug.LogAssertion("Image url can not be empty!");
            return;
        }

        JSONObject pointJson = new JSONObject();

        //Debug.Log(url.Replace(hostUrl, "").Replace("/image/", "").Replace(".png", "").Replace(".PNG", ""));

        //pointJson.Add("id", DateTime.Now.ToString("yyMMddHHmmss"));

        var splitedfiletype = filetype.Split(new[] { "." }, StringSplitOptions.None);
        var fileformat = "." + splitedfiletype[splitedfiletype.Length - 1];
        name = finalurl.Replace(hostUrl, "").Replace(url, "").Replace( fileformat , "").Replace(fileformat.ToUpper(), "");

        pointJson.Add("id", name);
        if (!edit) pointJson.Add("state", 0);
        else pointJson.Add("state",0);
        //else pointJson.Add("state", editJsn.GetNumber("state"));

        //pointJson.Add("img", AssetDatabase.GetAssetPath(image.sprite).Replace("Assets/Resources/", ""));

        pointJson.Add("image_url", finalurl.Replace(hostUrl, ""));

        JSONArray pointsArray = new JSONArray();


        int pointsDifficulty = 0;
        int id = 0;
        Points.Clear();
        Points.AddRange( FindObjectsOfType<PointParent>() );
        Points.Reverse();
        foreach (PointParent point in Points)
        {
            JSONObject pointJsonObject = new JSONObject();

            pointJsonObject.Add("id", id);

            if (!point.type.Equals("circle") && !point.type.Equals("quad"))
            {
                Debug.LogAssertion("Point type is wrong!");
                return;
            }

            pointJsonObject.Add("type", point.type);
            pointJsonObject.Add("difficulty", point.Difficulty);
            pointJsonObject.Add("position", point.transform.position.ToString("F3"));

            pointsDifficulty += point.Difficulty;

            JSONObject point0 = new JSONObject();
            point0.Add("position", point.points[0].transform.position.ToString());
            point0.Add("scale", "(" + point.points[0].transform.lossyScale.x.ToString("F3") +
                                "," +
                                point.points[0].transform.lossyScale.y.ToString("F3") +
                                "," +
                                point.points[0].transform.lossyScale.z.ToString("F3") +
                                ")");
            point0.Add("rotation", "(" + point.points[0].transform.rotation.eulerAngles.x.ToString("F0") +
                                   "," +
                                   point.points[0].transform.rotation.eulerAngles.y.ToString("F0") +
                                   "," +
                                   point.points[0].transform.rotation.eulerAngles.z.ToString("F0") +
                                   ")");

            pointJsonObject.Add("point0", point0);

            JSONObject point1 = new JSONObject();
            point1.Add("position", point.points[1].transform.position.ToString());
            point1.Add("scale", "(" + point.points[1].transform.lossyScale.x.ToString("F3") +
                                "," +
                                point.points[1].transform.lossyScale.y.ToString("F3") +
                                "," +
                                point.points[1].transform.lossyScale.z.ToString("F3") +
                                ")");
            point1.Add("rotation", "(" + point.points[1].transform.rotation.eulerAngles.x.ToString("F0") +
                                   "," +
                                   point.points[1].transform.rotation.eulerAngles.y.ToString("F0") +
                                   "," +
                                   point.points[1].transform.rotation.eulerAngles.z.ToString("F0") +
                                   ")");
            pointJsonObject.Add("point1", point1);
            pointsArray.Add(pointJsonObject);
            id++;
        }

        JSONArray tagsArray = new JSONArray();
        foreach (string tag in tags)
        {
            //JSONObject tagJson = new JSONObject();
            //tagJson.Add("tag", tag);
            //tagsArray.Add(tagJson);
            tagsArray.Add(tag);
        }        

        pointJson.Add("image_difficulty", pointsDifficulty);
        pointJson.Add("image_tags", tagsArray);
        pointJson.Add("points", pointsArray);
        pointJson.Add("points_count", pointsArray.Length);


        if (!edit) pointJson.Add("creation_datetime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        else pointJson.Add("creation_datetime", editJsn.GetString("creation_datetime"));

        Debug.Log(pointJson);

        SaveJson(pointJson.ToString());
#endif
    }

    public async void SaveJson(string json)
    {
#if UNITY_EDITOR
        string path = null;
        //path = "Assets/Resources/json/" + DateTime.Now.ToString("yyMMddHHmmss") + ".json";
        path = "Assets/Resources/json/" + name + ".json";

        using (FileStream fs = new FileStream(path, FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(json);
            }
        }

        loadingUi.SetActive(true);
        loadingUi.transform.Find("ResultMessage").GetComponent<RTLTMPro.RTLTextMeshPro>().text = "Updating";
        var updateresult = edit ? await WebApi_ImageEdit.UpdateImageJson(json,PlayerPrefs.GetString("Token")) 
            : await WebApi_ImageEdit.InsertImageJson(json, PlayerPrefs.GetString("Token"));

        if (updateresult.Status == "Success")
        {
            UnityEditor.AssetDatabase.Refresh();

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

            Selection.activeObject = obj;

            EditorGUIUtility.PingObject(obj);

            EditorApplication.ExecuteMenuItem("Edit/Play");
        }
        else
        {
            loadingUi.SetActive(true);
            loadingUi.transform.Find("ResultMessage").GetComponent<RTLTMPro.RTLTextMeshPro>().text = updateresult.Message;
        }

        
#endif
    }

    #region km
    string previousimagename;
    string previousfiletype;
    string previousurl;
    JSONObject editJsn;

    [Header("Points")]
    public bool HideOthers = true;

    //set hideothers property of point children when it changes in editor
    void OnValidate()
    {
        List<PointParent> _points = new List<PointParent>();

        _points.AddRange(FindObjectsOfType<PointParent>());

        foreach (PointParent pp in _points)
            pp.HideOtherPoints = HideOthers;

        //Debug.Log("");

        if (edit)
            CurrentModeLabel.GetComponent<RTLTMPro.RTLTextMeshPro>().text = "حالت : ویرایش";
        else
            CurrentModeLabel.GetComponent<RTLTMPro.RTLTextMeshPro>().text = "حالت : جدید";
    }

    private void Update()
    {        
        if (image.sprite != null)
        {
            if (image.sprite.name != previousimagename)
            {
                filename = image.sprite.name;
                previousimagename = filename;
            }
        }        

        if (filetype != previousfiletype)
        {
            PlayerPrefs.SetString("filetype", filetype);
            PlayerPrefs.Save();
        }

        if (url != previousurl)
        {
            PlayerPrefs.SetString("folderurl", url);
            PlayerPrefs.Save();
        }

        finalurl = hostUrl + url + filename + filetype;

    }

    public void LoadingUI_CloseBtnClicked()
    {
        saveUi.SetActive(true);
        loadingUi.SetActive(false);
    }

    public void addeditpoints(JSONObject Json,GameObject circlePrefab,GameObject quadPrefab)
    {
        try
        {
            //base json to edit
            editJsn = Json;

            //add tags            
            JSONArray _tags = Json.GetValue("image_tags").Array; 
            //tags.Clear();
            foreach(JSONValue jv in _tags)
            {
                tags.Add(jv.Str);
            }

            //generate points           
            JSONArray points = Json.GetArray("points");
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
                pointObject.HideOtherPoints = HideOthers;
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
                switch (UnityEngine.Random.Range(1, 5))
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

                //Points.Add(pointObject);

                pointObject.transform.parent = gameObject.transform;

            }

            
        }
        catch(Exception e)
        {
            var s = "";
        }

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
    #endregion
}
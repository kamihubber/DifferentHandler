using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Boomlagoon.JSON;
using RTLTMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DebugTool : MonoBehaviour
{
    #region getInstance

    public static DebugTool getInstance;

    private void OnEnable()
    {
        getInstance = this;
    }

    #endregion

    public SpriteRenderer image;
    public JSONObject imageJson;
    [Header("images list")] public GameObject imagesItemPrefab;
    public RectTransform imagesContent;

    [Header("ui")] public GameObject loadingCanvas;
    public GameObject imagesListCanvas;

    void Start()
    {
        showDetailsButton.SetActive(false);
        loadingCanvas.SetActive(false);
        loadImagesJsonList();
    }

    public void loadImagesJsonList()
    {
        //StartCoroutine(loadJsonList());

        StartCoroutine(loadJsonsList());
    }

    IEnumerator loadJsonsList()
    {
        string url = "https://golsaar.ir/5_diffrences/get-json-list.php";

        /*showDetailsButton.SetActive(false);
        loadingCanvas.SetActive(true);
        imagesListCanvas.SetActive(false);*/

        HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
        using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
        {
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                yield return response;

                string[] jsonList = reader.ReadToEnd().Replace("<br>", ",").Split(',');

                for (int i = 0; i < jsonList.Length-1; i++)
                {
                    string jsonUrl = jsonList[i];
                    Debug.Log(jsonUrl);
                }
            }
        }
    }

    IEnumerator loadJsonList()
    {
        showDetailsButton.SetActive(false);
        loadingCanvas.SetActive(true);
        imagesListCanvas.SetActive(false);


        yield return new WaitForSeconds(.1f);
        string url = "https://golsaar.ir/5_diffrences/json/";
        HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
        using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
        {
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string html = reader.ReadToEnd();
                Regex regex = new Regex("<a href=\".*\">(?<name>.*)</a>");
                MatchCollection matches = regex.Matches(html);
                if (matches.Count > 0)
                {
                    imagesListCanvas.SetActive(true);

                    //destroy children
                    for (int j = 0; j < imagesContent.childCount; j++)
                    {
                        Destroy(imagesContent.GetChild(j).gameObject);
                    }

                    //set height
                    int contentHeight = (int) (imagesContent.rect.width * 1.36f) * matches.Count;
                    imagesContent.rect.Set(imagesContent.rect.x, imagesContent.rect.y, imagesContent.rect.width,
                        contentHeight);
                    //set images size
                    imagesContent.GetComponent<GridLayoutGroup>().cellSize =
                        new Vector2(Screen.width / 3, Screen.width / 4);

                    for (int i = 1; i < matches.Count; i++)
                    {
                        Match match = matches[i];
                        if (match.Success)
                        {
                            string name = match.Groups["name"].ToString();

                            //load json
                            HttpWebRequest requestJson = (HttpWebRequest) WebRequest.Create(url + name);
                            using (HttpWebResponse responseJson = (HttpWebResponse) requestJson.GetResponse())
                            {
                                using (StreamReader readerJson = new StreamReader(responseJson.GetResponseStream()))
                                {
                                    string jsonString = readerJson.ReadToEnd().ToString();

                                    JSONObject json = JSONObject.Parse(jsonString);

                                    int state = (int) json.GetNumber("state");

                                    //pending state
                                    if (state == 0)
                                    {
                                        string imageUrl = url.Replace("json/", "") + json.GetString("image_url");
                                        GameObject img = Instantiate(imagesItemPrefab);
                                        img.transform.parent = imagesContent;
                                        img.transform.localScale = new Vector3(1, 1, 1);
                                        StartCoroutine(loadImage(imageUrl, sprite =>
                                        {
                                            img.GetComponent<Image>().sprite = sprite;
                                            img.GetComponent<ImageListItemController>().imageJsonString = jsonString;
                                            loadingCanvas.SetActive(false);
                                        }));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    IEnumerator loadImage(string url, System.Action<Sprite> callback)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
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

    public void updateImage(Sprite sprite, string json)
    {
        foreach (Transform point in pointsList)
        {
            Destroy(point.gameObject);
        }

        image.sprite = sprite;
        imageJson = JSONObject.Parse(json);
        Debug.Log(json);

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

            //left and right point
            JSONObject leftPoint = point.GetObject("point0");
            pointObject.points[0].transform.position = stringToVec(leftPoint.GetString("position"));
            pointObject.difficultyCanvas.position = pointObject.points[0].transform.position;
            pointObject.scaleX = stringToVec(leftPoint.GetString("scale")).x;
            pointObject.scaleY = stringToVec(leftPoint.GetString("scale")).y;
            pointObject.rotation = (int) stringToVec(leftPoint.GetString("rotation")).z;

            JSONObject rightPoint = point.GetObject("point1");
            pointObject.points[1].transform.position = stringToVec(rightPoint.GetString("position"));

            //set difficulty
            pointObject.showDifficulyLabel = true;
            pointObject.Difficulty = (int) point.GetNumber("difficulty");

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


        imagesListCanvas.SetActive(false);
        showDetailsButton.SetActive(true);
    }

    //get vector3 form string value
    public Vector3 stringToVec(string s)
    {
        string[] temp = s.Substring(1, s.Length - 2).Split(',');
        return new Vector3(float.Parse(temp[0]), float.Parse(temp[1]), float.Parse(temp[2]));
    }

    [Header("Image Details")] public GameObject detailsCanvas;
    public GameObject showDetailsButton;
    public RTLTextMeshPro detailsText;

    public void showImageDetails()
    {
        string details = "";
        details += "آی دی : " + imageJson.GetString("id") + "\n";
        details += "آدرس تصویر : " + imageJson.GetString("image_url") + "\n";
        details += "سختی تصویر : " + imageJson.GetNumber("image_difficulty") + "/100" + "\n";

        //tags
        string tags = "";
        JSONArray tagsArray = imageJson.GetArray("image_tags");
        for (int i = 0; i < tagsArray.Length; i++)
        {
            tags += tagsArray[i].Obj.GetString("tag");
            if (i <= tagsArray.Length)
                tags += "-";
        }

        tags = tags.Substring(0, tags.Length - 1);

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
}
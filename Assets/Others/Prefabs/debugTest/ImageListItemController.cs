using System;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;
using RTLTMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageListItemController : MonoBehaviour
{
    public string imageJsonString;
    public GameObject idBg;
    public GameObject imagePathBg;
    public RTLTextMeshPro idText;
    public RTLTextMeshPro imageText;

    //km//
    bool introduced = false;
    bool imagedownloaded = false;
    bool isdownloading = false;    
    float myspeed = 0;
    Vector3 lastPosition = Vector3.zero;
    
    public JSONObject Json;
    public string ImageUrl;
    //
    
    private void FixedUpdate()
    {
        if (idText.text.Length > 0) idBg.SetActive(true);
        else idBg.SetActive(false);
        
        if (imageText.text.Length > 0) imagePathBg.SetActive(true);
        else imagePathBg.SetActive(false);

        //km
        //calculate scroll speed
        myspeed = ( (transform.position - lastPosition).magnitude ) / (Time.fixedDeltaTime);        
        //Debug.Log("km - myspeed = " + myspeed);
        lastPosition = transform.position;

        //if we are visible and not downloading/downloaded image then start downloading
        if (ImageUrl != "" && !imagedownloaded && check_if_isvisible_bycamera(gameObject))
        {
            if (!isdownloading) StartCoroutine(downloadImage(ImageUrl, sprite =>
            {
                try
                {
                    gameObject.GetComponent<Image>().sprite = sprite;
                    gameObject.GetComponent<Image>().color = Color.white;
                    gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    gameObject.GetComponent<ImageListItemController>().imageJsonString = Json.ToString();
                    gameObject.GetComponent<ImageListItemController>().idText.text = Json.GetString("id");
                    gameObject.GetComponent<ImageListItemController>().imageText.text = Json.GetString("image-url");

                    DebugToolManager.getInstance.downloadedImagesCount++;

                    LoadingManager.getInstance.hide();
                }
                catch (Exception e)
                {
                }
            }));
        }
        //
    }

    public void updateImage()
    {
        //interact only if we have image downloaded
        if (imagedownloaded) DebugToolManager.getInstance.updateImage(GetComponent<Image>().sprite, imageJsonString);
    }



    //km//   

    private void Awake()
    {        
        lastPosition = transform.position;        
    }

    private void Update()
    {        
        /*
        //if we are visible and not downloading/downloaded image the start downloading
        if ( imageUrl != "" && !imagedownloaded && check_if_isvisible_bycamera(gameObject))
        {            
            if (!isdownloading) StartCoroutine(downloadImage(imageUrl, sprite =>
            {
                try
                {
                    gameObject.GetComponent<Image>().sprite = sprite;
                    gameObject.GetComponent<Image>().color = Color.white;
                    gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    gameObject.GetComponent<ImageListItemController>().imageJsonString = json.ToString();
                    gameObject.GetComponent<ImageListItemController>().idText.text = json.GetString("id");
                    gameObject.GetComponent<ImageListItemController>().imageText.text = json.GetString("image-url");

                    DebugToolManager.getInstance.downloadedImagesCount++;

                    LoadingManager.getInstance.hide();
                }
                catch (Exception e)
                {
                }
            }));
        }*/
    }

    bool check_if_isvisible_bycamera(GameObject obj)
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(obj.transform.position);
        //if we are being seen and we are not being scrolled , we are visible
        if (myspeed  <= 0 &&  screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    IEnumerator downloadImage(string url, System.Action<Sprite> callback)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            isdownloading = true;

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);

                isdownloading = false;

                callback(null);
            }
            else
            {
                if (www.isDone)
                {
                    isdownloading = false;
                    imagedownloaded = true;

                    var texture = DownloadHandlerTexture.GetContent(www);
                    var rect = new Rect(0, 0, 1400f, 1024f);
                    var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
                    callback(sprite);
                }
            }
        }
    }

    private void OnDestroy()
    {

    }


    //
}
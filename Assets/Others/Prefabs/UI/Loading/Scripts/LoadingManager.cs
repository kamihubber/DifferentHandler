using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    #region getInstance

    public static LoadingManager getInstance;

    private void OnEnable()
    {
        getInstance = this;
    }

    #endregion
    
    public bool state = false;
    public GameObject loading;
    public Image loadingSprite;
    public int activeSprite = 0;
    public List<Sprite> sprites;

    public void show()
    {        
        state = true;
        loading.SetActive(true);
        StopCoroutine(loadNextSprite());
        StartCoroutine(loadNextSprite());
    }
    public void hide()
    {
        StopCoroutine(loadNextSprite());
        state = false;
        loading.SetActive(false);
    }

    IEnumerator loadNextSprite()
    {
        while (state)
        {
            yield return new WaitForSeconds(.1f);

            loadingSprite.sprite = sprites[activeSprite];
            activeSprite++;

            if (activeSprite >= sprites.Count)
                activeSprite = 0;
        }
    }
}
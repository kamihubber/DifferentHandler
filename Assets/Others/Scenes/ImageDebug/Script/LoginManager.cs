using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using GolbaharSandBoxApiClient;
using System.Net.Http;
using System.Net.Http.Headers;
using UnityEngine.SceneManagement;

namespace Assets.Others.Scenes.ImageDebug.Script
{
    public class LoginManager : MonoBehaviour
    {

        [Header("UI")]
        public TMPro.TMP_InputField UserNameInput;
        public TMPro.TMP_InputField PassWordInput;

        private HttpClient WebApi_Request = new HttpClient();        

        //GolbaharSandBoxApiClient.Login golbahar_login = new GolbaharSandBoxApiClient.Login();
        Login golbahar_login = new Login();
        private async void Start()
        {
            DateTime TokenExpireDate;
            LoadingManager.getInstance.show();

            try
            {
                if (PlayerPrefs.HasKey("Token"))
                {
                    var tkn = PlayerPrefs.GetString("Token");

                    DateTime.TryParse(PlayerPrefs.GetString("TokenExpireDate"), out TokenExpireDate);

                    if (TokenExpireDate - DateTime.Today  > TimeSpan.FromDays(1))
                    {
                        //authorize
                        if (await golbahar_login.CheckToken(PlayerPrefs.GetString("Token")))
                        //if (await golbaharsandbox_login.CheckToken(PlayerPrefs.GetString("Token")))
                        {
                            //goto next sence
                            SceneManager.LoadSceneAsync("ImageDebug");
                        }
                        else
                        {
                            //if failed then try to refresh token
                            var refresh_result = await golbahar_login.RefreshToken(PlayerPrefs.GetString("RefreshToken"));
                            //var refresh_result = await golbaharsandbox_login.RefreshToken(PlayerPrefs.GetString("RefreshToken"));
                            if (refresh_result.Status == "Success")
                            {
                                PlayerPrefs.SetString("Token",refresh_result.Token);
                                PlayerPrefs.SetString("RefreshToken", refresh_result.RefreshToken);
                                PlayerPrefs.SetString("TokenExpireDate", refresh_result.expires);
                                PlayerPrefs.Save();

                                //goto next sence
                                SceneManager.LoadSceneAsync("ImageDebug");

                            }
                            //if it fails too then show user pass panel
                            else
                            {
                                LoadingManager.getInstance.hide();
                            }                            
                        }
                    }
                    else
                    {
                        //if failed then try to refresh token
                        LoadingManager.getInstance.show();

                        var z = PlayerPrefs.GetString("Token");
                        var x = PlayerPrefs.GetString("RefreshToken");
                        var refresh_result = await golbahar_login.RefreshToken(PlayerPrefs.GetString("RefreshToken"));   
                        //var refresh_result = await golbaharsandbox_login.RefreshToken(PlayerPrefs.GetString("RefreshToken"));

                        if (refresh_result.Status == "Success")
                        {
                            PlayerPrefs.SetString("Token", refresh_result.Token);
                            PlayerPrefs.SetString("RefreshToken", refresh_result.RefreshToken);
                            PlayerPrefs.SetString("TokenExpireDate", refresh_result.expires);
                            PlayerPrefs.Save();

                            //goto next sence
                            SceneManager.LoadSceneAsync("ImageDebug");
                        }
                        //if it fails too then show user pass panel
                        else
                        {
                            LoadingManager.getInstance.hide();
                        }
                    }
                }
                else LoadingManager.getInstance.hide();
            }
            catch(Exception ex)
            {

            }

            
        }

        public async void LoginBTN_Clicked()
        {
            LoadingManager.getInstance.show();
            var loginwithcredentials_result = await golbahar_login.LoginWithCredentials(UserNameInput.text,PassWordInput.text);
            //var loginwithcredentials_result = await golbaharsandbox_login.LoginWithCredentials(UserNameInput.text,PassWordInput.text);
            LoadingManager.getInstance.hide();

            if (loginwithcredentials_result.Status == "Success")
            {
                PlayerPrefs.SetString("Token", loginwithcredentials_result.Token);
                PlayerPrefs.SetString("RefreshToken", loginwithcredentials_result.RefreshToken);
                PlayerPrefs.SetString("TokenExpireDate", loginwithcredentials_result.expires);
                PlayerPrefs.Save();

                SceneManager.LoadSceneAsync("ImageDebug");
            }
            else
            {
                UserNameInput.text = "";
                PassWordInput.text = "";
            }
        }

        


    }
}

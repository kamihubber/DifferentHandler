using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;

public class Utils : MonoBehaviour
{
    
    public static bool IsValidEmail(string email)
    {
        try {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch {
            return false;
        }
    }

    public static DateTime getDateTime(string milisecounds)
    {
        var date = (new DateTime(1970, 1, 1)).AddMilliseconds(double.Parse(milisecounds));

        return date;
    }
    
    public static string getTimestamp()
    {
        return DateTime.UtcNow.ToString("yyMMddHHmmssfff", CultureInfo.InvariantCulture);
    }

    public static string CreateMD5(string input)
    {
        //input = "abcd321";

        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input.Substring(1));
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }

            return sb.ToString().ToLower();
        }
    }
    
    public static string EncodeToBase64(string toEncode)
    {
        byte[] toEncodeAsBytes
            = ASCIIEncoding.UTF8.GetBytes(toEncode);

        string returnValue
            = Convert.ToBase64String(toEncodeAsBytes);

        return returnValue;
    }

    public static bool hasSpecialChar(string input)
    {
        //string specialChar = @"\|!#$%&/()=?»«@£§€{}.-;'^<>_,@@";
        string specialChar = @"\|!#$%&/()=?»«@£§€{};'^<>,@@*";
        for (int i = 0; i < specialChar.Length; i++)
        {
            string ch = specialChar.Substring(i, 1);
            for (int j = 0; j < input.Length; j++)
            {
                if (input.Substring(j, 1).Equals(ch))
                {
                    Debug.Log(ch);
                    return true;
                }
            }
        }
        return false;
    }
    
    public static void KeepScreenOn()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
    public static void ScreenOnToSystemDefault()
    {
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }

    public static bool CertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
        bool isOk = true;
        // If there are errors in the certificate chain, look at each error to determine the cause.
        if (sslPolicyErrors != SslPolicyErrors.None) {
            for(int i=0; i<chain.ChainStatus.Length; i++) {
                if(chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown) {
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2)certificate);
                    if(!chainIsValid) {
                        isOk = false;
                    }
                }
            }
        }
        return isOk;
    }
    
    public static string GetHtmlFromUri(string resource)
    {
        //ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallback;

        ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallback;
        ServicePointManager.Expect100Continue = true;
        
        string html = string.Empty;
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(resource);
        try
        {
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                bool isSuccess = (int)resp.StatusCode < 299 && (int)resp.StatusCode >= 200;
                if (isSuccess)
                {
                    using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    {
                        char[] cs = new char[80];
                        reader.Read(cs, 0, cs.Length);
                        foreach(char ch in cs)
                        {
                            html +=ch;
                        }
                    }
                }
            }
        }
        catch(Exception e) 
        {
            Debug.Log(e.Message);
            return "";
        }
        return html;
    }
    
    /*public static string Base64Encode(string plainText) {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }
    
    public static string Base64Decode(string base64EncodedData) {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }*/

    public static void QuitApplication()
    {
        Application.Quit();
    }
}
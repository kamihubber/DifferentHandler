using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using RTLTMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ZipManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(download());
    }

    public RTLTextMeshPro jsonText;

    IEnumerator download()
    {
        jsonText.text = "Downloading...";
        string url = "https://golsaar.ir/5_diffrences/a.zip";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.Send();
            if (www.isNetworkError || www.isHttpError)
            {
                jsonText.text = www.error;
            }
            else
            {
                #region delete old files

                //delete files from zip directory
                if (Directory.Exists(Application.persistentDataPath + "/unzip"))
                {
                    string[] files = System.IO.Directory.GetFiles(Application.persistentDataPath + "/unzip");
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
                    if (Directory.Exists(Application.persistentDataPath + "/unzip"))
                        System.IO.Directory.Delete(Application.persistentDataPath + "/unzip");
                }
                catch (Exception e)
                {
                }

                //delete zip file
                try
                {
                    if (File.Exists(Application.persistentDataPath + "/dlzip.zip"))
                        System.IO.File.Delete(Application.persistentDataPath + "/dlzip.zip");
                }
                catch (Exception e)
                {
                }

                #endregion

                yield return new WaitForSeconds(0.2f);

                string savePath = Application.persistentDataPath + "/dlzip.zip";
                System.IO.File.WriteAllBytes(savePath, www.downloadHandler.data);

                if (www.downloadHandler.isDone)
                {
                    //Debug.Log("download completed : " + savePath);

                    StartCoroutine(ExtractZipFile(System.IO.File.ReadAllBytes(savePath),
                        Application.persistentDataPath + "/unzip/"));
                }
            }
        }
    }

    public IEnumerator ExtractZipFile(byte[] zipFileData, string targetDirectory, int bufferSize = 256 * 1024)
    {
        jsonText.text = "";

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

                        jsonText.text += entry.Name + "\n";

                        jsonText.text = File.ReadAllText(Application.persistentDataPath + "/unzip/" + entry.Name);

                        //Debug.Log(entry.Name);
                    }
                }
            }
        }
    }
}
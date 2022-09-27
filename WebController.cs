using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

public class SaveController : MonoBehaviour
{
    public bool RequestFlag = false;
    public bool IsSessionOnline = false;

    private static GameData gameData;

    private WebController WC;

    void Start() {
        WC = new WebController(this,"combinatorics"); //ваш индивидуальный токен игры
        UpdateGameData();
    }
    public void UpdateGameData() {
        gameData = new GameData { data = PlayerPrefs.GetString("SaveControllerLocalData")};
        if (IsSessionOnline)
            WC.RequestUpdateGameData();
    }

    public GameData GetGameData() {
        return gameData;
    }

    public void SetGameData(string data)
    {
        gameData.data = data;
        PlayerPrefs.SetString("SaveControllerLocalData", data);
        PlayerPrefs.Save();
        if (IsSessionOnline)
            WC.SetGameData(gameData);
    }

    public void UpdateGameDataFromRequest(string data)
    {
        gameData.data = data;
    }

    public void SetGameProgress(float progress) {
    if (IsSessionOnline)
        WC.SetGameProgress(progress);
    }

    public void SendNewMetricData(string name) {
        WC.SendMetricData(new MetricsData(name)) ;
    }

    public void SendMetricData(MetricsData metric){
        WC.SendMetricData(metric);
    }

    public string UpdateGameDataFromRequest()
    {
        if (!IsSessionOnline) {
            return PlayerPrefs.GetString("SaveControllerLocalData");
        }

        RequestFlag = false;
        WC.RequestUpdateGameData();
        while (!RequestFlag){ }
        return gameData.data;
    }
    
}


public class WebController : MonoBehaviour {

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string getToken();
#endif

    private static readonly string Server = "https://37.79.216.230:44333/";

    private static readonly string ServerSaveDataApi = "api/savedata/savedata";
    private static readonly string ServerLoadApi = "api/savedata/loaddata";
    private static readonly string ServerSaveMetricApi = "api/metrics/sendmetric";

    private SaveController SC;

    private string GameToken;
    private static string SessionToken;// session token

    public WebController(SaveController SC,string gameToken) {
        this.SC = SC;
        this.GameToken = gameToken;
#if UNITY_WEBGL && !UNITY_EDITOR
        SessionToken = getToken();
        WebGLInput.captureAllKeyboardInput = false;
        if(SessionToken!='')
            SC.IsSessionOnline = true;
#endif

    }

    public void RequestUpdateGameData()
    {
        StartCoroutine(postRequest(@"{""gametoken"": """ + GameToken + @"""}", ServerLoadApi));
    }

    public void SendMetricData(MetricsData metrics)
    {
        metrics.gameToken = GameToken;
        StartCoroutine(postRequest(metrics.SaveToString(), ServerSaveMetricApi));
    }

    public void SetGameProgress(float gameProgress)
    {
        GameData progress = SC.GetGameData();
        progress.progress = gameProgress;
        StartCoroutine(postRequest(progress.ToString(), ServerSaveDataApi));
    }

    public void SetGameData(GameData data) {
        StartCoroutine(postRequest(data.ToString(), ServerSaveDataApi));
    }

    private string AddQueryString(string api)
    {
        UriBuilder builder = new UriBuilder(Server + api);
        builder.Query += "token=" + SessionToken;//по такому подобию строки
        return builder.Uri.ToString();
    }

    public IEnumerator postRequest(string json, string api)
    {
        var uwr = new UnityWebRequest(Server + api, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.certificateHandler = new AcceptPublicKey();
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Authorization", "Bearer " + SessionToken);
        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();
        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            try
            {
                if (!string.IsNullOrEmpty(uwr.downloadHandler.text))
                    SC.UpdateGameDataFromRequest(uwr.downloadHandler.text);
            }
            catch { Debug.Log("no game data"); }
        }
        SC.RequestFlag = true;
    } 
}

public class GameData
{
    public String data; 
    public String gameToken;
    public float progress;
    public string SaveToString()
    {
        return JsonUtility.ToJson(this);
    }

    public static GameData CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<GameData>(jsonString);
    }
}
public class MetricsData
{
    public string name;
    public string gameToken;
    public MetricsData(string name)
    {
        this.name = name;
    }
    public string SaveToString()
    {
        return JsonUtility.ToJson(this);
    }

    public static MetricsData CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<MetricsData>(jsonString);
    }
}
class AcceptPublicKey : CertificateHandler
{
    // Encoded RSAPublicKey
    private static string PUB_KEY = "3082010a0282010100c67437469579ae2e3f88820a4a295fb14903eb2c5fa2be9cb098760d4c7377f4d767bd0fca12be116993b46975b09027260129cc68d44fa8789f0f139bef4572a09016e2221fdde4ef5691775bf343845775351a36642a509dab200d30dd6c1cb68aeb408f4ab4d8854d46571ae53a50c4c07e64ee154b71dd7ac761511b702aa4622f7cb84d8bb9e44509593f1a719270fb9d4134d3dfe340e9093448e49850e031af3f5847aece913d3c0716323f4c7db3ab3a52b76f8424d7b8c1cbc0cea8fb73c67d15bb2f01698d00277eeef100df17c825d7957a6b0316b3022618f164403eacd4057a9427dad19e497561e52816ab97b65cb84363b5b14b10f5b360a90203010001";
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        X509Certificate2 certificate = new X509Certificate2(certificateData);
        string pk = certificate.GetPublicKeyString();
        //Debug.Log(pk.ToLower());
        if (pk.ToLower().Equals(PUB_KEY.ToLower()))
            return true;
        return false;
    }
}

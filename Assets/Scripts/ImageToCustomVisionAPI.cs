using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using HoloToolkit.Unity.InputModule;
using UnityEngine.VR.WSA.WebCam;
using System.Linq;

public class ImageToCustomVisionAPI : MonoBehaviour, IInputClickHandler
{
    string customVisionURL = "URL"; // 自身のCustom Vision Services URL（filesのほう）を貼り付ける
    string apiKey = "API"; //自身のPrediction-Key（filesのほう）を貼り付ける

    public GameObject ImageFrameObject;
    public Text textObject;
    PhotoCapture photoCaptureObject = null;

    void Start()
    {
        InputManager.Instance.PushFallbackInputHandler(gameObject);
    }

    //カメラの設定 ここから。
    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.JPEG;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {

        if (result.success)
        {
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
        }
    }

    //カメラの設定 ここまで。
    private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {

        if (result.success)
        {
            List<byte> imageBufferList = new List<byte>();
            // Copy the raw IMFMediaBuffer data into our empty byte list.
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

            DisplayImage(imageBufferList.ToArray());                            //画像表示処理呼び出し
            StartCoroutine(GetVisionDataFromImages(imageBufferList.ToArray())); //API呼び出し
        }

        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }


    /// <summary>
    /// Get data from the Cognitive Services Custom Vision Services
    /// Stores the response into the responseData string
    /// </summary>
    /// <returns> IEnumerator - needs to be called in a Coroutine </returns>
    IEnumerator GetVisionDataFromImages(byte[] imageData)
    {

        var headers = new Dictionary<string, string>() {
            { "Prediction-Key", apiKey },
            { "Content-Type", "application/octet-stream" }
        };

        WWW www = new WWW(customVisionURL, imageData, headers);

        yield return www;
        string responseData = www.text; // Save the response as JSON string

        Debug.Log(responseData);

        ResponceJson json = JsonUtility.FromJson<ResponceJson>(responseData);

        float tmpProbability = 0.0f;
        string str = "";

        for (int i = 0; i < json.Predictions.Length; i++)
        {
            Prediction obj = (Prediction)json.Predictions[i];

            Debug.Log(obj.Tag + "：" + obj.Probability.ToString("P"));

            if (tmpProbability < obj.Probability)
            {
                str = obj.Probability.ToString("P") + "の確率で" + obj.Tag + "です";
                tmpProbability = obj.Probability;
            }
        }
        textObject.text = str;
    }

    // キャプチャした画像をImageに貼り付ける（ImageFrameObject実態はCubeを薄くしたもの）
    private void DisplayImage(byte[] imageData)
    {
        Texture2D imageTxtr = new Texture2D(2, 2);
        imageTxtr.LoadImage(imageData);
        ImageFrameObject.GetComponent<Renderer>().material.mainTexture = imageTxtr;
    }

    // エアタップの取得
    public void OnInputClicked(InputClickedEventData eventData)
    {
        textObject.text = "Call Custom Services...";
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }
}

[Serializable]
public class ResponceJson
{
    public string Id;
    public string Project;
    public string Iteration;
    public string Created;

    public Prediction[] Predictions;
}

[Serializable]
public class Prediction
{
    public string TagId;
    public string Tag;
    public float Probability;
}

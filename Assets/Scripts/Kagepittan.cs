using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;

#if UNITY_EDITOR
using UnityEditor;
#endif

using CvUnity = OpenCvSharp.Unity;

public class Kagepittan : MonoBehaviour
{
    public RawImage cameraImage;
    public RawImage backgroundImage;
    public RawImage outputImage;
    public RawImage targetImage;
    public RawImage diffImage;

    public Text matchText;
    public Text excessText;
    public Text scoreText;
    public Text pointText;
    public Text passText;
    public Text okText;
    public Text passCenterText;
    public Text finishText;
    public Slider scoreSlider;
    public Image highLine;
    public Image goalLine;
    public GameObject cameraObject;

    public AudioSource audioSource;
    public AudioClip okClip;

    private string[] targetList;

    private WebCamTexture webCamTexture;
    private CvUnity.TextureConversionParams webCamParams;

    private Texture2D cameraTexture;
    private Texture2D backgroundTexture;
    private Texture2D outputTexture;
    private Texture2D diffTexture;

    public Color32 goodColor;
    public Color32 badColor;

    private Mat backgroundMat;
    private Mat targetMat;

    private int targetPixelCount;

    public ConfigObject configObject;
    public Config config { get { return configObject.config; } }

    public int cameraIndex { get { return config.camera.index; } }
    public bool flipHorizontary { get { return config.camera.flip; } }
    public RectOffset cameraOffset { get { return config.camera.offset; } }
    public float maskThreshold { get { return config.imageProcess.maskThreshold; } }
    public float maskMaxVal { get { return config.imageProcess.maskMaxVal; } }
    public int smoothIterationCount { get { return config.imageProcess.smoothIterationCount; } }
    public float excessRate { get { return config.game.excessRate; } }
    public float goalRate { get { return config.game.goalRate; } }

    private float maxRate;
    private int currentTargetIndex;

    private int pointNum;
    private int passNum;

    private Coroutine coroutine;

    private void OnValidate()
    {
        UpdateLine(goalLine, goalRate);
    }

    // Use this for initialization
    void Start()
    {
#if !UNITY_EDITOR
        configObject.LoadConfig();
#endif

        BuildTargetList();

        var device = WebCamTexture.devices[cameraIndex];
        webCamTexture = new WebCamTexture(device.name);
        webCamTexture.Play();

        webCamParams = new CvUnity.TextureConversionParams();
        webCamParams.FlipHorizontally = flipHorizontary;
        webCamParams.RotationAngle = webCamTexture.videoRotationAngle;

        ChangeTarget(0);
    }

    private void OnDestroy()
    {
        webCamTexture.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if(Input.GetKeyDown(KeyCode.C))
        {
            cameraObject.SetActive(!cameraObject.activeSelf);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            UpdateBackground();
        }

        if (coroutine == null && Input.GetKeyDown(KeyCode.Return))
        {
            coroutine = StartCoroutine(PassCoroutine());
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            UpdateBackground();
            ResetResult();
            ChangeTarget(currentTargetIndex + 1);
        }

        if(Input.GetKeyDown(KeyCode.F))
        {
            //finishText.gameObject.SetActive(!finishText.gameObject.activeSelf);
        }

        UpdateImages();
    }

    private void ChangeTarget(int targetIndex)
    {
        maxRate = 0;

        currentTargetIndex = targetIndex % targetList.Length;

        if(targetImage.texture != null)
        {
            //Resources.UnloadAsset(targetImage.texture);
        }

        var data = File.ReadAllBytes(targetList[currentTargetIndex]);
        var texture = new Texture2D(4, 4);
        texture.LoadImage(data);

        targetImage.texture = texture;
        targetImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)targetImage.texture.width / targetImage.texture.height;

        if (targetMat != null)
        {
            targetMat.Dispose();
        }
        targetMat = new Mat();
        Mat targetColorMat = CvUnity.TextureToMat((Texture2D)targetImage.texture);
        Cv2.CvtColor(targetColorMat, targetMat, ColorConversionCodes.BGRA2GRAY);

        if (targetMat.Width > 512)
        {
            var size = new Size(512, (double)targetMat.Height / targetMat.Width * 512);
            targetMat = targetMat.Resize(size);
        }

        targetPixelCount = 0;

        for (int y = 0; y < targetMat.Height; ++y)
        {
            for (int x = 0; x < targetMat.Width; ++x)
            {
                int index = targetMat.Width * y + x;

                unsafe
                {
                    byte val = *(targetMat.DataPointer + index);
                    if (val == 0) { targetPixelCount++; }
                }
            }
        }

        UpdateLine(highLine, 0);
    }

    private void UpdateBackground()
    {
        Mat cameraMat = CvUnity.TextureToMat(webCamTexture, webCamParams);

        if (cameraMat.Width > 512)
        {
            var size = new Size(512, (double)cameraMat.Height / cameraMat.Width * 512);
            cameraMat = cameraMat.Resize(size);
        }

        backgroundMat = cameraMat.Clone();
    }

    private void UpdateImages()
    {
        if (backgroundMat == null) { return; }

        Mat cameraMat = CvUnity.TextureToMat(webCamTexture, webCamParams);

        if(cameraMat.Width > 512)
        {
            var size = new Size(512, (double)cameraMat.Height / cameraMat.Width * 512);
            cameraMat = cameraMat.Resize(size);
        }

        var width = cameraMat.Width - cameraOffset.left - cameraOffset.right;
        var height = cameraMat.Height - cameraOffset.top - cameraOffset.bottom;
        var posX = cameraOffset.left;
        var posY = cameraOffset.top;
        var cameraRect = new OpenCvSharp.Rect(posX, posY, width, height);

        cameraMat = cameraMat.Clone(cameraRect);
        Mat trimBgMat = backgroundMat.Clone(cameraRect);
        Mat diffBgMat = cameraMat.Clone();
        Cv2.Absdiff(diffBgMat, trimBgMat, diffBgMat);


        Mat grayscaleMat = new Mat();
        Cv2.CvtColor(diffBgMat, grayscaleMat, ColorConversionCodes.BGR2GRAY);

        //Mat blurMat = new Mat();
        //Cv2.GaussianBlur(grayscaleMat, blurMat, new Size(5, 5), 0);

        Mat maskMat = new Mat();
        Cv2.Threshold(grayscaleMat, maskMat, maskThreshold, maskMaxVal, ThresholdTypes.BinaryInv);
        //Cv2.Threshold(blurMat, maskMat, maskThreshold, maskMaxVal, ThresholdTypes.BinaryInv);

        Mat dilateMat = maskMat.Dilate(new Mat(), null, smoothIterationCount);
        Mat erodeMat = dilateMat.Erode(new Mat(), null, smoothIterationCount);

        Mat outputMat = erodeMat.Clone();

        float targetAspectRatio = (float)targetMat.Width / targetMat.Height;
        float outputAspectRatio = (float)erodeMat.Width / erodeMat.Height;

        if(targetMat.Height != outputMat.Height)
        {
            var rate = (double)targetMat.Height / outputMat.Height;
            var size = new Size(outputMat.Width * rate, outputMat.Height * rate);
            outputMat = outputMat.Resize(size);
        }

        var rect = new OpenCvSharp.Rect((outputMat.Width - targetMat.Width) / 2, 0, targetMat.Width, targetMat.Height);
        Mat resizeMat = outputMat.Clone(rect);
        Mat diffMat = new Mat();
        Cv2.Absdiff(targetMat, resizeMat, diffMat);

        Mat resultColorMat = new Mat(resizeMat.Rows, resizeMat.Cols, MatType.CV_8UC4);
        CountDiff(resizeMat, targetMat, resultColorMat);

        MatToImage(cameraMat, cameraImage, ref cameraTexture);
        //MatToImage(backgroundMat, backgroundImage, ref backgroundTexture);
        //MatToImage(resizeMat, outputImage, ref outputTexture);
        //MatToImage(diffMat, diffImage, ref diffTexture);
        MatToImage(resultColorMat, diffImage, ref diffTexture);
    }

    private void CountDiff(Mat resultMat, Mat targetMat, Mat colorMat)
    {
        int match = 0, excess = 0;

        for(int y=0; y<resultMat.Height; ++y)
        {
            for(int x=0; x<resultMat.Width; ++x)
            {
                int index = resultMat.Width * y + x;

                byte resultVal, targetVal;

                unsafe
                {
                    resultVal = *(resultMat.DataPointer + index);
                    targetVal = *(targetMat.DataPointer + index);

                    if (targetVal == 0 && resultVal == 0)
                    {
                        match++;

                        *(colorMat.DataPointer + index * 4 + 0) = goodColor.b;
                        *(colorMat.DataPointer + index * 4 + 1) = goodColor.g;
                        *(colorMat.DataPointer + index * 4 + 2) = goodColor.r;
                        *(colorMat.DataPointer + index * 4 + 3) = 255;
                    }
                    else if (targetVal == 255 && resultVal != 255)
                    {
                        excess++;

                        *(colorMat.DataPointer + index * 4 + 0) = badColor.b;
                        *(colorMat.DataPointer + index * 4 + 1) = badColor.g;
                        *(colorMat.DataPointer + index * 4 + 2) = badColor.r;
                        *(colorMat.DataPointer + index * 4 + 3) = 255;
                    }
                    else if(targetVal == 0)
                    {
                        *(colorMat.DataPointer + index * 4 + 0) = 255;
                        *(colorMat.DataPointer + index * 4 + 1) = 255;
                        *(colorMat.DataPointer + index * 4 + 2) = 255;
                        *(colorMat.DataPointer + index * 4 + 3) = 255;
                    }
                    else
                    {
                        *(colorMat.DataPointer + index * 4 + 0) = 0;
                        *(colorMat.DataPointer + index * 4 + 1) = 0;
                        *(colorMat.DataPointer + index * 4 + 2) = 0;
                        *(colorMat.DataPointer + index * 4 + 3) = 0;
                    }
                }
            }
        }

        float rate = (float)(match - (float)excess * excessRate) / targetPixelCount;
        float rate2 = rate / goalRate;

        int score = (int)(rate * 100f);

        matchText.text = string.Format("Good:{0}", match);
        excessText.text = string.Format("Bad:{0}", excess);
        scoreText.text = string.Format("Score:{0:D3}", score);

        scoreSlider.value = Mathf.Clamp01(rate2);

        UpdateLine(highLine, Mathf.Max(maxRate, rate2));

        if (coroutine == null && !finishText.gameObject.activeSelf)
        {
            if (rate > maxRate)
            {
                maxRate = rate;
            }

            if(maxRate >= goalRate)
            {
                coroutine = StartCoroutine(ChangeCoroutine());
            }
        }
    }

    private IEnumerator ChangeCoroutine()
    {
        SetPoint(pointNum + 1);
        SetOK(true);
        if(okClip != null)
        {
            audioSource.PlayOneShot(okClip);
        }

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.RightArrow));

        SetOK(false);

        ChangeTarget(currentTargetIndex + 1);

        yield return new WaitForSeconds(1);

        coroutine = null;
    }

    private IEnumerator PassCoroutine()
    {
        SetPass(passNum + 1);
        SetPass(true);

        yield return new WaitForSeconds(0.7f);

        SetPass(false);

        ChangeTarget(currentTargetIndex + 1);

        yield return new WaitForSeconds(0.7f);

        coroutine = null;
    }

    private void UpdateLine(Image image, float rate)
    {
        var anchorMin = image.rectTransform.anchorMin;
        anchorMin.y = rate;
        image.rectTransform.anchorMin = anchorMin;

        var anchorMax = image.rectTransform.anchorMax;
        anchorMax.y = rate;
        image.rectTransform.anchorMax = anchorMax;

        var pos = image.rectTransform.anchoredPosition;
        pos.y = 0;
        image.rectTransform.anchoredPosition = pos;
    }

    private void ResetResult()
    {
        SetPoint(0);
        SetPass(0);
        SetPass(false);
        SetOK(false);
    }

    private void SetPoint(int point)
    {
        pointNum = point;
        pointText.text = point.ToString("D2");
    }

    private void SetPass(int pass)
    {
        passNum = pass;
        passText.text = pass.ToString("D2");
    }

    private void SetOK(bool active)
    {
        okText.gameObject.SetActive(active);
    }

    private void SetPass(bool active)
    {
        passCenterText.gameObject.SetActive(active);
    }

    private void MatToImage(Mat mat, RawImage image, ref Texture2D texture)
    {
        if(mat == null) { return; }

        texture = CvUnity.MatToTexture(mat, texture);

        if (image.texture != texture)
        {
            image.texture = texture;
            image.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }
    }

    private void BuildTargetList()
    {
        var rootPath = GetImageRootPath();

        var files = Directory.GetFiles(rootPath);

        targetList = files
            .Where(_path => IsImageFile(_path))
            .OrderBy(_path => System.Guid.NewGuid())
            .ToArray();
    }

    private string GetImageRootPath()
    {
        return Path.Combine(Application.streamingAssetsPath, "Images");
    }

    private bool IsImageFile(string path)
    {
        string ext = Path.GetExtension(path);

        return ext == ".png";
    }
}

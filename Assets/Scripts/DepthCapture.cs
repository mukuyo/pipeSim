using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

[Serializable]
public class CameraIntrinsicData
{
    public float[] cam_K;  // Intrinsic matrix
    public float depth_scale;  // Depth scale factor
}



public class DepthCapture : MonoBehaviour
{
    private Camera cam;
    private Camera depthCam;
    private RenderTexture depthTexture;
    private RenderTexture depthAsColorTexture;
    private Texture2D depthTex2D;
    private RenderTexture colorTexture;
    private Texture2D colorTex2D;
    private int count = 0;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.nearClipPlane = 0.5f; // カメラが描画を開始する距離を設定
        cam.farClipPlane = 10f; // カメラが描画を終了する距離を設定
        cam.depthTextureMode = DepthTextureMode.Depth;

        // 深度用のカメラを作成
        GameObject depthCamObj = new GameObject("DepthCamera");
        depthCam = depthCamObj.AddComponent<Camera>();
        depthCam.enabled = false; // 手動でレンダリングするため無効にする
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        if (depthTexture == null || depthTexture.width != source.width || depthTexture.height != source.height)
        {
            if (depthTexture != null) depthTexture.Release();
            depthTexture = new RenderTexture(source.width, source.height, 24, RenderTextureFormat.Depth);

            if (depthAsColorTexture != null) depthAsColorTexture.Release();
            depthAsColorTexture = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGBFloat);

            depthTex2D = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false);

            if (colorTexture != null) colorTexture.Release();
            colorTexture = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32);

            colorTex2D = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        }

        // 深度用カメラを設定
        depthCam.CopyFrom(cam);
        depthCam.targetTexture = depthTexture;
        depthCam.Render();
        depthCam.targetTexture = null;

        // 深度テクスチャをカラーとしてレンダリング
        Graphics.Blit(depthTexture, depthAsColorTexture);

        // 深度テクスチャにノイズ効果を適用
        ApplyNoiseEffect(depthAsColorTexture);

        // カラー画像をコピー
        Graphics.Blit(source, colorTexture);

        // 結果を画面に描画
        Graphics.Blit(depthAsColorTexture, dest);
    }

    private void ApplyNoiseEffect(RenderTexture colorTexture)
    {
        RenderTexture.active = colorTexture;
        depthTex2D.ReadPixels(new Rect(0, 0, colorTexture.width, colorTexture.height), 0, 0);
        depthTex2D.Apply();

        // Retrieve the camera's near and far clipping plane values
        float n = cam.nearClipPlane;
        float f = cam.farClipPlane;

        for (int y = 0; y < depthTex2D.height; y++)
        {
            for (int x = 0; x < depthTex2D.width; x++)
            {
                // Retrieve the pixel's color data
                Color pixel = depthTex2D.GetPixel(x, y);
                float depth = pixel.r;  // Assuming depth is stored in the red channel

                // Convert normalized depth value back to actual depth in meters
                float depthInMeters = (f * n) / ((f - n) * (depth - 1) + f);

                // Convert depth in meters to millimeters
                float depthInMillimeters = depthInMeters * 1000.0f;

                // Clamp the depth value to a reasonable range (optional, depending on your use case)
                depthInMillimeters = Mathf.Clamp(depthInMillimeters, 0.0f, 10000.0f); // Adjust this range as needed

                // Update the depth value (for visualization, you might still keep it normalized)
                // Here, we set it back to the texture as a normalized value for demonstration
                float depthNormalized = (f * n) / (depthInMillimeters / 1000.0f * (f - n) + n);

                // Update the pixel color with the depth value (use green for visibility)
                Color color = new Color(depthNormalized, depthNormalized, depthNormalized, 1.0f);
                depthTex2D.SetPixel(x, y, color);
            }
        }

        depthTex2D.Apply();
        RenderTexture.active = null;

        // Copy the modified texture back to the RenderTexture
        Graphics.Blit(depthTex2D, colorTexture);
    }


    void Update()
    {
        // Call SaveScreenshot when the "P" key is pressed
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"Screenshot saved");
            SaveScreenshot();
            SaveCameraParameters(); // Save the camera parameters
        }
    }

    private void SaveCameraParameters()
    {
        CameraIntrinsicData cameraData = new CameraIntrinsicData
        {
            cam_K = new float[9] {
                cam.projectionMatrix.m00, cam.projectionMatrix.m01, cam.projectionMatrix.m02,
                cam.projectionMatrix.m10, cam.projectionMatrix.m11, cam.projectionMatrix.m12,
                cam.projectionMatrix.m20, cam.projectionMatrix.m21, cam.projectionMatrix.m22
            },
            depth_scale = 1.0f // Example value for depth scale
        };

        string json = JsonUtility.ToJson(cameraData, true);
        string filePath = $"Assets/Pictures/camera.json";
        File.WriteAllText(filePath, json);

        Debug.Log($"Camera parameters saved to {filePath}");
    }



    void SaveScreenshot()
    {
        // 深度画像の保存
        SaveTextureToFile(depthAsColorTexture, $"Assets/Pictures/depth/{count}.png", depthTex2D);

        // カラー画像の保存
        SaveTextureToFile(colorTexture, $"Assets/Pictures/rgb/{count}.jpg", colorTex2D);

        count = count + 1;
    }

    void SaveTextureToFile(RenderTexture renderTexture, string fileName, Texture2D texture2D)
    {
        // RenderTextureを一時的にアクティブに設定
        RenderTexture.active = renderTexture;

        // テクスチャを読み取って画像を保存
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        byte[] bytes = texture2D.EncodeToPNG();
        File.WriteAllBytes(fileName, bytes);
        Debug.Log($"Screenshot saved as {fileName}");

        // RenderTextureのアクティブを解除
        RenderTexture.active = null;
    }
}


using UnityEngine;
using System.IO;
using System;

public class DepthTextureEffect : MonoBehaviour
{
    private Camera cam;
    private Camera depthCam;
    private RenderTexture depthTexture;
    private RenderTexture depthAsColorTexture;
    private Texture2D depthTex2D;
    private RenderTexture colorTexture;
    private Texture2D colorTex2D;

    // ファイルに書き込むための変数
    private StreamWriter writer;
    private int frameCount = 0;
    private const int maxFrames = 1000;
    private float[,] pixel_stdev;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.nearClipPlane = 0.5f; // カメラが描画を開始する距離を設定
        cam.farClipPlane = 6f; // カメラが描画を終了する距離を設定
        cam.depthTextureMode = DepthTextureMode.Depth;

        // 深度用のカメラを作成
        GameObject depthCamObj = new GameObject("DepthCamera");
        depthCam = depthCamObj.AddComponent<Camera>();
        depthCam.enabled = false; // 手動でレンダリングするため無効にする
        writer = new StreamWriter("DepthData.txt", false);
        LoadPixelStdDevs();
    }

    void LoadPixelStdDevs()
    {
        // テキストファイルからピクセルの標準偏差を読み込む
        string filePath = "pixel_std_devs.txt";
        string[] lines = File.ReadAllLines(filePath);
        int height = lines.Length;
        int width = lines[0].Split('\t').Length - 1;
        pixel_stdev = new float[height, width];
        Debug.Log(width);
        for (int y = 0; y < height; y++)
        {
            string[] values = lines[y].Split('\t');
            for (int x = 0; x < width; x++)
            {
                // 空の文字列があればスキップする
                if (string.IsNullOrWhiteSpace(values[x]))
                {
                    Debug.LogWarning($"Empty or whitespace value on line {y + 1}, column {x + 1}");
                    continue;
                }

                // 浮動小数点数に変換できない場合にエラーをログに記録する
                if (!float.TryParse(values[x], out float parsedValue))
                {
                    Debug.LogError($"Failed to parse float value on line {y + 1}, value: {values[x]}");
                    continue;
                }

                // 正常に変換された場合は、配列に格納する
                pixel_stdev[y, x] = parsedValue;
            }
        }
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

        // フレームカウントを更新
        frameCount++;
        if (frameCount >= maxFrames)
        {
            // ファイルを閉じる
            writer.Close();
            writer = null;
        }
    }

    private void ApplyNoiseEffect(RenderTexture colorTexture)
    {
        RenderTexture.active = colorTexture;
        depthTex2D.ReadPixels(new Rect(0, 0, colorTexture.width, colorTexture.height), 0, 0);
        depthTex2D.Apply();

        // ノイズの適用
        float dist_a = 0.00039587120180480275f;
        float dist_b = 0.5424400827831032f;
        Debug.Log(depthTex2D.width);
        for (int y = 0; y < depthTex2D.height; y++)
        {
            for (int x = 0; x < depthTex2D.width; x++)
            {
                Color pixel = depthTex2D.GetPixel(x, y);
                float depth = pixel.r;

                // 深度値を実際の距離に変換
                float n = cam.nearClipPlane;
                float f = cam.farClipPlane;
                float depthInMeters = (f * n) / ((f - n) * (depth - 1) + f);

                float distance_stdev = dist_a * Mathf.Exp(dist_b * depthInMeters);

                float dist_noise = UnityEngine.Random.Range(-distance_stdev, distance_stdev);
                float pixel_noise = UnityEngine.Random.Range(-pixel_stdev[y, x], pixel_stdev[y, x]);

                depthInMeters = depthInMeters + dist_noise + pixel_noise;
                depthInMeters = Mathf.Clamp(depthInMeters, 0.0f, 10.0f);

                // 距離を深度値に戻す
                depth = (f * n) / (depthInMeters * (f - n) + n);

                // depth = 1.0f - depth;
                // 更新した深度値をカラーとして設定
                Color color = new Color(depth, depth, depth, 1.0f); // 緑色を設定
                depthTex2D.SetPixel(x, y, color);
            }
        }

        depthTex2D.Apply();
        RenderTexture.active = null;

        // 変換したテクスチャを再度RenderTextureにコピー
        Graphics.Blit(depthTex2D, colorTexture);
    }

    void Update()
    {
        // Sキーが押されたときにスクリーンショットを保存
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveScreenshot();
        }
    }

    void SaveScreenshot()
    {
        // 深度画像の保存
        SaveTextureToFile(depthAsColorTexture, "Assets/Picture/depth_screenshot.png", depthTex2D);

        // カラー画像の保存
        SaveTextureToFile(colorTexture, "Assets/Picture/color_screenshot.png", colorTex2D);
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

    void OnDestroy()
    {
        // オブジェクトが破棄されるときにファイルを閉じる
        if (writer != null)
        {
            writer.Close();
        }
    }
}

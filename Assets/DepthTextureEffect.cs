using UnityEngine;
using System.IO;

public class DepthTextureEffect : MonoBehaviour
{
    private Camera cam;
    private Camera depthCam;
    private RenderTexture depthTexture;
    private RenderTexture depthAsColorTexture;
    private Texture2D depthTex2D;

    // ファイルに書き込むための変数
    private StreamWriter writer;
    private int frameCount = 0;
    private const int maxFrames = 1000;

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

        // ファイルを開く
        writer = new StreamWriter("DepthData.txt", false);
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        if (depthTexture == null || depthTexture.width != source.width || depthTexture.height != source.height)
        {
            if (depthTexture != null) depthTexture.Release();
            depthTexture = new RenderTexture(source.width, source.height, 24, RenderTextureFormat.Depth);

            if (depthAsColorTexture != null) depthAsColorTexture.Release();
            depthAsColorTexture = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGBFloat);

            depthTex2D = new Texture2D(source.width, source.height, TextureFormat.RFloat, false);
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

        // 結果を白黒で画面に描画
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

        float pixel_stdev = 0.028445781f;

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

                float dist_noise = Random.Range(-distance_stdev, distance_stdev);
                float pixel_noise = Random.Range(-pixel_stdev, pixel_stdev);

                depthInMeters = depthInMeters + dist_noise + pixel_noise;
                depthInMeters = Mathf.Clamp(depthInMeters, 0.0f, 10.0f);

                // 距離を深度値に戻す
                depth = (f * n) / (depthInMeters * (f - n) + n);

                // 更新した深度値をカラーとして設定
                pixel = new Color(depth, depth, depth, 1.0f);
                depthTex2D.SetPixel(x, y, pixel);
            }
        }

        depthTex2D.Apply();
        RenderTexture.active = null;

        // 変換したテクスチャを再度RenderTextureにコピー
        Graphics.Blit(depthTex2D, colorTexture);
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

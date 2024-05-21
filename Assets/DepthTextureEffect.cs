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

        int centerX = colorTexture.width / 2;
        int centerY = colorTexture.height / 2;

        Color centerPixel = depthTex2D.GetPixel(centerX, centerY);
        float depth = centerPixel.r;

        // 深度値を実際の距離に変換
        float n = cam.nearClipPlane;
        float f = cam.farClipPlane;
        float depthInMeters = (f * n) / ((f - n) * (depth - 1) + f);

        // // ノイズの適用
        // float[] distances = { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f, 4.5f, 5f, 5.5f, 6f, 6.5f};
        // float[] stdevs = {0.000581636f, 0.000698358f, 0.001016795f, 0.001582388f, 0.002193503f, 0.00314924f, 0.005200843f, 0.006372291f, 0.008387831f, 0.009203792f,0.013668662f, 0.01819264f};

        // float stdev = 0;
        // for (int j = 0; j < distances.Length; ++j)
        // {
        //     if (distances[j] > depthInMeters)
        //     {
        //         stdev = stdevs[j];
        //         break;
        //     }
        // }
        float a = 0.00069114f;
        float b = -0.00158065f;
        float c = 0.00163197f;
        float stdev = a* depthInMeters * depthInMeters + b* depthInMeters + c;
        float noise = Random.Range(-stdev, stdev);
        depthInMeters += noise;
        depthInMeters = Mathf.Clamp(depthInMeters, 0.0f, 10.0f);

        // 距離を深度値に戻す
        depth = (f * n) / (depthInMeters * (f - n) + n);

        // 中央のピクセルの深度を出力
        if (writer != null)
        {
            writer.WriteLine(depthInMeters);
            Debug.Log("meters: " + depthInMeters + " stdev: " + stdev);
        }

        // 更新した深度値をカラーとして設定
        centerPixel = new Color(depth, depth, depth, 1.0f);
        depthTex2D.SetPixel(centerX, centerY, centerPixel);
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

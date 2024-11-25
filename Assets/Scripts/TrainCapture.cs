using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class TrainCapture : MonoBehaviour
{
    private Camera cam;

    private RenderTexture colorTexture;
    private Texture2D colorTex2D;
    private int count = -1;
    private const int width = 640;  // テクスチャの幅
    private const int height = 480; // テクスチャの高さ

    // カメラの内部パラメータを直接指定
    private readonly float[] cam_K = new float[]
    {
        606.661011f, 0.0f, 325.939575f,
        0.0f, 606.899597f, 243.979828f,
        0.0f, 0.0f, 1.0f
    };

    void Start()
    {
        // メインカメラの取得
        cam = GetComponent<Camera>();

        // カメラの内部パラメータを適用
        ApplyCameraIntrinsics();

        // RenderTextureとTexture2Dのセットアップ
        colorTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        colorTex2D = new Texture2D(width, height, TextureFormat.RGB24, false);
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        // カラー画像をレンダリング
        Graphics.Blit(source, colorTexture);
        
        // 表示テクスチャを画面にブリット
        Graphics.Blit(colorTexture, dest);
    }

    private void ApplyCameraIntrinsics()
    {
        if (cam_K.Length != 9)
        {
            Debug.LogError("無効なカメラ内部パラメータ");
            return;
        }

        // Unityカメラの射影行列を計算
        float near = cam.nearClipPlane;
        float far = cam.farClipPlane;

        Matrix4x4 projMatrix = new Matrix4x4();
        projMatrix[0, 0] = 2.0f * cam_K[0] / width;
        projMatrix[1, 1] = 2.0f * cam_K[4] / height;
        projMatrix[0, 2] = 1.0f - 2.0f * cam_K[2] / width;
        projMatrix[1, 2] = 1.0f - 2.0f * cam_K[5] / height;
        projMatrix[2, 2] = -(far + near) / (far - near);
        projMatrix[2, 3] = -2.0f * far * near / (far - near);
        projMatrix[3, 2] = -1.0f;
        projMatrix[3, 3] = 0.0f;

        cam.projectionMatrix = projMatrix;

        // 射影行列をログ出力
        Debug.Log("Projection Matrix:");
        Debug.Log(projMatrix);
    }

    void Update()
    {
        // "P"キーが押されたときにスクリーンショットを保存
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Screenshot saved");
            SaveScreenshot();
        }
    }

    void SaveScreenshot()
    {
        count = count + 1;
        SaveTextureToFile(colorTexture, "/home/th/ws/research/PipeIsoGen/data/train/rgb/" + count.ToString() + ".png", colorTex2D);
    }

    void SaveTextureToFile(RenderTexture renderTexture, string fileName, Texture2D texture2D)
    {
        // RenderTextureからテクスチャにピクセルを読み込み、保存する
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        byte[] bytes = texture2D.EncodeToPNG();
        File.WriteAllBytes(fileName, bytes);
        Debug.Log($"Screenshot saved as {fileName}");

        RenderTexture.active = null;
    }
}

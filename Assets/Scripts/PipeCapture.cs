using UnityEngine;
using System.IO;
using System;

public class PipeCapture : MonoBehaviour
{
    private Camera cam;
    private Camera depthCam;
    private RenderTexture depthTexture;
    private RenderTexture depthAsColorTexture;
    private Texture2D depthTex2D;
    private RenderTexture colorTexture;
    private Texture2D colorTex2D;
    private float[,] pixel_stdev;
    private const int width = 640; // Width for texture
    private const int height = 480; // Height for texture
    // private float dist_a = 0.0009667970390564464f;
    // private float dist_b = 0.2767966038314638f;

    private float dist_a = 0.0016241095577658135f;
    private float dist_b = 0.17552242127996232f;

    // private float dist_a = 0.043464f;
    // private float dist_b = 0.263680f;
    private string save_folder = "/home/th/ws/research/PipeIsoGen/data/sim/images/test"; // 修正

    // Directly specify intrinsic parameters
    private readonly float[] cam_K = new float[]
    {
        6.06661011e+02f, 0.0f, 3.25939575e+02f,
        0.0f, 6.06899597e+02f, 2.43979828e+02f,
        0.0f, 0.0f, 1.0f
    };

    void Start()
    {
        cam = GetComponent<Camera>();
        
        cam.depthTextureMode = DepthTextureMode.Depth;

        // Apply camera intrinsic parameters to the Unity camera
        ApplyCameraIntrinsics();

        // Create a depth camera
        GameObject depthCamObj = new GameObject("DepthCamera");
        depthCam = depthCamObj.AddComponent<Camera>();
        depthCam.enabled = false; // Disable rendering by default

        // Set up RenderTextures with appropriate formats
        depthTexture = new RenderTexture(width, height, 24, RenderTextureFormat.Depth);
        depthAsColorTexture = new RenderTexture(width, height, 0, RenderTextureFormat.R16);
        depthTex2D = new Texture2D(width, height, TextureFormat.R16, false);
        colorTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        colorTex2D = new Texture2D(width, height, TextureFormat.RGB24, false);
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        if (depthTexture == null || depthTexture.width != width || depthTexture.height != height)
        {
            ReleaseAndCreateRenderTextures();
        }

        // Set up the depth camera
        depthCam.CopyFrom(cam);
        depthCam.depthTextureMode = DepthTextureMode.Depth; // Ensure depth mode is enabled
        depthCam.targetTexture = depthTexture;
        depthCam.Render();
        depthCam.targetTexture = null;

        // Render the depth texture as a single-channel grayscale
        Graphics.Blit(depthTexture, depthAsColorTexture);

        // Apply any effects directly to the depth texture
        ApplyDepthEffect(depthAsColorTexture);

        // Copy the color image
        Graphics.Blit(source, colorTexture);

        // Render the result to the screen
        Graphics.Blit(colorTexture, dest);
    }

    private void ApplyCameraIntrinsics()
    {
        if (cam_K.Length != 9)
        {
            Debug.LogError("Invalid intrinsic data");
            return;
        }

        // Calculate the Unity camera projection matrix
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

        // Log the projection matrix
        Debug.Log("Projection Matrix:");
        Debug.Log(projMatrix);
    }

    private void ApplyDepthEffect(RenderTexture depthTexture)
    {
        RenderTexture.active = depthTexture;
        depthTex2D.ReadPixels(new Rect(0, 0, depthTexture.width, depthTexture.height), 0, 0);
        depthTex2D.Apply();

        Texture2D depthTexture2D = new Texture2D(width, height, TextureFormat.R16, false);
        Color[] depthPixels = new Color[width * height];

        for (int y = 0; y < depthTex2D.height; y++)
        {
            for (int x = 0; x < depthTex2D.width; x++)
            {
                float depth = depthTex2D.GetPixel(x, y).r;

                float linearDepth = cam.farClipPlane * cam.nearClipPlane / 
                                    (cam.farClipPlane - (cam.farClipPlane - cam.nearClipPlane) * depth);
                
                float distance_stdev = dist_a * Mathf.Exp(dist_b * linearDepth);
                float dist_noise = UnityEngine.Random.Range(-distance_stdev, distance_stdev);
                float depthInMm = (linearDepth) * 100.0f;
                // depthInMm = linearDepth * 100.0f;

                // if (y == depthTex2D.height / 2 && x == depthTex2D.width / 2)
                //     Debug.Log($"Depth in mm: {depthInMm}");

                ushort depthInUShort = (ushort)Mathf.Clamp(depthInMm, 0, ushort.MaxValue);
                
                if (depthInMm >= cam.farClipPlane * 1000.0f)
                    depthInUShort = 0;
                
                Color depthColor = new Color(depthInUShort / (float)ushort.MaxValue, 0, 0, 0);
                depthPixels[y * width + x] = depthColor;
            }
        }

        depthTexture2D.SetPixels(depthPixels);
        depthTexture2D.Apply();
        RenderTexture.active = null;

        Graphics.Blit(depthTexture2D, depthTexture);
    }

    private void ReleaseAndCreateRenderTextures()
    {
        if (depthTexture != null) depthTexture.Release();
        depthTexture = new RenderTexture(width, height, 24, RenderTextureFormat.Depth);

        if (depthAsColorTexture != null) depthAsColorTexture.Release();
        depthAsColorTexture = new RenderTexture(width, height, 0, RenderTextureFormat.R16);

        depthTex2D = new Texture2D(width, height, TextureFormat.R16, false);

        if (colorTexture != null) colorTexture.Release();
        colorTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

        colorTex2D = new Texture2D(width, height, TextureFormat.RGB24, false);
    }

    void Update()
    {
        // Call SaveScreenshot when the "P" key is pressed
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"Screenshot saved");
            SaveScreenshot();
        }
    }

    void SaveScreenshot()
    {

        // Save the color image
        SaveTextureToFile(colorTexture, save_folder + "/rgb/frame0.png", colorTex2D);

        // Save the depth image
        SaveDepthTextureToFile(depthAsColorTexture, save_folder + "/depth/frame0.png", depthTex2D);
    }

    void SaveTextureToFile(RenderTexture renderTexture, string fileName, Texture2D texture2D)
    {
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        byte[] bytes = texture2D.EncodeToPNG();
        File.WriteAllBytes(fileName, bytes);
        Debug.Log($"Screenshot saved as {fileName}");

        RenderTexture.active = null;
    }

    void SaveDepthTextureToFile(RenderTexture renderTexture, string fileName, Texture2D texture2D)
    {
        RenderTexture.active = renderTexture;
        Texture2D depthTexture2D = new Texture2D(width, height, TextureFormat.R16, false);
        depthTexture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        depthTexture2D.Apply();

        // Encode the texture to PNG with 16-bit depth data
        byte[] bytes = depthTexture2D.EncodeToPNG();
        File.WriteAllBytes(fileName, bytes);
        Debug.Log($"Depth image saved as {fileName}");

        RenderTexture.active = null;
    }
}

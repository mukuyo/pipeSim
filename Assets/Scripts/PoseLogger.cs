using UnityEngine;
using System.IO;

public class PoseLogger : MonoBehaviour
{
    public GameObject targetObject; // 姿勢を取得したいオブジェクト
    public Camera mainCamera;       // シーンのメインカメラ

    private readonly float[] cam_K = new float[]
    {
        606.661011f, 0.0f, 325.939575f,
        0.0f, 606.899597f, 243.979828f,
        0.0f, 0.0f, 1.0f
    };

    private const int width = 640;  // 画像幅
    private const int height = 480; // 画像高さ

    void Start()
    {
        ApplyCameraIntrinsics();
        LogObjectPose();
    }

    private void ApplyCameraIntrinsics()
    {
        if (cam_K.Length != 9)
        {
            Debug.LogError("Invalid intrinsic data");
            return;
        }

        Camera camera = mainCamera;
        camera.farClipPlane = 10f;
        camera.nearClipPlane = 0.5f;
        float near = camera.nearClipPlane;
        float far = camera.farClipPlane;

        Matrix4x4 projMatrix = new Matrix4x4();
        projMatrix[0, 0] = 2.0f * cam_K[0] / width;
        projMatrix[1, 1] = 2.0f * cam_K[4] / height;
        projMatrix[0, 2] = 1.0f - 2.0f * cam_K[2] / width;
        projMatrix[1, 2] = 1.0f - 2.0f * cam_K[5] / height;
        projMatrix[2, 2] = -(far + near) / (far - near);
        projMatrix[2, 3] = -2.0f * far * near / (far - near);
        projMatrix[3, 2] = -1.0f;
        projMatrix[3, 3] = 0.0f;

        camera.projectionMatrix = projMatrix;
    }

private void LogObjectPose()
{
    if (targetObject == null || mainCamera == null)
    {
        Debug.LogError("ターゲットオブジェクトまたはメインカメラが設定されていません。");
        return;
    }

    // カメラ座標系でのオブジェクトの姿勢を取得
    Transform objectTransform = targetObject.transform;
    Matrix4x4 worldToCameraMatrix = mainCamera.worldToCameraMatrix;
    Matrix4x4 objectToWorldMatrix = objectTransform.localToWorldMatrix;
    Matrix4x4 objectToCameraMatrix = worldToCameraMatrix * objectToWorldMatrix;

    // 回転行列（3x3）と平行移動ベクトル（1x3）を列優先で取得
    PoseData poseData = new PoseData();
    poseData.rotation = new Matrix3x3
    {
        row0 = new Vector3(objectToCameraMatrix[0, 0], objectToCameraMatrix[1, 0]-1, objectToCameraMatrix[2, 0]+1),
        row1 = new Vector3(objectToCameraMatrix[0, 1]-1, objectToCameraMatrix[1, 1], objectToCameraMatrix[2, 1]-1),
        row2 = new Vector3(objectToCameraMatrix[0, 2]+1, objectToCameraMatrix[1, 2]-1, objectToCameraMatrix[2, 2])
    };
    poseData.translation = new Vector3(objectToCameraMatrix[0, 3], -objectToCameraMatrix[1, 3], -objectToCameraMatrix[2, 3]);

    // JSON形式でシリアライズし、ファイルに保存
    string json = JsonUtility.ToJson(poseData, true);
    string filePath = "/home/th/research_ws/PipeIsoGen/data/outputs/pose/elbow/true_pose.json";
    File.WriteAllText(filePath, json);

    Debug.Log("姿勢データが保存されました: " + filePath);
    Debug.Log($"回転行列:\n{poseData.rotation.row0}\n{poseData.rotation.row1}\n{poseData.rotation.row2}");
    Debug.Log($"平行移動ベクトル:\n{poseData.translation}");
}

}

[System.Serializable]
public class PoseData
{
    public Matrix3x3 rotation;
    public Vector3 translation;
}

[System.Serializable]
public struct Matrix3x3
{
    public Vector3 row0;
    public Vector3 row1;
    public Vector3 row2;
}
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class MatrixData
{
    public RotationData rotation;
    public Vector3 translation;

    [System.Serializable]
    public class RotationData
    {
        public Vector3 row0;
        public Vector3 row1;
        public Vector3 row2;
    }
}

public class PoseEstimator : MonoBehaviour
{
    public enum PartType
    {
        Elbow,
        Tee
    }

    public PartType selectedPartType; // Select part type from the inspector
    public List<Transform> targetObjects; // List of target objects to get the poses of
    public Camera mainCamera; // Camera object

    // Camera intrinsic parameters
    private readonly float[] cam_K = new float[]
    {
        616.055542f, 0.0f, 321.531097f,
        0.0f, 616.339722f, 240.286011f,
        0.0f, 0.0f, 1.0f
    };

    private const int width = 640;  // Image width
    private const int height = 480; // Image height

    // Base path for JSON files
    private readonly string baseFilePath = "/home/th/ws/research/PipeIsoGen/data/outputs/pose/";

    void Start()
    {
        ApplyCameraIntrinsics();
        LogObjectsPose();
    }

    void Update()
    {
        // Uncomment if you want to log poses every frame
        // LogObjectsPose();
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

private void LogObjectsPose()
{
    foreach (Transform targetObject in targetObjects)
    {
        Matrix4x4 poseMatrix = GetPoseInCameraCoordinates(targetObject, mainCamera);
        Debug.Log($"Pose Matrix for {targetObject.name}:\n{poseMatrix}");

        Vector3 adjustedTranslation = new Vector3(
            poseMatrix.m03 * 100.0f - 0.35f,
            -poseMatrix.m13 * 100.0f + 4.65f,
            poseMatrix.m23 * 100.0f - 5.95f
        );  
        if (selectedPartType.ToString() == "Elbow") {
            adjustedTranslation = new Vector3(
                poseMatrix.m03 * 100.0f - 5.0f,
                -poseMatrix.m13 * 100.0f + 5.0f,
                poseMatrix.m23 * 100.0f - 3.375f
            );
        }

        SaveMatrixToJson(poseMatrix, adjustedTranslation);
    }
}

void SaveMatrixToJson(Matrix4x4 matrix, Vector3 adjustedTranslation)
{
    MatrixData matrixData = new MatrixData
    {
        rotation = new MatrixData.RotationData
        {
            row0 = new Vector3(matrix.m02, -matrix.m00, matrix.m01),
            row1 = new Vector3(-matrix.m12, matrix.m10, -matrix.m11),
            row2 = new Vector3(matrix.m22, -matrix.m20, matrix.m21)            
        },
        translation = adjustedTranslation
    };

    string json = JsonUtility.ToJson(matrixData, true);
    string filePath = Path.Combine(baseFilePath, selectedPartType.ToString().ToLower(), "gt.json");

    // Save to the dynamically constructed path
    File.WriteAllText(filePath, json);

    Debug.Log($"Saved pose data to {filePath}");
}


    Matrix4x4 GetPoseInCameraCoordinates(Transform objTransform, Camera cam)
    {
        Vector3 positionInCamera = cam.transform.InverseTransformPoint(objTransform.position);
        Quaternion rotationInCamera = Quaternion.Inverse(cam.transform.rotation) * objTransform.rotation;

        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotationInCamera);
        Vector4 translationVector = new Vector4(positionInCamera.x, positionInCamera.y, positionInCamera.z, 1.0f);

        Matrix4x4 poseMatrix = new Matrix4x4();
        poseMatrix.SetColumn(0, rotationMatrix.GetColumn(1));
        poseMatrix.SetColumn(1, rotationMatrix.GetColumn(2));
        poseMatrix.SetColumn(2, rotationMatrix.GetColumn(0));
        poseMatrix.SetColumn(3, translationVector);
        return poseMatrix;
    }

    // void SaveMatrixToJson(Matrix4x4 matrix)
    // {
    //     MatrixData matrixData = new MatrixData
    //     {
    //         rotation = new MatrixData.RotationData
    //         {
    //             // row0 = new Vector3(matrix.m00, matrix.m01, matrix.m02),
    //             // row1 = new Vector3(matrix.m10, matrix.m11, matrix.m12),
    //             // row2 = new Vector3(matrix.m20, matrix.m21, matrix.m22)
    //             row0 = new Vector3(matrix.m02, -matrix.m00, matrix.m01),
    //             row1 = new Vector3(-matrix.m12, matrix.m10, -matrix.m11),
    //             row2 = new Vector3(matrix.m22, -matrix.m20, matrix.m21)            
    //         },
    //         translation = new Vector3(matrix.m03 * 100.0f, -matrix.m13 * 100.0f, matrix.m23 * 100.0f)
    //     };

    //     string json = JsonUtility.ToJson(matrixData, true);
    //     string filePath = Path.Combine(baseFilePath, selectedPartType.ToString().ToLower(), "gt.json");

    //     // Save to the dynamically constructed path
    //     File.WriteAllText(filePath, json);

    //     Debug.Log($"Saved pose data to {filePath}");
    // }
}

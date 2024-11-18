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
    public List<Transform> elbowTargetObjects; // List of target objects for Elbow
    public List<Transform> teeTargetObjects; // List of target objects for Tee
    public Camera mainCamera; // Camera object

    // Camera intrinsic parameters
    private readonly float[] cam_K = new float[]
    {
        6.06661011e+02f, 0.0f, 3.25939575e+02f,
        0.0f, 6.06899597e+02f, 2.43979828e+02f,
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
        for (int i = 0; i < 2; i++)
        {
            List<Transform> targetObjects = i == 0 ? elbowTargetObjects : teeTargetObjects;
            List<MatrixData> poseDataList = new List<MatrixData>(); // List to hold poses for each target object

            foreach (Transform targetObject in targetObjects)
            {
                Transform tempObject = targetObject;
                float x_val = 0.04f;
                if (i == 0) {
                    if (targetObject.eulerAngles.y == 90) {
                        x_val *= -1;
                    } else if (targetObject.eulerAngles.y == 270) {
                        x_val *= 2;
                    }
                } else {
                    x_val = -0.02f;
                    if (targetObject.eulerAngles.y == 90) {
                        x_val *= 2;
                    } else if (targetObject.eulerAngles.y == 270) {
                        x_val *= 2;
                    }
                }
                // if (i == 0) {
                //     tempObject.position = new Vector3(targetObject.position.x + x_val, targetObject.position.y + 0.075f, targetObject.position.z - 0.05f);
                // } else {
                //     tempObject.position = new Vector3(targetObject.position.x + x_val, targetObject.position.y + 0.00f, targetObject.position.z - 0.05f);
                // }
                tempObject.position = new Vector3(targetObject.position.x, targetObject.position.y, targetObject.position.z);
                
                Matrix4x4 poseMatrix = GetPoseInCameraCoordinates(targetObject, mainCamera);
                // Debug.Log($"Pose Matrix for {targetObject.name}:\n{poseMatrix}");

                // Vector3 adjustedTranslation = (i == 0)
                //     ? new Vector3(
                //         poseMatrix.m03 * 100.0f - 5.0f,
                //         -poseMatrix.m13 * 100.0f + 5.0f,
                //         poseMatrix.m23 * 100.0f - 3.375f)
                //     : new Vector3(
                //         poseMatrix.m03 * 100.0f - 0.35f,
                //         -poseMatrix.m13 * 100.0f + 4.65f,
                //         poseMatrix.m23 * 100.0f - 5.95f);
                Debug.Log($"Target object position: {Mathf.Cos(targetObject.eulerAngles.y * Mathf.Deg2Rad)}");
                
                // float x_val = -5f;

                // if (targetObject.eulerAngles.y == 90) {
                //     x_val *= -1;
                // }
                // } else if (targetObject.eulerAngles.y == 180) {
                //     x_val *= -1;
                // } else if (targetObject.eulerAngles.y == 270) {
                //     x_val *= -1;
                // }

                Vector3 adjustedTranslation = (i == 0)
                    ? new Vector3(
                        // poseMatrix.m03 * 100.0f + 5.0f * Mathf.Sin(targetObject.eulerAngles.y * Mathf.Deg2Rad),
                        // -poseMatrix.m13 * 100.0f - 7.5f,
                        // poseMatrix.m23 * 100.0f + 20.0f)
                        poseMatrix.m03 * 100.0f,
                        -poseMatrix.m13 * 100.0f,
                        poseMatrix.m23 * 100.0f)
                    : new Vector3(
                        poseMatrix.m03 * 100.0f,
                        -poseMatrix.m13 * 100.0f,
                        poseMatrix.m23 * 100.0f);
                // Create a MatrixData object and add it to the list
                MatrixData matrixData = new MatrixData
                {
                    rotation = new MatrixData.RotationData
                    {
                        row0 = new Vector3(poseMatrix.m02, -poseMatrix.m00, poseMatrix.m01),
                        row1 = new Vector3(-poseMatrix.m12, poseMatrix.m10, -poseMatrix.m11),
                        row2 = new Vector3(poseMatrix.m22, -poseMatrix.m20, poseMatrix.m21)
                    },
                    translation = adjustedTranslation
                };
                poseDataList.Add(matrixData); // Add each pose to the list
            }

            // Serialize the pose data list to JSON and save it to a file
            string json = JsonUtility.ToJson(new PoseDataWrapper { poses = poseDataList }, true);
            string fileName = i == 0 ? "elbow/gt_poses.json" : "tee/gt_poses.json";
            string filePath = Path.Combine(baseFilePath, fileName);

            try
            {
                File.WriteAllText(filePath, json);
                // Debug.Log($"Pose data saved to {filePath}");
            }
            catch (IOException e)
            {
                // Debug.LogError($"Failed to write pose data to file: {e.Message}");
            }
        }
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

    [System.Serializable]
    private class PoseDataWrapper
    {
        public List<MatrixData> poses;
    }
}

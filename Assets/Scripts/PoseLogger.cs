using UnityEngine;

public class PoseLogger : MonoBehaviour
{
    public GameObject targetObject; // 真の姿勢を取得したいオブジェクト
    public Camera mainCamera;       // シーンのメインカメラ

    void Update()
    {
        // オブジェクトのワールド座標での平行移動ベクトルを取得
        Vector3 worldPosition = targetObject.transform.position;

        // ワールド座標系からカメラ座標系への変換
        Vector3 cameraPosition = mainCamera.worldToCameraMatrix.MultiplyPoint(worldPosition);

        // オブジェクトの回転を行列として取得
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(targetObject.transform.rotation);

        // ワールド座標系の回転行列をカメラ座標系に変換
        Matrix4x4 cameraRotationMatrix = mainCamera.worldToCameraMatrix * rotationMatrix;

        // 3x4行列の生成
        Matrix4x4 transformationMatrix = new Matrix4x4();

        // 3x3回転行列を設定
        transformationMatrix.SetColumn(0, cameraRotationMatrix.GetColumn(0));
        transformationMatrix.SetColumn(1, cameraRotationMatrix.GetColumn(1));
        transformationMatrix.SetColumn(2, cameraRotationMatrix.GetColumn(2));

        // カメラ座標系での平行移動ベクトルを最後の列に設定
        transformationMatrix.SetColumn(3, new Vector4(cameraPosition.x, cameraPosition.y, cameraPosition.z, 1));

        // 必要に応じて、3x4行列をファイルに保存したりコンソールに表示
        Debug.Log("Transformation Matrix (Camera Coordinate System):\n" + MatrixToString(transformationMatrix));
    }

    // 行列を文字列に変換するヘルパーメソッド
    string MatrixToString(Matrix4x4 mat)
    {
        return $"{mat[0, 0]:F3} {mat[0, 1]:F3} {mat[0, 2]:F3} {mat[0, 3]:F3}\n" +
               $"{mat[1, 0]:F3} {mat[1, 1]:F3} {mat[1, 2]:F3} {mat[1, 3]:F3}\n" +
               $"{mat[2, 0]:F3} {mat[2, 1]:F3} {mat[2, 2]:F3} {mat[2, 3]:F3}\n";
    }
}

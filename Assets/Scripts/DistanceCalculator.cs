using UnityEngine;

public class DistanceCalculator : MonoBehaviour
{
    public GameObject obj1;
    public GameObject obj2;

    void Start()
    {
        // オフセットを適用して、最初の位置を調整
        // Vector3 offset = new Vector3(-0.658f, 0.0f, 0.031f);
        // obj1.transform.position -= offset;
        // obj2.transform.position -= offset;
    }

    void Update()
    {
        // originalOffsetをオブジェクト名に基づいて設定
        Vector3 originalOffset1 = GetOriginalOffset(obj1.name);
        Vector3 originalOffset2 = GetOriginalOffset(obj2.name);

        // 回転後のオフセットを計算
        Vector3 positionOffset1 = obj1.transform.rotation * originalOffset1;
        Vector3 positionOffset2 = obj2.transform.rotation * originalOffset2;

        // 回転後のオフセットを適用した座標
        Vector3 newPosition1 = obj1.transform.position - positionOffset1;
        Vector3 newPosition2 = obj2.transform.position - positionOffset2;

        // obj1とobj2間の距離を計算
        float distance = Vector3.Distance(newPosition1, newPosition2);

        Debug.Log("Distance between obj1 and obj2: " + distance * 1000.0f);
        // Debug.Log("obj1 rotation: " + obj1.transform.localRotation.eulerAngles);
        // Debug.Log("positionOffset1: " + positionOffset1);
        Debug.Log("newPosition1: " + newPosition1);
        // Debug.Log("obj2 rotation: " + obj2.transform.localRotation.eulerAngles);
        // Debug.Log("positionOffset2: " + positionOffset2);
        Debug.Log("newPosition2: " + newPosition2);
    }

    // 名前によってoriginalOffsetを決定
    private Vector3 GetOriginalOffset(string objectName)
    {
        if (objectName.StartsWith("tee"))
        {
            // オフセットの方向を反転するロジックを追加
            return new Vector3(0.0f, 0.0245f, 0.0f); // 反転されたオフセット
        }
        else if (objectName.StartsWith("elbow"))
        {
            return new Vector3(0.0f, 0.030f, -0.030f);
        }
        else
        {
            Debug.LogWarning("Unknown object prefix: " + objectName);
            return Vector3.zero; // デフォルトのオフセット
        }
    }
}

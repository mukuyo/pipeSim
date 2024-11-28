using UnityEngine;

public class DistanceCalculator : MonoBehaviour
{
    public GameObject obj1;
    public GameObject obj2;

    void Update()
    {
        // obj1とobj2の距離を計算
        float distance = Vector3.Distance(obj1.transform.position, obj2.transform.position);
        
        // 距離をコンソールに表示
        Debug.Log("Distance: " + distance);
    }
}
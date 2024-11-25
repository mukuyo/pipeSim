using UnityEngine;

public class ModelSize : MonoBehaviour
{
    void Start()
    {
        // MeshRenderer コンポーネントのメッシュを取得
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        
        if (meshFilter != null)
        {
            // メッシュのバウンディングボックスを取得
            Bounds bounds = meshFilter.sharedMesh.bounds;
            
            // サイズを表示（X, Y, Z）
            Debug.Log("Model Size: " + bounds.size);
        }
    }
}
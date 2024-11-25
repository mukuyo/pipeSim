using UnityEngine;

public class PixelDrawer : MonoBehaviour
{
    public Texture2D texture;
    public Color dotColor = Color.red;

    void Start()
    {
        // テクスチャを作成または取得
        if (texture == null)
        {
            texture = new Texture2D(256, 256);
        }

        // ピクセルを設定
        texture.SetPixel(10, 10, dotColor); // (10, 10)に赤い点
        texture.SetPixel(20, 20, dotColor); // (20, 20)に赤い点
        
        // 変更を適用
        texture.Apply();

        // マテリアルにテクスチャを設定
        GetComponent<Renderer>().material.mainTexture = texture;
    }
}
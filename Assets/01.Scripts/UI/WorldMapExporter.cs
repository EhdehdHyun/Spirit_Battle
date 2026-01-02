using UnityEngine;
using UnityEditor;

public class WorldMapExporter
{
    [MenuItem("Tools/Export World Map")]
    public static void Export()
    {
        Camera cam = GameObject.Find("WorldMapCamera").GetComponent<Camera>();
        RenderTexture rt = cam.targetTexture;

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        RenderTexture.active = currentRT;

        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/WorldMap.png";
        System.IO.File.WriteAllBytes(path, bytes);

        Debug.Log("월드맵 이미지 생성됨: " + path);
    }
}
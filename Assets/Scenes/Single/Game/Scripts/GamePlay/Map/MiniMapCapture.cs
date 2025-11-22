using UnityEngine;
using System.IO;
using System.Collections;

public class MiniMapCapture : MonoBehaviour
{
    public Camera miniMapCamera;
    public int size = 4096; 

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        CaptureMiniMap();
    }

    void CaptureMiniMap()
    {
        RenderTexture rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        miniMapCamera.orthographic = true;
        miniMapCamera.targetTexture = rt;

        miniMapCamera.Render();

        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();

        miniMapCamera.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();

        string folder = Application.dataPath + "/MiniMaps/";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string fileName = "MiniMap_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";

        File.WriteAllBytes(folder + fileName, tex.EncodeToPNG());

        Debug.Log("MiniMap saved!  Path: " + folder + fileName);
    }
}

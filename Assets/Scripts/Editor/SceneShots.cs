using System.IO;
using UnityEngine;

namespace LunarEyes.EditorTools
{
    /// <summary>Renderiza vistas da cena para PNG (verificação visual sem entrar em Play).</summary>
    public static class SceneShots
    {
        public static string Shot(string outPath, Vector3 pos, Vector3 lookAt, float fov = 55f, int w = 1280, int h = 720)
        {
            var go = new GameObject("__ShotCam");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = fov;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 3000f;
                go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(lookAt - pos));
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                return outPath;
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}

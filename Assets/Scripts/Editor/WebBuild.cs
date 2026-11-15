using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LunarEyes.EditorTools
{
    /// <summary>Gera o build WebGL do Lunar Eyes em Builds/WebGL (menu "Lunar Eyes/Build WebGL").</summary>
    public static class WebBuild
    {
        public const string OutputPath = "Builds/WebGL";

        [MenuItem("Lunar Eyes/Build WebGL")]
        public static string Build()
        {
            // Gzip + fallback: funciona em qualquer servidor (Vercel, servidor local de teste).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.productName = "Lunar Eyes";

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/LunarStation.unity" },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded) ExposeInstance();
            string msg = $"{s.result} · {s.totalSize / (1024 * 1024)} MB · {(int)s.totalTime.TotalSeconds} s · erros {s.totalErrors}";
            Debug.Log("[WebBuild] " + msg);
            return msg;
        }

        // Guarda a instância em window.unityInstance (útil para testes e para a página anfitriã).
        static void ExposeInstance()
        {
            string index = System.IO.Path.Combine(OutputPath, "index.html");
            if (!System.IO.File.Exists(index)) return;
            string html = System.IO.File.ReadAllText(index);
            const string hook = ".then((unityInstance) => {";
            if (html.Contains(hook) && !html.Contains("window.unityInstance"))
                System.IO.File.WriteAllText(index, html.Replace(hook, hook + " window.unityInstance = unityInstance;"));
        }
    }
}

using Sirenix.OdinInspector;
using UnityEngine;

public class ScreenshotCapture : MonoBehaviour
{
    [Button("Capture Screenshot")]
    public void Capture()
    {
        string fileName = $"screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string path = System.IO.Path.Combine(Application.dataPath, "../Screenshots", fileName);

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path, 2);

        Debug.Log($"Saved screenshot: {path}");
    }
}
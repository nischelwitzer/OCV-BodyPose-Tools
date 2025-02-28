using OpenCVForUnity.VideoModule;
using UnityEngine;

public class CamRendererInfo : MonoBehaviour
{
    void Start()
    {
        Debug.Log("CamRendererInfo: Start");
        // Debug.Log("CamRendererInfo: Screen dpi: " + Screen.dpi);
        // Debug.Log("CamRendererInfo: Screen orientation: " + Screen.orientation);    
        // Debug.Log("CamRendererInfo: Screen currentResolution: " + Screen.currentResolution);
        // Debug.Log("CamRendererInfo: Screen fullScreen: " + Screen.fullScreen);  
        // Debug.Log("CamRendererInfo: Screen fullScreenMode: " + Screen.fullScreenMode);
        // Debug.Log("CamRendererInfo: Screen safeArea: " + Screen.safeArea);
        // Debug.Log("CamRendererInfo: Screen Position x/y: " + Screen.safeArea.position.x + " " + Screen.safeArea.position.y);
        Debug.Log("CamRendererInfo: Screen width/height: " + Screen.width + " " + Screen.height);
        Debug.Log("CamRendererInfo: Renderer center-position: " + this.gameObject.transform.position.x +" "+ this.gameObject.transform.position.y);
        Debug.Log("CamRendererInfo: Renderer width/height: " + this.gameObject.transform.localScale.x + " "+ this.gameObject.transform.localScale.y);
    }

}

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
* Event Helper
* usage: use with DMTTriggerEvent
* 
* example usage: can be used trigger actions on events
* 
* Author: FH JOANNEUM, IMA,´DMT, Nischelwitzer, 2025
* www.fh-joanneum.at & exhibits.fh-joanneum.at
*/

public class DMTEventHelpers : MonoBehaviour
{

    void Start()
    {
        Debug.Log("##### DMTEventHelpers: Start");
    }

    // ###############################################################################
    // change size on event

    public void HelperSize(float newSize) // 0.80 to 1.25
    {
        Vector3 newScaling = this.transform.localScale * newSize;
        this.transform.localScale = newScaling;
    }

    // ###############################################################################
    // switch scene on event

    public void HelperSceneManager(string whichScene)
    {
        if (whichScene != "")
        {
            Debug.LogWarning("Trigger event fired: change Scene > " + whichScene);
            SceneManager.LoadScene(whichScene);
        }
        else
            Debug.LogError("DMTTriggerScene: no Scene Name specified!");
    }

    // ###############################################################################
    // Helpers for easy logging on events

    public void HelperInfoLog(String logInfoText)
    {
        Debug.Log("##### HelperInfo: "+ logInfoText);
    }
    public void HelperInfoWarning(String logWarningText)
    {
        Debug.LogWarning("##### HelperWarning: " + logWarningText);
    }
    public void HelperInfoError(String logErrorText)
    {
        Debug.LogError("##### HelperError: " + logErrorText);
    }
    
    // ###############################################################################
    // change color by name
        
    public enum ColorNames
    {
        Red     = 0xFF0000,
        Green   = 0x00FF00,
        Blue    = 0x0000FF,
        Yellow  = 0xFFFF00,
        Cyan    = 0x00FFFF,
        Magenta = 0xFF00FF,
        Black   = 0x000000,
        White   = 0xFFFFFF,
        Gray    = 0x808080,
        Orange  = 0xFFA500,
        Purple  = 0x800080,
        Brown   = 0xA52A2A
    }

    private static Color HexToColor(uint hex)
    {
        float r = ((hex >> 16) & 0xFF) / 255f;
        float g = ((hex >> 8) & 0xFF) / 255f;
        float b = (hex & 0xFF) / 255f;
        return new Color(r, g, b);
    }
    
    public void HelperMaterialColor(string colorName)
    {
        // if (GetComponent<Image>() != null)
        if (GetComponent<Renderer>() != null)
        {
            ColorNames color;
            if (Enum.TryParse(colorName, out color))
            {
                
                uint hexColor = (uint)color;
                Color colorUnity = HexToColor(hexColor);
                Debug.Log($"Farbe gefunden: {color} mit Wert {(uint)color} "+ colorUnity);
                GetComponent<Renderer>().material.color = colorUnity;
                // GetComponent<Image>().tintColor = HexToColor(hexColor);
            }
        }
        else
            Debug.LogWarning("DMTEventHelpers: no Renderer found!");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IngameDebugConsole;

/*
 * ApplicationManager.cs (in empty GO ApplicationManager)
 * Version 3.2 - with InfoCanvas, OpenCV, Uduino
 * 
 * author: Alexander Nischelwitzer, FH JOANNEUM, IMA, DMT, NIS
 * last changed:27.03.2025
 * update: activate button for pause (p) and screenshot (s)
 * 
 * Use:
 * Required: InGameDebug Console https://assetstore.unity.com/packages/tools/gui/in-game-debug-console-68068 
 * Required: InfoCanvas - Infolayer for Data, Infos, OpenCV Cam, etc., pure Unity CANVAS
 * Optional: Uduino https://assetstore.unity.com/packages/tools/input-management/uduino-arduino-communication-simple-fast-and-stable-78402
 * 
 * description:
 * more App/System Infos
 * Unity DMT Tools for general use, Config at start UP, etc.
 * Screen Init, Startup Logging, Cursor On/Off, Window resize, etc.
 * Design Pattern: Singleton  
 * ButtonTools für Debugger, Exit, etc.
 * 
 * Features with public var
 * 
 * c ... Cursor On/Off
 * f ... full screen to window toggle - default: full screen
 * ! ... inGame DebugConsole - default: not shown
 * r ... reset playerPrefs (delete) 
 * i ... infos
 * p     pause (timeScale)
 * s.....screenshot
 * esc ... Quit
 * 
 * generale Features with IngameDebugConsole (always needed)
 * https://assetstore.unity.com/packages/tools/gui/in-game-debug-console-68068
 * 
 */


namespace DMT
{
    /// <summary>
    /// ApplicationManager with general inits and tools
    /// </summary>
    /// <remarks>
    /// needs the wonderful IngameDebugConsole from yasirkula 
    /// </remarks>

    public class ApplicationManager40 : MonoBehaviour
    {
        [Space(1, order = 0)]
        [Header("DMT Application Manager with FullHD Configuration", order = 1)]
        [Space(-8, order = 2)]
        [Header("press [h] for Help when running (v4.0)", order = 3)]
        [Space(10, order = 4)]

        [Tooltip("Hide (true) the Cursor at startup [c]")]
        public bool hideCursor = true;
        [Tooltip("Hide/Show the IngameDebugConsole at startup [!]")]
        public bool hideConsole = true;  // IngameDebugConsole
        [Tooltip("Hide/Show InfoCanvas at startup [TAB]")]
        public bool hideInfoCanvas = true;  // InfoCanvas

        [Space(10)]
        [Tooltip("Activate Screenshot command [s]")]
        public bool activeScreenshot = false;  // Screenshot active
        [Tooltip("Activate Pause [p]")]
        public bool activePause = false;  // Pause active

        [Space(10)]
        [Header("Screen resolution parameters")]
        [Tooltip("Overwrite Buildsettings and Registry when true")]
        public bool screenControl = true;
        [Tooltip("Use FullHD LandScape Mode, off = portrait Mode")]
        public bool screenLandscape = true;
        [Tooltip("X Resolution [1920 px]")]
        public int resX = 1920;
        [Tooltip("Y Resolution [1080 px]")]
        public int resY = 1080;

        // ---------------------------------------------------------

        // install https://assetstore.unity.com/packages/tools/gui/in-game-debug-console-68068 
        private GameObject myDebugConsole;   // reference to InGameDebugConsole 
        private GameObject myUduinoConsole;  // reference to Uduino UI_Minimal 
        private GameObject myInfoCanvas;

        private bool screenFull = true;     // full screen 

        [HideInInspector]
        public int startLog = 0;        // count startups of app 

        [ContextMenu("Show Help Info in Console")]
        public void WriteHelpMsg()
        {
            ShowInfos();
        }

        private bool singleton = true;
        private static ApplicationManager40 instance = null;

        // ---------------------------------------------------------
        // ---------------------------------------------------------

        private void Awake()
        {
            Debug.Log("##### Awake ApplicationManager.cs - check Singleton");
            // Only one instance of applicationManager is allowed
            if (instance == null)
            {
                instance = this;

                // If it is a singleton object, don't destroy it between scene changes
                if (singleton)
                    DontDestroyOnLoad(this.gameObject);
            }
            else if (this != instance)
            {
                Debug.LogWarning("!!!!! Warning: ApplicationManager is a Singleton");
                Destroy(gameObject);
                return;
            }

            ShowInfos();
        }

        void Start()
        {
            Debug.Log("##### Start: Init ApplicationManager.cs >> c..Cursor, !..inGameDebugger f..full/window");
            DebugLogConsole.AddCommand("info", "Shows ApplicationManager Info", ShowInfosIGD);

            #region AppMngr Init Area

            if (hideCursor) Cursor.visible = false;

            // ##############################################################################################
            // ##### Info Canvas

            myInfoCanvas = GameObject.Find("InfoCanvas");
            // myInfoCanvas.SetActive(!config.bHideScreenInfo);

            if (myInfoCanvas == null)
                Debug.LogWarning("!!!!! No InfoCanvas found - if you want include the Canvas to your project");
            else
              if (hideInfoCanvas) myInfoCanvas.SetActive(false);

            // ##############################################################################################
            // ##### InGame DebugConsole Controlling

            myDebugConsole = GameObject.Find("IngameDebugConsole");
            if (hideConsole) myDebugConsole.SetActive(false);
            DebugLogConsole.AddCommand("info", "Write ApplicationManager Info to Screen", InGameDebugInfo);

            if (myDebugConsole == null)
                Debug.LogError("!!!!! No IngameDebugConsole found - include the Asset to your project");

            // ##############################################################################################
            // ##### Uduino MiniInfo View

            myUduinoConsole = GameObject.Find("UI_Minimal");
            if (myUduinoConsole == null)
                Debug.LogWarning("!!!!! No Uduino UI_Minimal found - if you use no Arduino this is OK!");
            else
                if (myInfoCanvas) myUduinoConsole.SetActive(false);

            // ##############################################################################################

            IncStartLog(); // log StartUp in PlayerPrefs

            // set and show screen infos
            if (screenControl)
            {
                if (screenLandscape)
                    Screen.SetResolution(resX, resY, true);  // changed 17/4/2019 - kimus other smaller screen
                else
                    Screen.SetResolution(resY, resX, true);
                // Screen.SetResolution(768, 1366, true);
                // Screen.SetResolution(1080, 1920, true);
            }

            if (screenControl) Screen.fullScreenMode = FullScreenMode.FullScreenWindow;

            Debug.Log("##### ==2026=====================================================================NIS==");
            Debug.Log("##### ################################################################################");
            Debug.Log("<b><color='white'>##### ********************************************************************************</color></b>");

            // AppInit END ###################################################################
            #endregion
        }

        void Update()
        {
            #region AppMngr Update Area

            // ###################################################################
            // ## Application Manager - Update
            //
            // Keys for Debugging 
            //
            // f full screen to window toggle - default: full screen
            // ! inGame DebugConsole - default: not shown
            // c Cursor
            // r reset playerPrefs (delete) 
            // i..infos
            // p pause (timeScale)
            // s .....screenshot
            // 

            if (Input.GetKey("escape")) Application.Quit();

            // https://docs.unity3d.com/ScriptReference/PlayerPrefs.DeleteAll.html
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown("1"))
            {
                myDebugConsole.SetActive(!myDebugConsole.activeSelf);  // ! .. show Console
                Cursor.visible = myDebugConsole.activeSelf; // also turn cursor on or off 
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                bool stateInfoCanvas = false;

                if (myInfoCanvas != null)
                {
                    stateInfoCanvas = myInfoCanvas.activeSelf;
                    Debug.Log("InfoCanvas State old:" + stateInfoCanvas + " to " + !stateInfoCanvas);
                    myInfoCanvas.SetActive(!stateInfoCanvas);
                }
                else
                    Debug.LogWarning("No InfoCanvas found! So do not use it!");

                if (myUduinoConsole != null)
                {
                    bool stateInfoUduino = myUduinoConsole.activeSelf;
                    myUduinoConsole.SetActive(!stateInfoCanvas);
                }
            }

            if (Input.GetKeyDown("f")) // f full toggle
            {
                if (screenFull)
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                else
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                screenFull = !screenFull;
            }

            if (Input.GetKeyDown("p") && activePause) // p pause
            {
                if (Time.timeScale < 0.1f)
                    Time.timeScale = 1.0f;
                else
                    Time.timeScale = 0.0f;
            }

            if (Input.GetKeyDown("s") && activeScreenshot) StartCoroutine(TakeScreenShot());
            if (Input.GetKeyDown("c")) Cursor.visible = !Cursor.visible; // add startLog
            if (Input.GetKeyDown("h")) ShowInfos(); // show help
            if (Input.GetKeyDown("r")) ResetPlayerPrefs(); // init all PlayerPrefs

            // ## Application Manager: Update code end
            // ###################################################################
            #endregion
        }

        #region AppMngr Button Tools
        // used for button interaction - ButtonEvent
        public void AppMngrButtonDebug()
        {
            myDebugConsole.SetActive(!myDebugConsole.activeSelf);  // ! .. show Console
            Cursor.visible = myDebugConsole.activeSelf; // also turn cursor on or off 
        }


        // used for button interaction - ButtonEvent
        public void AppMngrButtonExit()
        {
            Application.Quit();
        }
        #endregion

        #region AppMngr methods definition 

        // ###################################################################
        // ## Application Manager: methods start

        void IncStartLog()
        {
            // https://docs.unity3d.com/ScriptReference/PlayerPrefs.DeleteAll.html
            int startLog = PlayerPrefs.GetInt("startLog", 0);
            PlayerPrefs.SetInt("startLog", ++startLog);

            Debug.Log("##### ApplicationManager: startLog Counter (prefab) <b><color='lightblue'>" + startLog + "</b></color> [regedit: " + Application.companyName + "]");
        }

        void ResetPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt("startLog", startLog);

            startLog = PlayerPrefs.GetInt("startLog", 0);
            Debug.Log("##### ApplicationManager [r]: ResetPlayerPrefs (prefab) StartLog>" + startLog);
        }

        void ShowInfos()
        {
            Debug.Log("<b><color='white'>##### ********************************************************************************</color></b>");
            Debug.Log("##### ================================================================================");
            Debug.Log("##### INFO AppMngr >> h   ... HelpScreen NOW       f ... FullScreen/Window            ");
            Debug.Log("#####              >> c   ... Cursor ON/OFF        r ... ResetPrefabs                 ");
            Debug.Log("#####              >> esc ... End Program          p     Pause Toggle                 ");
            Debug.Log("#####                                                                                 ");
            Debug.Log("***** =================================================================AppMng_4.0=nis=");
            Debug.Log("***** System Device-ID: " + SystemInfo.deviceUniqueIdentifier);
            Debug.Log("***** System OperatingSys: " + SystemInfo.operatingSystem);
            Debug.Log("***** System Memory: " + SystemInfo.systemMemorySize);
            Debug.Log("***** System Graphics: " + SystemInfo.graphicsDeviceName);
            Debug.Log("***** System Device Name: " + SystemInfo.deviceName);
            Debug.Log("***** System Device Model: " + SystemInfo.deviceModel);
            Debug.Log("***** --------------------------------------------------------------------------------");
            Debug.Log("***** Application Name: <b><color='yellow'>" + Application.productName + "</b></color>");
            Debug.Log("***** Application Version: <b><color='yellow'>" + Application.version + "</b></color>");
            Debug.Log("***** Application Platform: " + Application.platform);
            Debug.Log("***** Application URL: " + Application.absoluteURL);
            Debug.Log("***** Application ID: " + Application.cloudProjectId);
            Debug.Log("***** Application Language: <b><color='green'>" + Application.systemLanguage + "</b></color>");
            Debug.Log("***** ================================================================================");

            if (Application.systemLanguage == SystemLanguage.German)
                Debug.Log("##### System Langage: <b><color='green'>" + Application.systemLanguage + "</b></color>" + " -- Platform: "
                    + Application.platform + " -- Sys: " + SystemInfo.operatingSystem);
            else
                Debug.LogWarning("##### System Language <b><color='red'>(NOT GERMAY): " + Application.systemLanguage + "</b></color>"
                     + " -- Platform: " + Application.platform + " -- Sys: " + SystemInfo.operatingSystem);

            int startLog = PlayerPrefs.GetInt("startLog", 0);
            Debug.Log("##### ApplicationManager: startLog Counter (prefab) " + startLog + " [regedit: " + Application.companyName + "]");
            Debug.Log("##### Screen Info >> <b><color='white'>" + Screen.width + " x " + Screen.height + "</b></color>" + " -- Orient: " + Screen.orientation +
               " -- MonitorRes: " + Screen.currentResolution + " -- FullScr: " + Screen.fullScreen);

            Debug.Log("##### Info END                                                                        ");

        }



        IEnumerator TakeScreenShot()
        {
            // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ScreenCapture.CaptureScreenshot.html
            //  In the Editor, filename is a filepath relative to the project directory.
            yield return new WaitForEndOfFrame();
            string filename = "Assets/StreamingAssets/appManager_screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            ScreenCapture.CaptureScreenshot(filename, 2);
            Debug.Log("ApplicationManager Screenshot taken!");
        }

        void ShowInfosIGD() // cammand for InGame DebugConsole
        {
            Debug.Log("INFO APPMGR> ApplicationManager: [h]...HelpScreen [f]...FullScreen/Window [esc]...End Program \n" +
                      "INFO APPMGR> [c]...Cursor ON/OFF [r]...ResetPrefabs >>>[NIS/IMA/DMT]<<< ");
        }

        // ## Application Manager: methods end
        // ###################################################################

        #endregion

        #region AppMngr InGameDebugConsole Enhancements

        public void InGameDebugInfo()
        {
            ShowInfos();
        }

        #endregion

    }
}

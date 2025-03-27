using UnityEngine;
using IngameDebugConsole;

namespace DMT
{
    public class StaticStoreDebugConsoleCmds : MonoBehaviour
    {
        void Start()
        {
            DebugLogConsole.AddCommand("posehelp", "Show Pose Help Screen ", PoseHelp);
            DebugLogConsole.AddCommand<bool>("poseskel", "Show Skeleton Mask ", ShowSkeleton);
            DebugLogConsole.AddCommand<bool>("posebb", "Show BoundingBox Mask ", ShowBoundingBox);
        }

        public static void PoseHelp()
        {
            Debug.Log("Pose Help Screen");
            Debug.Log("  CMD: poseskeleton true/false");
        }

        public static void ShowSkeleton(bool show)
        {
            Debug.Log("!!!!! Show Skeleton Mask: " + show);
            DMT.StaticStore.ShowSkeletonLayer = show;
        }

        public static void ShowBoundingBox(bool show)
        {
            Debug.Log("!!!!! Show BoundingBox Mask: " + show);
            DMT.StaticStore.ShowBoundingBoxLayer = show;
        }
    }
}
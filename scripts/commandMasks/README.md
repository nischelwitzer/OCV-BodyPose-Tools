# Commands for InGame Debug Console

* Needed: https://github.com/yasirkula/UnityIngameDebugConsole

## Commands

* poseskel 0|1 for PoseSkeleton Layer ON/OFF
* posebb 0|1 for PoseBoundingBox Layer ON/OFF
* using IngameDebugConsole;

```
DebugLogConsole.AddCommand("posehelp", "Show Pose Help Screen ", PoseHelp);
DebugLogConsole.AddCommand<bool>("poseskel", "Show Skeleton Mask ", ShowSkeleton);
DebugLogConsole.AddCommand<bool>("posebb", "Show BoundingBox Mask ", ShowBoundingBox);
``` 

## Example Usage

```
public static void ShowSkeleton(bool show)
{
  Debug.Log("!!!!! Show Skeleton Mask: " + show);
  DMT.StaticStore.ShowSkeletonLayer = show;
}
```

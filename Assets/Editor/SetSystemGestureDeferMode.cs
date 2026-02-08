using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.iOS;
#endif

namespace ArcCreate.EditorScripts
{
    public class SetDeferSystemGestures : MonoBehaviour
    {
        [MenuItem("Tools/Set Defer System Gestures")]
        public static void SetGestureMode()
        {
#if UNITY_EDITOR
            // 设置延迟系统手势
            PlayerSettings.iOS.deferSystemGesturesMode = SystemGestureDeferMode.BottomEdge;
            Debug.Log("System Gestures Mode set to Defer");
#endif
        }
    }
}
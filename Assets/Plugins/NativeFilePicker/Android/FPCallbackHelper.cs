#if UNITY_EDITOR || UNITY_ANDROID
using System;
using UnityEngine;

namespace NativeFilePickerNamespace
{
    public class FPCallbackHelper : MonoBehaviour
    {
        private Action mainThreadAction;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (mainThreadAction != null)
            {
                var temp = mainThreadAction;
                mainThreadAction = null;
                temp();
            }
        }

        public void CallOnMainThread(Action function)
        {
            mainThreadAction = function;
        }
    }
}
#endif
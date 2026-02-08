// SPDX-FileCopyrightText: © 2021 Chris Marc Dailey (nitz)
// SPDX-License-Identifier: MIT

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using System.IO;
using UnityEditor.iOS.Xcode;
#endif

public class EntitlementsPostprocessStep : MonoBehaviour
{
    [PostProcessBuild(2)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        switch (target)
        {
            case BuildTarget.iOS:
                UpdateInfoPlist(pathToBuiltProject);
                break;
            default:
                // nothing to do for this platform.
                return;
        }
    }

    private static void UpdateInfoPlist(string path)
    {
#if UNITY_IOS
        var plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        var rootDict = plist.root;

        rootDict.SetString("Appearance", "Dark");
        rootDict.SetString("UIUserInterfaceStyle", "Dark");
        rootDict.SetBoolean("CADisableMinimumFrameDurationOnPhone", true);
        var nsAppTransportSecurity = rootDict.CreateDict("NSAppTransportSecurity");
        nsAppTransportSecurity.SetBoolean("NSAllowsArbitraryLoads", true);
        nsAppTransportSecurity.SetBoolean("NSAllowsLocalNetworking", true);
        rootDict.SetString(
            "NSLocalNetworkUsageDescription",
            "ArcCreate requires local network for setting up connection with desktop client.");

        plist.WriteToFile(plistPath);
#endif // UNITY_IOS
    }
}
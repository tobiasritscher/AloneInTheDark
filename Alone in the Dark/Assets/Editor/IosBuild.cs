using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

/// <summary>Release Xcode export, callable with -executeMethod IosBuild.Export.</summary>
public static class IosBuild
{
    public static void Export()
    {
#if UNITY_IOS
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            throw new BuildFailedException("Install iOS Build Support for this Unity editor.");

        string output = Path.GetFullPath(Environment.GetEnvironmentVariable("IOS_BUILD_PATH") ?? "Builds/iOS");
        string buildNumber = Environment.GetEnvironmentVariable("IOS_BUILD_NUMBER");
        if (string.IsNullOrEmpty(buildNumber) || !int.TryParse(buildNumber, out int number) || number < 1)
            throw new BuildFailedException("Set IOS_BUILD_NUMBER to a positive, unused build number.");

        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.buildNumber = buildNumber;
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        string team = Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
        if (!string.IsNullOrEmpty(team))
            PlayerSettings.iOS.appleDeveloperTeamID = team;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes in Build Settings.");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.iOS,
            options = BuildOptions.None,
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("iOS export failed: " + report.summary.result);

        // This offline game implements no encryption. Avoid an unanswered export-compliance
        // prompt preventing the build from becoming available to TestFlight testers.
        string plistPath = Path.Combine(output, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        plist.WriteToFile(plistPath);
        Debug.Log("iOS release export succeeded: " + output + " (build " + buildNumber + ")");
#else
        throw new BuildFailedException("Run Unity with -buildTarget iOS and iOS Build Support installed.");
#endif
    }
}

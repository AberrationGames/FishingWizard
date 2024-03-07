// SC Post Effects
// Staggart Creations
// http://staggart.xyz

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;
#if PPS
using UnityEngine.Rendering.PostProcessing;
#endif
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace SCPE
{
    public class Installer : Editor
    {
        public const string PPSInstallationMarkerGUID = "672cf712daadb2244b888f3abb6e100d";
        public const string PPSScriptPackageGUID = "7920bf546e454b34892bc7cc4dce8e12";

        public const string UniversalScriptPackageGUID = "7c20a298fa96a614db6060a1229f53d3";
        public const string URPInstallationMarkerGUID = "751b255a24402084da7237349738181f";

        public const string DemoScenePackageGUID = "b289996a45bf9ac419c8cd814cff0b56";
        public const string SampleTexturePackageGUID = "be50a037e746a564296e09704e7073a6";
        
        public class RunOnImport : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (string str in importedAssets)
                {
                    //Changed every version, so will trigger when updating or installing the first time
                    if (str.Contains("SCPE.cs"))
                    {
                        #if !URP && !PPS //Missing PPS dependency in built-in RP, prompt window
                        InstallerWindow.ShowWindow();
                        return;
                        #endif
                        
                        #if URP || PPS //Post-processing framework already present
                        ScriptPackages.Initialize();

                        //Prompt if no script packages were unpacked, or they're now outdated
                        if (Installer.ScriptPackages.PACKAGE_INSTALL_STATE == ScriptPackages.PackageInstallState.None || Installer.ScriptPackages.PACKAGE_VERSION_STATE == ScriptPackages.PackageVersionState.Outdated)
                        {
                            InstallerWindow.ShowWindow();

                            if (Installer.ScriptPackages.PACKAGE_VERSION_STATE == ScriptPackages.PackageVersionState.Outdated)
                            {
                                if (EditorUtility.DisplayDialog(SCPE.ASSET_NAME, "You are updating this asset.\n\nThe console may display shader errors, these will be automatically resolved after using the \"Update files\" option", "Ok"))
                                {
                                    Installer.Shaders.ClearConsoleLog();
                                }
                            }
                            
                            return;
                        }
                        #endif
                    }
                }
            }
        }

        private static List<UnityEditor.PackageManager.PackageInfo> packages;

        public static void Initialize()
        {
            AssetVersionCheck.CheckForUpdate();
            UnityVersionCheck.CheckCompatibility();
            PackageManager.RetreivePackageList();

            RenderPipelineInstallation.CheckInstallation(); //Check first, in case URP is installed
            PostProcessingInstallation.CheckPackageInstallation();

            ScriptPackages.Initialize();
        }
        
#if SCPE_DEV
        [MenuItem("SCPE/Installer/Add layer")]
#endif
        public static void SetupLayer()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty layers = tagManager.FindProperty("layers");

            bool hasLayer = false;

            //Skip default layers
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);

                if (layerSP.stringValue == SCPE.PP_LAYER_NAME)
                {
#if SCPE_DEV
                    Debug.Log("<b>SetupLayer</b> " + SCPE.PP_LAYER_NAME + " layer already present");
#endif
                    hasLayer = true;
                    return;
                }

                if (layerSP.stringValue == String.Empty)
                {
                    layerSP.stringValue = SCPE.PP_LAYER_NAME;
                    tagManager.ApplyModifiedProperties();
                    hasLayer = true;
#if SCPE_DEV
                    Debug.Log("<b>SetupLayer</b> " + SCPE.PP_LAYER_NAME + " layer added");
#endif
                    return;
                }
            }

            if (!hasLayer)
            {
                Debug.LogError("The layer \"" + SCPE.PP_LAYER_NAME + "\" could not be added, the maximum number of layers (32) has been exceeded");
#if UNITY_2018_3_OR_NEWER
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
#else
                EditorApplication.ExecuteMenuItem("Edit/Project Settings/Tags and Layers");
#endif
            }

        }

        public class Demo
        {
            public static string SCENES_PACKAGE_PATH
            {
                get { return SessionState.GetString(SCPE.ASSET_ABRV + "_DEMO_PACKAGE_PATH", string.Empty); }
                set { SessionState.SetString(SCPE.ASSET_ABRV + "_DEMO_PACKAGE_PATH", value); }
            }
            public static string SAMPLES_PACKAGE_PATH
            {
                get { return SessionState.GetString(SCPE.ASSET_ABRV + "_SAMPLES_PACKAGE_PATH", string.Empty); }
                set { SessionState.SetString(SCPE.ASSET_ABRV + "_SAMPLES_PACKAGE_PATH", value); }
            }
            
            public static bool SCENES_INSTALLED
            {
                get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_DEMO_INSTALLED", false); }
                set { SessionState.SetBool(SCPE.ASSET_ABRV + "_DEMO_INSTALLED", value); }
            }
            public static bool SAMPLES_INSTALLED
            {
                get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_SAMPLES_INSTALLED", false); }
                set { SessionState.SetBool(SCPE.ASSET_ABRV + "_SAMPLES_INSTALLED", value); }
            }
            
            public static void CheckInstallation()
            {
                SCPE.UpdateRootFolder();
                
                SCENES_INSTALLED = AssetDatabase.IsValidFolder(SCPE.PACKAGE_ROOT_FOLDER + "/Install/_DemoContent (Built-in RP)/");
                SCENES_INSTALLED = AssetDatabase.IsValidFolder(SCPE.PACKAGE_ROOT_FOLDER + "/Install/SampleTextures/");

#if SCPE_DEV
                Debug.Log("<b>Demo</b> Scenes installed: " + SCENES_INSTALLED);
                Debug.Log("<b>Demo</b> Samples installed: " + SCENES_INSTALLED);
#endif
            }

            public static void InstallScenes()
            {
                SCENES_PACKAGE_PATH = AssetDatabase.GUIDToAssetPath(Installer.DemoScenePackageGUID);

                if (!string.IsNullOrEmpty(SCENES_PACKAGE_PATH))
                {
                    AssetDatabase.ImportPackage(SCENES_PACKAGE_PATH, true);

                    SCENES_INSTALLED = true;
                }
                else
                {
                    Debug.LogError("The \"_DemoContent\" package could not be found, please ensure all the package contents were imported from the Asset Store.");
                    SCENES_INSTALLED = false;
                }
            }

            public static void InstallSamples()
            {
                SAMPLES_PACKAGE_PATH = AssetDatabase.GUIDToAssetPath(Installer.SampleTexturePackageGUID);
                
                if (!string.IsNullOrEmpty(SAMPLES_PACKAGE_PATH))
                {
                    AssetDatabase.ImportPackage(SAMPLES_PACKAGE_PATH, true);
                    
                    SAMPLES_INSTALLED = true;
                }
                else
                {
                    Debug.LogError("The \"SampleTextures\" package could not be found, please ensure all the package contents were imported from the Asset Store.");
                    SAMPLES_INSTALLED = false;
                }
            }
        }

        public class ScriptPackages
        {
            public static void Initialize()
            {
                PACKAGE_INSTALL_STATE = PackageInstallState.None;
                
                var pps = PPSUnpacked();
                if (pps) PACKAGE_INSTALL_STATE = PackageInstallState.BuiltInRP;
                
                var urp = URPUnpacked();
                if (urp) PACKAGE_INSTALL_STATE = PackageInstallState.URP;

                CheckScriptPackageVersion();
            }

            private static void CheckScriptPackageVersion()
            {
                string version;
                string path = string.Empty;
                
                if (PACKAGE_INSTALL_STATE == PackageInstallState.BuiltInRP)
                {
                    path = AssetDatabase.GUIDToAssetPath(PPSInstallationMarkerGUID);
                }
                else if (PACKAGE_INSTALL_STATE == PackageInstallState.URP)
                {
                    path = AssetDatabase.GUIDToAssetPath(URPInstallationMarkerGUID);
                }

                //Nothing installed yet
                if (path == string.Empty)
                {
                    PACKAGE_VERSION = "";
                    
                    //Script files unpacked, yet installation marker is missing
                    if(PACKAGE_INSTALL_STATE != PackageInstallState.None) Debug.LogError("[SC Post Effects] Unable to find installation marker file. Was the txt file removed or its GUID changed?");
                    
                    return;
                }

                string[] fileLines = File.ReadAllLines(path);
                version = fileLines[0];

                //Version 2.1.8 and older don't yet have the version number in the file
                if (version.Length > 5)
                {
                    PACKAGE_VERSION = "< 2.1.9";
                    PACKAGE_VERSION_STATE = PackageVersionState.Outdated;
                    return;
                }
                
                PACKAGE_VERSION = version;
                
                Version assetVersion = new System.Version(SCPE.INSTALLED_VERSION);
                Version scriptVersion = new System.Version(version);
                
                if (scriptVersion >= assetVersion) PACKAGE_VERSION_STATE = PackageVersionState.UpToDate;
                if (scriptVersion < assetVersion) PACKAGE_VERSION_STATE = PackageVersionState.Outdated;
                
                #if SCPE_DEV
                Debug.Log("Script package version " + version + ". State: " + PACKAGE_VERSION_STATE.ToString());
                #endif
            }

            public enum PackageInstallState
            {
                None,
                BuiltInRP,
                URP
            }
            public static PackageInstallState PACKAGE_INSTALL_STATE
            {
                get { return (PackageInstallState)SessionState.GetInt(SCPE.ASSET_ABRV + "PACKAGE_INSTALL_STATE", 0); }
                set { SessionState.SetInt(SCPE.ASSET_ABRV + "PACKAGE_INSTALL_STATE", (int)value); }
            }
            public enum PackageVersionState
            {
                UpToDate,
                Outdated,
            }
            public static PackageVersionState PACKAGE_VERSION_STATE
            {
                get { return (PackageVersionState)SessionState.GetInt(SCPE.ASSET_ABRV + "PACKAGE_VERSION_STATE", 0); }
                set { SessionState.SetInt(SCPE.ASSET_ABRV + "PACKAGE_VERSION_STATE", (int)value); }
            }
            
            public static string PACKAGE_VERSION
            {
                get { return SessionState.GetString(SCPE.ASSET_ABRV + "PACKAGE_VERSION", "0.0.0"); }
                set { SessionState.SetString(SCPE.ASSET_ABRV + "PACKAGE_VERSION", value); }
            }

            private static bool IsUnpacked(string markerGuid, string fileName)
            {
                string path = AssetDatabase.GUIDToAssetPath(markerGuid);
                UnityEngine.Object file = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

                bool markerPresent = file;

                //Fallback in case the file was not yet imported
                if (!markerPresent)
                {
                    SCPE.UpdateRootFolder();
                    
                    string runtimeFolder = SCPE.PACKAGE_ROOT_FOLDER + "/Runtime";
                    
                    var info = new DirectoryInfo(runtimeFolder);
                    FileInfo[] fileInfo = info.GetFiles();

                    foreach (FileInfo item in fileInfo)
                    {
                        if (item.Name.Contains(fileName)) markerPresent = true;
                    }
                }

    #if SCPE_DEV
                Debug.Log(fileName + " installation marker " + (markerPresent ? "" : "NOT") + " present");
    #endif
                return markerPresent;
            }
            
    #if SCPE_DEV
            [MenuItem("SCPE/TEST/Is URP unpacked?")]
    #endif
            public static bool URPUnpacked()
            {
                //<=2.1.8 upgrade, remove old package for safety
                string path = AssetDatabase.GUIDToAssetPath("91f16b1b54b30554b8d0074f9d4bab1b");
                UnityEngine.Object file = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

                if (file)
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log("[SC Post Effects] The now obsolete \"_URP_VolumeSystem\" package was deleted. Script packages can now be found in the <i>/Install</i> folder.");
                }
                
                return IsUnpacked(URPInstallationMarkerGUID, "URP_INSTALLED.txt");
            }
            
    #if SCPE_DEV
            [MenuItem("SCPE/TEST/Is PPS unpacked?")]
    #endif
            public static bool PPSUnpacked()
            {
                return IsUnpacked(PPSInstallationMarkerGUID, "PPS_INSTALLED.txt");
            }

            
            private static System.Diagnostics.Stopwatch sw;

            public static void UnpackScriptShaders()
            {
                bool shadersRequireMove = Installer.Shaders.CheckResourcesState();

                if (shadersRequireMove)
                {
                    EditorUtility.DisplayDialog(SCPE.ASSET_NAME, "You are updating the asset to v2.3.0+.\n\nShaders have been found in \'Resources\' folders, which is no longer necessary.\n\nThey'll automatically be moved for you, this may take up to a minute.", "OK");
                    
                    Installer.Shaders.MoveShadersOutOfResourceFolders();
                }
                
                if (RenderPipelineInstallation.CurrentPipeline == RenderPipelineInstallation.Pipeline.BuiltIn)
                {
                    UnpackPPSScripts();
                }
                if (RenderPipelineInstallation.CurrentPipeline == RenderPipelineInstallation.Pipeline.URP)
                {
                    UnpackURPFiles();
                }
                
                Installer.Shaders.CheckShaderReferences();
            }
            
            public static void UnpackPPSScripts()
            {
                string packagePath = AssetDatabase.GUIDToAssetPath(Installer.PPSScriptPackageGUID);

                if (packagePath == string.Empty)
                {
                    Debug.LogError("Script package with the GUID: <b>" + Installer.PPSScriptPackageGUID + "</b>. Could not be found in the project, was it changed or not imported? It should be located in <i>" + SCPE.PACKAGE_ROOT_FOLDER + "/Install</i>");
                    return;
                }

                sw = new Stopwatch();
                sw.Start();
                
                AssetDatabase.ImportPackage(packagePath, true);
                AssetDatabase.importPackageCompleted += new AssetDatabase.ImportPackageCallback(PackageImportCallback);

                Installer.ScriptPackages.PACKAGE_INSTALL_STATE = Installer.ScriptPackages.PackageInstallState.BuiltInRP;
                PACKAGE_VERSION_STATE = PackageVersionState.UpToDate;
            }
            
            public static void UnpackURPFiles()
            {
                string guid = Installer.UniversalScriptPackageGUID;
                string packagePath = AssetDatabase.GUIDToAssetPath(guid);

                if (packagePath == string.Empty)
                {
                    Debug.LogError("URP script package with the GUID: <b>" + guid + "</b>. Could not be found in the project, was it changed or not imported? It should be located in <i>" + SCPE.PACKAGE_ROOT_FOLDER + "/Install</i>");
                    return;
                }

                sw = new Stopwatch();
                sw.Start();

                AssetDatabase.ImportPackage(packagePath, true);
                AssetDatabase.importPackageCompleted += new AssetDatabase.ImportPackageCallback(PackageImportCallback);
            
                Installer.ScriptPackages.PACKAGE_INSTALL_STATE = Installer.ScriptPackages.PackageInstallState.URP;
                PACKAGE_VERSION_STATE = PackageVersionState.UpToDate;
            }

            static void PackageImportCallback(string packageName)
            {
                sw.Stop();
                
                Debug.Log(string.Format($"Successfully imported the <b>{packageName}</b> package. Duration: {sw.Elapsed.Minutes}m{sw.Elapsed.Seconds}s"));
                
                AssetDatabase.importPackageCompleted -= PackageImportCallback;
            }
        }

        public class Shaders
        {
            #if SCPE_DEV
            [MenuItem("SCPE/Installer/Shaders need moving?")]
            #endif
            public static bool CheckResourcesState()
            {
                bool state = false;

                SCPE.UpdateRootFolder();
                string effectDir = SCPE.PACKAGE_ROOT_FOLDER + "/Runtime/";
                
                DirectoryInfo rootInfo = new DirectoryInfo(effectDir);
                DirectoryInfo[] runtimeDirs = rootInfo.GetDirectories();
                
                //Every directory under /Runtime
                foreach (DirectoryInfo dir in runtimeDirs)
                {
                    if(state) continue;

                    //Directories inside the Effect's folder
                    DirectoryInfo[] dirs = dir.GetDirectories();
                    
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        //Debug.Log(dirs[i].Name);
                        if (dirs[i].Name.Contains("Resources"))
                        {
                            state = true;
                            break;
                        }
                    }
                }
                
                #if SCPE_DEV
                if(state) Debug.Log("There are shaders in Resources folders, and need moving");
                #endif
                
                return state;
            }

            #if SCPE_DEV
            [MenuItem("SCPE/Installer/Move shaders")]
            #endif
            public static void MoveShadersOutOfResourceFolders()
            {
                SCPE.UpdateRootFolder();
                string effectDir = SCPE.PACKAGE_ROOT_FOLDER + "/Runtime/";
                
                DirectoryInfo rootInfo = new DirectoryInfo(effectDir);
                DirectoryInfo[] runtimeDirs = rootInfo.GetDirectories();

                int counter = 0;
                //Every directory under /Runtime
                foreach (DirectoryInfo dir in runtimeDirs)
                {
                    //Directories inside the Effect's folder
                    DirectoryInfo[] dirs = dir.GetDirectories();
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        //Debug.Log(dirs[i].Name);
                        if (dirs[i].Name.Contains("Resources"))
                        {
                            FileInfo[] files = dirs[i].GetFiles();

                            for (int j = 0; j < files.Length; j++)
                            {
                                if (files[j].Extension == ".shader")
                                {
                                    string dataPath = Application.dataPath;
                                    dataPath = dataPath.Replace("/", "\\");
                                    
                                    string targetFolder = files[j].FullName.Replace("\\Resources", string.Empty);
                                    string relativeFilePath = files[j].FullName.Replace(dataPath, "Assets");
                                    
                                    //Convert to relative path
                                    targetFolder = targetFolder.Replace(dataPath, "Assets");
                                    
                                    #if SCPE_DEV
                                    //Debug.Log($"Moving from <i>{relativeFilePath}</i> to <i>{targetFolder}</i>");
                                    #endif

                                    //UpdateIncludePaths(files[j].FullName);
                                    
                                    AssetDatabase.MoveAsset(relativeFilePath, targetFolder);
                                    
                                    //Now delete the empty Resources folder
                                    AssetDatabase.DeleteAsset(dirs[i].FullName.Replace(dataPath, "Assets"));

                                    counter++;
                                }
                            }
                        }
                    }
                }
                
                AssetDatabase.Refresh();

                if (counter > 0)
                {
                    ClearConsoleLog();
                    EditorUtility.DisplayDialog(SCPE.ASSET_NAME, $"Successfully moved {counter} shaders out of their \'Resources\' folder.\n\nOnly shaders for effects added to volume profiles will now be included in a build.", "OK");
                }
            }
            
            #if SCPE_DEV
            [MenuItem("SCPE/Installer/CheckShaderReferences")]
            #endif
            public static void CheckShaderReferences()
            {
                //Some users use URP but still have the Post-Processing package installed, this throws a compile error
                #if !(URP && PPS)
                string profileType = "PostProcessProfile";
                #if URP
                profileType = "VolumeProfile";
                #endif
                
                //Find all profile assets in the project
                string[] guids = AssetDatabase.FindAssets("t: " + profileType);

                bool anyChange = false;
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    
                    int settingsCount = 0;

                    #if PPS
                    PostProcessProfile profile = (PostProcessProfile)AssetDatabase.LoadAssetAtPath(path, typeof(PostProcessProfile));
                    settingsCount = profile.settings.Count;
                    #endif
                    
                    #if URP
                    VolumeProfile profile = (VolumeProfile)AssetDatabase.LoadAssetAtPath(path, typeof(VolumeProfile));
                    settingsCount = profile.components.Count;
                    #endif
                    
                    ScriptableObject s = null;
                    
                    //Go over every settings type on the profile
                    //Execute the 'SerializeShader' method on the class if it's present
                    for (int j = 0; j < settingsCount; j++)
                    {
                        #if PPS
                        s = profile.settings[j];
                        #endif
                        
                        #if URP && !PPS
                        s = profile.components[j];
                        #endif
                        
                        //Function is present on all SCPE setting classes
                        MethodInfo resetMethod = (s.GetType()).GetMethod("SerializeShader", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (resetMethod != null)
                        {
                            bool wasReferenced = (bool)resetMethod.Invoke(s, null);;

                            if (wasReferenced)
                            {
                                anyChange = true;
                                
                                #if PPS || URP
                                EditorUtility.SetDirty(profile);
                                
                                Debug.LogFormat("<b>[SC Post Effects - Automatic update]</b> <i>{0}</i> shader is now referenced on the \"<i>{1}</i>\" profile asset", 
                                    s.GetType().Name, 
                                    profile.name);
                                #endif
                            }
                        }
                    }
                }
                
                if(anyChange) AssetDatabase.SaveAssets();
                #if SCPE_DEV
                else Debug.Log("[CheckShaderReferences] All shaders referenced on profile assets");
                #endif
                #endif //Not both URP && PPS
            }

            public static void SerializeShader(Object profileComponent)
            {
                MethodInfo resetMethod = (profileComponent.GetType()).GetMethod("SerializeShader", BindingFlags.NonPublic | BindingFlags.Instance);
                resetMethod.Invoke(profileComponent, null);
                
                EditorUtility.SetDirty(profileComponent);
            }

            private static void UpdateIncludePaths(string shaderFilePath)
            {
                string fileContents = File.ReadAllText(shaderFilePath);
                
                fileContents = fileContents.Replace("#include \"../", "#include \"");
                
                File.WriteAllText(shaderFilePath, fileContents);
            }
            
            //The console may show shader compile errors even after they've been fixed by updating. 
            //Unlike script compile errors, these won't automatically clear.
            //Instead, force the console to clear
            public static void ClearConsoleLog()
            {
                Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
                Type type = assembly.GetType("UnityEditor.LogEntries");
                MethodInfo clearMethod = type.GetMethod("Clear");
                clearMethod.Invoke(new object(), null);
            }
        }
    }

    public class PackageManager
    {
        public static List<UnityEditor.PackageManager.PackageInfo> packages;

        public static void RetreivePackageList()
        {
            UnityEditor.PackageManager.Requests.ListRequest listRequest = Client.List(true, true);

            while (listRequest.Status == StatusCode.InProgress)
            {
                //Waiting...
            }

            if (listRequest.Status == StatusCode.Failure) Debug.LogError("[SC Post Effects] Failed to retrieve packages from Package Manager...");

            PackageCollection packageInfos = listRequest.Result;
            packages = packageInfos.ToList();
        }
    }

    public class PostProcessingInstallation
    {
        public static string PACKAGE_ID = "com.unity.postprocessing";
        
        public static string MIN_PPS_VERSION = "2.3.0";
        public static string MAX_PPS_VERSION = "9.9.9.9";
        public static string LATEST_COMPATIBLE_VERSION
        {
            get { return SessionState.GetString("LATEST_PPS_VERSION", string.Empty); }
            set { SessionState.SetString("LATEST_PPS_VERSION", value); }
        }

        public enum VersionStatus
        {
            NotInstalled,
            Outdated,
            Compatible,
            InCompatible
        }
        public static VersionStatus PPSVersionStatus
        {
            get { return (VersionStatus)SessionState.GetInt("PPS_VERSION_STATUS", 2); }
            set { SessionState.SetInt("PPS_VERSION_STATUS", (int)value); }
        }
        public static string PPS_VERSION
        {
            get { return SessionState.GetString("PPS_VERSION", string.Empty); }
            set { SessionState.SetString("PPS_VERSION", value); }
        }

        public static void CheckPackageInstallation()
        {
            PPSVersionStatus = VersionStatus.NotInstalled;

            if (PackageManager.packages == null) PackageManager.RetreivePackageList();

            foreach (UnityEditor.PackageManager.PackageInfo p in PackageManager.packages)
            {
                if (p.name == PACKAGE_ID)
                {
                    PPS_VERSION = p.version.Replace("-preview", string.Empty);
                    LATEST_COMPATIBLE_VERSION = p.versions.latestCompatible;

                    //Validate installed version against compatible range
                    System.Version curVersion = new System.Version(PPS_VERSION);
                    System.Version minVersion = new System.Version(MIN_PPS_VERSION);
                    System.Version maxVersion = new System.Version(MAX_PPS_VERSION);
                    System.Version latestVersion = new System.Version(LATEST_COMPATIBLE_VERSION);

                    //Clamp to maximum compatible version
                    if (latestVersion > maxVersion) latestVersion = maxVersion;

                    if (curVersion >= minVersion && curVersion <= maxVersion) PPSVersionStatus = VersionStatus.Compatible;
                    if (curVersion < minVersion || curVersion < latestVersion) PPSVersionStatus = VersionStatus.Outdated;
                    if (curVersion < minVersion || curVersion > maxVersion) PPSVersionStatus = VersionStatus.InCompatible;
#if SCPE_DEV
                    Debug.Log("<b>CheckPPSInstallation</b> PPS version " + p.version + " Installed. Required: " + MIN_PPS_VERSION);
#endif
                }
            }

            //PPS not installed
            if (PPSVersionStatus == VersionStatus.NotInstalled)
            {
                UnityEditor.PackageManager.Requests.SearchRequest r = Client.Search(PACKAGE_ID);
                while (r.Status == StatusCode.InProgress)
                {
                    //Waiting
                }
                if (r.IsCompleted)
                {
                    LATEST_COMPATIBLE_VERSION = r.Result[0].versions.latestCompatible;

                    //Clamp to maximum compatible version
                    System.Version maxVersion = new System.Version(MAX_PPS_VERSION);
                    System.Version latestVersion = new System.Version(LATEST_COMPATIBLE_VERSION);
                    if (latestVersion > maxVersion) LATEST_COMPATIBLE_VERSION = MAX_PPS_VERSION;
                }
            }
        }

        public static void InstallFromPackageManager()
        {
            AddRequest addRequest = UnityEditor.PackageManager.Client.Add(PACKAGE_ID + "@" + LATEST_COMPATIBLE_VERSION);

#if SCPE_DEV
            Debug.Log("<b>PostProcessingInstallation</b> Installed from Package Manager");
#endif

            //In case of updating an already installed version
            PPSVersionStatus = VersionStatus.Compatible;
            PPS_VERSION = LATEST_COMPATIBLE_VERSION;

            while(!addRequest.IsCompleted || addRequest.Status == StatusCode.InProgress) { }
        }
    }

    public class RenderPipelineInstallation
    {
        public const string URP_PACKAGE_ID = "com.unity.render-pipelines.universal";
        public const string MIN_URP_VERSION = "10.3.2";
        public const string MAX_URP_VERSION = "999.999.999"; //Not limited 

        public const string HDRP_PACKAGE_ID = "com.unity.render-pipelines.high-definition";
        public const string MIN_HDRP_VERSION = "7.2.0";
        public const string MAX_HDRP_VERSION = "9.99.99"; //Currently no limit

        public enum Pipeline
        {
            BuiltIn,
            URP,
            HDRP
        }
        public static Pipeline CurrentPipeline
        {
            get { return (Pipeline)SessionState.GetInt("SCPE_PIPELINE", 0); }
            set { SessionState.SetInt("SCPE_PIPELINE", (int)value); }
        }

        public enum Version
        {
            Compatible,
            Outdated,
            Incompatible
        }
        public static Version VersionStatus
        {
            get { return (Version)SessionState.GetInt("SRP_VERSION_STATUS", 0); }
            set { SessionState.SetInt("SRP_VERSION_STATUS", (int)value); }
        }

        //Applies to current SRP
        public static string SRP_VERSION
        {
            get { return SessionState.GetString("SRP_VERSION", string.Empty); }
            set { SessionState.SetString("SRP_VERSION", value); }
        }
        public static string MIN_SRP_VERSION;

        public static string LATEST_COMPATIBLE_VERSION
        {
            get { return SessionState.GetString("LATEST_SRP_VERSION", string.Empty); }
            set { SessionState.SetString("LATEST_SRP_VERSION", value); }
        }

        private static System.Version curVersion = new System.Version();
        private static System.Version minVersion = new System.Version();
        private static System.Version maxVersion = new System.Version();
        private static System.Version latestVersion = new System.Version();

#if SCPE_DEV
        [MenuItem("SCPE/TEST/Check SRP installation")]
#endif
        public static void CheckInstallation()
        {
            //Default
            CurrentPipeline = Pipeline.BuiltIn;
            
            if (PackageManager.packages == null) PackageManager.RetreivePackageList();

            foreach (UnityEditor.PackageManager.PackageInfo p in PackageManager.packages)
            {
                if (p.name == URP_PACKAGE_ID)
                {
                    CurrentPipeline = Pipeline.URP;

                    minVersion = new System.Version(MIN_URP_VERSION);
                    maxVersion = new System.Version(MAX_URP_VERSION);

                    CheckVersion(p);

                    return;
                }
                if (p.name == HDRP_PACKAGE_ID)
                {
                    CurrentPipeline = Pipeline.HDRP;

                    minVersion = new System.Version(MIN_HDRP_VERSION);
                    maxVersion = new System.Version(MAX_HDRP_VERSION);

                    CheckVersion(p);

                    return;
                }
            }
            
            #if SCPE_DEV
            Debug.Log("<b>Render Pipeline:</b> " + CurrentPipeline);
            #endif
        }

        private static void CheckVersion(UnityEditor.PackageManager.PackageInfo p)
        {
            //Remove any characters after - (-preview.99 suffix)
            SRP_VERSION = p.version.Split('-')[0];
            LATEST_COMPATIBLE_VERSION = p.versions.latestCompatible.Split('-')[0];

            curVersion = new System.Version(SRP_VERSION);
            latestVersion = new System.Version(LATEST_COMPATIBLE_VERSION);

            MIN_SRP_VERSION = minVersion.ToString();

            //Within range of minimum and maximum versions
            if (curVersion >= minVersion && curVersion <= maxVersion)
            {
                VersionStatus = Version.Compatible;
            }
            //Outside range of compatible versions
            if (curVersion < minVersion || curVersion > maxVersion)
            {
                VersionStatus = Version.Incompatible;
            }
            if (curVersion < minVersion)
            {
                VersionStatus = Version.Outdated;
            }

            //HDRP isn't supported
            if (p.name == HDRP_PACKAGE_ID) VersionStatus = Version.Incompatible;

#if SCPE_DEV
            Debug.Log("<b>SRP Installation</b> " + p.name + " " + SRP_VERSION + " Installed (" + VersionStatus + ")");
#endif
        }

        public static void UpdateToLatest()
        {
            string packageID = null;

            switch (CurrentPipeline)
            {
                case Pipeline.URP:
                    packageID = URP_PACKAGE_ID;
                    break;
            }

            AddRequest addRequest = UnityEditor.PackageManager.Client.Add(packageID + "@" + LATEST_COMPATIBLE_VERSION);

#if SCPE_DEV
            Debug.Log("<b>RenderPipelineInstallation</b> Updated " + CurrentPipeline + " to " + LATEST_COMPATIBLE_VERSION);
#endif

            //In case of updating an already installed version
            SRP_VERSION = LATEST_COMPATIBLE_VERSION;
            VersionStatus = Version.Compatible;

            if (EditorUtility.DisplayDialog("SRP Update", CurrentPipeline + " will start updating in a moment, please let it finish first", "OK")) { }
        }
    }

    public class UnityVersionCheck
    {
        public static string GetUnityVersion()
        {
            string version = UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion();
                
            //Remove GUID in parenthesis 
            return version.Substring(0, version.LastIndexOf(" ("));
        }
        
        public static string UnityVersion
        {
            get { return Application.unityVersion; }
        }

        public static bool COMPATIBLE
        {
            get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_COMPATIBLE_VERSION", true); }
            set { SessionState.SetBool(SCPE.ASSET_ABRV + "_COMPATIBLE_VERSION", value); }
        }
        public static bool BETA
        {
            get { return SessionState.GetBool(SCPE.ASSET_ABRV + "_BETA_VERSION", false); }
            set { SessionState.SetBool(SCPE.ASSET_ABRV + "_BETA_VERSION", value); }
        }

        public static void CheckCompatibility()
        {
            //Defaults
            COMPATIBLE = false;
            BETA = GetUnityVersion().Contains("f") == false;

            //Positives
#if UNITY_2020_3_OR_NEWER
            COMPATIBLE = true;
#endif

#if SCPE_DEV
            Debug.Log("<b>UnityVersionCheck</b> [Compatible: " + COMPATIBLE + "] - [Beta/Alpha: " + BETA + "]");
#endif
        }
    }
}
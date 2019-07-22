using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

public class MenuWindow : EditorWindow
{
    private const string FunctionName = "Intellisense Help Translator";
    private static string _managedDllPath;
    private static string _editorVersion;
    private static string _language;
    private static string _workPath = Utility.ConvertPath(Directory.GetCurrentDirectory() + "/Temp/" + FunctionName);

    [MenuItem("Help/" + FunctionName)]
    private static void ShowMenu()
    {
        GetWindow<MenuWindow>(FunctionName);
    }

    private void Awake()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            _managedDllPath = Utility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + "/Data/Managed");
        }
        else if (Application.platform == RuntimePlatform.OSXEditor)
        {
            // TODO macOS
            _managedDllPath = Utility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + "/Unity/Contents/Managed");
        }
        else if (Application.platform == RuntimePlatform.LinuxEditor)
        {
            // TODO linux
            _managedDllPath = System.AppDomain.CurrentDomain.BaseDirectory;
        }

        // TODO: macOSだと en が返ってきちゃう & 中国語だと 2文字じゃなく zh_CN ?
        // _language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _language = "ja"; // es, ko, zh_CN(?)

        // 欲しいのは "2018.4.3f1" のうち "2018.4" の部分 
        Regex r = new Regex(@"\d+\.\d+", RegexOptions.IgnoreCase);
        Match m = r.Match(UnityEngine.Application.unityVersion);
        if (m.Success)
        {
            _editorVersion = m.Groups[0].Value;
        }
    }

    private void OnGUI()
    {
//        Awake();

        GUILayout.Label("Install", EditorStyles.boldLabel);

        GUILayout.Label("Step 1");
        if (GUILayout.Button("Generate xml files"))
        {
            Generate();
        }

        // TODO バッチを実行するのか表示するのか
        GUILayout.Label("Step 2");
        if (GUILayout.Button("Show install script"))
        {
            string path = Utility.ConvertPath(_workPath + "/");
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var proc = new System.Diagnostics.Process();

                proc.StartInfo.FileName = path + "install.bat";
                proc.StartInfo.Verb = "RunAs";
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                EditorUtility.RevealInFinder(path + "install.sh");
                // TODO macOS
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                // TODO linux
            }
        }

        GUILayout.Space(10);

        GUILayout.Label("Uninstall", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        if (GUILayout.Button("Show install script"))
        {
            
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

//        EditorGUI.BeginDisabledGroup(true);
        if (GUILayout.Button("Clean temporary files"))
        {
            Clean(_workPath);
        }
//        EditorGUI.EndDisabledGroup();
        
        GUILayout.Space(10);
        GUILayout.Label("Information", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("Managed Dll path", _managedDllPath);
        EditorGUILayout.TextField("Unity generation", _editorVersion);
        EditorGUILayout.TextField("Target language", _language);
        EditorGUILayout.TextField("work path", _workPath);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        GUILayout.Label("Special thanks to", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextArea("SharpZipLib\nhttp://icsharpcode.github.io/SharpZipLib/\n\nHtmlAgilityPack\nhttps://github.com/zzzprojects/html-agility-pack");
        EditorGUI.EndDisabledGroup();

        // for test
        GUILayout.Space(10);
        if (GUILayout.Button("Open Unity Editor folder"))
        {
            EditorUtility.RevealInFinder(_managedDllPath);
        }
        if (GUILayout.Button("Clean xml folder"))
        {
            Clean(Utility.ConvertPath(_workPath + "/xml/"));
        }
        if (GUILayout.Button("GenerateXml"))
        {
            XmlGenerator.Generate(Utility.ConvertPath(_managedDllPath + "/"),
                Utility.ConvertPath(_workPath + "/xml/"),
                _language,
                Utility.ConvertPath(_workPath + "/ScriptReference/"));
        }
        if (GUILayout.Button("TranslateXml"))
        {
            XmlGenerator.Translate("C:/Program Files/Unity/Hub/Editor/2018.4.3f1/Editor/Data/Managed/UnityEngine/UnityEngine.GridModule.xml",
                Utility.ConvertPath(_workPath + "/xml/UnityEngine.GridModule.xml"),
                Utility.ConvertPath(_workPath + "/ScriptReference/"),
                ""
                );
        }
        if (GUILayout.Button("Generate batch"))
        {
            GenerateBatch();
        }
    }

    private void Generate()
    {
        // 作業フォルダを作る
        if (!Directory.Exists(_workPath))
        {
            Directory.CreateDirectory(_workPath);
        }

        // 公式からリファレンスをダウンロード
        ReferenceDownloader.Download(
            "https://storage.googleapis.com/localized_docs/" + _language + "/" + _editorVersion + "/UnityDocumentation.zip",
            Utility.ConvertPath(_workPath + "/UnityDocumentation.zip")
            );
    }

    public static void DownloadComplete()
    {
        // ダウンロードが終わったらzipを展開
        ReferenceExtractor.Unzip(Utility.ConvertPath(_workPath + "/UnityDocumentation.zip"), _workPath);
        
        // Unity内部のXmlリファレンスとダウンロードしたHtmlの翻訳済みリファレンスから、翻訳済みXmlを合成
        XmlGenerator.Generate(Utility.ConvertPath(_managedDllPath + "/"),
            Utility.ConvertPath(_workPath + "/xml/"),
            _language,
            Utility.ConvertPath(_workPath + "/ScriptReference/")
            );

        GenerateBatch();
    }

    private static void GenerateBatch()
    {
        string batchFile;
        StreamWriter sw;

        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            batchFile = Utility.ConvertPath(_workPath + "/install.bat");
            sw = File.CreateText(batchFile);
            sw.WriteLine("xcopy /Y /S /E \"" + Utility.ConvertPath(_workPath + "/xml") + "\" \"" + _managedDllPath + "\"");
            sw.Close();
        }
        else if (Application.platform == RuntimePlatform.OSXEditor)
        {
            batchFile = Utility.ConvertPath(_workPath + "/install.sh");
            sw = File.CreateText(batchFile);
            // TODO macOS
            sw.WriteLine("sudo cp -pr \"" + Utility.ConvertPath(_workPath + "/xml") + "\" \"" + _managedDllPath + "\"");
            sw.Close();
        }
        else if (Application.platform == RuntimePlatform.LinuxEditor)
        {
            // TODO linux
        }
    }
 
    private void Clean(string targetPath)
    {
        if (Directory.Exists(targetPath))
        {
            string[] filePaths = Directory.GetFiles(targetPath);
            foreach (string filePath in filePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }
 
            string[] directoryPaths = Directory.GetDirectories(targetPath);
            foreach (string directoryPath in directoryPaths)
            {
                Clean(directoryPath);
            }

            Directory.Delete(targetPath, false);
        }
    }
}

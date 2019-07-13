using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using UnityEditor;

public class MenuWindow : EditorWindow
{
    private const string functionName = "Intellisense Translator(unofficial)";
    private static string editorPath;
    private static string editorVersion;
    private static string language;
    private static string workPath = Directory.GetCurrentDirectory() + "/" + functionName;

    [MenuItem("Help/" + functionName)]
    private static void ShowMenu()
    {
        GetWindow<MenuWindow>(functionName);
    }

    private void Awake()
    {
        editorPath = System.AppDomain.CurrentDomain.BaseDirectory;
        language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        // 欲しいのは "2018.4.3f1" のうち "2018.4" の部分 
        Regex r = new Regex(@"\d+\.\d+", RegexOptions.IgnoreCase);
        Match m = r.Match(UnityEngine.Application.unityVersion);
        if (m.Success)
        {
            editorVersion = m.Groups[0].Value;
        }
    }

    private void OnGUI()
    {
        Awake();

        GUILayout.Label("Install", EditorStyles.boldLabel);

        GUILayout.Label("Step 1");
        if (GUILayout.Button("Generate xml files"))
        {
            Generate();
        }

        GUILayout.Label("Step 2");
        if (GUILayout.Button("Show install script"))
        {
            string path = workPath + "/xml/";
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                EditorUtility.RevealInFinder(path + "install.sh");
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                System.Diagnostics.Process.Start(path + "install.bat");
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor)
            {
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
            Clean(workPath);
        }
//        EditorGUI.EndDisabledGroup();
        
        GUILayout.Space(10);
        GUILayout.Label("Information", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("UnityEditor path", editorPath);
        EditorGUILayout.TextField("Unity version", editorVersion);
        EditorGUILayout.TextField("Target language", language);

        GUILayout.Space(10);

        EditorGUILayout.TextArea("Special thanks to\n\nSharpZipLib\nhttp://icsharpcode.github.io/SharpZipLib/\n\nHtmlAgilityPack\nhttps://github.com/zzzprojects/html-agility-pack");
        EditorGUI.EndDisabledGroup();

        // for test
        GUILayout.Space(10);
        if (GUILayout.Button("Open Unity Editor folder"))
        {
            EditorUtility.RevealInFinder(editorPath + "/Data/");
        }
        if (GUILayout.Button("Clean xml folder"))
        {
            Clean(workPath + "/xml/");
        }
        if (GUILayout.Button("GenerateXml"))
        {
            XmlGenerator.Generate(editorPath + "/Data/Managed/", workPath + "/xml/", language, workPath + "/ScriptReference/");
        }
        if (GUILayout.Button("TranslateXml"))
        {
            XmlGenerator.Translate("C:/Program Files/Unity/Hub/Editor/2018.4.3f1/Editor/Data/Managed/UnityEngine/UnityEngine.GridModule.xml",
                workPath + "/xml/UnityEngine.GridModule.xml",
                workPath + "/ScriptReference/",
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
        if (!Directory.Exists(workPath))
        {
            Directory.CreateDirectory(workPath);
        }

        // 公式からリファレンスをダウンロード
        ReferenceDownloader.Download(
            "https://storage.googleapis.com/localized_docs/" + language + "/" + editorVersion + "/UnityDocumentation.zip",
            workPath + "/UnityDocumentation.zip"
            );
    }

    public static void DownloadComplete()
    {
        // ダウンロードが終わったらzipを展開
        ReferenceExtractor.Unzip(workPath + "/UnityDocumentation.zip", workPath);
        
        // Unity内部のXmlリファレンスとダウンロードしたHtmlの翻訳済みリファレンスから、翻訳済みXmlを合成
        XmlGenerator.Generate(editorPath + "/Data/Managed/", workPath + "/xml/", language, workPath + "/ScriptReference/");
    }

    private static void GenerateBatch()
    {
        string batchFile;

        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            batchFile = workPath + "/install.bat";
        }
        else
        {
            batchFile = workPath + "/install.sh";
        }
        
        StreamWriter sw = File.CreateText(batchFile);
        sw.WriteLine("xcopy /Y /S /E \"" + workPath + "/xml/" + "\" \"" + editorPath + "/Data/Managed/\"");
// xcopy /Y /S /E "C:\work\ts\unity3d-intellisense-translate\unityProject\Intellisense Translator(unofficial)\xml" "C:\Program Files\Unity2018.4.3f1\Editor\Data\Managed"
        sw.Close();
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

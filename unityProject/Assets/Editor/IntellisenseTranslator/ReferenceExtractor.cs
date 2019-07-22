using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using ICSharpCode.SharpZipLib.Zip;

public class ReferenceExtractor
{
    public static void Unzip(string zipfile, string targetPath)
    {
        ZipEntry theEntry;
        ZipInputStream s;
        int fileNum = 0;
        int count = 0;

        using (s = new ZipInputStream(File.OpenRead(zipfile)))
        {
            while ((theEntry = s.GetNextEntry()) != null)
            {
                fileNum++;
            }
        }

        using (s = new ZipInputStream(File.OpenRead(zipfile)))
        {
            while ((theEntry = s.GetNextEntry()) != null) {

                try
                {
                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName      = Path.GetFileName(theEntry.Name);

                    if (directoryName.StartsWith("ScriptReference"))
                    {
                        // create directory
                        if ( directoryName.Length > 0 ) {
                            Directory.CreateDirectory(targetPath + "/" + directoryName);
                        }

                        if (fileName != String.Empty)
                        {
                            using (FileStream streamWriter = File.Create(targetPath + "/" + theEntry.Name))
                            {

                                int size = 8192;
                                byte[] data = new byte[8192];
                                while (true)
                                {
                                    size = s.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        streamWriter.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    EditorUtility.DisplayProgressBar(
                        "Extracting Reference",
                        theEntry.Name,
                        count / (float) fileNum
                    );
                }
                catch (Exception)
                {
                    // Zip内のファイル名に不正な文字が使われてるケースがいくつかある
                    Debug.LogWarning(theEntry.Name);
                }

                count++;
            }
        }        
        EditorUtility.ClearProgressBar();
    }
}

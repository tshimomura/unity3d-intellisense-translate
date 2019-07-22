using System;
using System.Net;
using System.ComponentModel;
using UnityEditor;

public class ReferenceDownloader
{
    public static void Download(string address, string file)
    {
        var wc = new WebClient();
        var uri = new Uri(address);

        wc.DownloadProgressChanged  += OnDownloadChanged;
        wc.DownloadFileCompleted    += OnDownloadCompleted;
        wc.DownloadFileAsync( uri, file );
    }

    private static void OnDownloadChanged( object sender, DownloadProgressChangedEventArgs e )
    {
        EditorUtility.DisplayProgressBar(
            "Downloading Reference",
            e.BytesReceived + "bytes (" + e.ProgressPercentage + "%)",
            e.ProgressPercentage / 100f
        );
    }    
    
    private static void OnDownloadCompleted( object sender, AsyncCompletedEventArgs e )
    {
        EditorUtility.ClearProgressBar();
        MenuWindow.DownloadComplete();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using ImageViewer.Controls;

namespace ImageViewer.Utils;

public static class Common
{
    public static List<string> ListImages(string? path)
    {
        var res = new List<string>();
        if (string.IsNullOrEmpty(path))
            return res;

        var files = Directory.GetFiles(path);
        Array.Sort(files, new NaturalComparer());
        foreach (var file in files) {
            var ext = Path.GetExtension(file).ToLower();
            if (ImageControl.SupportedFiles.Contains(ext)) {
                res.Add(file);
            }
        }

        return res;
    }
}
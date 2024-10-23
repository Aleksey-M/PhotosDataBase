using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cocona;

var app = CoconaApp.Create();


app.AddCommand("sort-images", (string sourceDir, string targetRootDir) =>
{
    var sDirExists = Directory.Exists(sourceDir);
    var tDirExists = Directory.Exists(targetRootDir);

    if (!sDirExists) Console.WriteLine($"Source directory '{sourceDir}' does not exists");
    if (!tDirExists) Console.WriteLine($"Target directory '{targetRootDir}' does not exists");

    if (!sDirExists || !tDirExists) return;

    //
    var allFiles = GetAllFiles(sourceDir);
    Console.WriteLine($"'{allFiles.Count}' file(s) finded in '{sourceDir}' directory");

    foreach (var fName in allFiles)
    {
        var folderName = GetFileSubfolderNameByDate(Path.GetFileName(fName));
        var fullFolderName = Path.Combine(targetRootDir, folderName);
        MoveFileToSubfolder(fName, fullFolderName);
    }

    Console.WriteLine("Done");

});


app.Run();




#region static methods
static List<string> GetAllFiles(string directory)
{
    var files = Directory.GetFiles(directory);
    var dirs = Directory.GetDirectories(directory);

    var filesList = files.ToList();
    foreach (var d in dirs)
    {
        var childDirFiles = GetAllFiles(d);
        filesList.AddRange(childDirFiles);
    }

    return filesList;
}


static string GetFileSubfolderNameByDate(string fileName)
{
    var parts = fileName.Split('_');
    if (parts.Length >= 2)
    {
        if ((parts[0] == "IMG" || parts[0] == "VID" || parts[0] == "WP") && parts[1].Length == 8)
        {
            return parts[1].Insert(6, ".").Insert(4, ".");
        }

        if (parts[0] == "Screenshot")
        {
            var scrDate = parts[1].Split("-");
            if (scrDate[0].Length == 8)
            {
                return scrDate[0].Insert(6, ".").Insert(4, ".");
            }
        }

        return "Unknown";

    }
    else
    {
        var parts2 = fileName.Split('.');
        if (long.TryParse(parts2[0], out _) && parts2[0].Length >= 8)
        {
            return parts2[0].Substring(4, 4) + "." + parts2[0].Substring(2, 2) + "." + parts2[0][..2];
        }
        else
            return "Unknown";
    }
}

static void MoveFileToSubfolder(string fileName, string targetSubfolder)
{
    if (!Directory.Exists(targetSubfolder))
    {
        Directory.CreateDirectory(targetSubfolder);
    }

    var targetFileName = Path.Combine(targetSubfolder, Path.GetFileName(fileName));

    if (File.Exists(targetFileName))
    {
        targetFileName = Path.Combine(
            targetSubfolder,
            Path.GetFileNameWithoutExtension(fileName)
                + Guid.NewGuid().ToString()
                + Path.GetExtension(fileName));

    }

    File.Move(fileName, targetFileName);
}
#endregion

record DataFileInfo(string FileName, string Path, long FileSize, DateTime CreatedDate, DateTime ModifiedDate);

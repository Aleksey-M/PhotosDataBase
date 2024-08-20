open System
open System.IO
open System.Linq


type DataFileInfo = {
    FileName: string
    Path: string
    FileSize: int64
    CreatedDate: DateTime
    ModifiedDate: DateTime
}

let firstDir = @""
let secondDir = @""


let rec getAllFiles directory =
    let files = Directory.GetFiles(directory)
    let dirs = Directory.GetDirectories(directory)

    let filesList : ResizeArray<string> = files.ToList()
    for d in dirs do
        filesList.AddRange(getAllFiles d)     

    filesList


let readFileInfo fileName =
    let fi = new FileInfo(fileName)
    {
        FileName = fi.Name
        Path = fi.DirectoryName
        FileSize = fi.Length
        CreatedDate = fi.CreationTimeUtc
        ModifiedDate = fi.LastWriteTimeUtc
    }


let firstDirFiles = List.ofSeq (getAllFiles firstDir) |> List.map readFileInfo
printfn "В папке %s прочитано файлов: %i" firstDir firstDirFiles.Length

let secondDirFiles = List.ofSeq (getAllFiles secondDir) |> List.map readFileInfo
printfn "В папке %s прочитано файлов: %i" secondDir secondDirFiles.Length


// firstDirFiles
//     |> Seq.filter (fun x -> not (List.exists (fun y -> y.FileName = x.FileName && y.FileSize = x.FileSize) secondDirFiles ))
//     |> Seq.map (fun x -> Path.Combine(x.Path, x.FileName))
//     |> Seq.iter (fun x -> printfn "%s" x)

let esclusiveFiles =
    firstDirFiles
    |> Seq.filter (fun x -> not (List.exists (fun y -> y.FileName = x.FileName && y.FileSize = x.FileSize) secondDirFiles ))
    |> Seq.map (fun x -> Path.Combine(x.Path, x.FileName))

File.WriteAllLines (@"exclusive.txt", esclusiveFiles)

printfn "done"
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

// папка с файлами для сравнения
let mainDir = @""
// папка, из которой нужно удалить дубликаты
let workDir = @""


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


let mainDirFiles = List.ofSeq (getAllFiles mainDir) |> List.map readFileInfo
printfn "В папке %s прочитано файлов: %i" mainDir mainDirFiles.Length

let workDirFiles = List.ofSeq (getAllFiles workDir) |> List.map readFileInfo
printfn "В папке %s прочитано файлов: %i" workDir workDirFiles.Length


let duplicates =
    workDirFiles
    |> Seq.filter (fun x -> (List.exists (fun y -> y.FileName = x.FileName && y.FileSize = x.FileSize) mainDirFiles ))
    |> Seq.toList

let size =
    duplicates
    |> Seq.map (fun x -> x.FileSize)
    |> Seq.sum 


printfn "Найдено %i одинаковых файлов, занимающих %i Гб" duplicates.Length (size/1024L/1024L)
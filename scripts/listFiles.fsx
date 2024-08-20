open System
open System.IO
open System.Linq
open System.Text.Json

type DataFileInfo = {
    FileName: string
    Path: string
    FileSize: int64
    CreatedDate: DateTime
    ModifiedDate: DateTime
}

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




let jsonFileName = "files.json"
let folder = getAllFiles @""


let records = List.ofSeq folder |> List.map readFileInfo

printfn "Прочитано файлов: %i" records.Length

let json = JsonSerializer.Serialize(records)
File.WriteAllText(jsonFileName, json)

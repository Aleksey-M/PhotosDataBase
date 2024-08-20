open System.IO
open System.Linq


let directory = @""
let targetRootDir = @""



let rec getAllFiles directory =
    let files = Directory.GetFiles(directory)
    let dirs = Directory.GetDirectories(directory)

    let filesList : ResizeArray<string> = files.ToList()
    for d in dirs do
        filesList.AddRange(getAllFiles d)     

    filesList


let insertDotsTo8CharsDateString (dateStr : string) =
    if dateStr.Length = 8 then
        dateStr.Insert(6, ".").Insert(4, ".")
    else
         "Unknown"


let isLongNumber (input: string) =
    match System.Int64.TryParse(input) with
    | (true, _) -> true
    | (false, _) -> false

let getDateStringFromNokiaXPress (fn : string) =
    let name = fn.Split(',')[0]
    if isLongNumber name then
        name.Substring(4, 4) + "." + name.Substring(2, 2) + "." + name.Substring(0, 2)
    else
        "Unknown"



let getFileSubfolderNameByDate (fileName:string) =
    let parts = List.ofSeq (fileName.Split('_'))
    let fullDateSecondSegment = ["IMG"; "VID"; "WP"]

    match parts with
    | a::b::_ when a = "Screenshot" -> insertDotsTo8CharsDateString (b.Split("-")[0])
    | a::b::_ when List.contains a fullDateSecondSegment -> insertDotsTo8CharsDateString b
    | a::rest -> getDateStringFromNokiaXPress a
    | _ -> "Unknown"



let allFiles: ResizeArray<string> = getAllFiles directory
printfn $"'{allFiles.Count}' file(s) finded in '{directory}' directory"


let moveFileToSubfolder (fileName: string) (targetSubfolder: string) =
    if not (Directory.Exists(targetSubfolder)) then
        Directory.CreateDirectory(targetSubfolder) |> ignore 

    let targetFileName = Path.Combine(targetSubfolder, Path.GetFileName(fileName))
    let checkedTargetFile =
        if File.Exists targetFileName then 
            Path.Combine(
            targetSubfolder,
            Path.GetFileNameWithoutExtension(fileName)
                + System.Guid.NewGuid().ToString()
                + Path.GetExtension(fileName))
        else
            targetFileName

    File.Move(fileName, checkedTargetFile)


// сортировка файлов по папкам
List.ofSeq allFiles
    |> List.map (fun x ->
        let subfolder = getFileSubfolderNameByDate (System.IO.Path.GetFileName x)
        let fillFolderName = Path.Combine(targetRootDir, subfolder)
        moveFileToSubfolder x fillFolderName)


printfn "Done"
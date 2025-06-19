open System
open System.IO
open System.IO.Compression


let path = fsi.CommandLineArgs |> Seq.last
let dir = if (Path.Exists(path)) then path else ""


let extractZipFile (zipPath: string) (extractPath: string) =
    if File.Exists(zipPath) then
        ZipFile.ExtractToDirectory(zipPath, extractPath)
        printfn "Extraction complete. Files extracted to %s" extractPath
    else
        printfn "The file %s does not exist." zipPath

let getAllFiles directory = 
        Directory.GetFileSystemEntries(directory, @"*.nar", SearchOption.AllDirectories)
            |> Seq.filter(fun x -> File.Exists(x))
            |> Seq.toArray



if (dir <> "") then    
    let narFiles = getAllFiles dir
    
    if (narFiles.Length = 0) then
        Console.WriteLine($"В папке {dir} не найдены nar-файлы")
    else
        Console.WriteLine($"В папке {dir} найдено {narFiles.Length} nar-файла(ов)")
        narFiles |> Seq.iter(fun x -> Console.WriteLine(x))
        narFiles |> Seq.iter (fun x -> extractZipFile x @$"{x}.files")
        Console.WriteLine("Все файлы извлечены")
        
else
    Console.WriteLine("Путь не указан")


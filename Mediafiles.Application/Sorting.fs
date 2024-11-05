///Сортировка файлов по папкам в зависимости от даты, полученной из названия файла
module Sorting

open System.IO
open System

let getAllFiles directory = 
        Directory.GetFileSystemEntries(directory, @"*.*", SearchOption.AllDirectories)
            |> Seq.filter(fun x -> File.Exists(x))
            |> Seq.toArray

let isLongNumber (input: string) =
    match Int64.TryParse(input) with
    | (true, _) -> true
    | (false, _) -> false

let isValidDateString (input: string) =
    match DateTime.TryParse(input) with
    | (true, d) when d.Year > 2000 && d.Year < 2100 -> true
    | (_, _) -> false

///Файлы Nokia Express 5800 (25102013246.jpg)
let (|IsNokia5800|_|) (fName:string) =
    let head = fName//fName.Split('.') |> Seq.head
    if head.Length = 11 && isLongNumber(head) then
        let result = head.Substring(4, 4) + "." + head.Substring(2, 2) + "." + head.Substring(0, 2)
        if isValidDateString result then
            Some (result)
        else 
            None
    else
        None

///Файлы Андроид телефонов и Nokia Lumia (IMG_20191024_131013.jpg, WP_20170618_14_37_30_Pro.jpg)
let (|IsIMGorWP|_|) (fName:string) =
    let parts = fName.Split('_')
    if parts.Length >= 3 && (List.contains parts[0] ["IMG"; "VID"; "WP"]) && parts[1].Length = 8 then
        let result = parts[1].Insert(6, ".").Insert(4, ".")
        if isValidDateString result then
            Some (result)
        else 
            None
    else
        None
    
///Скриншоты (Screenshot_20231124-083735.png)
let (|IsScreenshot|_|) (fName:string) =
    let parts = fName.Split('_')
    if parts.Length = 2 && parts[0].ToLower() = "screenshot" &&  parts[1].Contains('-') && parts[1].Length >= 8 then
        let result = parts[1].Substring(0, 8).Insert(6, ".").Insert(4, ".")
        if isValidDateString result then
            Some (result)
        else 
            None
    else
        None

///Файлы Samsung (20241016_090915.jpg)
let (|IsSamsung|_|) (fName:string) =
    let parts = fName.Split('_')
    if parts.Length = 2 &&  parts[1].Length = 8 then
        let result = parts[0].Insert(6, ".").Insert(4, ".")
        if isValidDateString result then
            Some (result)
        else 
            None
    else
        None

let getDirName (fn:string) =
    match IO.Path.GetFileName fn with
    | IsScreenshot dn -> (fn, dn)
    | IsIMGorWP dn -> (fn, dn)
    | IsNokia5800 dn -> (fn, dn)
    | IsSamsung dn -> (fn, dn)
    | _ -> (fn, "Unknown")

let groupFilesByFolderNames (files: string array) =
    files
    |> Seq.map (fun x -> getDirName x)
    |> Seq.groupBy snd
    |> Seq.map (fun (key, values) -> (key, values |> Seq.map fst, values |> Seq.length))
    |> Seq.sortBy( fun (x, _, _) -> x)


///Перенос файлов в указанную папку с проверкой на уникальность по названию
let moveFilesToFolder (dirName: string, files: string seq) =
    if not (Directory.Exists(dirName)) then
        Directory.CreateDirectory(dirName) |> ignore 

    files
    |> Seq.map (fun x -> (x, Path.Combine(dirName, Path.GetFileName(x))))
    |> Seq.map (fun (x, y) -> 
        if File.Exists y then
            (x, Path.Combine(dirName, Path.GetFileNameWithoutExtension(y) + Guid.NewGuid().ToString() + Path.GetExtension(y)))
        else
            (x, y))
    |> Seq.iter (fun (x, y) -> File.Move(x, y))

///Выполнение сортировки файлов
let sortFiles (sourceDir:string) (targetRootDir:string) =
    let allFiles = getAllFiles sourceDir
    System.Console.WriteLine($"'{allFiles.Length}' file(s) finded in '{sourceDir}' directory")

    let groupedFiles = groupFilesByFolderNames allFiles
    groupedFiles
    |> Seq.map (fun (dir, files, c) ->
        moveFilesToFolder (Path.Combine(targetRootDir, dir), files)
        $"{dir} - {c}")
    |> Seq.toArray
#r "nuget:RabbitMQ.Client"
#load "sharedTypes.fsx"

open System
open System.IO
open System.Text
open RabbitMQ.Client
open System.Text.Json
open SharedTypes

let readFiles path collectionName =
    let factory = ConnectionFactory(HostName = "localhost")
    factory.ClientProvidedName <- "Files Info reader"
    let connection = factory.CreateConnection()
    let channel = connection.CreateModel()

    channel.QueueDeclare("files-data", true, false, false) |> ignore

    Directory.GetFileSystemEntries(
    path,
    @"*.*",
    SearchOption.AllDirectories)
            |> Seq.filter(fun x -> File.Exists(x))
            |> Seq.map(fun x -> readFileInfo x collectionName)
            |> Seq.map(fun x -> JsonSerializer.Serialize(x))
            |> Seq.map(fun x -> Encoding.UTF8.GetBytes(x))
            |> Seq.iter(fun x -> channel.BasicPublish(exchange = String.Empty, routingKey = "files-data", body = x))

    connection.Close()
//


let mutable basePath = String.Empty
let mutable collectionName = String.Empty

if fsi.CommandLineArgs.Length >= 2 then
    basePath <- fsi.CommandLineArgs[1]

if fsi.CommandLineArgs.Length >= 3 then
    collectionName <- fsi.CommandLineArgs[2]

if not (Directory.Exists basePath) then
    Console.WriteLine("Путь к папке не найден")
else
    Console.WriteLine("Чтение файлов в очередь...")
    readFiles basePath collectionName

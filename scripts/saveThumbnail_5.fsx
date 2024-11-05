#r "nuget:RabbitMQ.Client"
#r "nuget:MongoDB.Driver"
#load "sharedTypes.fsx"

open SharedTypes
open System
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events
open MongoDB.Driver
open System.Text.Json


let factory = ConnectionFactory(HostName = "localhost")
factory.ClientProvidedName <- "Mongo Thumbnail Writer"

let connection = factory.CreateConnection()
let channel = connection.CreateModel()
channel.BasicQos(0u, 1us, false)

// входная очередь
channel.QueueDeclare("thumbnail-images", true, false, false) |> ignore

// Подключение к MongoDb
let client = new MongoClient("mongodb://localhost:27017")
let db = client.GetDatabase("metadata_db")
db.CreateCollection("files-metadata")
let metadataCollection = db.GetCollection<FileFullMetadata>("files-metadata")
let inline bsonFieldVal name = StringFieldDefinition<_,_> (name)

let consumer = new EventingBasicConsumer(channel)
consumer.Received |> Observable.add (fun args ->
    let body = args.Body.ToArray()
    let message = Encoding.UTF8.GetString(body)
    let msg = JsonSerializer.Deserialize<ThumbnailingRec>(message)

    let existed = metadataCollection.Find(fun f-> f.fileId = msg.fileId.ToString()).First()

    //if (existed |> Option.Some) then
        //Console.WriteLine($"Метаданные файла с Id={msg.fileId} еще не сохранены в коллекцию")
        //channel.BasicNack(args.DeliveryTag, false, false)
    //else

    Console.WriteLine("1")
    let entityFilter = Builders.Filter.Eq (bsonFieldVal "fileId", existed.fileId)
    Console.WriteLine("2")
    let update = Builders.Update.Set (bsonFieldVal "thumbnailBase64", msg.thumbnailBase64)
    Console.WriteLine("3")
    let result = metadataCollection.UpdateOne (entityFilter, update)
    Console.WriteLine("4")
    printfn "%O" result
    channel.BasicAck(args.DeliveryTag, false)
)

channel.BasicConsume(queue = "thumbnail-images",
                     autoAck = false,
                     consumer = consumer)

Console.WriteLine("Mongo Db Thumbnail Writer started")
Console.ReadKey() |> ignore
connection.Close()
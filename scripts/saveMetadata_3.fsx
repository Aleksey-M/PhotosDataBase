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
factory.ClientProvidedName <- "Mongo Metadata Writer"

let connection = factory.CreateConnection()
let channel = connection.CreateModel()

// входная очередь
channel.QueueDeclare("files-metadata", true, false, false) |> ignore
channel.BasicQos(0u, 1us, false)
// топология выходных очередей
channel.ExchangeDeclare("for-thumbnailing", ExchangeType.Direct, durable=true) |> ignore
channel.QueueDeclare("for-thumbnailing-Image", true, false, false) |> ignore
channel.QueueDeclare("for-thumbnailing-Video", true, false, false) |> ignore
channel.QueueDeclare("for-thumbnailing-Zip", true, false, false) |> ignore
channel.QueueBind("for-thumbnailing-Image", "for-thumbnailing", "Image")
channel.QueueBind("for-thumbnailing-Video", "for-thumbnailing", "Video")
channel.QueueBind("for-thumbnailing-Zip", "for-thumbnailing", "Zip")

// Подключение к MongoDb
let client = new MongoClient("mongodb://localhost:27017")
let db = client.GetDatabase("metadata_db")
db.CreateCollection("files-metadata")
let metadataCollection = db.GetCollection<FileFullMetadata>("files-metadata")

let consumer = new EventingBasicConsumer(channel)
consumer.Received |> Observable.add (fun args ->
    let body = args.Body.ToArray()
    let message = Encoding.UTF8.GetString(body)
    let msg = JsonSerializer.Deserialize<FileMetadata>(message)
       
    let existed = metadataCollection.Find(fun f-> f.fileId = msg.fileData.id.ToString()).ToEnumerable()
    if (existed |> Seq.isEmpty) then
        let dataRec = createDbEntity msg
        metadataCollection.InsertOne(dataRec)

        // отправка сообщения на создание превью
        let thmbMsg= createThumbnailingMsg msg
        let messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(thmbMsg))
        channel.BasicPublish("for-thumbnailing",  routingKey = msg.typeDescr, body = messageBytes)
    else
        Console.WriteLine($"Файл с Id={msg.fileData.id} уже обработан")
)

channel.BasicConsume(queue = "files-metadata",
                     autoAck = true,
                     consumer = consumer)

Console.WriteLine("Mongo Db Writer started")
Console.ReadKey() |> ignore
connection.Close()
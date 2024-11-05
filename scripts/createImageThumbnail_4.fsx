#r "nuget:RabbitMQ.Client"
#load "sharedTypes.fsx"

open SharedTypes
open System
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Text.Json
open System.IO

let factory = ConnectionFactory(HostName = "localhost")
factory.ClientProvidedName <- "Images Thumbnails creator"
let connection = factory.CreateConnection()
let channel = connection.CreateModel()
channel.BasicQos(0u, 1us, false)

channel.ExchangeDeclare("for-thumbnailing", ExchangeType.Direct, durable=true) |> ignore
channel.QueueDeclare("for-thumbnailing-Image", true, false, false) |> ignore
channel.QueueDeclare("for-thumbnailing-Video", true, false, false) |> ignore
channel.QueueDeclare("for-thumbnailing-Zip", true, false, false) |> ignore
channel.QueueDeclare("thumbnail-images", true, false, false) |> ignore
channel.QueueBind("for-thumbnailing-Image", "for-thumbnailing", "Image")
channel.QueueBind("for-thumbnailing-Video", "for-thumbnailing", "Video")
channel.QueueBind("for-thumbnailing-Zip", "for-thumbnailing", "Zip")


let consumer = new EventingBasicConsumer(channel)
consumer.Received |> Observable.add (fun args ->
    try
        let body = args.Body.ToArray()
        let message = Encoding.UTF8.GetString(body)
        let msg = JsonSerializer.Deserialize<ThumbnailingRec>(message)

        let fName = if msg.tempVideoPreviewName <> "" then msg.tempVideoPreviewName else msg.fileNameFull
        let bytes = File.ReadAllBytes(fName)
        let (thumbnail, w, h) = scaleImage (bytes, 300, 30)
        let b64thumbnail = Convert.ToBase64String(thumbnail)

        let newMsg = {msg with thumbnailBase64 = b64thumbnail}

        if msg.tempVideoPreviewName <> "" then
            File.Delete(msg.tempVideoPreviewName)

        let messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newMsg))
        channel.BasicPublish(exchange = String.Empty, routingKey = "thumbnail-images", body = messageBytes)
        channel.BasicAck(args.DeliveryTag, false)
    with exc ->
        Console.WriteLine($"При получении миниатюры изображения произошла ошибка: {exc.Message}{exc.StackTrace}")
        channel.BasicNack(args.DeliveryTag, false, requeue = true)
)

channel.BasicConsume(queue = "for-thumbnailing-Image",
                     autoAck = false,
                     consumer = consumer)

Console.WriteLine("Images Thumbnails creator started")
Console.ReadKey() |> ignore
connection.Close()
#r "nuget:FFMpegCore"
#r "nuget:RabbitMQ.Client"
#load "sharedTypes.fsx"

open FFMpegCore
open SharedTypes
open System
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Text.Json
open System.IO

let factory = ConnectionFactory(HostName = "localhost")
factory.ClientProvidedName <- "Video Previews Creator"
let connection = factory.CreateConnection()
let channel = connection.CreateModel()

channel.ExchangeDeclare("for-thumbnailing", ExchangeType.Direct, durable=true) |> ignore
channel.QueueDeclare("for-thumbnailing-Image", true, false, false) |> ignore
channel.QueueDeclare("for-thumbnailing-Video", true, false, false) |> ignore
channel.QueueDeclare("for-thumbnailing-Zip", true, false, false) |> ignore
channel.QueueBind("for-thumbnailing-Image", "for-thumbnailing", "Image")
channel.QueueBind("for-thumbnailing-Video", "for-thumbnailing", "Video")
channel.QueueBind("for-thumbnailing-Zip", "for-thumbnailing", "Zip")


let consumer = new EventingBasicConsumer(channel)
consumer.Received |> Observable.add (fun args ->
    try
        let body = args.Body.ToArray()
        let message = Encoding.UTF8.GetString(body)
        let msg = JsonSerializer.Deserialize<ThumbnailingRec>(message)
        let newMsg = {msg with tempVideoPreviewName = $"{Path.GetTempFileName()}.png"}

        let result = FFMpeg.Snapshot(msg.fileNameFull, newMsg.tempVideoPreviewName,  System.Nullable(), System.TimeSpan.FromSeconds(1))
        if (not result) then failwith $"Не удалось создать превью для {msg.fileNameFull}"  

        let messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newMsg))
        channel.BasicPublish("for-thumbnailing",  routingKey = "Image", body = messageBytes)
        channel.BasicAck(args.DeliveryTag, false)
    with exc ->
        Console.WriteLine($"При обработке сообщения для получения метаданных произошла ошибка: {exc.Message}{exc.StackTrace}")
        channel.BasicNack(args.DeliveryTag, false, requeue = true)
)

channel.BasicConsume(queue = "for-thumbnailing-Video",
                     autoAck = false,
                     consumer = consumer)

Console.WriteLine("Video Previews creator started")
Console.ReadKey() |> ignore
connection.Close()

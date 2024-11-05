#r "nuget:RabbitMQ.Client"
#load "sharedTypes.fsx"

open System
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events
open SharedTypes
open System.Text.Json

let factory = ConnectionFactory(HostName = "localhost")
factory.ClientProvidedName <- "Files Metadata reader"
let connection = factory.CreateConnection()
let channel = connection.CreateModel()
channel.BasicQos(0u, 1us, false)

// входная очередь с записями о файлах
channel.QueueDeclare("files-data", true, false, false) |> ignore
// очередь для отправки прочитанных метаданных
channel.QueueDeclare("files-metadata", true, false, false) |> ignore

// слушаем входящую очередь
let inConsumer = new EventingBasicConsumer(channel)
inConsumer.Received |> Observable.add (fun args ->    
    try
        let body = args.Body.ToArray()
        let response = Encoding.UTF8.GetString(body)
        let msg = JsonSerializer.Deserialize<FileData>(response)
        
        let typeDescr = getTypeDescr msg.fileExtension

        if typeDescr = "Unknown" || typeDescr = "Zip" then
            // пропуск файла 
            Console.WriteLine($"Пропускаем: {msg.fileNameFull}")
            channel.BasicAck(args.DeliveryTag, false)
        else
            //чтение метаданных 
            let meta = readMetadata (msg, typeDescr)
            match meta with
            | Readed dat -> 
                // отправка в очередь
                let messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dat))
                channel.BasicPublish(exchange = String.Empty,
                    routingKey = "files-metadata",
                    body = messageBytes)
                channel.BasicAck(args.DeliveryTag, false)
            | WithError m ->
                 // ошибка чтения метаданных 
                Console.WriteLine($"При чтении метаданных для файла {msg.fileNameFull} произошла ошибка: {m}")
                channel.BasicNack(args.DeliveryTag, false, requeue = true)
                //channel.BasicAck(args.DeliveryTag, false)
    with exc ->
        Console.WriteLine($"При обработке сообщения для получения метаданных произошла ошибка: {exc.Message}{exc.StackTrace}")
        channel.BasicNack(args.DeliveryTag, false, requeue = true)
)

channel.BasicConsume(queue = "files-data",
    autoAck = false,
    consumer = inConsumer)

Console.WriteLine("Files Metadata reader started")
Console.ReadKey() |> ignore
connection.Close()
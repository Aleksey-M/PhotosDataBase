namespace Mediafiles.Infrastructure

open System.Threading.Tasks
open System.IO
open LiteDB
open Mediafiles.Domain.Entities
open Mediafiles.Application.Repositories

///Код для инициализации базы (создание коллекций, индексов и др.)
module LiteDbInitializer =

    let initializeDb (connectionUri:string) =
        if not (File.Exists(connectionUri)) then
            let db = new LiteDatabase(connectionUri)
            db.UserVersion <- 1
            db.UtcDate <- true

            let collection = db.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName)
            collection.EnsureIndex(fun x -> x.CollectionName) |> ignore


            
        Task.CompletedTask

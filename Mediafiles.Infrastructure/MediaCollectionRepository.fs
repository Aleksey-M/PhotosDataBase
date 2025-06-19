namespace Mediafiles.Infrastructure.Repositories

open Mediafiles.Application.Repositories
open System.Threading.Tasks
open System.Collections.Generic
open Mediafiles.Domain.Entities
open LiteDB
open System
open Mediafiles.Application.Validation


type MediaCollectionRepository(liteDb:LiteDatabase) =

    interface IMediaCollectionRepository with
        member this.GetCollection(id: int): Task<OperationResult> = 
            try
            let c = liteDb.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName)

            if c.Exists(fun x -> x.Id = id) then
                Task.FromResult(Success (c.FindById(id)))
            else
                Task.FromResult(ValidationError "Media collection not found.")
            with
            | ex ->
                Console.WriteLine("An error occurred: " + ex.Message)
                Task.FromResult(ExecException $"{ex.Message} {ex.StackTrace}")


        member this.AddCollection(collectionInfo: MediaCollectionInfo): Task<OperationResult> =
            try
            let valResult = Validator.validateMediaCollection collectionInfo

            match valResult with
            | WithErrors s -> Task.FromResult(ValidationError s)
            | Validated m ->
                let c = liteDb.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName)
                if (c.Exists(fun x -> x.CollectionName = m.CollectionName)) then
                    Task.FromResult(ValidationError $"Collection with name '{m.CollectionName}' already exists")
                else
                    c.Insert(m) |> ignore
                    Task.FromResult(Success m)
            with
            | ex ->
                Console.WriteLine("An error occurred: " + ex.Message)
                Task.FromResult(ExecException $"{ex.Message} {ex.StackTrace}")


        member this.DeleteCollection(id: int): Task = 
            try
            liteDb.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName).Delete(new BsonValue(id)) |> ignore
            with
            | ex ->
                Console.WriteLine("An error occurred: " + ex.Message)

            Task.CompletedTask


        member this.GetCollections(): Task<List<MediaCollectionInfo>> = 
            try
            let list = liteDb.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName).FindAll()
            Task.FromResult(new List<MediaCollectionInfo>(list))
            with
            | ex ->
                Console.WriteLine("An error occurred: " + ex.Message)
                Task.FromResult(new List<MediaCollectionInfo>())


        member this.UpdateCollection(collectionInfo: MediaCollectionInfo): Task<OperationResult> = 
            try
            let valResult = Validator.validateMediaCollection collectionInfo

            match valResult with
            | WithErrors s -> Task.FromResult(ValidationError s)
            | Validated m ->
                let c = liteDb.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName)
                if (c.Exists(fun x -> x.CollectionName = m.CollectionName && x.Id <> m.Id)) then
                    Task.FromResult(ValidationError $"Collection with name '{m.CollectionName}' already exists")
                else
                    c.Update(m) |> ignore
                    Task.FromResult(Success m)
            with
            | ex ->
                Console.WriteLine("An error occurred: " + ex.Message)
                Task.FromResult(ExecException $"{ex.Message} {ex.StackTrace}")
    


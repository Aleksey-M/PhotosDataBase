namespace Mediafiles.Tests

open Xunit
open Mediafiles.Infrastructure.Repositories
open Mediafiles.Domain.Entities
open LiteDB
open Mediafiles.Application.Repositories

type MediaCollectionRepositoryTests() =

    let createInMemoryDatabase() =
        let mapper = BsonMapper.Global
        let memoryStream = new System.IO.MemoryStream()
        let db = new LiteDatabase(memoryStream, mapper)
        db.UserVersion <- 1
        db.UtcDate <- true

        let collection = db.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName)
        collection.EnsureIndex(fun x -> x.CollectionName) |> ignore
        db
    
    [<Fact>]
    member this.``AddCollection should add a valid collection``() =
        let db = createInMemoryDatabase()
        let repo = MediaCollectionRepository(db) :> IMediaCollectionRepository
        let collection = MediaCollectionInfo(CollectionName = "valid_name", CollectionDescr = "description")
        
        let result = repo.AddCollection(collection).Result
        
        match result with
        | Success _ -> Assert.True(true)
        | _ -> Assert.True(false, "Expected Success")
        
    [<Fact>]
    member this.``AddCollection should return validation error for invalid collection name``() =
        let db = createInMemoryDatabase()
        let repo = MediaCollectionRepository(db) :> IMediaCollectionRepository
        let collection = MediaCollectionInfo(CollectionName = "Invalid Name!", CollectionDescr = "description")
        
        let result = repo.AddCollection(collection).Result
        
        match result with
        | ValidationError msg -> Assert.Equal("Некорректное название коллекции", msg)
        | _ -> Assert.True(false, "Expected ValidationError")
    
    [<Fact>]
    member this.``GetCollections should return all collections``() =
        let db = createInMemoryDatabase()
        let repo = MediaCollectionRepository(db) :> IMediaCollectionRepository
        let collection1 = MediaCollectionInfo(Id = 1, CollectionName = "valid_name", CollectionDescr = "description1")
        let collection2 = MediaCollectionInfo(Id = 2, CollectionName = "valid_name_new", CollectionDescr = "description2")
        db.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName).Insert(collection1) |> ignore
        db.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName).Insert(collection2) |> ignore

        let result = repo.GetCollections().Result
        
        Assert.Equal(2, result.Count)

    [<Fact>]
    member this.``GetCollection should return collection if it exists``() =
        let db = createInMemoryDatabase()
        let repo = MediaCollectionRepository(db) :> IMediaCollectionRepository
        let collection = MediaCollectionInfo(Id = 1, CollectionName = "valid_name", CollectionDescr = "description")
        db.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName).Insert(collection) |> ignore
        
        let result = repo.GetCollection(1).Result
        
        match result with
        | Success col -> Assert.Equal(collection.CollectionName, col.CollectionName)
        | _ -> Assert.True(false, "Expected Success")
    
    [<Fact>]
    member this.``GetCollection should return validation error if collection does not exist``() =
        let db = createInMemoryDatabase()
        let repo = MediaCollectionRepository(db) :> IMediaCollectionRepository
        
        let result = repo.GetCollection(1).Result
        
        match result with
        | ValidationError msg -> Assert.Equal("Media collection not found.", msg)
        | _ -> Assert.True(false, "Expected ValidationError")
    
    [<Fact>]
    member this.``DeleteCollection should delete collection if it exists``() =
        let db = createInMemoryDatabase()
        let repo = MediaCollectionRepository(db) :> IMediaCollectionRepository
        let collection = MediaCollectionInfo(Id = 1, CollectionName = "valid_name", CollectionDescr = "description")
        db.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName).Insert(collection) |> ignore
        
        repo.DeleteCollection(1).Wait()
        
        let exists = db.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName).Exists(fun x -> x.Id = 1)
        Assert.False(exists)
    
    [<Fact>]
    member this.``UpdateCollection should update collection if it is valid``() =
        let db = createInMemoryDatabase()
        let repo = MediaCollectionRepository(db) :> IMediaCollectionRepository
        let collection = MediaCollectionInfo(Id = 1, CollectionName = "valid_name", CollectionDescr = "description")
        db.GetCollection<MediaCollectionInfo>(DbConstants.collectionsInfoName).Insert(collection) |> ignore
        
        let updatedCollection = MediaCollectionInfo(Id = 1, CollectionName = "updated_name", CollectionDescr = "updated description")
        let result = repo.UpdateCollection(updatedCollection).Result
        
        match result with
        | Success col -> Assert.Equal("updated_name", col.CollectionName)
        | _ -> Assert.True(false, "Expected Success")
    
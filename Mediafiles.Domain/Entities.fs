namespace Mediafiles.Domain.Entities

open System

/// Сущности для хранения в коллекциях базы
//module Entities

type Tag() =
    member val Name = "" with get, set
    member val Value = "" with get, set


type Metadata() =
    member val Name = "" with get, set
    member val Tags: Tag array = Array.empty with get, set

//type MediaFileInfo() =


type MediaCollectionInfo() =
    member val CollectionName = "" with get, set
    member val CollectionDescr = "" with get, set
    member val CreateDate: DateTime = DateTime.UtcNow with get, set
    member val TotalSize = 0UL with get, set
    member val TotalFiles = 0u with get, set



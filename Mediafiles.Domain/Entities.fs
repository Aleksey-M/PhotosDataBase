namespace Mediafiles.Domain.Entities

open System

// Сущности для хранения в коллекциях базы

type Tag() =
    member val Name = "" with get, set
    member val Value = "" with get, set

type Metadata() =
    member val Name = "" with get, set
    member val Tags = [||] with get, set

///Метаданные медиафайла
type MediaFileInfo() =
    member val Id = 0 with get, set
    member val FileId = Guid.Empty with get, set
    member val CollectionName = "" with get, set
    member val FileNameFull = "" with get, set
    member val FileSize = 0L with get, set
    member val FileCreatedDate = DateTime.MinValue with get, set
    member val FileModifiedDate = DateTime.MinValue with get, set
    member val TypeDescr = "" with get, set
    member val Metadata = [||] with get, set
    member val ThumbnailBase64 = None with get, set
    member val Exception = None with get, set
    member val AddToBaseDate = DateTime.MinValue with get, set
    member val DeletedDate = None with get, set


///Коллекция медиафайлов
type MediaCollectionInfo() =
    member val Id = 0 with get, set
    member val CollectionName = "" with get, set
    member val CollectionDescr = "" with get, set
    member val CreateDate: DateTime = DateTime.UtcNow with get, set




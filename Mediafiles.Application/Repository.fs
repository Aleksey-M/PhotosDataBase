namespace Mediafiles.Application.Repository

open Mediafiles.Domain.Entities

type IMediaCollectionRepository =
    abstract member GetCollections: unit -> MediaCollectionInfo array
    abstract member AddCollection: MediaCollectionInfo -> string
    abstract member UpdateCollection: MediaCollectionInfo -> unit
    abstract member DeleteCollection: MediaCollectionInfo -> unit
namespace Mediafiles.Application.Repositories

open System.Threading.Tasks
open System.Collections.Generic
open Mediafiles.Domain.Entities
open System


type OperationResult =
| Success of MediaCollectionInfo
| ValidationError of string
| ExecException of string


///Репозиторий для хранения информации о коллекциях медиафайлов 
type IMediaCollectionRepository =
    abstract member GetCollections: unit -> Task<List<MediaCollectionInfo>>
    abstract member AddCollection: MediaCollectionInfo -> Task<OperationResult>
    abstract member UpdateCollection: MediaCollectionInfo -> Task<OperationResult>
    abstract member GetCollection: int -> Task<OperationResult>
    abstract member DeleteCollection: int -> Task


//type IMediaRepository =
    


module DbConstants =
    [<Literal>]
    let collectionsInfoName = "collections_info"


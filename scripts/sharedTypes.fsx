#r "nuget:MetadataExtractor"
#r "nuget:SkiaSharp"

open System
open System.IO
open MetadataExtractor
open SkiaSharp

type FileData = {
    id: Guid
    collectionName: string
    fileNameFull: string
    fileName: string
    fileExtension: string
    fileSize: int64
    fileCreatedDate: DateTime
    fileModifiedDate: DateTime
}

type MetadataTag() = 
    member val name = "" with get, set
    member val value = "" with get, set


type MetadataDirectory() = 
    member val name = "" with get, set
    member val tags: MetadataTag array = Array.Empty() with get, set


type FileMetadata = {
    fileData: FileData
    typeDescr: string
    metadata: MetadataDirectory array
}

type FileFullMetadata() = 
    member val fileId = "" with get, set
    member val collectionName = "" with get, set
    member val fileNameFull = "" with get, set
    member val fileSize: int64 = 0 with get, set
    member val fileCreatedDate = "" with get, set
    member val fileModifiedDate = "" with get, set
    member val typeDescr = "" with get, set
    member val metadata: MetadataDirectory array = Array.Empty() with get, set
    member val thumbnailBase64 = "" with get, set



let (|IsImage|_|) (ext : string) =
    if (seq {".jpg"; ".jpeg"; ".png"; ".gif"; ".nar"} |> Seq.contains(ext.ToLower())) then
        Some "Image"
    else
        None

let (|IsVideo|_|) (ext : string) =
    if (seq {".mp4"; ".mov"; ".3gp"} |> Seq.contains(ext.ToLower())) then
        Some "Video"
    else
        None

let (|IsZippedSet|_|) (ext : string) =
    if ( ".nar" = ext.ToLower()) then
        Some "Zip"
    else
        None

let getTypeDescr ext = 
    match ext with
    | IsImage x -> x
    | IsVideo x -> x
    | IsZippedSet x -> x
    | _ -> "Unknown"

let readFileInfo fileName collectionName = 
    {
        id = Guid.NewGuid()
        collectionName = collectionName
        fileNameFull = fileName
        fileName = Path.GetFileName(fileName)
        fileExtension = Path.GetExtension(fileName)
        fileSize = (new FileInfo(fileName)).Length
        fileCreatedDate = File.GetCreationTime(fileName)
        fileModifiedDate = File.GetLastWriteTime(fileName)
    }

type readMetadataResults =
    | Readed of FileMetadata
    | WithError of string

let readMetadata (f: FileData, typeDescr: string) =
    try
        let rawMetadata = ImageMetadataReader.ReadMetadata(f.fileNameFull)
        let metadata =
            rawMetadata 
            |> Seq.map (fun d ->
                 let dir = new MetadataDirectory()
                 dir.name <- d.Name
                 dir.tags <- (d.Tags
                    |> Seq.map (fun t ->
                        let tag = new MetadataTag()
                        tag.name <- t.Name
                        tag.value <- t.Description
                        tag)
                    |> Seq.toArray)

                 dir)
            |> Seq.toArray


        Readed {
            fileData = f
            typeDescr = typeDescr
            metadata = metadata               
        }
    with exc -> WithError @$"Error: {exc.Message}{exc.StackTrace}"


let createDbEntity (m:FileMetadata) =
    let ffm = new FileFullMetadata()
    ffm.fileId <- m.fileData.id.ToString()
    ffm.collectionName <- m.fileData.collectionName
    ffm.fileNameFull <- m.fileData.fileNameFull
    ffm.fileSize <- m.fileData.fileSize
    ffm.fileCreatedDate <- m.fileData.fileCreatedDate.ToString()
    ffm.fileModifiedDate <- m.fileData.fileModifiedDate.ToString()
    ffm.typeDescr <- m.typeDescr
    ffm.metadata <- m.metadata
    ffm
    

type ThumbnailingRec = {
    fileId: Guid
    fileNameFull: string
    tempVideoPreviewName: string
    typeDescr: string
    thumbnailBase64: string
}

let createThumbnailingMsg (m:FileMetadata) =
    {
        fileId = m.fileData.id
        fileNameFull = m.fileData.fileNameFull
        typeDescr = m.typeDescr
        tempVideoPreviewName = ""
        thumbnailBase64 = ""
    }


let scaleImage (data: byte array, maxSizePx: int, jpegQuality: int) =
    use bitmap = SKBitmap.Decode(data)
    let _width = bitmap.Width
    let _height = bitmap.Height

    if bitmap.ColorType <> SKImageInfo.PlatformColorType then
        bitmap.CopyTo(bitmap, SKImageInfo.PlatformColorType) |> ignore
    
    let mutable width = 0
    let mutable height = 0

    if bitmap.Width >= bitmap.Height then
        width <- maxSizePx
        height <- Convert.ToInt32((double)bitmap.Height / (double)bitmap.Width * (double)maxSizePx)
    else
        height <- maxSizePx
        width <- Convert.ToInt32((double)bitmap.Width / (double)bitmap.Height * (double)maxSizePx)
    
    let imageInfo = new SKImageInfo(width, height)
    use thumbnail = bitmap.Resize(imageInfo, SKFilterQuality.Medium)
    use img = SKImage.FromBitmap(thumbnail)
    use jpeg = img.Encode(SKEncodedImageFormat.Jpeg, jpegQuality)
    use memoryStream = new MemoryStream()

    jpeg.AsStream().CopyTo(memoryStream)
    (memoryStream.ToArray(), _width, _height)
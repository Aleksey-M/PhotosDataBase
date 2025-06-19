namespace Mediafiles.Application.Validation

open Mediafiles.Domain.Entities
open System.Text.RegularExpressions

type MediaCollectionValidationResult = 
| Validated of MediaCollectionInfo
| WithErrors of string


module Validator = 
    let validateMediaCollection (md:MediaCollectionInfo) =
        let mutable message = ""

        let rx = Regex(@"^[a-z0-9_]+$")
        if not (rx.IsMatch(md.CollectionName)) then
            message <- "Некорректное название коллекции"

        if message <> "" then
            WithErrors message
        else
            Validated md
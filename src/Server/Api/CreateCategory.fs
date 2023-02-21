module CreateCategory

open System
open Authentication
open Data
open FsToolkit.ErrorHandling
open FSharp.UMX
open DataAccess
open Shared

let createCategoryService
  (getCurrentUserId: GetCurrentUserId)
  (findCategory: FindCategoryByNameForUser)
  (saveCategory: SaveAsync<Category>)
  : CreateCategoryService =
  fun request -> asyncResult {
    let currentUserId = getCurrentUserId () |> Option.get

    let! _ =
      findCategory currentUserId request.CategoryName
      |> AsyncResult.requireNone CategoryAlreadyExists

    let category = {
      Id = % Guid.NewGuid()
      Name = request.CategoryName
      UserId = currentUserId
    }

    do! saveCategory category

    return {
      Id = category.Id
      Name = category.Name
    }
  }

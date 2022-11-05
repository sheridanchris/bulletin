module InitialData

open System
open Marten
open Marten.Schema
open Data

let newsSources: NewsSource list =
    [ { Id = Guid.NewGuid()
        ShortName = "CNN"
        RssFeed = "http://rss.cnn.com/rss/cnn_topstories.rss" }

      { Id = Guid.NewGuid()
        ShortName = "NBC News"
        RssFeed = "https://feeds.nbcnews.com/nbcnews/public/news" }

      { Id = Guid.NewGuid()
        ShortName = "GlobalNews CA"
        RssFeed = "https://globalnews.ca/feed" } ]

type InitialData() =
    interface IInitialData with
        member this.Populate(store, cancellation) =
            use session = store.LightweightSession()
            session |> Session.storeMany newsSources
            session |> Session.saveChangesTask cancellation

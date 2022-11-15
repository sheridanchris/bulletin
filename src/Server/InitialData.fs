module InitialData

open System
open Marten
open Marten.Schema
open Data

let newsSources: NewsSource list = [
  {
    Id = Guid.NewGuid()
    ShortName = "CNN"
    RssFeed = "http://rss.cnn.com/rss/cnn_topstories.rss"
  }

  {
    Id = Guid.NewGuid()
    ShortName = "NBC News"
    RssFeed = "https://feeds.nbcnews.com/nbcnews/public/news"
  }

  {
    Id = Guid.NewGuid()
    ShortName = "NY Times"
    RssFeed = "https://rss.nytimes.com/services/xml/rss/nyt/World.xml"
  }

  {
    Id = Guid.NewGuid()
    ShortName = "HuffPost"
    RssFeed = "https://chaski.huffpost.com/us/auto/vertical/world-news"
  }

  {
    Id = Guid.NewGuid()
    ShortName = "GlobalNews CA"
    RssFeed = "https://globalnews.ca/feed"
  }

  {
    Id = Guid.NewGuid()
    ShortName = "BBC"
    RssFeed = "http://feeds.bbci.co.uk/news/rss.xml"
  }

  {
    Id = Guid.NewGuid()
    ShortName = "The Guardian"
    RssFeed = "https://www.theguardian.com/us/rss"
  }

  {
    Id = Guid.NewGuid()
    ShortName = "CNBC"
    RssFeed = "https://search.cnbc.com/rs/search/combinedcms/view.xml?partnerId=wrss01&id=100003114"
  }
]

type InitialData() =
  interface IInitialData with
    member _.Populate(store, cancellation) =
      use session = store.LightweightSession()
      session |> Session.storeMany newsSources
      session |> Session.saveChangesTask cancellation
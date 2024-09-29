[<RequireQualifiedAccess>]
module Parser

open System
open Domain
open FSharp.Data
open System.Xml
open System.Globalization
open System.Net.Http
open FsToolkit.ErrorHandling

// https://validator.w3.org/feed/docs/atom.html#sampleFeed
type Atom =
  XmlProvider<
    Sample="""
<feed xmlns="http://www.w3.org/2005/Atom">
  <entry>
    <title>Atom-Powered Robots Run Amok</title>
    <link href="http://example.org/2003/12/13/atom03"/>
    <summary>Some text.</summary>
    <published>2003-12-13T09:17:51-08:00</published>
  </entry>
  <entry>
    <title>Atom-Powered Robots Run Amok</title>
    <link href="http://example.org/2003/12/13/atom03"/>
    <summary>Some text.</summary>
  </entry>
</feed>
"""
   >

// https://validator.w3.org/feed/docs/rss1.html#s7
type RSSv1 =
  XmlProvider<"""
<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns="http://purl.org/rss/1.0/">
  <item rdf:about="http://xml.com/pub/2000/08/09/xslt/xslt.html">
    <title>Processing Inclusions with XSLT</title>
    <link>http://xml.com/pub/2000/08/09/xslt/xslt.html</link>
    <description>
     Processing document inclusions with general XML tools can be 
     problematic. This article proposes a way of preserving inclusion 
     information through SAX-based processing.
    </description>
  </item>
  <item rdf:about="http://xml.com/pub/2000/08/09/rdfdb/index.html">
    <title>Putting RDF to Work</title>
    <link>http://xml.com/pub/2000/08/09/rdfdb/index.html</link>
    <description>
     Tool and API support for the Resource Description Framework 
     is slowly coming of age. Edd Dumbill takes a look at RDFDB, 
     one of the most exciting new RDF toolkits.
    </description>
  </item>
</rdf:RDF>
""">

// https://www.rssboard.org/files/sample-rss-2.xml
// TODO: Is this compatible with v0.91 and v0.92??
type RSSv2 =
  XmlProvider<"""
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
   <channel>
      <item>
         <title>Louisiana Students to Hear from NASA Astronauts Aboard Space Station</title>
         <link>http://www.nasa.gov/press-release/louisiana-students-to-hear-from-nasa-astronauts-aboard-space-station</link>
         <description>As part of the state's first Earth-to-space call, students from Louisiana will have an opportunity soon to hear from NASA astronauts aboard the International Space Station.</description>
      </item>
      <item>
         <title>Blank Title. Idk why this one doesn't have a title</title>
         <link>http://www.nasa.gov/press-release/nasa-awards-integrated-mission-operations-contract-iii</link>
         <pubDate>Wed, 21 Aug 2024 17:51:18 GMT</pubDate>
      </item>
   </channel>
</rss>
    """>

let private (|StringEqualsIgnoreCase|_|) (str1: string) (str2: string) =
  if String.Equals(str1, str2, StringComparison.InvariantCultureIgnoreCase) then
    Some StringEqualsIgnoreCase
  else
    None

let private versionToFeedType version =
  match version with
  | "2.0" -> Some(FeedType.Rss RssVersion.V2)
  | _ -> None

let private parseRssVersionAttribute (rootElement: XmlElement) =
  rootElement.Attributes["version"]
  |> Option.ofObj
  |> Option.map (_.Value >> versionToFeedType)
  |> Option.flatten

let private atomToFeedEntry currentDateTime feedId (entry: Atom.Entry) =
  let publishedAt = entry.Published |> Option.defaultValue currentDateTime

  {
    Id = 0
    FeedId = feedId
    Title = entry.Title
    Url = entry.Link.Href
    Description = Some entry.Summary
    IsFavorited = false
    PublishedAt = publishedAt
    UpdatedAt = publishedAt
  }

let private rssV1ToFeedEntry currentDateTime feedId (item: RSSv1.Item) = {
  Id = 0
  FeedId = feedId
  Title = item.Title
  Url = item.Link
  Description = Some item.Description
  IsFavorited = false
  PublishedAt = currentDateTime
  UpdatedAt = currentDateTime
}

let private rssV2ToFeedEntry currentDateTime feedId (item: RSSv2.Item) : Domain.FeedEntry =
  // Theoretically this should always be a RFC 2822 date.
  // I modified the sample feed to reflect that.
  // If it causes errors then OOPS!
  let publishedAt = item.PubDate |> Option.defaultValue currentDateTime

  {
    Id = 0
    FeedId = feedId
    Title = item.Title
    Url = item.Link
    Description = item.Description
    IsFavorited = false
    PublishedAt = publishedAt
    UpdatedAt = publishedAt
  }

let determineFeedType (document: XmlDocument) =
  let rootElement = document.DocumentElement

  match rootElement.Name with
  | StringEqualsIgnoreCase "feed" -> Some FeedType.Atom
  | StringEqualsIgnoreCase "rdf" -> Some(FeedType.Rss RssVersion.V1)
  | StringEqualsIgnoreCase "rss" -> parseRssVersionAttribute rootElement
  | _ -> None

let determineFeedTypeFromXmlString (xmlString: string) =
  let xmlDocument = XmlDocument()
  xmlDocument.LoadXml xmlString
  determineFeedType xmlDocument

let parse currentDateTime feedId feedType content =
  let parseEntries parser toFeedEntry = parser >> Seq.map toFeedEntry

  let parser =
    match feedType with
    | FeedType.Atom -> parseEntries (Atom.Parse >> _.Entries) (atomToFeedEntry currentDateTime feedId)
    | FeedType.Rss RssVersion.V1 -> parseEntries (RSSv1.Parse >> _.Items) (rssV1ToFeedEntry currentDateTime feedId)
    | FeedType.Rss RssVersion.V2 ->
      parseEntries (RSSv2.Parse >> _.Channel.Items) (rssV2ToFeedEntry currentDateTime feedId)

  parser content

let onlyNewEntries lastEntryPublishedAt entries =
  match lastEntryPublishedAt with
  | None -> entries
  | Some lastEntryPublishedAt -> entries |> Seq.filter (fun entry -> entry.PublishedAt > lastEntryPublishedAt)

let getAndParseFeed (httpClient: HttpClient) currentDateTime lastEntryPublishedAt (feed: Feed) =
  feed.Url
  |> httpClient.GetStringAsync
  |> Task.map (parse currentDateTime feed.Id feed.Type >> onlyNewEntries lastEntryPublishedAt)

module Domain

open System

type UserId = UserId of Guid
type PostId = PostId of Guid

type VoteType =
    | Positive = 1
    | Negative = -1

type User = { UserId: UserId; Username: string }

type Vote = { VoterId: UserId; VoteType: VoteType }

type Post = {
    Id: PostId
    Headline: string
    Link: string
    Author: User option
    PublishedDate: DateTime
    Votes: Vote list
}

module Post =
    let authorName post =
        post.Author
        |> Option.map (fun user -> user.Username)
        |> Option.defaultValue "automated bot, probably."

    let findUserVote userId post =
        post.Votes |> List.tryFind (fun vote -> vote.VoterId = userId)

    let calculateScore post =
        post.Votes |> List.sumBy (fun vote -> int vote.VoteType)

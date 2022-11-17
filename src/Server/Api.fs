module Api

open System
open System.Security.Claims
open System.Threading
open BCrypt.Net
open Data
open Marten
open Marten.Pagination
open Microsoft.AspNetCore.Http
open Shared
open FsToolkit.ErrorHandling
open DataAccess

let toPaginated (mapping: 'a -> 'b) (pagedList: IPagedList<'a>) : Paginated<'b> = {
  Items = pagedList |> Seq.map mapping |> Seq.toList
  CurrentPage = int pagedList.PageNumber
  PageSize = int pagedList.PageSize
  PageCount = int pagedList.PageCount
  HasNextPage = pagedList.HasNextPage
  HasPreviousPage = pagedList.HasPreviousPage
}

let calculateScore votes =
  votes |> List.sumBy (fun vote -> int vote.VoteType)

let toPostModel (upvoted: bool) (downvoted: bool) (post: Post) : PostModel =
  let published = DateTime.friendlyDifference post.Published DateTime.UtcNow
  let score = calculateScore post.Votes

  {
    Id = post.Id
    Title = post.Headline
    Link = post.Link
    PublishedAt = published
    Author = post.AuthorName
    Score = score
    Upvoted = upvoted
    Downvoted = downvoted
    Source = post.FeedName
  }

let toUserModel (user: User) : UserModel = {
  Id = user.Id
  Username = user.Username
  EmailAddress = user.EmailAddress
}

let signIn (querySession: IQuerySession) (context: HttpContext) (loginRequest: LoginRequest) = taskResult {
  let! user =
    querySession
    |> tryFindUserAsync (FindByUsername loginRequest.Username)
    |> TaskResult.requireSome InvalidUsernameAndOrPassword

  do!
    BCrypt.Verify(loginRequest.Password, user.PasswordHash)
    |> Result.requireTrue InvalidUsernameAndOrPassword

  do! Auth.signInWithProperties Auth.defaultProperties context user
  return toUserModel user
}

let createAccount
  (querySession: IQuerySession)
  (documentSession: IDocumentSession)
  (context: HttpContext)
  (createAccountRequest: CreateAccountRequest)
  =
  taskResult {
    let findByUsername = FindByUsername createAccountRequest.Username
    let findByEmail = FindByEmailAddress createAccountRequest.EmailAddress

    do!
      querySession
      |> tryFindUserAsync findByUsername
      |> TaskResult.requireNone UsernameTaken

    do!
      querySession
      |> tryFindUserAsync findByEmail
      |> TaskResult.requireNone EmailAddressTaken

    let passwordHash = BCrypt.HashPassword(createAccountRequest.Password)

    let user = {
      Id = Guid.NewGuid()
      Username = createAccountRequest.Username
      EmailAddress = createAccountRequest.EmailAddress
      PasswordHash = passwordHash
    }

    do! documentSession |> saveUserAsync user
    do! Auth.signInWithProperties Auth.defaultProperties context user
    return toUserModel user
  }

let getCurrentUser (querySession: IQuerySession) (context: HttpContext) = task {
  let nameIdentifier =
    context.User.FindFirstValue(ClaimTypes.NameIdentifier)
    |> Option.ofNull
    |> Option.map Guid.Parse

  match nameIdentifier with
  | None -> return Anonymous
  | Some userId ->
    let findById = FindById userId
    let! user = querySession |> tryFindUserAsync findById |> TaskOption.map toUserModel
    return user |> Option.map User |> Option.defaultValue Anonymous
}

let getVoteType (voteType: VoteType) =
  let upvoted = voteType = VoteType.Positive
  let downvoted = voteType = VoteType.Negative
  upvoted, downvoted

let getPosts (querySession: IQuerySession) (context: HttpContext) (getPostsModel: GetPostsModel) = task {
  let! posts = querySession |> getPostsAsync getPostsModel CancellationToken.None

  let nameIdentifier =
    context.User.FindFirstValue(ClaimTypes.NameIdentifier)
    |> Option.ofNull
    |> Option.map Guid.Parse

  return
    match nameIdentifier with
    | None -> toPaginated (toPostModel false false) posts
    | Some userId ->
      posts
      |> toPaginated (fun post ->
        let upvoted, downvoted =
          post.Votes
          |> List.tryFind (fun vote -> vote.VoterId = userId)
          |> Option.map (fun vote -> getVoteType vote.VoteType)
          |> Option.defaultValue (false, false)

        toPostModel upvoted downvoted post)
}

// TODO: This should be under a secured API variant.
let toggleVote
  (context: HttpContext)
  (postId: Guid)
  (voteType: VoteType)
  (querySession: IQuerySession)
  (documentSession: IDocumentSession)
  =
  taskResult {
    let typeToResult postModel =
      if voteType = VoteType.Positive then
        VoteResult.Positive postModel
      elif voteType = VoteType.Negative then
        VoteResult.Negative postModel
      else
        VoteResult.NoVote postModel

    let! nameIdentifier =
      context.User.FindFirstValue(ClaimTypes.NameIdentifier)
      |> Option.ofNull
      |> Option.map Guid.Parse
      |> Result.requireSome VoteError.Unauthorized

    let! post =
      querySession
      |> tryFindPostAsync postId
      |> TaskResult.requireSome VoteError.PostNotFound

    let userVote =
      post.Votes |> List.tryFindWithIndex (fun vote -> vote.VoterId = nameIdentifier)

    match userVote with
    | Some(index, vote) ->
      let post, resultFunc, upvoted, downvoted =
        if vote.VoteType = voteType then
          let newVotes = List.removeAt index post.Votes
          let newScore = calculateScore newVotes

          { post with
              Votes = List.removeAt index post.Votes
              Score = newScore
          },
          VoteResult.NoVote,
          false,
          false
        else
          let newVotes = List.updateAt index { vote with VoteType = voteType } post.Votes
          let newScore = calculateScore newVotes

          let newPost =
            { post with
                Votes = newVotes
                Score = newScore
            }

          let upvoted, downvoted = getVoteType voteType
          newPost, typeToResult, upvoted, downvoted

      do! documentSession |> savePostAsync post
      let postModel = toPostModel upvoted downvoted post
      return resultFunc postModel
    | None ->
      let vote = {
        VoterId = nameIdentifier
        VoteType = voteType
      }

      let upvoted, downvoted = getVoteType voteType
      let newVotes = vote :: post.Votes
      let newScore = calculateScore newVotes

      let post =
        { post with
            Votes = newVotes
            Score = newScore
        }

      do! documentSession |> savePostAsync post
      return typeToResult (toPostModel upvoted downvoted post)
  }

let serverApi (context: HttpContext) : ServerApi =
  let querySession = context.GetService<IQuerySession>()
  let documentSession = context.GetService<IDocumentSession>()

  {
    Login = fun request -> signIn querySession context request |> Async.AwaitTask
    CreateAccount = fun request -> createAccount querySession documentSession context request |> Async.AwaitTask
    GetCurrentUser = fun () -> getCurrentUser querySession context |> Async.AwaitTask
    ToggleUpvote =
      fun postId ->
        toggleVote context postId VoteType.Positive querySession documentSession
        |> Async.AwaitTask
    ToggleDownvote =
      fun postId ->
        toggleVote context postId VoteType.Negative querySession documentSession
        |> Async.AwaitTask
    GetPosts = fun model -> getPosts querySession context model |> Async.AwaitTask
  }

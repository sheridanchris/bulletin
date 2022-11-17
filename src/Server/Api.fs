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
  Items = pagedList |> Seq.map mapping
  CurrentPage = int pagedList.PageNumber
  PageSize = int pagedList.PageSize
  PageCount = int pagedList.PageCount
  HasNextPage = pagedList.HasNextPage
  HasPreviousPage = pagedList.HasPreviousPage
}

let toPostModel (upvoted: bool) (downvoted: bool) (post: Post) : PostModel =
  let published = DateTime.friendlyDifference post.Published DateTime.UtcNow

  {
    Id = post.Id
    Title = post.Headline
    Link = post.Link
    PublishedAt = published
    Author = post.AuthorName
    Score = post.Score
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

let getPosts (querySession: IQuerySession) (context: HttpContext) (getPostsModel: GetPostsModel) = task {
  let! posts = querySession |> getPostsAsync getPostsModel CancellationToken.None

  let nameIdentifier =
    context.User.FindFirstValue(ClaimTypes.NameIdentifier)
    |> Option.ofNull
    |> Option.map Guid.Parse

  match nameIdentifier with
  | None -> return toPaginated (toPostModel false false) posts
  | Some userId ->
    let ids = [| for post in posts -> post.Id |]
    let! votes = getPostVotesAsync ids userId CancellationToken.None querySession

    return
      posts
      |> toPaginated (fun post ->
        let vote = votes |> Seq.tryFind (fun vote -> vote.PostId = post.Id)

        let upvoted, downvoted =
          match vote with
          | None -> false, false
          | Some vote -> vote.VoteType = VoteType.Positive, vote.VoteType = VoteType.Negative

        toPostModel upvoted downvoted post)
}

let serverApi (context: HttpContext) : ServerApi =
  let querySession = context.GetService<IQuerySession>()
  let documentSession = context.GetService<IDocumentSession>()

  {
    Login = fun request -> signIn querySession context request |> Async.AwaitTask
    CreateAccount = fun request -> createAccount querySession documentSession context request |> Async.AwaitTask
    GetCurrentUser = fun () -> getCurrentUser querySession context |> Async.AwaitTask
    GetPosts = fun model -> getPosts querySession context model |> Async.AwaitTask
  }

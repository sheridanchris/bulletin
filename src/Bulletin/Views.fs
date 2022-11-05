module Views

open System
open Data
open DataAccess

type Post =
    { id: Guid
      headline: string
      link: string
      score: int
      upvoted: bool
      downvoted: bool
      author: string
      published: string }

type PaginatedPosts =
    { posts: Post seq
      current_page: int64
      has_next_page: bool
      has_previous_page: bool
      pages: int64 }

type CommentTree =
    { id: Guid
      text: string
      score: int
      upvoted: bool
      downvoted: bool
      author: string
      depth: int
      published: string
      children: CommentTree list }

let rec createPostModel paginatedResult =
    let postModel (post, author) =
        let published = DateTime.friendlyDifference post.Published DateTime.UtcNow

        { id = post.Id
          headline = post.Headline
          link = post.Link
          score = post.Score
          upvoted = false // todo
          downvoted = false // todo
          author = author
          published = published }

    let responseModel posts =
        { posts = posts
          current_page = paginatedResult.CurrentPage
          has_next_page = paginatedResult.HasNextPage
          has_previous_page = paginatedResult.HasPreviousPage
          pages = paginatedResult.PageCount }

    paginatedResult.Items |> Seq.map postModel |> responseModel

let createCommentTree (comment: Comment) =
    let rec commentModelRecursive (depth: int) (comment: Comment) =
        let published = DateTime.friendlyDifference comment.Published DateTime.UtcNow

        { id = comment.Id
          text = comment.Text
          score = comment.Score
          upvoted = false // todo
          downvoted = false // todo
          author = comment.AuthorId
          depth = depth
          published = published
          children = buildTree depth comment.Children }

    and buildTree (depth: int) (comments: Comment list) =
        match comments with
        | [] -> []
        | x :: xs ->
            let depth = depth + 1
            let model = commentModelRecursive depth x
            let res = (List.map (commentModelRecursive depth) xs)
            model :: res

    commentModelRecursive 1 comment

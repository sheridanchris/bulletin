open System

type Comment =
    { Id: Guid
      Text: string
      Deleted: bool
      PostId: Guid
      AuthorId: string
      Published: DateTime
      Children: Comment list
      Score: int }

type CommentTree =
    { id: Guid
      text: string
      score: int
      upvoted: bool
      downvoted: bool
      author: string
      depth: int
      children: CommentTree list }

let commentModel (comment: Comment) =
    let rec commentModelRecursive (depth: int) (comment: Comment) =
        { id = comment.Id
          text = comment.Text
          score = comment.Score
          upvoted = false // todo
          downvoted = false // todo
          author = comment.AuthorId
          depth = depth
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

let comments (maxIter: int) (children: unit -> Comment list) =
    [ for _ in 0..maxIter ->
          { Id = Guid.NewGuid()
            Text = ""
            Deleted = false
            PostId = Guid.NewGuid()
            AuthorId = "author"
            Published = DateTime.UtcNow
            Children = children ()
            Score = 0 } ]

let commentTree =
    List.map commentModel (comments 1 (fun () -> comments 1 (fun () -> comments 1 (fun _ -> []))))

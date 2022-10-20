// This is temporary... until the new version of Marten.FSharp gets pushed to NuGet by its author.

namespace Marten

open System
open System.Linq
open Marten
open FsToolkit.ErrorHandling
open System
open System.Linq.Expressions
open System.Threading

module Lambda =
    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Linq.RuntimeHelpers.LeafExpressionConverter

    let rec translateExpr (linq: Expression) =
        match linq with
        | :? MethodCallExpression as mc ->
            match mc.Arguments.[0] with
            | :? LambdaExpression as le ->
                let args, body = translateExpr le.Body
                le.Parameters.[0] :: args, body
            | :? System.Linq.Expressions.MemberExpression as me ->
                // Not sure what to do here.  I'm sure there will be hidden bugs
                [], linq
            | _ as unknown ->
                // Not sure what to do here.  I'm sure there will be hidden bugs
                // x.GetType() |> printfn "x: %A"
                [], linq
        | _ -> [], linq

    let inline toLinq<'a> expr =
        let args, body = expr |> QuotationToExpression |> translateExpr
        Expression.Lambda<'a>(body, args |> Array.ofList)

    let inline ofArity1 (expr: Expr<'a -> 'b>) = toLinq<Func<'a, 'b>> expr
    let inline ofArity2 (expr: Expr<'a -> 'b -> 'c>) = toLinq<Func<'a, 'b, 'c>> expr

module Session =

    type PrimaryKey =
        | Guid of Guid
        | String of string
        | Int of int32
        | Int64 of int64

    let deleteEntity (entity: 'a) (session: IDocumentSession) =
        session.Delete(entity)
        session

    let deleteByGuid<'a> (id: Guid) (session: IDocumentSession) =
        session.Delete<'a>(id)
        session

    let deleteByString<'a> (id: string) (session: IDocumentSession) =
        session.Delete<'a>(id)
        session

    let deleteByInt<'a> (id: int) (session: IDocumentSession) =
        session.Delete<'a>(id)
        session

    let deleteByInt64<'a> (id: int64) (session: IDocumentSession) =
        session.Delete<'a>(id)
        session

    let delete<'a> (primaryKey: PrimaryKey) (session: IDocumentSession) =
        session
        |> match primaryKey with
           | Guid g -> deleteByGuid<'a> g
           | String s -> deleteByString<'a> s
           | Int i -> deleteByInt<'a> i
           | Int64 i -> deleteByInt64<'a> i

    let deleteBy<'a> (predicate: Quotations.Expr<'a -> bool>) (session: IDocumentSession) =
        predicate |> Lambda.ofArity1 |> session.DeleteWhere
        session

    let loadByGuid<'a> (id: Guid) (session: IQuerySession) = session.Load<'a>(id) |> Option.ofNull

    let loadByInt<'a> (id: int) (session: IQuerySession) = session.Load<'a>(id) |> Option.ofNull

    let loadByInt64<'a> (id: int64) (session: IQuerySession) = session.Load<'a>(id) |> Option.ofNull

    let loadByString<'a> (id: string) (session: IQuerySession) = session.Load<'a>(id) |> Option.ofNull

    let load<'a> (primaryKey: PrimaryKey) (session: IQuerySession) =
        session
        |> match primaryKey with
           | Guid g -> loadByGuid<'a> g
           | String s -> loadByString<'a> s
           | Int i -> loadByInt<'a> i
           | Int64 i -> loadByInt64<'a> i

    let loadByGuidTaskCt<'a> (cancellationToken: CancellationToken) (id: Guid) (session: IQuerySession) =
        session.LoadAsync<'a>(id, cancellationToken) |> Task.map Option.ofNull

    let loadByGuidTask<'a> (id: Guid) (session: IQuerySession) =
        loadByGuidTaskCt<'a> CancellationToken.None id session

    let loadByIntTaskCt<'a> (cancellationToken: CancellationToken) (id: int32) (session: IQuerySession) =
        session.LoadAsync<'a>(id, cancellationToken) |> Task.map Option.ofNull

    let loadByIntTask<'a> (id: int32) (session: IQuerySession) =
        loadByIntTaskCt<'a> CancellationToken.None id session

    let loadByInt64TaskCt<'a> (cancellationToken: CancellationToken) (id: int64) (session: IQuerySession) =
        session.LoadAsync<'a>(id, cancellationToken) |> Task.map Option.ofNull

    let loadByInt64Task<'a> (id: int64) (session: IQuerySession) =
        loadByInt64TaskCt<'a> CancellationToken.None id session

    let loadByStringTaskCt<'a> (cancellationToken: CancellationToken) (id: string) (session: IQuerySession) =
        session.LoadAsync<'a>(id, cancellationToken) |> Task.map Option.ofNull

    let loadByStringTask<'a> (id: string) (session: IQuerySession) =
        loadByStringTaskCt<'a> CancellationToken.None id session

    let loadByGuidAsync<'a> (id: Guid) (session: IQuerySession) =
        async {
            let! ct = Async.CancellationToken
            return! session |> loadByGuidTaskCt<'a> ct id |> Async.AwaitTask
        }

    let loadByIntAsync<'a> (id: int32) (session: IQuerySession) =
        async {
            let! ct = Async.CancellationToken
            return! session |> loadByIntTaskCt<'a> ct id |> Async.AwaitTask
        }

    let loadByInt64Async<'a> (id: int64) (session: IQuerySession) =
        async {
            let! ct = Async.CancellationToken
            return! session |> loadByInt64TaskCt<'a> ct id |> Async.AwaitTask
        }

    let loadByStringAsync<'a> (id: string) (session: IQuerySession) =
        async {
            let! ct = Async.CancellationToken
            return! session |> loadByStringTaskCt<'a> ct id |> Async.AwaitTask
        }

    let loadTaskCt<'a> (cancellationToken: CancellationToken) (primaryKey: PrimaryKey) (session: IQuerySession) =
        session
        |> match primaryKey with
           | Guid g -> loadByGuidTaskCt<'a> cancellationToken g
           | String s -> loadByStringTaskCt<'a> cancellationToken s
           | Int i -> loadByIntTaskCt<'a> cancellationToken i
           | Int64 i -> loadByInt64TaskCt<'a> cancellationToken i

    let loadTask<'a> (primaryKey: PrimaryKey) (session: IQuerySession) =
        loadTaskCt CancellationToken.None primaryKey session

    let loadAsync<'a> (pk: PrimaryKey) (session: IQuerySession) =
        async {
            let! ct = Async.CancellationToken
            return! session |> loadTaskCt<'a> ct pk |> Async.AwaitTask
        }

    let query<'a> (session: IQuerySession) = session.Query<'a>()

    let sql<'a> (sqlString: string) (parameters: obj array) (session: IQuerySession) =
        session.Query<'a>(sqlString, parameters)

    let sqlTaskCt<'a>
        (cancellationToken: CancellationToken)
        (sqlString: string)
        (parameters: obj[])
        (session: IQuerySession)
        =
        session.QueryAsync<'a>(sqlString, cancellationToken, parameters = parameters)

    let sqlTask<'a> (sqlString: string) (parameters: obj[]) (session: IQuerySession) =
        sqlTaskCt<'a> CancellationToken.None sqlString parameters session

    let sqlAsync<'a> (sqlString: string) (parameters: obj[]) (session: IQuerySession) =
        async {
            let! ct = Async.CancellationToken
            return! session |> sqlTaskCt<'a> ct sqlString parameters |> Async.AwaitTask
        }

    let saveChanges (session: IDocumentSession) = session.SaveChanges()

    let saveChangesTaskCt (cancellationToken: CancellationToken) (session: IDocumentSession) =
        session.SaveChangesAsync(cancellationToken)

    let saveChangesTask (session: IDocumentSession) =
        saveChangesTaskCt CancellationToken.None session

    let saveChangesAsync (session: IDocumentSession) =
        async {
            let! ct = Async.CancellationToken
            return! session |> saveChangesTaskCt ct |> Async.AwaitTask
        }

    let storeSingle (entity: 'a) (session: IDocumentSession) =
        session.Store([| entity |])
        session

    let storeMany (entities: seq<'a>) (session: IDocumentSession) =
        entities |> session.Store
        session

module Queryable =

    let exactlyOne (q: IQueryable<'a>) = q.Single()

    let exactlyOneTaskCt (cancellationToken: CancellationToken) (q: IQueryable<'a>) = q.SingleAsync(cancellationToken)

    let exactlyOneTask (q: IQueryable<'a>) =
        exactlyOneTaskCt CancellationToken.None q

    let exactlyOneAsync (q: IQueryable<'a>) =
        async {
            let! ct = Async.CancellationToken
            return! q |> exactlyOneTaskCt ct |> Async.AwaitTask
        }

    let filter (predicate: Quotations.Expr<'a -> bool>) (q: IQueryable<'a>) = predicate |> Lambda.ofArity1 |> q.Where

    let head (q: IQueryable<'a>) = q.First()

    let headTaskCt (cancellationToken: CancellationToken) (q: IQueryable<'a>) = q.FirstAsync(cancellationToken)

    let headTask (q: IQueryable<'a>) = headTaskCt CancellationToken.None q

    let headAsync (q: IQueryable<'a>) =
        async {
            let! ct = Async.CancellationToken
            return! q |> headTaskCt ct |> Async.AwaitTask
        }

    let map (mapper: Quotations.Expr<'a -> 'b>) (q: IQueryable<'a>) = mapper |> Lambda.ofArity1 |> q.Select

    let toList (q: IQueryable<'a>) = q.ToList()

    let toListTaskCt (cancellationToken: CancellationToken) (q: IQueryable<'a>) = q.ToListAsync(cancellationToken)

    let toListTask (q: IQueryable<'a>) = toListTaskCt CancellationToken.None q

    let toListAsync (q: IQueryable<'a>) =
        async {
            let! ct = Async.CancellationToken
            return! q |> toListTaskCt ct |> Async.AwaitTask
        }

    let tryExactlyOne (q: IQueryable<'a>) = q.SingleOrDefault() |> Option.ofNull

    let tryExactlyOneTaskCt (cancellationToken: CancellationToken) (q: IQueryable<'a>) =
        q.SingleOrDefaultAsync(cancellationToken) |> Task.map Option.ofNull

    let tryExactlyOneTask (q: IQueryable<'a>) =
        tryExactlyOneTaskCt CancellationToken.None q

    let tryExactlyOneAsync (q: IQueryable<'a>) =
        async {
            let! ct = Async.CancellationToken
            return! q |> tryExactlyOneTaskCt ct |> Async.AwaitTask
        }

    let tryHead (q: IQueryable<'a>) = q.FirstOrDefault() |> Option.ofNull

    let tryHeadTaskCt (cancellationToken: CancellationToken) (q: IQueryable<'a>) =
        q.FirstOrDefaultAsync(cancellationToken) |> Task.map Option.ofNull

    let tryHeadTask (q: IQueryable<'a>) = tryHeadTaskCt CancellationToken.None q

    let tryHeadAsync (q: IQueryable<'a>) =
        async {
            let! ct = Async.CancellationToken
            return! q |> tryHeadTaskCt ct |> Async.AwaitTask
        }

    let countWhere<'a> (predicate: Quotations.Expr<'a -> bool>) (q: IQueryable<'a>) =
        predicate |> Lambda.ofArity1 |> q.Count

    let count (q: IQueryable<'a>) = q.Count()

    let countLongWhere (predicate: Quotations.Expr<'a -> bool>) (q: IQueryable<'a>) =
        predicate |> Lambda.ofArity1 |> q.LongCount

    let countLong (q: IQueryable<'a>) = q.LongCount()

    let countTaskCt (cancellationToken: CancellationToken) (q: IQueryable<'a>) = q.CountAsync(cancellationToken)

    let countTask (q: IQueryable<'a>) = countTaskCt CancellationToken.None q

    let countAsync (q: IQueryable<'a>) =
        async {
            let! ct = Async.CancellationToken
            return! q |> countTaskCt ct |> Async.AwaitTask
        }

    let countWhereTaskCt
        (cancellationToken: CancellationToken)
        (predicate: Quotations.Expr<'a -> bool>)
        (q: IQueryable<'a>)
        =
        q.CountAsync(Lambda.ofArity1 predicate, cancellationToken)

    let countWhereTask (predicate: Quotations.Expr<'a -> bool>) (q: IQueryable<'a>) =
        countWhereTaskCt CancellationToken.None predicate q

    let countWhereAsync (predicate: Quotations.Expr<'a -> bool>) (q: IQueryable<'a>) =
        async {
            let! ct = Async.CancellationToken
            return! q |> countWhereTaskCt ct predicate |> Async.AwaitTask
        }

    let countLongWhereTaskCt
        (cancellationToken: CancellationToken)
        (predicate: Quotations.Expr<'a -> bool>)
        (q: IQueryable<'a>)
        =
        q.LongCountAsync(Lambda.ofArity1 predicate, cancellationToken)

    let countLongWhereTask (predicate: Quotations.Expr<'a -> bool>) (q: IQueryable<'a>) =
        countLongWhereTaskCt CancellationToken.None predicate q

    let countLongWhereAsync (predicate: Quotations.Expr<'a -> bool>) (q: IQueryable<'a>) =
        async {
            let! ct = Async.CancellationToken
            return! countLongWhereTaskCt ct predicate q |> Async.AwaitTask
        }

    let min<'a, 'b when 'b: comparison> (predicate: Quotations.Expr<'a -> 'b>) (q: IQueryable<'a>) =
        predicate |> Lambda.ofArity1 |> q.Min

    let max<'a, 'b when 'b: comparison> (predicate: Quotations.Expr<'a -> 'b>) (q: IQueryable<'a>) =
        predicate |> Lambda.ofArity1 |> q.Max

    let orderBy<'a, 'b when 'b: comparison> (keySelector: Quotations.Expr<'a -> 'b>) (q: IQueryable<'a>) =
        keySelector |> Lambda.ofArity1 |> q.OrderBy

    let orderByDescending<'a, 'b when 'b: comparison> (keySelector: Quotations.Expr<'a -> 'b>) (q: IQueryable<'a>) =
        keySelector |> Lambda.ofArity1 |> q.OrderByDescending

    let thenBy (keySelector: Quotations.Expr<'a -> 'b>) (oq: IOrderedQueryable<'a>) =
        keySelector |> Lambda.ofArity1 |> oq.ThenBy

    let skip (amount: int) (q: IQueryable<'a>) = q.Skip(amount)

    let take (amount: int) (q: IQueryable<'a>) = q.Take(amount)

    let paging (skipped: int) (takeAmount: int) (q: IQueryable<'a>) = q |> skip skipped |> take takeAmount

    let includeSingle<'a, 'b, 'c>
        (selector: Quotations.Expr<'a -> obj>)
        (continueWith: IQueryable<'a> -> 'c)
        (q: IQueryable<'a>)
        =
        let mutable value: 'b = Unchecked.defaultof<_>
        let q = q.Include(Lambda.ofArity1 selector, (fun x -> value <- x))
        let result = continueWith q
        result, value

    let includeList<'a, 'b>
        (selector: Quotations.Expr<'a -> obj>)
        (seq: Collections.Generic.IList<'b>)
        (q: IQueryable<'a>)
        =
        q.Include(Lambda.ofArity1 selector, seq)

    let includeDict<'a, 'b, 'c>
        (selector: Quotations.Expr<'a -> obj>)
        (dict: Collections.Generic.Dictionary<'b, 'c>)
        (q: IQueryable<'a>)
        =
        q.Include(Lambda.ofArity1 selector, dict)

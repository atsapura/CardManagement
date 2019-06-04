namespace CardManagement.Data
module CommandRepository =
    open CardManagement.Common.Errors
    open CardMongoConfiguration
    open CardDomainEntities
    open System
    open System.Threading
    open MongoDB.Driver
    open System.Threading.Tasks
    open FsToolkit.ErrorHandling
    open MongoDB.Driver
    open System.Linq.Expressions

    type CreateUserAsync = UserEntity -> IoResult<unit>
    type CreateCardAsync = CardEntity * CardAccountInfoEntity -> IoResult<unit>
    type ReplaceUserAsync = UserEntity -> IoResult<unit>
    type ReplaceCardAsync = CardEntity -> IoResult<unit>
    type UpdateCardAccountAsync = CardAccountInfoEntity -> IoResult<unit>

    let updateOptions =
        let opt = UpdateOptions()
        opt.IsUpsert <- false
        opt

    let private (|DuplicateKey|_|) (ex: Exception) =
        match ex with
        | :? MongoWriteException as ex when ex.WriteError.Category = ServerErrorCategory.DuplicateKey ->
            Some ex
        | _ -> None

    let inline private executeInsertAsync (func: 'a -> Async<unit>) arg =
        try
            async {
                do! func(arg)
                return Ok ()
            }
        with
        | DuplicateKey ex ->
            async { return EntityAlreadyExists (arg.GetType().Name, (entityId arg)) |> Error }

    let inline private executeReplaceAsync (update: _ -> Task<ReplaceOneResult>) arg =
        async {
            let! updateResult =
                update(idComparer arg, arg, updateOptions) |> Async.AwaitTask
            if not updateResult.IsAcknowledged then
                return sprintf "Update was not acknowledged for %A" arg |> failwith
            elif updateResult.MatchedCount = 0L then
                return EntityNotFound (arg.GetType().Name, entityId arg) |> Error
            else return Ok()
        }

    let createUserAsync (mongoDb : MongoDb) : CreateUserAsync =
        fun userEntity ->
        let insertUser = mongoDb.GetCollection<UserEntity>(userCollection).InsertOneAsync >> Async.AwaitTask
        userEntity |> executeInsertAsync insertUser

    let createCardAsync (mongoDb: MongoDb) : CreateCardAsync =
        fun (card, accountInfo) ->
        let insertCardCommand = mongoDb.GetCollection<CardEntity>(cardCollection).InsertOneAsync >> Async.AwaitTask
        let insertAccInfoCommand =
            mongoDb.GetCollection<CardAccountInfoEntity>(cardAccountInfoCollection).InsertOneAsync >> Async.AwaitTask
        asyncResult {
            do! card |> executeInsertAsync insertCardCommand
            do! accountInfo |> executeInsertAsync insertAccInfoCommand
        }

    let replaceUserAsync (mongoDb: MongoDb) : ReplaceUserAsync =
        fun user ->
            let replaceCommand (selector: Expression<_>, user, options) =
                mongoDb.GetCollection(userCollection).ReplaceOneAsync(selector, user, options)
            user |> executeReplaceAsync replaceCommand

    let replaceCardAsync (mongoDb: MongoDb) : ReplaceCardAsync =
        fun card ->
            let replaceCommand (selector: Expression<_>, card, options) =
                mongoDb.GetCollection(cardCollection).ReplaceOneAsync(selector, card, options)
            card |> executeReplaceAsync replaceCommand

    let replaceCardAccountInfoAsync (mongoDb: MongoDb) : ReplaceCardAsync =
        fun accInfo ->
            let replaceCommand (selector: Expression<_>, accInfo, options) =
                mongoDb.GetCollection(cardAccountInfoCollection).ReplaceOneAsync(selector, accInfo, options)
            accInfo |> executeReplaceAsync replaceCommand

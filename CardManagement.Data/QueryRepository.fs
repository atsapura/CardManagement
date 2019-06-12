namespace CardManagement.Data

module QueryRepository =
    open System.Linq.Expressions
    open System.Linq
    open CardManagement
    open CardDomain
    open Common.Errors
    open CardDomainEntities
    open MongoDB.Driver
    open MongoDB.Driver.Linq
    open CardMongoConfiguration
    open Microsoft.FSharp.Linq.RuntimeHelpers
    open System

    type IoQueryResult<'a> = Async<'a option>

    type GetCardAsync = MongoDb -> CardNumberString -> IoQueryResult<(CardEntity * CardAccountInfoEntity)>
    type GetUserAsync = MongoDb -> UserId -> IoQueryResult<UserEntity>
    type GetUserCardsAsync = MongoDb -> UserId -> Async<(CardEntity * CardAccountInfoEntity) list>

    let private runSingleQuery dbQuery id =
        async {
            let! result = dbQuery id |> Async.AwaitTask
            return unsafeNullToOption result
        }

    let private runListQuery (dbQuery: 'a -> IMongoQueryable<_>) arg =
        async {
            let queryResult = dbQuery arg
            if queryResult = null then return []
            else
            let! result = queryResult.ToListAsync() |> Async.AwaitTask
            return result |> List.ofSeq
        }

    let private getCardQuery (mongoDb: MongoDb) cardnumber =
        mongoDb.GetCollection<CardEntity>(cardCollection)
            .Find(fun c -> c.CardNumber = cardnumber)
            .FirstOrDefaultAsync()

    let private getAccountInfoQuery (mongoDb: MongoDb) cardnumber =
        mongoDb.GetCollection<CardAccountInfoEntity>(cardAccountInfoCollection)
            .Find(fun c -> c.CardNumber = cardnumber)
            .FirstOrDefaultAsync()

    let getCardAsync : GetCardAsync =
        fun mongoDb cardNumber ->
            let cardQuery = getCardQuery mongoDb
            let accInfoQuery = getAccountInfoQuery mongoDb
            async {
                let! card = runSingleQuery cardQuery cardNumber
                let! accInfo = runSingleQuery accInfoQuery cardNumber
                return
                    match card, accInfo with
                    | Some card, Some accInfo -> Some (card, accInfo)
                    | _ -> None
            }

    let private getUserCall (mongoDb: MongoDb) userId =
        mongoDb.GetCollection<UserEntity>(userCollection)
            .Find(fun u -> u.UserId = userId)
            .FirstOrDefaultAsync()

    let getUserInfoAsync : GetUserAsync =
        fun mongoDb userId ->
            let query = getUserCall mongoDb
            runSingleQuery query userId

    let private getUserCards (mongoDb: MongoDb) userId =
        mongoDb.GetCollection<CardEntity>(cardCollection)
            .Find(fun c -> c.UserId = userId)
            .ToListAsync()

    let private getUserAccountInfos (mongoDb: MongoDb) (cardNumbers: #seq<_>) =
        mongoDb.GetCollection<CardAccountInfoEntity>(cardAccountInfoCollection)
            .Find(fun a -> cardNumbers.Contains a.CardNumber)
            .ToListAsync()

    let getUserCardsAsync : GetUserCardsAsync =
        fun mongoDb userId ->
            let cardsCall = getUserCards mongoDb >> Async.AwaitTask
            let getUserAccountInfosCall = getUserAccountInfos mongoDb >> Async.AwaitTask
            async {
                let! cards = cardsCall userId
                let! accountInfos = Seq.map (fun (c: CardEntity) -> c.CardNumber) cards |> getUserAccountInfosCall
                let accountInfos = accountInfos.ToDictionary(fun a -> a.CardNumber)
                return [
                    for card in cards do
                        yield (card, accountInfos.[card.CardNumber])
                ]
            }


namespace CardManagement.Data

module QueryRepository =
    open System.Linq
    open CardDomainEntities
    open MongoDB.Driver
    open CardMongoConfiguration
    open System

    type IoQueryResult<'a> = Async<'a option>

    type GetCardAsync = MongoDb -> CardNumberString -> IoQueryResult<(CardEntity * CardAccountInfoEntity)>
    type GetUserAsync = MongoDb -> UserId -> IoQueryResult<UserEntity>
    type GetUserCardsAsync = MongoDb -> UserId -> Async<(CardEntity * CardAccountInfoEntity) list>
    type GetBalanceOperationsAsync = MongoDb -> (CardNumberString * DateTimeOffset * DateTimeOffset) -> Async<BalanceOperationEntity list>

    let private runSingleQuery dbQuery id =
        async {
            let! result = dbQuery id |> Async.AwaitTask
            return unsafeNullToOption result
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

    let private getUserQuery (mongoDb: MongoDb) userId =
        mongoDb.GetCollection<UserEntity>(userCollection)
            .Find(fun u -> u.UserId = userId)
            .FirstOrDefaultAsync()

    let getUserInfoAsync : GetUserAsync =
        fun mongoDb userId ->
            let query = getUserQuery mongoDb
            runSingleQuery query userId

    let private getUserCardsQuery (mongoDb: MongoDb) userId =
        mongoDb.GetCollection<CardEntity>(cardCollection)
            .Find(fun c -> c.UserId = userId)
            .ToListAsync()

    let private getUserAccountInfosQuery (mongoDb: MongoDb) (cardNumbers: #seq<_>) =
        mongoDb.GetCollection<CardAccountInfoEntity>(cardAccountInfoCollection)
            .Find(fun a -> cardNumbers.Contains a.CardNumber)
            .ToListAsync()

    let getUserCardsAsync : GetUserCardsAsync =
        fun mongoDb userId ->
            let cardsCall = getUserCardsQuery mongoDb >> Async.AwaitTask
            let getUserAccountInfosCall = getUserAccountInfosQuery mongoDb >> Async.AwaitTask
            async {
                let! cards = cardsCall userId
                let! accountInfos = Seq.map (fun (c: CardEntity) -> c.CardNumber) cards |> getUserAccountInfosCall
                // I didn't manage to make `Join` work, so here's some ugly hack
                let accountInfos = accountInfos.ToDictionary(fun a -> a.CardNumber)
                return [
                    for card in cards do
                        yield (card, accountInfos.[card.CardNumber])
                ]
            }

    let private getBalanceOperationsQuery (mongoDb: MongoDb) (cardNumber, fromDate, toDate) =
        mongoDb.GetCollection<BalanceOperationEntity>(balanceOperationCollection)
            .Find(fun bo -> bo.Id.CardNumber = cardNumber && bo.Id.Timestamp >= fromDate && bo.Id.Timestamp < toDate)
            .ToListAsync()

    let getBalanceOperationsAsync : GetBalanceOperationsAsync =
        fun mongoDb (cardNumber, fromDate, toDate) ->
            let operationsCall = getBalanceOperationsQuery mongoDb >> Async.AwaitTask
            async {
                let! result = operationsCall (cardNumber, fromDate, toDate)
                return result |> List.ofSeq
            }

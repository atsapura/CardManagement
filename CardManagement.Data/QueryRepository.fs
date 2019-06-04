namespace CardManagement.Data

module QueryRepository =
    open System.Linq
    open CardManagement
    open CardDomain
    open Common.Errors
    open CardDomainEntities
    open MongoDB.Driver
    open MongoDB.Driver.Linq
    open CardMongoConfiguration

    type IoQueryResult<'a> = Async<'a option>

    type GetCardAsync = MongoDb -> CardNumber -> IoQueryResult<(CardEntity * CardAccountInfoEntity)>
    type GetUserAsync = MongoDb -> UserId -> IoQueryResult<UserEntity>
    type GetUserCardsAsync = MongoDb -> UserId -> Async<(CardEntity * CardAccountInfoEntity) list>

    let private runSingleQuery dbQuery id =
        async {
            let! result = dbQuery id |> Async.AwaitTask
            return unsafeNullToOption result
        }

    let private runListQuery (dbQuery: 'a -> IMongoQueryable<_>) arg =
        async {
            let! result = (dbQuery arg).ToListAsync() |> Async.AwaitTask
            return result |> List.ofSeq
        }

    let private getCardQuery (mongoDb: MongoDb) cardnumber =
        query {
            for cardEntity in mongoDb.GetCollection<CardEntity>(cardCollection).AsQueryable() do
                join accountInfo in mongoDb.GetCollection<CardAccountInfoEntity>(cardAccountInfoCollection).AsQueryable()
                    on (cardEntity.CardNumber = accountInfo.CardNumber)
                where (cardEntity.CardNumber = cardnumber)
                select (cardEntity, accountInfo)
        } :?> IMongoQueryable<CardEntity * CardAccountInfoEntity>

    let private getCardCall mongoDb cardNumber =
        let query = getCardQuery mongoDb cardNumber
        query.FirstOrDefaultAsync()

    let getCardAsync : GetCardAsync =
        fun mongoDb cardNumber ->
            let query = getCardCall mongoDb
            runSingleQuery query cardNumber

    let private getUserCall (mongoDb: MongoDb) userId =
        mongoDb.GetCollection<UserEntity>(userCollection)
            .Find(fun u -> u.UserId = userId)
            .FirstOrDefaultAsync()

    let getUserInfoAsync : GetUserAsync =
        fun mongoDb userId ->
            let query = getUserCall mongoDb
            runSingleQuery query userId

    let private getUserCardsQuery (mongoDb: MongoDb) userId =
        query {
            for cardEntity in mongoDb.GetCollection<CardEntity>(cardCollection).AsQueryable() do
                join accountInfo in mongoDb.GetCollection<CardAccountInfoEntity>(cardAccountInfoCollection).AsQueryable()
                    on (cardEntity.CardNumber = accountInfo.CardNumber)
                where (cardEntity.UserId = userId)
                select (cardEntity, accountInfo)
        } :?> IMongoQueryable<(CardEntity * CardAccountInfoEntity)>

    let getUserCardsAsync : GetUserCardsAsync =
        fun mongoDb userId ->
            let query = getUserCardsQuery mongoDb
            runListQuery query userId


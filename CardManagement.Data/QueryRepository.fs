namespace CardManagement.Data

module QueryRepository =
    open System.Linq
    open CardManagement
    open CardDomain
    open Common.Errors
    open CardDomainEntities
    open MongoDB.Driver
    open MongoDB.Driver.Linq

    type MongoDb = IMongoDatabase

    let [<Literal>] private card = "Card"
    let [<Literal>] private user = "User"
    let [<Literal>] private cardAccountInfo = "cardAccountInfo"

    type CardNumber = string

    type GetCardAsync = MongoDb -> CardNumber -> IoResult<(CardEntity * CardAccountInfoEntity)>
    type GetUserAsync = MongoDb -> UserId -> IoResult<UserEntity>

    let private wrapQuery dbQuery entityName id =
        async {
            let! result = dbQuery id |> Async.AwaitTask
            return
                if result |> isNullUnsafe then id.ToString() |> notFound entityName
                else Ok result
        }

    let private getCardQuery (mongoDb: MongoDb) cardnumber =
        query {
            for cardEntity in mongoDb.GetCollection<CardEntity>(card).AsQueryable() do
                join accountInfo in mongoDb.GetCollection<CardAccountInfoEntity>(cardAccountInfo).AsQueryable()
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
            wrapQuery query card cardNumber

    let private getUserCall (mongoDb: MongoDb) userId =
        mongoDb.GetCollection<UserEntity>(user)
            .Find(fun u -> u.UserId = userId)
            .FirstOrDefaultAsync()

    let getUserAsync : GetUserAsync =
        fun mongoDb userId ->
            let query = getUserCall mongoDb
            wrapQuery query user userId


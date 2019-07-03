namespace CardManagement.Data
(*
This is a composition root for data access layer. It combines model mapping and DB interaction,
So it provides nice API for business logic layer: now they you don't have to do mapping in there,
BL layer doesn't even know about entities existence, there's even no reference to DAL project from BL.
And since we are dealing with functions, you don't even have to create interfaces to decouple this layers:
every function has it's signature as an interface.
*)
module CardDataPipeline =
    open CardManagement.Common.Errors
    open CardManagement.CardDomain
    open CardManagement.Data.CardMongoConfiguration
    open FsToolkit.ErrorHandling
    open CardManagement.Common
    open System

    type CreateCardAsync = Card*AccountInfo -> IoResult<unit>
    type CreateUserAsync = UserInfo -> IoResult<unit>
    type ReplaceCardAsync = Card -> IoResult<unit>
    type ReplaceUserAsync = UserInfo -> IoResult<unit>
    type GetUserInfoAsync = UserId -> IoQueryResult<UserInfo>
    type GetUserWithCardsAsync = UserId -> IoQueryResult<User>
    type GetCardAsync = CardNumber -> IoQueryResult<Card>
    type GetCardWithAccinfoAsync = CardNumber -> IoQueryResult<(Card*AccountInfo)>
    type GetBalanceOperationsAsync = CardNumber * DateTimeOffset * DateTimeOffset -> Async<BalanceOperation list>
    type CreateBalanceOperationAsync = BalanceOperation -> IoResult<unit>

    let createCardAsync (mongoDb: MongoDb) : CreateCardAsync =
        fun (card, accountInfo) ->
        let cardEntity, _ = card |> DomainToEntityMapping.mapCardToEntity
        let accountInfoEntity = (card.CardNumber, accountInfo) |> DomainToEntityMapping.mapAccountInfoToEntity
        (cardEntity, accountInfoEntity) |> CommandRepository.createCardAsync mongoDb

    let createUserAsync (mongoDb: MongoDb) : CreateUserAsync =
        fun user ->
        user |> DomainToEntityMapping.mapUserToEntity
        |> CommandRepository.createUserAsync mongoDb

    let replaceCardAsync (mongoDb: MongoDb) : ReplaceCardAsync =
        fun card ->
        let cardEntity, maybeAccInfo = card |> DomainToEntityMapping.mapCardToEntity
        asyncResult {
            do! cardEntity |> CommandRepository.replaceCardAsync mongoDb
            match maybeAccInfo with
            | None -> return ()
            | Some accInfo -> return! accInfo |> CommandRepository.replaceCardAccountInfoAsync mongoDb
        }

    let replaceUserAsync (mongoDb: MongoDb) : ReplaceUserAsync =
        fun user ->
        user |> DomainToEntityMapping.mapUserToEntity
        |> CommandRepository.replaceUserAsync mongoDb

    let getUserInfoAsync (mongoDb: MongoDb) : GetUserInfoAsync =
        fun userId ->
        async {
            let! userInfo = QueryRepository.getUserInfoAsync mongoDb userId
            return userInfo |> Option.map EntityToDomainMapping.mapUserInfoEntity
        }

    let getUserWithCards (mongoDb: MongoDb) : GetUserWithCardsAsync =
        fun userId ->
        async {
            let! userInfo = getUserInfoAsync mongoDb userId
            return!
                match userInfo with
                | None -> None |> async.Return
                | Some userInfo ->
                    async {
                        let! cardList = QueryRepository.getUserCardsAsync mongoDb userId
                        let cards = List.map EntityToDomainMapping.mapCardEntity cardList
                        let user =
                            { UserInfo = userInfo
                              Cards = cards }
                        return Some user
                    }
        }

    let getCardAsync (mongoDb: MongoDb) : GetCardAsync =
        fun cardNumber ->
        async {
            let! card = QueryRepository.getCardAsync mongoDb cardNumber.Value
            return card |> Option.map EntityToDomainMapping.mapCardEntity
        }

    let getCardWithAccountInfoAsync (mongoDb: MongoDb) : GetCardWithAccinfoAsync =
        fun cardNumber ->
        async {
            let! card = QueryRepository.getCardAsync mongoDb cardNumber.Value
            return card |> Option.map EntityToDomainMapping.mapCardEntityWithAccountInfo
        }

    let getBalanceOperationsAsync (mongoDb: MongoDb) : GetBalanceOperationsAsync =
        fun (cardNumber, fromDate, toDate) ->
        async {
            let! operations = QueryRepository.getBalanceOperationsAsync mongoDb (cardNumber.Value, fromDate, toDate)
            return List.map EntityToDomainMapping.mapBalanceOperationEntity operations
        }

    let createBalanceOperationAsync (mongoDb: MongoDb) : CreateBalanceOperationAsync =
        fun balanceOperation ->
        balanceOperation |> DomainToEntityMapping.mapBalanceOperationToEntity
        |> CommandRepository.createBalanceOperationAsync mongoDb

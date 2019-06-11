namespace CardManagement.Data

module CardDataPipeline =
    open CardManagement.Common.Errors
    open CardManagement.CardDomain
    open CardManagement.Data.CardMongoConfiguration
    open FsToolkit.ErrorHandling
    open CardManagement.Common

    type CreateCardAsync = Card*AccountInfo -> IoResult<unit>
    type CreateUserAsync = UserInfo -> IoResult<unit>
    type ReplaceCardAsync = Card -> IoResult<unit>
    type ReplaceUserAsync = UserInfo -> IoResult<unit>
    type GetUserInfoAsync = UserId -> IoResult<UserInfo option>
    type GetUserWithCardsAsync = UserId -> IoResult<User option>
    type GetCardAsync = CardNumber -> IoResult<Card option>
    type GetCardWithAccinfoAsync = CardNumber -> IoResult<(Card*AccountInfo) option>

    let createCardAsync (mongoDb: MongoDb) : CreateCardAsync =
        fun (card, accountInfo) ->
        let cardEntity, _ = card |> DomainToEntityMapping.mapCardToEntity
        let accountInfoEntity = (card.Number, accountInfo) |> DomainToEntityMapping.mapAccountInfoToEntity
        (cardEntity, accountInfoEntity) |> CommandRepository.createCardAsync mongoDb

    let createUserAsync (mongoDb: MongoDb) : CreateUserAsync =
        fun user ->
        user |> DomainToEntityMapping.mapUserToEntity
        |> CommandRepository.createUserAsync mongoDb

    let replaceCardAsync (mongoDb: MongoDb) : ReplaceCardAsync =
        fun card ->
        let cardEntity, _ = card |> DomainToEntityMapping.mapCardToEntity
        cardEntity |> CommandRepository.replaceCardAsync mongoDb

    let replaceUserAsync (mongoDb: MongoDb) : ReplaceUserAsync =
        fun user ->
        user |> DomainToEntityMapping.mapUserToEntity
        |> CommandRepository.replaceUserAsync mongoDb

    let getUserInfoAsync (mongoDb: MongoDb) : GetUserInfoAsync =
        fun userId ->
        async {
            let! userInfo = QueryRepository.getUserInfoAsync mongoDb userId
            return
                match userInfo with
                | None -> Ok None
                | Some userInfo ->
                    EntityToDomainMapping.mapUserInfoEntity userInfo
                    |> Result.map Some
                    |> Result.mapError InvalidDbData
        }

    let getUserWithCards (mongoDb: MongoDb) : GetUserWithCardsAsync =
        fun userId ->
        async {
            let! userInfo = getUserInfoAsync mongoDb userId
            return!
                match userInfo with
                | Ok None -> Ok None |> async.Return
                | Error e -> Error e |> async.Return
                | Ok (Some userInfo) ->
                    async {
                        let! cardList = QueryRepository.getUserCardsAsync mongoDb userId
                        let user =
                            result {
                                let! cards =
                                    List.map EntityToDomainMapping.mapCardEntity cardList
                                    |> Result.combine
                                return
                                    { UserInfo = userInfo
                                      Cards = cards |> Set.ofList }
                                    |> Some
                            } |> Result.mapError InvalidDbData
                        return user
                    }
        }

    let getCardAsync (mongoDb: MongoDb) : GetCardAsync =
        fun cardNumber ->
        async {
            let! card = QueryRepository.getCardAsync mongoDb cardNumber.Value
            return
                match card with
                | None -> Ok None
                | Some (card, accountInfo) ->
                    (card, accountInfo) |> EntityToDomainMapping.mapCardEntity
                    |> Result.map Some
                    |> Result.mapError InvalidDbData
        }

    let getCardWithAccountInfoAsync (mongoDb: MongoDb) : GetCardWithAccinfoAsync =
        fun cardNumber ->
        async {
            let! card = QueryRepository.getCardAsync mongoDb cardNumber.Value
            return
                match card with
                | None -> Ok None
                | Some (card, accountInfo) ->
                    (card, accountInfo) |> EntityToDomainMapping.mapCardEntityWithAccountInfo
                    |> Result.map Some
                    |> Result.mapError InvalidDbData
        }

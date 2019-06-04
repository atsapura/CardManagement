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
                        let! cardList =
                            QueryRepository.getUserCardsAsync mongoDb userId
                        let user = 
                            result {
                                let! cards =
                                    List.map (EntityToDomainMapping.mapCardEntity >> Result.mapError InvalidDbData) cardList
                                    |> Result.combine
                                return
                                    { UserInfo = userInfo
                                      Cards = cards |> Set.ofList }
                                    |> Some
                            }
                        return user
                    }
        }
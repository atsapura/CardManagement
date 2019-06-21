namespace CardManagement.Api
open Newtonsoft.Json
open Microsoft.FSharp.Reflection
open System
open System.Collections.Concurrent

[<AutoOpen>]
module CustomConverters =

    let private unionCaseCache = ConcurrentDictionary<Type, UnionCaseInfo[]>()

    let getUnionCasesFromCache typ =
        match unionCaseCache.TryGetValue typ with
        | (true, cases) -> cases
        | _ ->
            let cases = FSharpType.GetUnionCases typ
            unionCaseCache.TryAdd(typ, cases) |> ignore
            cases

    type OptionConverter() =
        inherit JsonConverter()
        override x.CanConvert(typ) = typ.IsGenericType && typ.GetGenericTypeDefinition() = typedefof<option<_>>

        override x.WriteJson(writer, value, serializer) =
            let value = 
                if isNull value then null
                else 
                    let _,fields = FSharpValue.GetUnionFields(value, value.GetType())
                    fields.[0]  
            serializer.Serialize(writer, value)

        override x.ReadJson(reader, typ, existingValue, serializer) =
            let innerType = 
                let innerType = typ.GetGenericArguments().[0]
                if innerType.IsValueType then typedefof<Nullable<_>>.MakeGenericType([|innerType|])
                else innerType        
        
            let cases = getUnionCasesFromCache typ
            if reader.TokenType = JsonToken.Null then FSharpValue.MakeUnion(cases.[0], Array.empty)
            else
                let value = serializer.Deserialize(reader, innerType)
                if isNull value then FSharpValue.MakeUnion(cases.[0], Array.empty)
                else FSharpValue.MakeUnion(cases.[1], [|value|])

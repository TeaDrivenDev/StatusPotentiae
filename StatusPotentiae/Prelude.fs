namespace TeaDriven

[<AutoOpen>]
module Prelude =
    open System

    let asFst second first = first, second
    let asSnd first second = first, second

    let swap (a, b) = b, a

    type FullJoinResult<'TLeft, 'TRight> =
        | LeftOnly of 'TLeft
        | RightOnly of 'TRight
        | JoinMatch of 'TLeft * 'TRight

    let private ofObj (o : 'T) =
        match box o with
        | null -> None
        | value -> Some (value :?> 'T)
    
    [<RequireQualifiedAccess>]
    module Option =
        let orValue alternativeValue opt = defaultArg opt alternativeValue
        let toNullable opt =
            match opt with
            | Some value -> Nullable value
            | None -> Nullable()

    [<RequireQualifiedAccess>]
    module Seq =
        open System.Linq
        open System.Collections

        let ofType<'T> (sequence : IEnumerable) = sequence.OfType<'T>()

        let groupByFst sequence =
            sequence |> Seq.groupBy fst |> Seq.map (fun (first, tuples) -> first, tuples |> Seq.map snd)

        let join innerKeySelector outerKeySelector (inner : 'TInner seq) (outer : 'TOuter seq) =
            query {
                for o in outer do
                join i in inner on
                    (outerKeySelector o = innerKeySelector i)
                select (o, i)
            }

        let leftJoin innerKeySelector outerKeySelector (inner : seq<'TInner>) (outer : seq<'TOuter>) =
            query {
                for o in outer do
                leftOuterJoin i in inner on
                    (outerKeySelector o = innerKeySelector i) into result
                for joined in result.DefaultIfEmpty() do
                select (o, joined |> ofObj)
            }

        let fullOuterJoin innerKeySelector outerKeySelector (right : 'TRight seq) (left : 'TLeft seq) =
            let optionizeFirst (a, b) = Some a, b

            let valueInOuter =
                leftJoin innerKeySelector outerKeySelector right left
                |> Seq.map optionizeFirst

            let valueInInnerOnly =
                leftJoin outerKeySelector innerKeySelector left right
                |> Seq.filter (snd >> Option.isNone)
                |> Seq.map (optionizeFirst >> swap)

            Seq.append valueInOuter valueInInnerOnly
            |> Seq.map (function
                | Some leftItem, Some rightItem -> JoinMatch (leftItem, rightItem)
                | Some leftItem, None -> LeftOnly leftItem
                | None, Some rightItem -> RightOnly rightItem
                | None, None -> failwith "This can never happen.")

        let groupJoin innerKeySelector outerKeySelector (inner : 'TInner seq) (outer : 'TOuter seq) =
            outer
                .GroupJoin(inner,
                           Func<_, 'TKey> outerKeySelector,
                           Func<_, _> innerKeySelector,
                           Func<_, _, _> (fun outer innerGroup -> outer, innerGroup)) :> _ seq

        let toLookup (keySelector : 'TSource -> 'TLookup) (sequence : 'TSource seq) =
            sequence.ToLookup(Func<_, _> keySelector)

        let toLookupWithSelector (keySelector : 'TSource -> 'TLookup) (elementSelector : 'TSource -> 'TElement) (sequence : 'TSource seq) =
            sequence.ToLookup(Func<_, _> keySelector, Func<_, _> elementSelector)

    [<RequireQualifiedAccess>]
    module Dictionary =
        open System.Collections.Generic

        let tryGetValue key (dictionary : IDictionary<'TKey, 'TValue>) =
            match dictionary.TryGetValue key with
            | true, value -> Some value
            | false, _ -> None

    [<RequireQualifiedAccess>]
    module String =
        let split separator (s : string) = s.Split separator
        let trim (s : string) = s.Trim()
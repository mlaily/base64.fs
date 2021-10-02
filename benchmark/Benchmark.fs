open System
open System.Text
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open System.Reflection
open BenchmarkDotNet.Configs

module Base64_Mine =
    /// Decompose a number in numerals according to the desired base (radix).
    let splitNumerals radix (x: int) =
        let rec splitNumerals' =
            function
            | head :: tail ->
                if head < radix then head :: tail
                else head % radix :: (splitNumerals' (head / radix :: tail))
            | _ -> failwith "Don't call this function with an empty list!"
        splitNumerals' [x] |> List.rev

    /// Returns the exponentiation result of a list of positional numerals in the desired base (radix).
    let unsplitNumerals radix (xs : int list) =
        (0, xs |> List.rev |> List.indexed)
        ||> List.fold (fun state (i, x) -> state + x * (pown radix i))

    /// Using the provided charset, returns a map of char -> index in the charset.
    let getCharsetIndexMap =
        Array.indexed
        >> Array.map (fun (x, y) -> y, x)
        >> Map.ofArray

    type Pad = | Left | Right
    let padList padType targetSize padValue (list: 'a list) =
        if list.Length >= targetSize then list
        else
            let padding = List.init (targetSize - list.Length) (fun _ -> padValue)
            match padType with
            | Left -> padding @ list
            | Right -> list @ padding

    // Standard base64 charset:
    let base64Charset =
          [ 'A' .. 'Z' ]
        @ [ 'a' .. 'z' ]
        @ [ '0' .. '9' ]
        @ [ '+'; '/' ]
        |> Array.ofList
    let base64CharsetIndices = getCharsetIndexMap base64Charset

    /// Encodes an array of bytes to a base64 string.
    ///  (Note we don't bother adding padding chars)
    let base64Encode (bytes: byte array) =
        bytes
        |> List.ofArray
        |> List.collect ( // Make an uninterrupted string of bits from the input bytes:
            int // Cast bytes to ints
            >> splitNumerals 2 // Convert input bytes to bits (binary)
            >> padList Left 8 0) // Pad with leading zeros so we have 8 bits for all input octets
        |> List.chunkBySize 6 // Split into sextets
        |> List.map (
            padList Right 6 0 // Pad incomplete sextets with trailing zeros
            >> unsplitNumerals 2 // Convert binary back to ints
            >> (Array.get base64Charset)) // Map ints to chars
        |> System.String.Concat

    /// Decodes a base64 string into an array of bytes.
    let base64Decode (str: string) =
        str.TrimEnd('=').ToCharArray() // Remove trailing padding and split chars
        |> List.ofArray
        |> List.map (fun char -> base64CharsetIndices |> Map.find char) // Map chars back to ints
        |> List.collect ( // Recreate the uninterrupted bit string:
            splitNumerals 2 // Convert ints to binary
            >> padList Left 6 0) // Pad with leading zeros so we have 6 bits for all input sextets
        |> List.chunkBySize 8 // Regroup octets
        |> List.map (
            unsplitNumerals 2 // Convert binary back to ints
            >> byte) // Cast ints to bytes
        |> Array.ofList

module Base64_FsSnip =
    // http://www.fssnip.net/7PR/title/Base64-Base32-Base16-encoding-and-decoding

    /// RFC 4648: The Base 64 Alphabet
    let baseCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/="
    /// RFC 4648: The "URL and Filename safe" Base 64 Alphabet
    let safeCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_="

    let encodeBase64 bytes =
        let tripletToList ending (x, y, z) =
          let A = safeCharset

          let triplet = (int x <<< 16)
                    ||| (int y <<<  8)
                    ||| (int z)
          let a = (triplet &&& 0xFC0000) >>> 18
          let b = (triplet &&& 0x03F000) >>> 12
          let c = (triplet &&& 0x000FC0) >>>  6
          let d = (triplet &&& 0x00003F)
          match ending with
          | 1 -> [A.[a]; A.[b];  '=' ;  '=' ;] // 01==
          | 2 -> [A.[a]; A.[b]; A.[c];  '=' ;] // 
          | _ -> [A.[a]; A.[b]; A.[c]; A.[d];] // 

        let rec parse result input =
          match input with
          | a :: b :: c :: tail -> parse (result @ tripletToList 3 (a, b, c)) tail
          | a :: [b]            -> result @ tripletToList 2 (a,   b, 0uy)
          | [a]                 -> result @ tripletToList 1 (a, 0uy, 0uy)
          | []                  -> result
      
        bytes
        |> Array.toList
        |> parse []
        |> List.toArray
        |> System.String.Concat

    let decodeBase64 (text: string) = 
        if text.Length % 4 <> 0 then [||] else
          let A = [for c in safeCharset -> c]
                  |> List.mapi (fun i a -> a, i)
                  |> Map.ofList

          let (.@) (m: Map<char, int>) key = try m.[key] with _ -> 0
        
          let quadToList ending (a, b, c, d) =
            let quad = (A.@ a &&& 0x3F <<< 18)
                   ||| (A.@ b &&& 0x3F <<< 12)
                   ||| (A.@ c &&& 0x3F <<<  6)
                   ||| (A.@ d &&& 0x3F)
            let x = (quad &&& 0xFF0000) >>> 16
            let y = (quad &&& 0x00FF00) >>>  8
            let z = (quad &&& 0x0000FF)
            match ending with
            | 2 -> [byte x;]
            | 3 -> [byte x; byte y;]
            | _ -> [byte x; byte y; byte z;]
        
          let rec parse result input =
            match input with
            | a :: b ::'='::['=']      -> result @ quadToList 2 (a, b, '=', '=')
            | a :: b :: c ::['=']      -> result @ quadToList 3 (a, b,  c , '=')
            | a :: b :: c :: d :: tail -> parse (result @ quadToList 4 (a, b, c, d)) tail
            | _                        -> result

          [for c in text -> c]
          |> parse []
          |> List.toArray

[<MemoryDiagnoser>]
[<BenchmarkCategory("Encode")>]
type Base64Encode() =

    let bytes = Encoding.UTF8.GetBytes("Many hands make light work.")

    [<Benchmark(Baseline=true)>]
    member _.BCL () = Convert.ToBase64String(bytes)

    [<Benchmark>]
    [<BenchmarkDotNet.Attributes.BenchmarkCategory>]
    member _.Mine () = Base64_Mine.base64Encode bytes

    [<Benchmark>]
    member _.FsSnip () = Base64_FsSnip.encodeBase64 bytes

[<MemoryDiagnoser>]
[<BenchmarkCategory("Decode")>]
type Base64Decode () =

    let base64String = "TWFueSBoYW5kcyBtYWtlIGxpZ2h0IHdvcmsu"

    [<Benchmark(Baseline=true)>]
    member _.BCL () = Convert.FromBase64String(base64String)

    [<Benchmark>]
    member _.Mine () = Base64_Mine.base64Decode base64String

    [<Benchmark>]
    member _.FsSnip () = Base64_FsSnip.decodeBase64 base64String

[<EntryPoint>]
let Main args =
    let summary =
        BenchmarkSwitcher
            .FromAssembly(Assembly.GetExecutingAssembly())
            .RunAllJoined(DefaultConfig.Instance.AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory))
    0
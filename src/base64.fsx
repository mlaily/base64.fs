open System.Text

/// Decompose a number in numerals according to the desired base (radix).
let splitNumerals radix (x: int) =
    let rec splitNumerals' =
        function
        | head :: tail ->
            if head < radix then head :: tail
            else head % radix :: (splitNumerals' (head / radix :: tail))
        | _ -> failwith "Don't call this function with an empty list!"
    splitNumerals' [ x ] |> List.rev

/// Returns the exponentiation result of a list of positional numerals
/// in the desired base (radix).
let unsplitNumerals radix (xs: int list) =
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
    @ [ '+'; '/'   ]
    |> Array.ofList
let base64CharsetIndices = getCharsetIndexMap base64Charset

/// Encodes an array of bytes to a base64 string.
/// (Note we don't bother adding padding chars)
let base64Encode (bytes: byte array) =
    bytes
    |> List.ofArray
    |> List.collect ( // Make an uninterrupted string of bits from the input bytes:
        int // Cast bytes to ints
        >> splitNumerals 2 // Convert input bytes to bits (binary)
        >> padList Left 8 0) // Pad with zeros so we have 8 bits for all input octets
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
    |> List.map (fun char ->
        base64CharsetIndices
        |> Map.find char) // Map chars back to ints
    |> List.collect ( // Recreate the uninterrupted bit string:
        splitNumerals 2 // Convert ints to binary
        >> padList Left 6 0) // Pad with zeros so we have 6 bits for all input sextets
    |> List.chunkBySize 8 // Regroup octets
    |> List.map (
        unsplitNumerals 2 // Convert binary back to ints
        >> byte) // Cast ints to bytes
    |> Array.ofList

System.Convert.ToBase64String(Encoding.UTF8.GetBytes("Many hands make light work."))
|> printf "%A"

base64Encode (Encoding.UTF8.GetBytes("Many hands make light work."))
|> printf "%A"

base64Decode "TWFueSBoYW5kcyBtYWtlIGxpZ2h0IHdvcmsu"
|> Encoding.UTF8.GetString
|> printfn "%A"

System.Convert.FromBase64String("TWFueSBoYW5kcyBtYWtlIGxpZ2h0IHdvcmsu")
|> Encoding.UTF8.GetString
|> printf "%A"

#r "nuget: FSharp.Formatting"

open System.IO
open FSharp.Formatting.Literate
open FSharp.Formatting.Literate.Evaluation

let snippet = """
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
    |> Array.ofList"""

let srcDir = Path.Combine(__SOURCE_DIRECTORY__, "src")
let fsxContent = File.ReadAllText(Path.Combine(srcDir, "base64.fsx"))

let fsi = FsiEvaluator()
let docOl = Literate.ParseScriptString(snippet, fsiEvaluator = fsi)
printfn ""
printf "%s" (Literate.ToHtml(docOl))
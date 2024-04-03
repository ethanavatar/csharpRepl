open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Reflection
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.Emit

type Code =
    struct
        new (usings: string list, body: string list) = { usings = usings; body = body }
        val usings: string list
        val body: string list
    end

type Output =
    | Success of string
    | Error of string

let mainFunction (body: string list) =
    [ "public static void Main() {"
      body |> String.concat "\n"
      "}" ] |> String.concat "\n"

let using (names: string list) =
    let f s = sprintf "using %s;" s in
    names |> List.map f |> String.concat "\n"

let programClass (mainFunction: string) =
    [ "public class Program {"
      mainFunction
      "}" ] |> String.concat "\n"

let source (code: Code) =
    let mainFunction = mainFunction code.body in
    (using code.usings) + "\n" + (programClass mainFunction)

let references =
    let getReference (a: Assembly): MetadataReference =
        MetadataReference.CreateFromFile(a.Location)
            |> (fun x -> x :> MetadataReference) in

    let references: MetadataReference seq =
        AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.map getReference in

    let references = Seq.append references [
        getReference typedefof<System.Console>.Assembly ] in

    references

let resetConsoleOut () =
    let consoleOut = new StreamWriter(Console.OpenStandardOutput()) in
    consoleOut.AutoFlush <- true
    Console.SetOut(consoleOut) |> ignore

let compile (code: string): Output =
    let syntaxTree: SyntaxTree = CSharpSyntaxTree.ParseText(code) in
    let assemblyName = Path.GetRandomFileName() in

    let compilation = CSharpCompilation.Create(
        assemblyName,
        syntaxTrees = [| syntaxTree |],
        references = references,
        options = new CSharpCompilationOptions(OutputKind.ConsoleApplication)) in

    let stream = new MemoryStream() in
    let emitResult = compilation.Emit(stream) in
    if emitResult.Success then
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        let assembly = Assembly.Load(stream.ToArray()) in
        let programType = assembly.GetType("Program") in
        let mainMethod = programType.GetMethod("Main") in

        let capturedOutput = new StringWriter() in

        Console.SetOut(capturedOutput) |> ignore;
        mainMethod.Invoke(null, null) |> ignore;
        resetConsoleOut () |> ignore;

        capturedOutput.ToString() |> Success
    else
        emitResult.Diagnostics
            |> Seq.map (fun e -> e.ToString() |> sprintf "[Compilation Error] %s")
            |> String.concat "\n"
            |> Error

let repl =
    let rec loop (code: Code, lastResultLines: int) =
        printf "C# >> ";
        let statement = Console.ReadLine() in
        if statement = "exit" then ()
        else
            let new_code = Code(code.usings, code.body @ [ statement ]) in
            let source_code = source new_code in
            let result = compile source_code in
            match result with
            | Success(output) ->
                let output = output.Replace("\r\n", "\n") in
                let lines = output.Split([| '\n' |], StringSplitOptions.RemoveEmptyEntries) in
                if lines.Length > 0 then
                    for i in lastResultLines..(lines.Length - 1) do
                        printfn "%s" lines.[i] |> ignore
                    loop (new_code, lines.Length)
                else
                    loop (new_code, lastResultLines)
            | Error(error) ->
                printfn "%s" error |> ignore
                loop (code, lastResultLines)
    let initial_code = Code([ "System" ], [ ]) in
    loop (initial_code, 0)

let main =
    repl
    0

main

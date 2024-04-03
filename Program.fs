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
        new (usings: string list, body: string) = { usings = usings; body = body }
        val usings: string list
        val body: string
    end

let mainFunction (body: string) =
    [ "public static void Main() {"
      body
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

let compile (code: string) =
    let syntaxTree: SyntaxTree = CSharpSyntaxTree.ParseText(code) in
    let assemblyName = Path.GetRandomFileName() in

    let getReference (a: Assembly): MetadataReference =
        MetadataReference.CreateFromFile(a.Location)
            |> (fun x -> x :> MetadataReference) in

    let references: MetadataReference seq =
        AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.map getReference in

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
        mainMethod.Invoke(null, null) |> ignore
    else
        let log (d: Diagnostic) =
            printfn "[Compilation Error] %A" d
        emitResult.Diagnostics |> Seq.iter log
    ()

let repl =
    let rec loop () =
        printf "C# >> "
        let code = Console.ReadLine()
        if code = "exit" then
            ()
        else
            let code = Code([ "System" ], code)
            compile (source code)
            loop ()
    loop ()

let main =
    repl
    0

main

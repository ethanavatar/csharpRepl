# C# REPL

A super minimal C# REPL (Read-Eval-Print Loop) written in F#

It works by appending the user's input as a new line in the main function, compiling it, re-running the whole program, and truncating the lines that have already been output previously.

This is a toy program. If you want a real C# REPL, consider [waf/CSharpRepl](https://github.com/waf/CSharpRepl) or [dotnet/interactive](https://github.com/dotnet/interactive).

## Usage

```bash
$ dotnet run --configuration Release
C# >> Console.WriteLine("Hello, Sailor!");
Hello, Sailor!
C# >> var a = 34;
C# >> var b = 35;
C# >> Console.WriteLine(a + b);
69
C# >> exit
```


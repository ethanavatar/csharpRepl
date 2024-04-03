# C# REPL

A super minimal C# REPL (Read-Eval-Print Loop) written in F#

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


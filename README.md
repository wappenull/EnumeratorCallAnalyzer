# EnumeratorCallAnalyzer
A Roslyn analyzer that catch wrong Unity coroutine call. 

It will catch a coroutine call without yield return similar to:

    Coroutine( );
    
Suppose `Coroutine` is a function defined with:

    IEnumerator Coroutine( ) { ... }
    
And will attempt to change to

    yield return Coroutine( );
    
![Alt text](./checker.gif)

 - Solution file is created with VS2019
 - The solution will output VSIX for install with Visual Studio.
 - Roslyn SDK required to compile, see guide below.
 - It could serve as starting point for making more convenient analyzer to suit developer's need.

It was created with the help of 
 - [Roslyn Analyzers and how to use them with Unity][3] (This article also show more practical examples on how to use analyzer to enforce coding style)
 - [Writing a Roslyn analyzer][4]
 - [How to write a Roslyn Analyzer][5]

Repo was created by inspration from discussion of https://stackoverflow.com/questions/67820221 (Thanks everyone!)

(I always got dupe hammer in SO whenever I started a new question, this time it was not too bad, I guess)

  [3]: https://arztsamuel.github.io/en/blogs/2019/Roslyn-Analyzers-and-Unity.html
  [4]: https://www.meziantou.net/writing-a-roslyn-analyzer.htm
  [5]: https://devblogs.microsoft.com/dotnet/how-to-write-a-roslyn-analyzer/

``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1263 (21H2)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.401
  [Host]     : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT DEBUG
  DefaultJob : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT


```
|         Type | Method |         Mean |      Error |     StdDev |  Ratio | RatioSD |  Gen 0 |  Gen 1 | Allocated |
|------------- |------- |-------------:|-----------:|-----------:|-------:|--------:|-------:|-------:|----------:|
| Base64Decode |    BCL |     88.88 ns |   0.405 ns |   0.379 ns |   1.00 |    0.00 | 0.0088 |      - |      56 B |
| Base64Decode |   Mine | 12,720.92 ns | 212.608 ns | 188.471 ns | 143.17 |    2.09 | 9.2010 | 0.2289 |  57,752 B |
| Base64Decode | FsSnip | 18,965.48 ns | 106.858 ns |  99.955 ns | 213.39 |    1.48 | 4.9133 | 0.0610 |  30,936 B |
|              |        |              |            |            |        |         |        |        |           |
| Base64Encode |    BCL |     39.46 ns |   0.448 ns |   0.397 ns |   1.00 |    0.00 | 0.0153 |      - |      96 B |
| Base64Encode |   Mine | 11,415.46 ns | 130.660 ns | 122.220 ns | 289.12 |    4.28 | 9.1553 | 0.2441 |  57,512 B |
| Base64Encode | FsSnip |  1,415.43 ns |  13.940 ns |  13.040 ns |  35.85 |    0.51 | 1.0967 | 0.0038 |   6,880 B |

﻿# List.Add vs ValueList.Add

|                       Method |   Size |          Mean |        Error |        StdDev |           Min |           Max | Ratio | RatioSD |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|----------------------------- |------- |--------------:|-------------:|--------------:|--------------:|--------------:|------:|--------:|---------:|---------:|---------:|----------:|
|                         List |     10 |      78.24 ns |     1.509 ns |      1.338 ns |      75.81 ns |      80.37 ns |  1.00 |    0.00 |   0.0516 |        - |        - |     216 B |
|      ListWithInitialCapacity |     10 |      28.46 ns |     0.594 ns |      0.942 ns |      27.45 ns |      30.51 ns |  0.36 |    0.01 |   0.0229 |        - |        - |      96 B |
|   ValueListWithInitialBuffer |     10 |      38.85 ns |     0.417 ns |      0.348 ns |      38.31 ns |      39.36 ns |  0.50 |    0.01 |        - |        - |        - |         - |
| ValueListWithInitialCapacity |     10 |      55.44 ns |     1.136 ns |      1.215 ns |      53.70 ns |      58.00 ns |  0.71 |    0.02 |        - |        - |        - |         - |
|                         List |    100 |     332.32 ns |     6.540 ns |      7.532 ns |     324.07 ns |     352.31 ns |  1.00 |    0.00 |   0.2828 |        - |        - |    1184 B |
|      ListWithInitialCapacity |    100 |     170.12 ns |     3.192 ns |      2.666 ns |     166.80 ns |     175.68 ns |  0.51 |    0.01 |   0.1090 |        - |        - |     456 B |
|   ValueListWithInitialBuffer |    100 |     276.44 ns |     5.256 ns |      4.916 ns |     270.34 ns |     287.37 ns |  0.83 |    0.02 |        - |        - |        - |         - |
| ValueListWithInitialCapacity |    100 |     254.65 ns |     5.040 ns |      7.846 ns |     244.77 ns |     273.32 ns |  0.77 |    0.02 |        - |        - |        - |         - |
|                         List |   1000 |   2,147.15 ns |    48.391 ns |     47.527 ns |   2,089.86 ns |   2,267.76 ns |  1.00 |    0.00 |   2.0103 |        - |        - |    8424 B |
|      ListWithInitialCapacity |   1000 |   1,576.12 ns |    27.580 ns |     25.799 ns |   1,540.86 ns |   1,634.26 ns |  0.73 |    0.02 |   0.9689 |        - |        - |    4056 B |
|   ValueListWithInitialBuffer |   1000 |   2,864.50 ns |    56.692 ns |     71.698 ns |   2,770.19 ns |   3,036.76 ns |  1.34 |    0.03 |        - |        - |        - |         - |
| ValueListWithInitialCapacity |   1000 |   2,172.43 ns |    36.719 ns |     32.551 ns |   2,125.56 ns |   2,244.07 ns |  1.01 |    0.03 |        - |        - |        - |         - |
|                         List |  10000 |  22,859.69 ns |   515.381 ns |    482.088 ns |  22,412.01 ns |  23,835.97 ns |  1.00 |    0.00 |  31.2195 |   6.2256 |        - |  131400 B |
|      ListWithInitialCapacity |  10000 |  15,370.75 ns |   239.992 ns |    212.747 ns |  15,085.94 ns |  15,826.92 ns |  0.67 |    0.01 |   9.5215 |   1.1597 |        - |   40056 B |
|   ValueListWithInitialBuffer |  10000 |  26,312.83 ns |   530.856 ns |    545.150 ns |  25,736.71 ns |  27,681.63 ns |  1.15 |    0.03 |        - |        - |        - |         - |
| ValueListWithInitialCapacity |  10000 |  21,474.21 ns |   406.697 ns |    360.526 ns |  20,945.72 ns |  22,013.32 ns |  0.94 |    0.02 |        - |        - |        - |         - |
|                         List | 100000 | 411,521.86 ns | 7,983.492 ns | 12,662.665 ns | 394,822.66 ns | 438,239.11 ns |  1.00 |    0.00 | 285.6445 | 285.6445 | 285.6445 | 1048976 B |
|      ListWithInitialCapacity | 100000 | 270,025.57 ns | 2,313.264 ns |  1,931.680 ns | 267,871.19 ns | 274,255.03 ns |  0.67 |    0.01 | 124.5117 | 124.5117 | 124.5117 |  400057 B |
|   ValueListWithInitialBuffer | 100000 | 234,698.10 ns | 4,583.009 ns |  5,959.206 ns | 226,911.60 ns | 247,792.65 ns |  0.57 |    0.02 |        - |        - |        - |         - |
| ValueListWithInitialCapacity | 100000 | 215,531.99 ns | 4,298.353 ns |  4,414.092 ns | 210,339.01 ns | 226,442.70 ns |  0.53 |    0.02 |        - |        - |        - |         - |
using WayCoder;

var map = RepoMapGenerator.Generate(@"D:\code-agents\WayCoder", forceRefresh: true);
Console.WriteLine(map.Length > 500 ? map[..500] : map);
Console.WriteLine();
Console.WriteLine($"--- total length: {map.Length}");

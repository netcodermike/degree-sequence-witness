using System.Text.Json;

try
{
    if (args.Length != 1) throw new ArgumentException("Pass one JSON input file");
    using var data = JsonDocument.Parse(File.ReadAllText(args[0]));
    Console.WriteLine(JsonSerializer.Serialize(Analyze(data.RootElement)));
    return 0;
}
catch (Exception error) when (error is IOException or JsonException or FormatException
                               or ArgumentException or InvalidOperationException
                               or KeyNotFoundException or ArithmeticException or IndexOutOfRangeException)
{
    Console.Error.WriteLine(error.Message);
    return 2;
}

static object Analyze(JsonElement input)
{
    int[] degrees = input.GetProperty("degrees").EnumerateArray()
        .Select(value => value.GetInt32()).ToArray();
    int vertexCount = degrees.Length;
    if (degrees.Any(degree => degree < 0))
        throw new ArgumentException("Degrees must be nonnegative integers.");
    if (degrees.Any(degree => degree >= vertexCount))
        return Rejection(vertexCount, "degree_exceeds_maximum");
    if (degrees.Sum(degree => (long)degree) % 2 != 0)
        return Rejection(vertexCount, "odd_degree_sum");

    var pending = degrees.Select((degree, vertex) => (Vertex: vertex, Degree: degree))
        .Where(item => item.Degree > 0)
        .ToList();
    var edges = new List<(int U, int V)>();
    while (pending.Count > 0)
    {
        pending.Sort((left, right) =>
        {
            int byDegree = right.Degree.CompareTo(left.Degree);
            return byDegree == 0 ? left.Vertex.CompareTo(right.Vertex) : byDegree;
        });
        var current = pending[0];
        pending.RemoveAt(0);
        if (current.Degree > pending.Count)
            return Rejection(vertexCount, "insufficient_remaining_neighbors");

        for (int index = 0; index < current.Degree; index++)
        {
            var neighbor = pending[index];
            edges.Add((Math.Min(current.Vertex, neighbor.Vertex), Math.Max(current.Vertex, neighbor.Vertex)));
            pending[index] = (neighbor.Vertex, neighbor.Degree - 1);
        }
        pending.RemoveAll(item => item.Degree == 0);
    }

    return new
    {
        vertex_count = vertexCount, graphical = true, reason = (string?)null, edge_count = edges.Count,
        edges = edges.OrderBy(edge => edge.U).ThenBy(edge => edge.V)
            .Select(edge => new { u = edge.U, v = edge.V }).ToArray(),
    };
}

static object Rejection(int vertexCount, string reason) =>
    new { vertex_count = vertexCount, graphical = false, reason, edge_count = 0, edges = Array.Empty<object>() };

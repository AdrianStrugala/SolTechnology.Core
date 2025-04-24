public record Street
{
    public string Id { get; init; }          // = id(r)
    public string FromId { get; init; }      // = a.id
    public string ToId { get; init; }        // = b.id

    public string? Name { get; init; } // r.name
    public double? Length { get; init; } // r.length
    public int? Lanes { get; init; } // r.lanes
    public string? Oneway { get; init; } // r.oneway
    public string? Bridge { get; init; } // r.bridge
    public string? Tunnel { get; init; } // r.tunnel
    public string? Highway { get; init; } // r.highway
    public string? Service { get; init; } // r.service
    public string? Junction { get; init; } // r.junction
    public string? Ref { get; init; } // r.ref
    public string? Access { get; init; } // r.access
    public string? Surface { get; init; } // r.surface
    public double? Width { get; init; } // r.width
    public string? Lit { get; init; } // r.lit

    public string? TurnLanes { get; init; }
}
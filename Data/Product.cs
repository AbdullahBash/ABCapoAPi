﻿namespace ABCapoAPi.Data;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

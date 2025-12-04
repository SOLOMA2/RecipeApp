using System.ComponentModel.DataAnnotations;

public class RateRecipeRequest
{
    [Range(1, 5)]
    public int Value { get; set; }
}



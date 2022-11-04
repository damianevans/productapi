namespace Asos.CommercialIntegration.Cohort.ProductApi.Models;

public class Product
{
    public int ProductId { get; set; }
    public DateTime DateCreated { get; set; }

    public ProductDetails ProductDetails { get; set; }

}
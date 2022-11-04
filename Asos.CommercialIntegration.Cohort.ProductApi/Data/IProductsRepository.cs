using Asos.CommercialIntegration.Cohort.ProductApi.Models;

namespace Asos.CommercialIntegration.Cohort.ProductApi.Data;

public interface IProductsRepository
{
    public List<Product> AllProducts();
    public Product GetById(int id);
}
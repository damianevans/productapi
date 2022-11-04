using Asos.CommercialIntegration.Cohort.ProductApi.Data;
using Asos.CommercialIntegration.Cohort.ProductApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Asos.CommercialIntegration.Cohort.ProductApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductsController : Controller
    {
        private readonly ILogger<ProductsController> _logger;
        private readonly IProductsRepository _repository;


        public ProductsController(ILogger<ProductsController> logger, IProductsRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpGet]
        [Produces("application/json")]
        public IEnumerable<Product> AllProducts()
        {
            return _repository.AllProducts();
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public ActionResult<Product> GetById(int id)
        {
            var product = _repository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        [HttpGet("all-productIds")]
        [HttpGet("allIds")]
        [Produces("application/json")]
        public IEnumerable<int> AllProductIds()
        {
            foreach (var product in _repository.AllProducts())
            {
                yield return product.ProductId;
            }
            
        }
    }
}

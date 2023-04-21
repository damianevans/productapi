using Asos.CommercialIntegration.Cohort.ProductApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Reflection;

namespace Asos.CommercialIntegration.Cohort.ProductApi.Data;

class ProductsRepository : IProductsRepository
{
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _config;
    private const string CACHE_KEY = "allProducts";
    private const string REQUESTCOUNT_KEY = "requestCount";
    private int ERROR_THRESHOLD = 300;
    public ProductsRepository(IMemoryCache cache, IConfiguration config)
    {
        _config = config;
        _memoryCache = cache;
        ERROR_THRESHOLD = _config.GetValue<int>("ErrorThreshold");
    }

    public List<Product> AllProducts()
    {
        List<Product> allProducts;
        if (_memoryCache.TryGetValue(CACHE_KEY, out allProducts))
        {
             return allProducts;
        }

        var cacheOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);
        _memoryCache.Set(CACHE_KEY, LoadAllProducts());

        return _memoryCache.Get<List<Product>>(CACHE_KEY);
    }

    public Product GetById(int id)
    {
        var cacheOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);
        int requestCount = 0;
        _memoryCache.TryGetValue(REQUESTCOUNT_KEY, out requestCount);
        requestCount++;
        _memoryCache.Set(REQUESTCOUNT_KEY, requestCount);
        
        if (requestCount > ERROR_THRESHOLD)
        {
            requestCount = 0;
            _memoryCache.Set(REQUESTCOUNT_KEY, requestCount);
            throw new Exception("Unspecified Server Error Occurred");
        }
        
        List<Product> allProducts;
        if (_memoryCache.TryGetValue(CACHE_KEY, out allProducts))
        {
            return allProducts.Find(x => x.ProductId == id);
        }


        _memoryCache.Set(CACHE_KEY, LoadAllProducts());
        return _memoryCache.Get<List<Product>>(CACHE_KEY).Find(x => x.ProductId == id);
    }

    private IList<Product> LoadAllProducts()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Asos.CommercialIntegration.Cohort.ProductApi.Data.all-products.json";
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        using (var streamReader = new StreamReader(stream))
        using (JsonTextReader reader = new JsonTextReader(streamReader))
        {
            var serializer = new JsonSerializer();
            return serializer.Deserialize<List<Product>>(reader);
        }
    }
}
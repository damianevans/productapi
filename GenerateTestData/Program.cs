using System.IO;
using Asos.CommercialIntegration.Cohort.ProductApi.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

Random rnd = new Random();

var today = DateTime.Now;
string[] sex = new string[] { "Men's", "Women's" };
string[] colours = new string[] { "White", "Grey", "Pink", "Black", "Magenta", "Blue", "Red", "Green", "Brown", "White" };
string[] brand = new string[] { "Nike", "Asos Design", "Levis", "Dolche & Gabanna", "Louis Vuitton", "River Island", "Jack & Jones" };
string[] itemType = new string[] { "T-Shirt", "Jeans", "Cocktail Dress", "Handbag", "Skirt", "Trainers", "Polo shirt", "Shorts",
    "Tank Top", "Football Shirt", "Socks", "Crop Top", "Shirt", "Short sleeve shirt", "Skinny Suit", "Leather Biker Jacket", "Parka coat", "Puffer jacket",
    "Trench coat"
};
string[] isoCodes = new string[] { "GB", "DE", "VN", "CN", "KH", "IT", "FR", "ID" };
List<Product> allProducts = new List<Product>();

for (int i = 0; i < 59999; i++)
{
    Console.Write($"\rWriting file: {Math.Round((double)i / 59999) * 100}%");
    var dateCreated = today.AddDays((rnd.Next(0,60)) * -1);
    dateCreated = dateCreated.AddMinutes((rnd.Next(0, 300)) * -1);
    var randomColour = colours[rnd.Next(colours.Length)];
    var description = $"{brand[ rnd.Next(brand.Length) ]} {sex[rnd.Next(sex.Length)]} {randomColour} {itemType[rnd.Next(itemType.Length)]}";
    //var productId = $"674{rnd.Next(0, 59999).ToString().PadLeft(5,'0')}";
    var productId = $"674{i.ToString().PadLeft(5,'0')}";
    var randomSupplierId = $"7317{rnd.Next(0, 9999).ToString().PadLeft(4, '0')}";
    var randomCountry = isoCodes[rnd.Next(isoCodes.Length)];

    var product = new Product()
    {
        ProductId = Convert.ToInt32(productId),
        DateCreated = dateCreated,
        ProductDetails = new ProductDetails()
        {
            Colour = randomColour,
            ISOCountryCode = randomCountry,
            SupplierId = Convert.ToInt32(randomSupplierId),
            Size = $"UK{rnd.Next(1,26).ToString()}",
            ProductDescription = description
        }
    };
    allProducts.Add(product);
   
}

File.WriteAllText(@"C:\tmp\ni-cohort-all-products.json", JsonConvert.SerializeObject(allProducts, new JsonSerializerSettings()
{
    Formatting = Formatting.Indented,
    ContractResolver = new CamelCasePropertyNamesContractResolver()
}));



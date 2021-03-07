using System;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using ElasticSearch.AspNetCore.Sample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;

namespace ElasticSearch.AspNetCore.Sample.Controllers
{
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IElasticClient _elasticClient;

        public HomeController(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        [HttpGet("/search")]
        public async Task<IActionResult> Search(string query, int page = 1, int pageSize = 5)
        {
            var response = await _elasticClient.SearchAsync<Product>
            (
                s => s.Query(q => 
                        q.Match(d => 
                            d.Query(query)))
                    .From((page - 1) * pageSize)
                    .Size(pageSize));

            if (!response.IsValid)
            {
                return BadRequest();
            }

            return Ok(response.Documents);
        }

        [HttpGet("/search/clean")]
        public async Task<IActionResult> Clean()
        {
            var response = await _elasticClient.DeleteByQueryAsync<Product>(q => q.MatchAll());

            return Ok($"{response.Deleted}");
        }

        [HttpGet("/search/import")]
        public async Task<IActionResult> ImportFakeProducts(int count)
        {
            var productFaker = new Faker<Product>()
                .CustomInstantiator(f => new Product())
                .RuleFor(p => p.Ean, f => f.Commerce.Ean13())
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Description, f => f.Lorem.Sentence(f.Random.Int(5, 20)))
                .RuleFor(p => p.Brand, f => f.Company.CompanyName())
                .RuleFor(p => p.Category, f => f.Commerce.Categories(1).First())
                .RuleFor(p => p.Price, f => f.Commerce.Price(1, 1000, 2, "€"))
                .RuleFor(p => p.Quantity, f => f.Random.Int(0, 1000))
                .RuleFor(p => p.Rating, f => f.Random.Float(0, 1))
                .RuleFor(p => p.ReleaseDate, f => f.Date.Past(2));

            var fakeProducts = productFaker.Generate(count);

            var response = await _elasticClient.IndexManyAsync(fakeProducts);

            if (response.Errors)
            {
                return BadRequest(response.ItemsWithErrors);
            }

            return Ok(new
            {
                message = $"{fakeProducts.Count} fake product(s) imported"
            });
        }
    }
}
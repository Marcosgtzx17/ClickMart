using ClickMart.web.DTOs.ProductoDTOs;

namespace ClickMart.web.Services
{
    public class ProductoCatalogService
    {
        private readonly ApiService _api;
        private const string BASE = "Producto"; // api/Producto

        public ProductoCatalogService(ApiService api) => _api = api;

        // Debe devolver [{ productoId, nombre, precio }]
        public Task<List<ProductoLiteDTO>?> GetAllAsync(string token) =>
            _api.GetAsync<List<ProductoLiteDTO>>($"{BASE}", token);
    }
}

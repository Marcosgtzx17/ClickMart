using ClickMart.web.DTOs.CatalogoDTOs;

namespace ClickMart.web.Services
{
    public class CatalogoService
    {
        private readonly ApiService _api;
        public CatalogoService(ApiService api) => _api = api;

        // GET /api/categoria
        public Task<List<CategoriaDTO>?> GetCategoriasAsync(string? token = null) =>
            _api.GetAsync<List<CategoriaDTO>>("categoria", token);

        // GET /api/distribuidor
        public Task<List<DistribuidorDTO>?> GetDistribuidoresAsync(string? token = null) =>
            _api.GetAsync<List<DistribuidorDTO>>("distribuidor", token);
    }
}

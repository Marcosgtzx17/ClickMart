using ClickMart.web.DTOs.ProductoDTOs;

namespace ClickMart.web.Services
{
    public class ProductoService
    {
        private readonly ApiService _api;
        private const string Base = "producto";

        public ProductoService(ApiService api) => _api = api;

        // GET /api/producto
        public Task<List<ProductoResponseDTO>?> GetAllAsync(string? token = null) =>
            _api.GetAsync<List<ProductoResponseDTO>>($"{Base}", token);

        // GET /api/producto/{id}
        public Task<ProductoResponseDTO?> GetByIdAsync(int id, string? token = null) =>
            _api.GetAsync<ProductoResponseDTO>($"{Base}/{id}", token);

        // POST /api/producto
        public Task<ProductoResponseDTO?> CreateAsync(ProductoCreateDTO dto, string token) =>
            _api.PostAsync<ProductoCreateDTO, ProductoResponseDTO>($"{Base}", dto, token);

        // PUT /api/producto/{id}
        public Task<bool> UpdateAsync(int id, ProductoUpdateDTO dto, string token)
        {
            return _api.PutAsync<ProductoUpdateDTO, ProductoResponseDTO>($"{Base}/{id}", dto, token)
                       .ContinueWith(t => t.Exception is null);
        }

        // DELETE /api/producto/{id}
        public Task<bool> DeleteAsync(int id, string token) =>
            _api.DeleteAsync($"{Base}/{id}", token);

        // POST /api/producto/{id}/imagen  (multipart/form-data; campo: archivo)
        public async Task<bool> UploadImageAsync(int id, IFormFile archivo, string token)
        {
            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var bytes = ms.ToArray();

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            content.Add(fileContent, "archivo", archivo.FileName);

            return await _api.PostMultipartAsync($"{Base}/{id}/imagen", content, token);
        }
    }
}

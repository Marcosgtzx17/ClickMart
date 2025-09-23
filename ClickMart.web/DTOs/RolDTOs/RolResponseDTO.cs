using System.Text.Json.Serialization;

namespace ClickMart.web.DTOs.RolDTOs
{
    public class RolResponseDTO
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("rolId")] public int RolId { get; set; }
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;

        public int ValorId => RolId != 0 ? RolId : Id;
    }
}

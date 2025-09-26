
using ClickMart.web.DTOs.PedidoDTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClickMart.web.Models
{
    public class PedidoCreateVM
    {
        public PedidoCreateDTO Pedido { get; set; } = new();
        public List<SelectListItem> Usuarios { get; set; } = new();
    }
}

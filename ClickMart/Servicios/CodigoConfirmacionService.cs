using ClickMart.DTOs.CodigoConfirmacionDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;


namespace ClickMart.Servicios
{
    public class CodigoConfirmacionService : ICodigoConfirmacionService
    {
        private readonly ICodigoConfirmacionRepository _repo;
        private static readonly TimeSpan Expiracion = TimeSpan.FromMinutes(10);
        private static readonly char[] Digitos = "0123456789".ToCharArray();
        private readonly Random _rng = new();


        public CodigoConfirmacionService(ICodigoConfirmacionRepository repo)
        {
            _repo = repo;
        }


        public async Task<CodigoConfirmacionResponseDTO> GenerarAsync(string email)
        {
            var codigo = Gen(6);
            var entity = new CodigoConfirmacion
            {
                Email = email,
                Codigo = codigo,
                FechaGeneracion = DateTime.UtcNow,
                Usado = 0
            };
            entity = await _repo.AddAsync(entity);


            return new CodigoConfirmacionResponseDTO
            {
                IdCodigo = entity.IdCodigo,
                Email = entity.Email,
                Codigo = entity.Codigo, // en prod, se envía por correo
                FechaGeneracion = entity.FechaGeneracion,
                Usado = entity.Usado
            };
        }


        public async Task<bool> ValidarAsync(string email, string codigo)
        {
            var minFecha = DateTime.UtcNow - Expiracion;
            var usable = await _repo.GetUsableAsync(email, codigo, minFecha);
            if (usable is null) return false;
            return await _repo.MarkUsedAsync(usable.IdCodigo);
        }


        private string Gen(int len)
        {
            var chars = new char[len];
            for (int i = 0; i < len; i++)
                chars[i] = Digitos[_rng.Next(Digitos.Length)];
            return new string(chars);
        }
    }
}
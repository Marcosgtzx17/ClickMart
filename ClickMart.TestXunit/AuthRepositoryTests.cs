using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using ClickMart.Repositorios;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Xunit;

namespace ClickMart.TestXunit
{
    public class AuthRepositoryTests
    {
        // ==== Helpers ====
        private IConfiguration GetTestConfiguration()
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave_secreta_12345678911111111111111111111111111111111111111",
                ["Jwt:Issuer"] = "AuthService",
                ["Jwt:Audience"] = "AuthServiceUsers"
            };
            return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
        }

        private static Usuario MakeUser(
            int usuarioId = 101,
            string nombre = "Henry",
            string direccion = "Calle 1",
            string telefono = "7777-7777",
            string email = "henry@clickmart.com",
            string? plainPassword = "123",
            string rolNombre = "Usuario",
            bool includeRol = true,
            int rolId = 2
        )
        {
            var u = new Usuario
            {
                UsuarioId = usuarioId,
                Nombre = nombre,
                Direccion = direccion,
                Telefono = telefono,
                Email = email,
                PasswordHash = plainPassword is null ? "hash" : BCrypt.Net.BCrypt.HashPassword(plainPassword),
                RolId = rolId
            };
            if (includeRol) u.Rol = new Rol { RolId = rolId, Nombre = rolNombre };
            return u;
        }

        private static JwtSecurityToken Decode(string token)
            => new JwtSecurityTokenHandler().ReadJwtToken(token);

        // ===== HU1001: Registro exitoso =====
        [Fact]
        public async Task RegistroExitoso_retornaDTOyToken()
        {
            // Arrange
            var config = GetTestConfiguration();
            var mockRepo = new Mock<IUsuarioRepository>(MockBehavior.Strict);

            var loaded = MakeUser(usuarioId: 321, email: "user@clickmart.com");

            // Blindaje Strict: tolera espacios y case en el email
            mockRepo.SetupSequence(r => r.GetByEmailAsync(
                    It.Is<string>(s => s != null && s.Trim().Equals("user@clickmart.com", StringComparison.OrdinalIgnoreCase))
                ))
                .ReturnsAsync((Usuario?)null)
                .ReturnsAsync(loaded);

            mockRepo.Setup(r => r.AddAsync(It.IsAny<Usuario>()))
                    .ReturnsAsync((Usuario u) => { u.UsuarioId = 321; return u; });

            var sut = new AuthRepository(mockRepo.Object, config);

            var dto = new UsuarioRegistroDTO
            {
                Nombre = " User ",
                Direccion = " Dir ",
                Telefono = " 7777 ",
                Email = " user@clickmart.com ",
                Password = "123",
                RolId = null // usa 2 por defecto
            };

            // Act
            var result = await sut.RegistrarAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(321, result!.UsuarioId);
            Assert.Equal("user@clickmart.com", result.Email);
            Assert.Equal("Usuario", result.Rol);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));

            var jwt = Decode(result.Token!);
            // Alineado con tu configuración de prueba
            Assert.Equal("AuthService", jwt.Issuer);
            Assert.Contains("AuthServiceUsers", jwt.Audiences);
            Assert.True(jwt.ValidTo > DateTime.UtcNow);

            // Verify también tolerante a espacios
            mockRepo.Verify(r => r.GetByEmailAsync(
                It.Is<string>(s => s != null && s.Trim().Equals("user@clickmart.com", StringComparison.OrdinalIgnoreCase))
            ), Times.Exactly(2));

            mockRepo.Verify(r => r.AddAsync(It.Is<Usuario>(u =>
                u.Nombre == "User" &&
                u.Direccion == "Dir" &&
                u.Telefono == "7777" &&
                u.Email == "user@clickmart.com" &&
                u.RolId == 2 &&
                !string.IsNullOrWhiteSpace(u.PasswordHash)
            )), Times.Once);

            mockRepo.VerifyNoOtherCalls();
        }

        // ===== HU1002: Registro con correo existente =====
        [Fact]
        public async Task RegistroConCorreoExistente_retornaNull()
        {
            // Arrange
            var config = GetTestConfiguration();
            var mockRepo = new Mock<IUsuarioRepository>(MockBehavior.Strict);

            var existente = MakeUser(email: "dup@clickmart.com");
            mockRepo.Setup(r => r.GetByEmailAsync("dup@clickmart.com"))
                    .ReturnsAsync(existente);

            var sut = new AuthRepository(mockRepo.Object, config);

            var dto = new UsuarioRegistroDTO
            {
                Nombre = "X",
                Direccion = "Y",
                Telefono = "Z",
                Email = "dup@clickmart.com",
                Password = "123"
            };

            // Act
            var result = await sut.RegistrarAsync(dto);

            // Assert
            Assert.Null(result);
            mockRepo.Verify(r => r.GetByEmailAsync("dup@clickmart.com"), Times.Once);
            mockRepo.Verify(r => r.AddAsync(It.IsAny<Usuario>()), Times.Never);
            mockRepo.VerifyNoOtherCalls();
        }

        // ===== HU1003: Registro con campos vacíos =====
        [Fact]
        public async Task RegistroConCamposVacios_lanzaArgumentException()
        {
            // Arrange
            var config = GetTestConfiguration();
            var mockRepo = new Mock<IUsuarioRepository>(MockBehavior.Strict);

            var sut = new AuthRepository(mockRepo.Object, config);

            var dto = new UsuarioRegistroDTO
            {
                Nombre = "   ",
                Direccion = "",
                Telefono = "",
                Email = "   ",
                Password = ""
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.RegistrarAsync(dto));
            Assert.Contains("Complete todos los campos obligatorios", ex.Message);
            mockRepo.VerifyNoOtherCalls();
        }

        // ===== HU1004: Inicio de sesión exitoso =====
        [Fact]
        public async Task LoginExitoso_retornaDTOyToken()
        {
            // Arrange
            var config = GetTestConfiguration();
            var mockRepo = new Mock<IUsuarioRepository>(MockBehavior.Strict);

            var usuario = MakeUser(plainPassword: "Password123!");
            mockRepo.Setup(r => r.GetByEmailAsync(usuario.Email))
                    .ReturnsAsync(usuario);

            var sut = new AuthRepository(mockRepo.Object, config);

            var login = new UsuarioLoginDTO
            {
                Email = usuario.Email,
                Password = "Password123!"
            };

            // Act
            var result = await sut.LoginAsync(login);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(usuario.Email, result!.Email);
            Assert.Equal("Usuario", result.Rol);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));

            var jwt = Decode(result.Token!);
            Assert.Equal("AuthService", jwt.Issuer);
            Assert.Contains("AuthServiceUsers", jwt.Audiences);

            var uid = jwt.Claims.First(c => c.Type == "uid").Value;
            Assert.Equal(usuario.UsuarioId.ToString(), uid);

            mockRepo.Verify(r => r.GetByEmailAsync(usuario.Email), Times.Once);
            mockRepo.VerifyNoOtherCalls();
        }

        // ===== HU1005: Inicio con credenciales inválidas =====
        [Fact]
        public async Task LoginCredencialesInvalidas_retornaNull()
        {
            // Arrange
            var config = GetTestConfiguration();
            var mockRepo = new Mock<IUsuarioRepository>(MockBehavior.Strict);

            var usuario = MakeUser(plainPassword: "Correcta123!");
            mockRepo.Setup(r => r.GetByEmailAsync(usuario.Email))
                    .ReturnsAsync(usuario);

            var sut = new AuthRepository(mockRepo.Object, config);

            var login = new UsuarioLoginDTO
            {
                Email = usuario.Email,
                Password = "incorrecta"
            };

            // Act
            var result = await sut.LoginAsync(login);

            // Assert
            Assert.Null(result);
            mockRepo.Verify(r => r.GetByEmailAsync(usuario.Email), Times.Once);
            mockRepo.VerifyNoOtherCalls();
        }

        // ===== HU1006: Inicio con campos vacíos =====
        [Fact]
        public async Task LoginConCamposVacios_lanzaArgumentException()
        {
            // Arrange
            var config = GetTestConfiguration();
            var mockRepo = new Mock<IUsuarioRepository>(MockBehavior.Strict);
            var sut = new AuthRepository(mockRepo.Object, config);

            var login = new UsuarioLoginDTO
            {
                Email = "   ",
                Password = ""
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.LoginAsync(login));
            Assert.Contains("Complete todos los campos obligatorios", ex.Message);
            mockRepo.VerifyNoOtherCalls();
        }
    }
}